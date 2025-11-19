using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;
using VitaWave.Common;
using VitaWave.Common.TLVs;
using VitaWave.ModuleControl.Interfaces;
using VitaWave.ModuleControl.Settings;

namespace VitaWave.ModuleControl.Data
{
    public class ModuleTargetTracker
    {
        private Queue<EventPacket> _eventQueue = new();         // Used for initial filtering only BEFORE correlation
        private List<TrackedTarget> _trackedTargets = new();    // Used for correlation and algorithms
        private List<ResultEvent> _resultsToSend = new();
        private string moduleID = "";
        private int MAX_EVENT_QUEUE_SIZE = 100;
        private readonly ISignalRClient _client;

        // General constants
        const double ASSUMED_WALKING_SPEED_MPS = 1.1; // m/s
        const int NUM_MS_PER_FRAME_MILLISECONDS = 55;

        // Filtering constants
        const int MAX_NUMBER_OF_TRACKED_TARGETS = 1;
        const int NUM_REQUIRED_HEIGHT_DELTAS = 50;
        const int RECORRELATION_FRAME_THRESHOLD = 50;
        private readonly int MIN_NUMBER_TID_MENTIONS;
        private readonly double MIN_MOVEMENT_METERS_FOR_NEW;
        const int MAX_NUMBER_OF_PAST_DATA = 250;

        // Correlation constants
        const double POSITION_PROXIMITY_THRESHOLD = 1;
        const double HEIGHT_PROXIMITY_THRESHOLD = .3;

        // Fall detection constants
        const double FALL_HEIGHT_DROP_THRESHOLD = .5; // meters - minimum drop to consider as fall
        const int FALL_MAX_FRAMES = 20; // maximum frames over which a fall can occur
        const double FALL_RATE_THRESHOLD = 0.03; // meters per frame minimum rate
        const double LOW_HEIGHT_THRESHOLD = 0.3; // meters - avgHeight considered "on ground"
        const int FRAMES_LOW_FOR_FALL = 10; // frames target must stay low after drop
        const int FRAMES_MISSING_FOR_FALL = 15; // frames target can be missing and still count as fall

        // Movement constants
        private readonly double MOVEMENT_THRESHOLD_METERS_PER_FRAME = ASSUMED_WALKING_SPEED_MPS * NUM_MS_PER_FRAME_MILLISECONDS / 1000 * 2.0;

        public ModuleTargetTracker(ISignalRClient client)
        {
            MIN_MOVEMENT_METERS_FOR_NEW = NUM_REQUIRED_HEIGHT_DELTAS * ASSUMED_WALKING_SPEED_MPS * NUM_MS_PER_FRAME_MILLISECONDS / 1000 * .5;
            MIN_NUMBER_TID_MENTIONS = MAX_EVENT_QUEUE_SIZE / 4;
            this.moduleID = SettingsManager.GetConfigSettings().Identifier;
            _client = client;
        }

        object _lock = new object();
        public void Add(EventPacket e)
        {
            lock (_lock)
            {
                _eventQueue.Enqueue(e);
                if (_eventQueue.Count > MAX_EVENT_QUEUE_SIZE)
                    _eventQueue.Dequeue();

                ProcessNewEvent();
            }
        }


        private void ProcessNewEvent()
        {
            if (_eventQueue.Count < MAX_EVENT_QUEUE_SIZE)
                return;

            // Check if we should allow recorrelation for stale tracked targets
            bool allowRecorrelation = _trackedTargets.Count == 1 &&
                                     _trackedTargets[0].FrameCountSinceLastSeen > RECORRELATION_FRAME_THRESHOLD;

            // Build a list of best correlations for each incoming target
            var targetCorrelations = new List<(Target target, uint? correlatedTID, double score)>();

            foreach (var target in _eventQueue.Last().Targets)
            {
                var correlation = FindBestCorrelation(target, allowRecorrelation);
                targetCorrelations.Add((target, correlation.correlatedTID, correlation.score));
            }

            // Sort by best correlation score (lower is better)
            targetCorrelations = targetCorrelations
                .OrderBy(tc => tc.correlatedTID.HasValue ? tc.score : double.MaxValue)
                .ToList();

            // Process correlations in order of quality
            var usedTrackedTargets = new HashSet<uint>();

            foreach (var (target, correlatedTID, score) in targetCorrelations)
            {
                if (correlatedTID.HasValue && !usedTrackedTargets.Contains(correlatedTID.Value))
                {
                    UpdateTrackedTarget(target, correlatedTID.Value);
                    usedTrackedTargets.Add(correlatedTID.Value);
                }
                else if (!correlatedTID.HasValue && EntryFilter(target) && _trackedTargets.Count < MAX_NUMBER_OF_TRACKED_TARGETS)
                {
                    AddNewTarget(target);
                }
            }

            for (int i = 0; i < _trackedTargets.Count; i++)
            {
                var tracked = _trackedTargets[i];
                if (!tracked.UpdatedThisFrame)
                {
                    tracked.FrameCountSinceLastSeen++;
                }
            }

            _resultsToSend.Clear();

            _trackedTargets.ForEach(t => t.UpdatedThisFrame = false);
            // Run fall detection on all tracked targets
            // foreach (var tracked in _trackedTargets)
            // {
            //     CheckForFall(tracked);
            // }

            if (_trackedTargets.Count > 0)
            {
                for (int i = 0; i < _trackedTargets.Count; i++)
                {
                    var result = CheckStatus(_trackedTargets[i]); // Currently only tracking one target
                    AddIfStatusChanged(_trackedTargets[i], result);
                }
            }
#if DEBUG
            if (_resultsToSend.Count > 0)
            {
                _client.SendDataAsync(_resultsToSend);
            }
#else
            // Send out all generated events

            if(usedTrackedTargets.Count > 0)
            {
                _trackedTargets[0].Target.Status = _trackedTargets[0].LastResultID.ToString();
                _client.SendDataAsync(new List<ResultEvent>()
            {
                new ResultEvent()
                {
                    ModuleID = moduleID,
                    ResultId = _trackedTargets[0].LastResultID,
                    Target = _trackedTargets[0].Target
                }
            });
            }
#endif
        }

        private bool EntryFilter(Target target)
        {
            var moduleTargetReferences = _eventQueue
                .SelectMany(e => e.Targets)
                .Where(t => t.TargetHeight.TargetID == target.TID)
                .ToList();

            // NUMBER HEIGHT DELTA FILTERING

            if (moduleTargetReferences.Count < NUM_REQUIRED_HEIGHT_DELTAS) // Not enough data yet for counting deltas
                return false;

            var heightDeltas = 0;
            for (int j = 1; j < moduleTargetReferences.Count - 1; j++)
            {
                if (moduleTargetReferences[j - 1].TargetHeight.MaxZ != moduleTargetReferences[j].TargetHeight.MaxZ)
                {
                    heightDeltas++;
                }
            }
            if (heightDeltas < NUM_REQUIRED_HEIGHT_DELTAS)
            {
                return false;
            }

            var totalMoved = moduleTargetReferences
                .Skip(1)
                .Select((t, i) => Math.Sqrt(
                    Math.Pow(t.X - moduleTargetReferences[i].X, 2) +
                    Math.Pow(t.Y - moduleTargetReferences[i].Y, 2)))
                .Sum();

            return totalMoved > MIN_MOVEMENT_METERS_FOR_NEW;
        }

        private bool CorrelationFilter(Target target)
        {
            return _eventQueue
                .SelectMany(e => e.Targets)
                .Where(t => t.TargetHeight.TargetID == target.TID)
                .Count() > MIN_NUMBER_TID_MENTIONS;
        }

        private (uint? correlatedTID, double score) FindBestCorrelation(Target target, bool allowRecorrelation = false)
        {
            var trackedTargetPool = _trackedTargets
                .Where(t => !t.UpdatedThisFrame) // Only consider unseen targets
                .Where(t => allowRecorrelation || t.FrameCountSinceLastSeen <= RECORRELATION_FRAME_THRESHOLD)
                .ToList();

            if (trackedTargetPool.Count == 0)
                return (null, double.MaxValue);

            var positionCorrelated = CorrelateByPosition(target, trackedTargetPool);
            var heightCorrelated = CorrelateByHeight(target, trackedTargetPool);

            // Position correlation has highest priority
            if (positionCorrelated.Count > 0)
            {
                // Check if the best position match is also in avgHeight correlated
                var bestPositionTID = positionCorrelated[0].TID;
                var bestPositionScore = positionCorrelated[0].Delta;

                if (heightCorrelated.Any(h => h.TID == bestPositionTID))
                {
                    // Both position and avgHeight agree - excellent match
                    var heightScore = heightCorrelated.First(h => h.TID == bestPositionTID).Delta;
                    var combinedScore = bestPositionScore + heightScore * 0.5; // Weight position more
                    return (bestPositionTID, combinedScore);
                }

                // Position only
                return (bestPositionTID, bestPositionScore);
            }

            // Height correlation only
            if (heightCorrelated.Count > 0)
            {
                return (heightCorrelated[0].TID, heightCorrelated[0].Delta + 10); // Penalize avgHeight-only matches
            }

            return (null, double.MaxValue);
        }

        private List<(uint TID, double Delta)> CorrelateByPosition(Target target, IEnumerable<TrackedTarget> trackedTargets)
        {
            var correlated = new List<(uint TID, double Delta)>();

            if (trackedTargets.Count() == 0)
                return new List<(uint TID, double Delta)>();

            var newX = target.X;
            var newY = target.Y;

            foreach (var tracked in trackedTargets)
            {
                var delta = Math.Sqrt(
                    Math.Pow(tracked.Target.X - newX, 2) +
                    Math.Pow(tracked.Target.Y - newY, 2));

                if (delta <= POSITION_PROXIMITY_THRESHOLD)
                {
                    correlated.Add((tracked.Target.TID, delta));
                }
            }

            // Sort by delta (smallest distance first)
            correlated.Sort((a, b) => a.Delta.CompareTo(b.Delta));

            return correlated;
        }

        private List<(uint TID, double Delta)> CorrelateByHeight(Target target, IEnumerable<TrackedTarget> trackedTargets)
        {
            var correlated = new List<(uint TID, double Delta)>();
            var newHeight = target.TargetHeight.MaxZ;

            foreach (var tracked in trackedTargets)
            {
                var delta = Math.Abs(tracked.UnderstoodHeight - newHeight);

                if (delta <= HEIGHT_PROXIMITY_THRESHOLD)
                {
                    correlated.Add((tracked.Target.TID, delta));
                }
            }

            correlated.Sort((a, b) => a.Delta.CompareTo(b.Delta));
            return correlated;
        }

        private void AddNewTarget(Target newTarget)
        {
            uint currentTID;

            var understoodHeight = _eventQueue
                    .SelectMany(e => e.Targets)
                    .Where(t => t.TID == newTarget.TID)
                    .Average(t => t.TargetHeight.MaxZ);

            if (_trackedTargets.Count == 0)
                currentTID = 1;
            else
                currentTID = _trackedTargets.Select(t => t.Target.TID).Max() + 1;

            var newTracked = new TrackedTarget(newTarget, understoodHeight);

            newTracked.Target.TID = currentTID;
            _trackedTargets.Add(newTracked);
        }

        private void UpdateTrackedTarget(Target target, uint trackedTargetToUpdate)
        {
            var tracked = _trackedTargets.First(t => t.Target.TID == trackedTargetToUpdate);
            target.TID = tracked.Target.TID; // Preserve TID

            tracked.FrameCount_Height.Enqueue((tracked.FrameCountSinceLastSeen, target.Z));
            if (tracked.FrameCount_Height.Count() > FALL_MAX_FRAMES + 1)
            {
                tracked.FrameCount_Height.Dequeue();
            }

            tracked.Target = target;
            tracked.FrameCountSinceLastSeen = 0;
            tracked.LastSeen = DateTime.Now;
            tracked.UpdatedThisFrame = true;
            tracked.PastTargetData.Enqueue(target);
            if (tracked.PastTargetData.Count()> MAX_NUMBER_OF_PAST_DATA)
            {
                tracked.PastTargetData.Dequeue();
            }
        }

        private void AddNewResultEvent(ResultID e, TrackedTarget t)
        {
            var ev = new ResultEvent
            {
                ModuleID = moduleID,
                ResultId = e,
                Target = t.Target
            };

            ev.Target.Status = ev.ResultId.ToString();
            _resultsToSend.Add(ev);
        }


        // ALGORITHMS

        private void CheckForFall(TrackedTarget tracked)
        {
            // Need sufficient avgHeight history to detect falls
            if (tracked.FrameCount_Height.Count < FALL_MAX_FRAMES)
                return;

            var recentHistory = tracked.FrameCount_Height.ToList();

            // Find the maximum avgHeight in recent history
            var maxHeight = recentHistory.Max(h => h.Item2);
            var currentHeight = tracked.Target.TargetHeight.MaxZ;
            var heightDrop = maxHeight - currentHeight;

            // Check if there was a significant drop
            if (heightDrop >= FALL_HEIGHT_DROP_THRESHOLD)
            {
                // Calculate the frames over which the drop occurred
                var maxHeightFrame = recentHistory.Last(h => h.Item2 == maxHeight);
                var maxHeightIndex = recentHistory.IndexOf(maxHeightFrame);
                var framesSinceDrop = recentHistory.Count - maxHeightIndex - 1;

                if (framesSinceDrop > 0 && framesSinceDrop <= FALL_MAX_FRAMES)
                {
                    var dropRate = heightDrop / framesSinceDrop;

                    // Check if drop rate is fast enough
                    if (dropRate >= FALL_RATE_THRESHOLD)
                    {
                        // Check if target is now low or disappeared
                        bool isLow = currentHeight <= LOW_HEIGHT_THRESHOLD;
                        bool disappeared = tracked.FrameCountSinceLastSeen >= FRAMES_MISSING_FOR_FALL;

                        // Check if target stayed low
                        bool stayedLow = false;
                        if (isLow)
                        {
                            var recentLowFrames = recentHistory
                                .TakeLast(Math.Min(FRAMES_LOW_FOR_FALL, recentHistory.Count))
                                .Count(h => h.Item2 <= LOW_HEIGHT_THRESHOLD);
                            stayedLow = recentLowFrames >= Math.Min(FRAMES_LOW_FOR_FALL, recentHistory.Count);
                        }

                        if (isLow && stayedLow || disappeared)
                        {
                            var now = DateTime.Now;

                            if (tracked.FallDetectedTime.HasValue &&
                                (now - tracked.FallDetectedTime.Value).TotalSeconds < 59)
                            {
                                // Already detected a fall recently
                                return;
                            }

                            tracked.FallDetectedTime = DateTime.Now;
                            AddNewResultEvent(ResultID.Fall, tracked);
                        }
                    }
                }
            }
        }

        private ResultID CheckStatus(TrackedTarget tracked)
        {
            var pastData = tracked.PastTargetData.ToList();

            if (pastData.Count < 200)
                return tracked.LastResultID;

            if (tracked.FrameCountSinceLastSeen >= 36000 * 18) // 10000ms / 55ms = 18 or 18 frames per second
            {
                return ResultID.NonDetection10Hr;
            }
            else if (tracked.FrameCountSinceLastSeen >= 7200 * 18) 
            {
                return ResultID.Inactive2Hr;
            }
            else if (tracked.FrameCountSinceLastSeen >= 75)
            {
                if (tracked.LastResultID == ResultID.Active)
                    tracked.PastTargetData.Clear();
                return ResultID.Standing;
            }

            var totalDistance = pastData.Skip(1)
                .Select((t, i) => Math.Sqrt(
                    Math.Pow(t.X - pastData[i].X, 2) +
                    Math.Pow(t.Y - pastData[i].Y, 2)))
                .Sum();


            var neededDistanceForActive = .5;

            if (totalDistance >= neededDistanceForActive)
            {
                return ResultID.Active;
            }
            else
            {
                if (tracked.LastResultID == ResultID.Active)
                    tracked.PastTargetData.Clear();
                return ResultID.Standing;
            }
        }

        private void AddIfStatusChanged(TrackedTarget tracked, ResultID newStatus)
        {
            if (tracked.LastResultID != newStatus)
            {
                AddNewResultEvent(newStatus, tracked);
            }

            tracked.LastResultID = newStatus;
        }

        public class TrackedTarget
        {
            public Target Target { get; set; } = new Target();
            public bool UpdatedThisFrame { get; set; } = true;
            public float UnderstoodHeight { get; init; }
            public int FrameCountSinceLastSeen { get; set; } = 0;
            public ResultID LastResultID { get; set; } = ResultID.Unknown;
            public DateTime LastSeen { get; set; } = DateTime.Now;
            public Queue<(int, double)> FrameCount_Height = new();
            public Queue<Target> PastTargetData = new();
            public DateTime? FallDetectedTime { get; set; } = null;

            public TrackedTarget(Target target, float understoodHeight)
            {
                Target = target;
                UnderstoodHeight = understoodHeight;
            }

            public TrackedTarget() { }
        }
    }
}
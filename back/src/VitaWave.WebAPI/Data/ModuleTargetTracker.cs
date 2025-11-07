using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VitaWave.Common;
using VitaWave.Common.TLVs;
using VitaWave.WebAPI.Hubs;

namespace VitaWave.Data
{
    public class ModuleTargetTracker
    {
        private event EventHandler<ResultEvent>? _algResultRaise;
        private Queue<EventPacket> _eventQueue = new();         // Used for initial filtering only BEFORE correlation
        private List<TrackedTarget> _trackedTargets = new();    // Used for correlation and algorithms
        private int MAX_EVENT_QUEUE_SIZE = 500;

        // General constants
        const double ASSUMED_WALKING_SPEED_MPS = 1.1; // m/s
        const int NUM_MS_PER_FRAME_MILLISECONDS = 55;

        // Filtering constants
        const int MAX_NUMBER_OF_TRACKED_TARGETS = 1;
        const int NUM_REQUIRED_HEIGHT_DELTAS = 200;
        const int RECORRELATION_FRAME_THRESHOLD = 200;
        private readonly int MIN_NUMBER_TID_MENTIONS;
        private readonly double MIN_MOVEMENT_METERS_FOR_NEW;

        // Correlation constants
        const double POSITION_PROXIMITY_THRESHOLD = 1;
        const double HEIGHT_PROXIMITY_THRESHOLD = .3;

        // Fall detection constants
        const double FALL_HEIGHT_DROP_THRESHOLD = 0.4; // meters - minimum drop to consider as fall
        const int FALL_MAX_FRAMES = 20; // maximum frames over which a fall can occur
        const double FALL_RATE_THRESHOLD = 0.02; // meters per frame minimum rate
        const double LOW_HEIGHT_THRESHOLD = 0.6; // meters - height considered "on ground"
        const int FRAMES_LOW_FOR_FALL = 10; // frames target must stay low after drop
        const int FRAMES_MISSING_FOR_FALL = 15; // frames target can be missing and still count as fall

        public ModuleTargetTracker(EventHandler<ResultEvent>? eventRaise)
        {
            _algResultRaise = eventRaise;
            MIN_MOVEMENT_METERS_FOR_NEW = NUM_REQUIRED_HEIGHT_DELTAS * ASSUMED_WALKING_SPEED_MPS * NUM_MS_PER_FRAME_MILLISECONDS / 1000 * .5;
            MIN_NUMBER_TID_MENTIONS = MAX_EVENT_QUEUE_SIZE / 4;
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

            _trackedTargets.ForEach(t => t.UpdatedThisFrame = false);

            // Run fall detection on all tracked targets
            foreach (var tracked in _trackedTargets)
            {
                CheckForFall(tracked);
            }

#if DEBUG

            var points = _trackedTargets.Select(t => t.Target.Copy()).ToList();
            foreach (var point in points)
            {
                point.X = -1 * point.X;
            }
            ChartHubSends.hubContext!.BroadcastUnfilteredPoints(points);
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
                // Check if the best position match is also in height correlated
                var bestPositionTID = positionCorrelated[0].TID;
                var bestPositionScore = positionCorrelated[0].Delta;

                if (heightCorrelated.Any(h => h.TID == bestPositionTID))
                {
                    // Both position and height agree - excellent match
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
                return (heightCorrelated[0].TID, heightCorrelated[0].Delta + 10); // Penalize height-only matches
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

            tracked.FrameCount_Height.Add((tracked.FrameCountSinceLastSeen, target.Z));

            tracked.Target = target;
            tracked.FrameCountSinceLastSeen = 0;
            tracked.LastSeen = DateTime.Now;
            tracked.UpdatedThisFrame = true;

        }

        private void Notify(ResultEvent e)
        {
            Task.Run(() =>
            {
                _algResultRaise?.Invoke(this, e);
            });
        }


        // ALGORITHMS

        private void CheckForFall(TrackedTarget tracked)
        {
            // Need sufficient height history to detect falls
            if (tracked.FrameCount_Height.Count < 2)
                return;

            // Already detected a fall for this target
            if (tracked.FallDetected)
                return;

            var recentHistory = tracked.FrameCount_Height.TakeLast(FALL_MAX_FRAMES).ToList();

            if (recentHistory.Count < 2)
                return;

            // Find the maximum height in recent history
            var maxHeight = recentHistory.Max(h => h.Item2);
            var currentHeight = tracked.Target.Z;
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
                            tracked.FallDetected = true;
                            tracked.FallDetectedTime = DateTime.Now;

                            Console.WriteLine($"[FALL DETECTED] TID: {tracked.Target.TID}, " +
                                            $"Height Drop: {heightDrop:F3}m, " +
                                            $"Drop Rate: {dropRate:F3}m/frame, " +
                                            $"Frames: {framesSinceDrop}, " +
                                            $"Final Height: {currentHeight:F3}m, " +
                                            $"Status: {(disappeared ? "Target Lost" : "Target Low")}");

                            // Notify via event
                            var fallEvent = new ResultEvent
                            {
                                TID = (int)tracked.Target.TID,
                                ResultId = ResultID.Fall
                            };
                            Notify(fallEvent);
                        }
                    }
                }
            }
        }

        // const double HEIGHT_DROP_RATIO = .5; //amount person can fall
        // public bool ThresholdFallDetection = true;
        // private bool FallDetectThresholdChecker(TrackedTarget target)
        // {

        // }
    }

    public class TrackedTarget
    {
        public Target Target { get; set; } = new Target();
        public bool UpdatedThisFrame { get; set; } = true;
        public float UnderstoodHeight { get; init; }
        public int FrameCountSinceLastSeen { get; set; } = 0;
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public StaticRegion StaticRegion { get; set; } = StaticRegion.Standing;
        public List<(int, double)> FrameCount_Height = new();
        public bool FallDetected { get; set; } = false;
        public DateTime? FallDetectedTime { get; set; } = null;

        public TrackedTarget(Target target, float understoodHeight)
        {
            Target = target;
            UnderstoodHeight = understoodHeight;
        }

        public TrackedTarget() { }
    }

    public enum StaticRegion
    {
        Standing,
        Sitting,
        Laying
    }
}
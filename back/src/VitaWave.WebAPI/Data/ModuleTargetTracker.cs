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
        private int MAX_EVENT_QUEUE_SIZE = 300;

        // General constants
        const double ASSUMED_WALKING_SPEED_MPS = 1.1; // m/s
        const int NUM_MS_PER_FRAME_MILLISECONDS = 55;

        // Filtering constants
        const int MAX_NUMBER_OF_TRACKED_TARGETS = 5;
        const int NUM_REQUIRED_HEIGHT_DELTAS = 100;
        private readonly int MIN_NUMBER_TID_MENTIONS; 
        private readonly double MIN_MOVEMENT_METERS_FOR_NEW;

        // Correlation constants
        const double POSITION_PROXIMITY_THRESHOLD = 2;
        const double HEIGHT_PROXIMITY_THRESHOLD = .3;

        // Algorithm constants

        public ModuleTargetTracker(EventHandler<ResultEvent>? eventRaise)
        {
            _algResultRaise = eventRaise;
            MIN_MOVEMENT_METERS_FOR_NEW = NUM_REQUIRED_HEIGHT_DELTAS * ASSUMED_WALKING_SPEED_MPS / 2 * NUM_MS_PER_FRAME_MILLISECONDS / 1000;
            MIN_NUMBER_TID_MENTIONS = MAX_EVENT_QUEUE_SIZE / 4;
        }

        object _lock = new object();
        public void Add(EventPacket e)
        {
            lock(_lock)
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

            foreach (var target in _eventQueue.Last().Targets)
            {
                if (CorrelateTarget(target)) { }
                else if (EntryFilter(target))
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

        private bool CorrelateTarget(Target target)
        {
            var trackedTargetPool = _trackedTargets
                .Where(t => !t.UpdatedThisFrame) // Only consider unseen targets
                .ToList();

            var positionCorrelated = CorrelateByPosition(target, trackedTargetPool);

            // Position correlation has highest priority
            if (positionCorrelated.Count == 1)
            {
                // Take the first match
                UpdateTrackedTarget(target, positionCorrelated[0]);
                return true;
            }

            var heightCorrelated = CorrelateByHeight(target, trackedTargetPool);

            if (positionCorrelated.Count > 0 && heightCorrelated.Count > 0)
            {
                // Find the first common TID in both lists
                var commonTID = positionCorrelated.Intersect(heightCorrelated).FirstOrDefault();
                if (commonTID != 0)
                {
                    UpdateTrackedTarget(target, commonTID);
                    return true;
                }
            }

            if (positionCorrelated.Count > 0)
            {
                UpdateTrackedTarget(target, positionCorrelated[0]);
                return true;
            }

            if (heightCorrelated.Count > 0)
            {
                UpdateTrackedTarget(target, heightCorrelated[0]);
                return true;
            }

            return false;
        }

        private List<uint> CorrelateByPosition(Target target, IEnumerable<TrackedTarget> trackedTargets)
        {
            var correlated = new List<(uint TID, double Delta)>();

            if (trackedTargets.Count() == 0)
                return new List<uint>();

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

            // Return only the TIDs in sorted order
            return correlated.Select(c => c.TID).ToList();
        }

        private List<uint> CorrelateByHeight(Target target, IEnumerable<TrackedTarget> trackedTargets)
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
            return correlated.Select(c => c.TID).ToList();
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
            tracked.Target = target;
            tracked.FrameCountSinceLastSeen = 0;
            tracked.LastSeen = DateTime.Now;
            tracked.UpdatedThisFrame = true;
        }

        private void Notify(ResultEvent e)
        {
            Task.Run(() => {
                _algResultRaise?.Invoke(this, e);
            });
        }
    }

    public class TrackedTarget
    {
        public Target Target { get; set; } = new Target();
        public bool UpdatedThisFrame { get; set; } = true;
        public float UnderstoodHeight { get; init; }
        public int FrameCountSinceLastSeen { get; set; } = 0;
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public StaticRegion StaticRegion { get; set; } = StaticRegion.Standing;

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

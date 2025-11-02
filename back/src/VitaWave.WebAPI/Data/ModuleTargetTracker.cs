using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private int MAX_EVENT_QUEUE_SIZE = 1000;

        // Filtering constants
        const int MAX_NUMBER_OF_TRACKED_TARGETS = 3;
        const int NUM_REQUIRED_HEIGHT_DELTAS = 200; // About 1.1 seconds at 5.5ms per frame

        // Correlation constants
        const double POSITION_PROXIMITY_THRESHOLD = 1;
        const double HEIGHT_PROXIMITY_THRESHOLD = 0.1;

        // Algorithm constants


        public ModuleTargetTracker(EventHandler<ResultEvent>? eventRaise)
        {
            _algResultRaise = eventRaise;
        }


        object _lock = new object();
        public void Add(EventPacket e)
        {
            lock(_lock)
            {
                _eventQueue.Enqueue(e);
                if (_eventQueue.Count > MAX_EVENT_QUEUE_SIZE)
                    _eventQueue.Dequeue();
            }

            ProcessNewEvent();
        }


        private void ProcessNewEvent()
        {
            var initialFiltered = InitialFilter(); // Filtering before correlation and new TID generation
            if (initialFiltered.Count == 0)
                return;

            foreach (var target in initialFiltered)
            {
                if (!CorrelateToExisting(target))
                {
                    AddNewTarget(target);
                }
            }

#if DEBUG
            var points = _trackedTargets.Select(t => t.Target).ToList();
            ChartHubSends.hubContext!.BroadcastUnfilteredPoints(points);
#endif
        }

        private List<TrackedTarget> InitialFilter()
        {
            var list = new List<TrackedTarget>();
            for (int i = 0; i < _eventQueue.Last().Targets.Count; i++)
            {
                var target = _eventQueue.Last().Targets[i];
                var moduleTarget = _eventQueue
                    .SelectMany(e => e.Targets)
                    .Where(t => t.TID == target.TID)
                    .ToList();

                // HEIGHT DELTA FILTERING

                if (moduleTarget.Count < NUM_REQUIRED_HEIGHT_DELTAS) // Not enough data yet for counting deltas
                    continue;

                var heightDeltas = 0;
                for (int j = 1; j < moduleTarget.Count - 1; j++)
                {
                    if (moduleTarget[j - 1].TargetHeight.MaxZ != moduleTarget[j].TargetHeight.MaxZ)
                    {
                        heightDeltas++;
                    }
                }
                if (heightDeltas >= NUM_REQUIRED_HEIGHT_DELTAS)
                {
                    list.Add(new TrackedTarget(target));
                }

                // ADD MORE FILTERING METHODS HERE IF NEEDED (use the local list to filter down more)

            }
            return list;
        }

        

        private bool CorrelateToExisting(TrackedTarget newTarget)
        {
            if (_trackedTargets.Count == 0)
                return false;

            int selectedIndex = -1;
            var newHeight = newTarget.Target.TargetHeight.MaxZ;
            var smallestDelta = double.MaxValue;

            for (int i = 0; i < _trackedTargets.Count; i++)
            {
                var thisDelta = Math.Abs(_trackedTargets[i].Target.TargetHeight.MaxZ - newHeight);
                if (thisDelta < smallestDelta)
                {
                    smallestDelta = thisDelta;
                    selectedIndex = i;
                }
            }

            if (smallestDelta <= HEIGHT_PROXIMITY_THRESHOLD)
            {
                newTarget.Target.TID = _trackedTargets[selectedIndex].Target.TID;
                _trackedTargets[selectedIndex].Target = newTarget.Target;
                _trackedTargets[selectedIndex].FrameCountSinceLastSeen = 0;
                _trackedTargets[selectedIndex].LastSeen = DateTime.Now;
                return true;
            }

            return false;
        }

        private void AddNewTarget(TrackedTarget newTarget)
        {
            uint currentTID;

            var moduleTargets = _eventQueue
                    .SelectMany(e => e.Targets)
                    .Where(t => t.TID == newTarget.Target.TID)
                    .ToList();

            var average = moduleTargets.Average(t => t.TargetHeight.MaxZ);

            if (_trackedTargets.Count == 0)
                currentTID = 1;
            else
                currentTID = _trackedTargets.Select(t => t.Target.TID).Max() + 1;

            newTarget.Target.TID = currentTID;
            newTarget.Target.TargetHeight.MaxZ = average;
            _trackedTargets.Add(newTarget);
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
        public int FrameCountSinceLastSeen { get; set; } = 0;
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public bool IsStaticRegion { get; set; } = false;

        public TrackedTarget(Target target)
        {
            Target = target;
        }

        public TrackedTarget() { }
    }
}

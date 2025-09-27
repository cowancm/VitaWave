using System;

namespace AlgoTestClass
{
    public class Algo
    {
        // Define delegate and event
        public event EventHandler<EventArgsWithData> OnEventDetected;

        public class EventArgsWithData : EventArgs
        {
            public string ModuleID { get; set; }
            public string Tid { get; set; }
            public string Event { get; set; }
            public int Criticality { get; set; }
        }

        // Simulate triggering event
        public void RunAlgo()
        {
            // Example: Algo detects something
            var data = new EventArgsWithData
            {
                ModuleID = "ModuleX",
                Tid = "T123",
                Event = "FallDetected",
                Criticality = 9
            };

            // Raise the event
            OnEventDetected?.Invoke(this, data);
        }
    }
}

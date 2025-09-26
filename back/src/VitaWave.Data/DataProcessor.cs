using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using VitaWave.Common;

namespace VitaWave.Data
{
    public class DataProcessor
    {
        public event EventHandler<PhysicalEvent> EventRaise = delegate { };
        public DataProcessor(DataFacilitator dataFacilitator) 
        {
            dataFacilitator.DataReceived += DataFacilitator_DataReceived;
        }

        private void DataFacilitator_DataReceived(object? sender, List<Common.EventPacket> events)
        {
            // this is for ashton and Mati to figure out
            // events is a list of event packets. ctrl + click event packet to see what you can do with it
            // events has a predefined length of MAX_EVENT_WINDOW, a const in DataFacilitor.

            // basically, you can base your algs on the last MAX_EVENT_WINDOW
            // this method will be hit every time a new module parsing arrived over the wire

            // this guy also runs on it's own thread, ie, the life cycle of this function being called is however long this chain goes
            // so there is no dead lock worries
            // rm all of these comments when you guys start.

            // I would recommend making another class for the algs, this method calls those, gets the result, and calls the notify fn below with result
            // just make sure the moduleID goes with it ie:
            var resultOfAlg = PhysicalEvent.Standing;
            resultOfAlg.ModuleID = events.First().ModuleID;
            Notify(resultOfAlg);
        }

        public void Notify(PhysicalEvent e)
        {
            if (EventRaise.GetInvocationList() != null)
                EventRaise.Invoke(this, e);
        }
    }
}

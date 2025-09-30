using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VitaWave.Data;

namespace AlgTesting
{
    public class ResultConsoleOut
    {
        public ResultConsoleOut(DataProcessor dataProcessor) 
        {
            dataProcessor.EventRaise += OnNewEvent;
        }

        private void OnNewEvent(object? sender, VitaWave.Common.ResultEvent e)
        {
            var messageBody = $"The following event took place \"{e.Event}\" at ${e.DateTimeString}.";
        }
    }
}

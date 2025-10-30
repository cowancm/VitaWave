using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VitaWave.Common;
using VitaWave.Data.Algs;

namespace VitaWave.Data
{
    public class DataFacilitator
    {
        public event EventHandler<ResultEvent>? EventRaise;
        private ConcurrentDictionary<AlgKey, AlgSet> algs = new();

        public void Add(EventPacket e)
        {
            var filteredPersons = Filterer.Fit(e.Points);//e.Filter();

            if(filteredPersons.Count > 0)
            {
                Log.Information($"Wow! {filteredPersons.Count}");
            }

            if (filteredPersons == null || filteredPersons.Count == 0)
                return;

            //foreach (var person in filteredPersons)
            //{
            //    var key = new AlgKey
            //    {
            //        ModuleID = e.ModuleID,
            //        TID = person.TID,
            //    };

            //    AlgSet? algSet;

            //    if(!algs.TryGetValue(key, out algSet))
            //    {
            //        algSet = new AlgSet();
            //        algs.TryAdd(key, algSet);
            //    }

            //    var result = ExecuteAlgs(algSet, person);

            //    if (result.Item1 != null)
            //    {
            //        Raise(result.Item1);
            //    }

            //    if (result.Item2 != null)
            //    {
            //        Raise(result.Item2);
            //    }
            //}
        }

        private static (ResultEvent?,ResultEvent?) ExecuteAlgs(AlgSet algs, FilteredPerson person)
        {
            ResultEvent? physicalResult = null;
            ResultEvent? metaResult = null;

            if      (algs.FallDetectAlg.AddAndExecute(person, out physicalResult)) { } // Fall detect reigns first
            else if (algs.DynamicAlg.AddAndExecute(person, out physicalResult))    { } // Then Dynamic detection
            else if (algs.StaticAlg.AddAndExecute(person, out physicalResult))     { } // Then Static detection

            if (algs.MetaAlg.AddAndExecute(person, out metaResult)) { } // Finally, meta alg will be it's own (since it's based on a whole bunch of stuff

            return (physicalResult, metaResult);
        }

        public void Raise(ResultEvent e)
        {
            if (EventRaise != null)
                EventRaise.Invoke(this, e);
        }

        public void Clear(string moduleKey)
        {
            try
            {
                var keysToRemove = algs
                    .Where(x => x.Key.ModuleID == moduleKey)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    algs.TryRemove(key);
                }
            }
            catch (Exception ex) { }
        }
    }

    public class AlgSet
    {
        public AbstractAlg DynamicAlg       { get; set; } = new DynamicAlgV1();
        public AbstractAlg FallDetectAlg    { get; set; } = new FallDetectAlgV1();
        public AbstractAlg StaticAlg        { get; set; } = new StaticAlgV1();
        public AbstractAlg MetaAlg          { get; set; } = new MetaAlgV1();
    }

    public record AlgKey //must be a record
    {
        public string TID { get; set; } = "";
        public string ModuleID { get; set; } = "";
    }
}

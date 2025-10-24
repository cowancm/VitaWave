using System.Collections.Generic;
using VitaWave.Common;

namespace VitaWave.Data.Algs
{
    public abstract class AbstractAlg
    {
        private readonly object _lock = new();
        protected readonly Queue<FilteredPerson> _queue = new();
        protected int AlgWindowSize;

        public bool AddAndExecute(FilteredPerson fp, out ResultEvent? resultEvent)
        {
            resultEvent = null;

            lock (_lock)
            {
                _queue.Enqueue(fp);

                if (_queue.Count == AlgWindowSize + 1)
                {
                    _queue.Dequeue();
                    resultEvent = Execute();
                    return resultEvent != null && true;
                }
            }
            return false;
        }

        public abstract ResultEvent? Execute();

        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
            }
        }
    }
}

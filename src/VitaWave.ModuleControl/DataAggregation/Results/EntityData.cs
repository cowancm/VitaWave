using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitaWave.ModuleControl.DataAggregation.Results
{

    //What we're saving for our DB
    public class EntityData
    {
        public EntityStatus Status { get; set; }
        public EntityAction Action {
            get { return EntityConstants.GetActionTypeByEntityStatus(Status) ?? EntityAction.UNKNOWN; }
        }

        //time
    }
}

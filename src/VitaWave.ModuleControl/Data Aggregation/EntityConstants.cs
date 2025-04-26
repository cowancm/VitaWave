using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitaWave.ModuleControl.DataAggregation
{

    //later will prolly be proto, for now, these will be defined here 
    internal static class EntityConstants
    {
        public static Dictionary<EntityStatus, EntityAction> _actionDict =>
            new Dictionary<EntityStatus, EntityAction>()
            {
                {EntityStatus.SITTING,   EntityAction.STATIC},
                {EntityStatus.STANDING,  EntityAction.STATIC},
                {EntityStatus.ACTIVE,    EntityAction.ACTIVE},
                {EntityStatus.UNKNOWN,   EntityAction.UNKNOWN},
            };

        public static EntityAction? GetActionTypeByEntityStatus(EntityStatus status)
        {
            if (_actionDict.TryGetValue(status, out var action))
            {
                return action;
            }
            else return null;
        }
    }

    public enum EntityStatus
    {
        UNKNOWN,
        SITTING,
        STANDING,
        ACTIVE,
    }

    public enum EntityAction
    {
        UNKNOWN,
        STATIC,
        ACTIVE,
    }
}

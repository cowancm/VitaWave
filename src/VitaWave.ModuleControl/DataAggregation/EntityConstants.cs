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

        public static Dictionary<EntityStatus, EntityActionType> _actionDict =>
            new Dictionary<EntityStatus, EntityActionType>()
            {
                {EntityStatus.SITTING,   EntityActionType.STATIC},
                {EntityStatus.STANDING,  EntityActionType.STATIC},
                {EntityStatus.ACTIVE,    EntityActionType.ACTIVE}
            };


        public static EntityActionType? GetActionTypeByEntityStatus(EntityStatus status)
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
        SITTING,
        STANDING,
        ACTIVE
    }

    public enum EntityActionType
    {
        STATIC,
        ACTIVE
    }


}

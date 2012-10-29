using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public class Entity
    {
        public enum EntityType : byte
        {
            Unit,
            Building,
            Worker,
            Resources,
            HomeBuilding,
            SupplyBuilding,
        }

        public enum DamageElement : byte
        {
            Normal,
            Fire,
            Water,
            Electric,
            Acid,
        }

        public enum Signature : byte
        {
            Damage,
            Move,
            Custom,
            Update,
            Spell,
            Use,
            EntityToUseChange,
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shared
{
    public class UnitXMLData
    {

        public class SpellXMLData
        {
            public bool IsBuildSpell;
            public float EnergyCost;
            public ushort WoodCost;
            public ushort AppleCost;
            public ushort GlueCost;
            public string BuildString;
        }

        public float Speed;
        public string Name;
        public bool RangedUnit;
        public float Range;
        public float StandardAttackDamage;
        public ushort AttackRechargeTime;
        public ushort AttackDelay;
        public string Type; //Is this a worker, attacking unit, etc

        public List<SpellXMLData> Spells; 

        public UnitXMLData()
        {
            Spells = new List<SpellXMLData>();
            Speed = 0;
            Name = "";
            Type = "worker";
        }

        public static List<UnitXMLData> Load(string file)
        {
            var retList = new List<UnitXMLData>();

            var xElement = XDocument.Load(file);

            var units = xElement.Elements("units").Elements("unit");


            foreach (var element in units)
            {
                var unitAdd = new UnitXMLData();

                var unitName = element.Attribute("name").Value;
                var unitSpeed = Convert.ToSingle(element.Attribute("speed").Value);
                var typeAttribute = element.Attribute("type");
                if(typeAttribute != null)
                    unitAdd.Type = typeAttribute.Value;

                unitAdd.Name = unitName;
                unitAdd.Speed = unitSpeed;

                var attackElement = element.Element("attack");
                if (attackElement != null)
                {

                    var rangedUnit = false;
                    var range = 0f;
                    var damage = 0f;
                    var rechargeTime = (ushort)0;
                    var delayTime = (ushort)0;

                    if (attackElement.Attribute("rangedUnit") != null)
                        rangedUnit = Convert.ToBoolean(attackElement.Attribute("rangedUnit").Value);
                    if (attackElement.Attribute("attackRange") != null)
                        range = Convert.ToSingle(attackElement.Attribute("attackRange").Value);
                    if (attackElement.Attribute("damage") != null)
                        damage = Convert.ToSingle(attackElement.Attribute("damage").Value);
                    if (attackElement.Attribute("rechargeTime") != null)
                        rechargeTime = Convert.ToUInt16(attackElement.Attribute("rechargeTime").Value);
                    if (attackElement.Attribute("delayTime") != null)
                        delayTime = Convert.ToUInt16(attackElement.Attribute("delayTime").Value);
                    
                    unitAdd.RangedUnit = rangedUnit;
                    unitAdd.Range = range;
                    unitAdd.StandardAttackDamage = damage;
                    unitAdd.AttackRechargeTime = rechargeTime;
                    unitAdd.AttackDelay = delayTime;

                }

                var spellElements = element.Elements("spells").Elements("spell");

                if(spellElements != null)
                foreach (var spellElement in spellElements)
                {
                    var isBuildSpell = false;
                    var energyCost = 0f;
                    var buildingString = "";
                    var woodCost = (ushort) 0;
                    var appleCost = (ushort) 0;
                    var glueCost = (ushort) 0;


                    if (spellElement.Attribute("isBuildSpell") != null)
                        isBuildSpell = Convert.ToBoolean(spellElement.Attribute("isBuildSpell").Value);
                    if (spellElement.Attribute("energy") != null)
                        energyCost = Convert.ToSingle(spellElement.Attribute("energy").Value);
                    if (spellElement.Attribute("building") != null)
                        buildingString = spellElement.Attribute("building").Value;
                    if (spellElement.Attribute("wood") != null)
                        woodCost = Convert.ToUInt16(spellElement.Attribute("wood").Value);
                    if (spellElement.Attribute("apples") != null)
                        appleCost = Convert.ToUInt16(spellElement.Attribute("apples").Value);
                    if (spellElement.Attribute("glue") != null)
                        glueCost = Convert.ToUInt16(spellElement.Attribute("glue").Value);

                    var spellAdd = new SpellXMLData { EnergyCost = energyCost, IsBuildSpell = isBuildSpell };
                    spellAdd.EnergyCost = energyCost;
                    spellAdd.BuildString = buildingString;
                    spellAdd.WoodCost = woodCost;
                    spellAdd.GlueCost = glueCost;
                    spellAdd.AppleCost = appleCost;
                    unitAdd.Spells.Add(spellAdd);
                }

                retList.Add(unitAdd);
            }

            return retList;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shared
{
    public class BuildingXMLData
    {
        public class UnitXMLData
        {
            public ushort WoodCost;
            public ushort GlueCost;
            public ushort AppleCost;
            public ushort CreationTime;
            public byte SupplyCost;
            public string UnitTypeString;
        }

        public float Health;
        public float MaxHealth;
        public ushort BuildTime;

        public string Name; //identifier for building
        public List<UnitXMLData> Units;
        public byte SupplyAdd;  //amount to add to player's max supply (usually for supply depos)
        public string BuildingType; //determines functionality (is this a grinder, standard building, base)

        public BuildingXMLData()
        {
            Health = 1;
            MaxHealth = 0;
            BuildTime = 0;
            BuildingType = "default";
            SupplyAdd = 0;
            Name = "";
            Units = new List<UnitXMLData>();
        }

        public static List<BuildingXMLData> Load(string file)
        {
            var retList = new List<BuildingXMLData>();

            var xElement = XDocument.Load(file);
            var buildingElements = xElement.Element("buildings").Elements("building");

            foreach (var buildingElement in buildingElements)
            {
                var addBuilding = new BuildingXMLData();

                addBuilding.Name = buildingElement.Attribute("name").Value;

                var supplyAttribute = buildingElement.Attribute("supply");
                if (supplyAttribute != null)
                    addBuilding.SupplyAdd = Convert.ToByte(supplyAttribute.Value);

                var type = buildingElement.Attribute("type");
                if(type != null)
                    addBuilding.BuildingType = type.Value;

                var health = buildingElement.Attribute("hp");
                if (health != null)
                    addBuilding.Health = Convert.ToSingle(health.Value);

                var maxhealth = buildingElement.Attribute("maxhp");
                if (maxhealth != null)
                    addBuilding.MaxHealth = Convert.ToSingle(maxhealth.Value);

                var buildingTime = buildingElement.Attribute("buildTime");
                if (buildingTime != null)
                    addBuilding.BuildTime= Convert.ToUInt16(buildingTime.Value);

                var unitElements = buildingElement.Element("units").Elements("unit");

                foreach (var unitElement in unitElements)
                {
                    var appleCost = (ushort)0;
                    var woodCost = (ushort)0;
                    var buildTime = (ushort)0;
                    var glueCost = (ushort)0;
                    var supply = (byte)0;
                    var unitString = "";

                    if (unitElement.Attribute("glue") != null)
                        glueCost = Convert.ToUInt16(unitElement.Attribute("glue").Value);
                    if (unitElement.Attribute("apples") != null)
                        appleCost = Convert.ToUInt16(unitElement.Attribute("apples").Value);
                    if (unitElement.Attribute("wood") != null)
                        woodCost = Convert.ToUInt16(unitElement.Attribute("wood").Value);
                    if (unitElement.Attribute("buildtime") != null)
                        buildTime = Convert.ToUInt16(unitElement.Attribute("buildtime").Value);
                    if (unitElement.Attribute("name") != null)
                    {
                        unitString = unitElement.Attribute("name").Value;
                    }
                    if (unitElement.Attribute("supply") != null)
                        supply = Convert.ToByte(unitElement.Attribute("supply").Value);


                    addBuilding.Units.Add(new UnitXMLData()
                    {
                        AppleCost = appleCost,
                        WoodCost = woodCost,
                        CreationTime = buildTime,
                        GlueCost = glueCost,
                        SupplyCost = supply,
                        UnitTypeString = unitString,
                    });
                }

                retList.Add(addBuilding);
            }

            return retList;
        }
    }
}

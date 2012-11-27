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
            public string Name;
        }
        public string Name;
        public List<UnitXMLData> Units;
        public byte SupplyAdd;

        public BuildingXMLData()
        {
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


                var unitElements = buildingElement.Element("units").Elements("unit");

                foreach (var unitElement in unitElements)
                {
                    var appleCost = (ushort)0;
                    var woodCost = (ushort)0;
                    var buildTime = (ushort)0;
                    var glueCost = (ushort)0;
                    var unitid = "";
                    var supply = (byte)0;

                    if (unitElement.Attribute("glue") != null)
                        glueCost = Convert.ToUInt16(unitElement.Attribute("glue").Value);
                    if (unitElement.Attribute("apples") != null)
                        appleCost = Convert.ToUInt16(unitElement.Attribute("apples").Value);
                    if (unitElement.Attribute("wood") != null)
                        woodCost = Convert.ToUInt16(unitElement.Attribute("wood").Value);
                    if (unitElement.Attribute("buildtime") != null)
                        buildTime = Convert.ToUInt16(unitElement.Attribute("buildtime").Value);
                    if (unitElement.Attribute("unit") != null)
                        unitid = unitElement.Attribute("unit").Value;
                    if (unitElement.Attribute("supply") != null)
                        supply = Convert.ToByte(unitElement.Attribute("supply").Value);


                    addBuilding.Units.Add(new UnitXMLData()
                    {
                        AppleCost = appleCost,
                        WoodCost = woodCost,
                        CreationTime = buildTime,
                        GlueCost = glueCost,
                        SupplyCost = supply,
                        Name = unitid,
                    });
                }
            }

            return retList;
        }
    }
}

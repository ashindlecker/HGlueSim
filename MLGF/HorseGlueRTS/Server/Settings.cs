using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Server
{
    class Settings
    {
        public static List<BuildingXMLData> BuildingSettings;
        public static List<UnitXMLData> UnitSettings;
 
        public static void Init(string buildingsFile, string unitsFile)
        {
            BuildingSettings = BuildingXMLData.Load(buildingsFile);
            UnitSettings = UnitXMLData.Load(unitsFile);
        }

        public static BuildingXMLData GetBuilding(string name)
        {
            foreach (var buildingSetting in BuildingSettings)
            {
                if(buildingSetting.Name.ToLower() == name.ToLower())
                {
                    return buildingSetting;
                }
            }
            return null;
        }
        public static UnitXMLData GetUnit(string name)
        {
            foreach (var unitSetting in UnitSettings)
            {
                if (unitSetting.Name.ToLower() == name.ToLower())
                {
                    return unitSetting;
                }
            }
            return null;
        }
    }
}

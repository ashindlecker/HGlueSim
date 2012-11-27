using Shared;
using System.Collections.Generic;

namespace Client
{
    public class Settings
    {
        public static float SOUNDVOLUME = .5f;
        public static float MUSICVOLUME = .2f;

        public static List<HotkeySheet> Hotkeys;

        public static void Init()
        {
            Hotkeys = HotkeySheet.LoadSheetsFromXML("Resources/Data/Hotkeys.xml");
        }

        public static HotkeySheet GetSheet(string name)

        {
            foreach (var hotkeySheet in Hotkeys)
            {
                if(hotkeySheet.Name == name)
                    return hotkeySheet;
            }
            return null;
        }
    }
}
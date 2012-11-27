using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Window;
using System.Xml.Linq;

namespace Shared
{
    public class Hotkey
    {
        public Keyboard.Key Key;
        public byte SpellId;
        public bool RequiresClick;
    }
    

    public class HotkeySheet
    {
        public List<Hotkey> Hotkeys;
        public string Name;

        public HotkeySheet()
        {
            Name = "";
            Hotkeys = new List<Hotkey>();
        }

        public Hotkey ProcessInput(Keyboard.Key key)
        {
            return Hotkeys.FirstOrDefault(hotkey => hotkey.Key == key);
        }

        public static List<HotkeySheet> LoadSheetsFromXML(string file)
        {
            var retList = new List<HotkeySheet>();

            var xElements = XDocument.Load(file);
            var sheetElements = xElements.Elements("hotkeysheets").Elements("sheet");

            foreach (var sheetElement in sheetElements)
            {
                var add = new HotkeySheet();
                add.Name = sheetElement.Attribute("name").Value;

                var keyElements = sheetElement.Elements("key");

                foreach (var keyElement in keyElements)
                {
                    var addKey = new Hotkey();
                    var intKey = (int) keyElement.Attribute("key").Value.ToLower()[0];
                    const int LETTERAOFFSET = 97;
                    intKey -= LETTERAOFFSET;

                    addKey.Key = (Keyboard.Key) intKey;
                    Console.WriteLine(addKey.Key);
                    addKey.RequiresClick = Convert.ToBoolean(keyElement.Attribute("usesClick").Value);
                    addKey.SpellId = Convert.ToByte(keyElement.Attribute("spellType").Value);
                    add.Hotkeys.Add(addKey);
                }
                retList.Add(add);
            }

            return retList;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ClockPlus
{
    class ConfigManager
    {
        private DB db = new DB("config.csv");
        private Dictionary<string, string> globalSettings;
        private static ConfigManager instance = null;
        public static ConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ConfigManager();

                    var json = System.IO.File.ReadAllText("global.json");
                    var jss = new JavaScriptSerializer();
                    instance.globalSettings = jss.Deserialize<Dictionary<string,string>>(json);

                }
                return instance;
            }
        }
        public Color getDefaultColorMaster1()
        {
            if (globalSettings.ContainsKey("defaultColorMaster1"))
            {

                return ColorTranslator.FromHtml(globalSettings["defaultColorMaster1"]);
            }
            else
            {
                return ColorTranslator.FromHtml("#888888");
            }
        }
        public Color getDefaultColorMaster2()
        {
            if (globalSettings.ContainsKey("defaultColorMaster2"))
            {
                return ColorTranslator.FromHtml(globalSettings["defaultColorMaster2"]);
            }
            else
            {
                return ColorTranslator.FromHtml("#888888");
            }
        }
        public int getSize()
        {
            if (globalSettings.ContainsKey("size"))
            {
                return Convert.ToInt32(globalSettings["size"]);
            }
            else
            {
                return 500;
            }
        }
        public int getNumberOfSlots()
        {
            if (globalSettings.ContainsKey("numberOfSlots"))
            {
                return Convert.ToInt32(globalSettings["numberOfSlots"]);
            }
            else
            {
                return 12;
            }
        }
        public int getLowRefreshRate()
        {
            if (globalSettings.ContainsKey("lowRefreshRate"))
            {
                return Convert.ToInt32(globalSettings["lowRefreshRate"]);
            }
            else
            {
                return 5;
            }
        }
        public Point getLocation()
        {
            int x, y;
            if (globalSettings.ContainsKey("locationX"))
            {
                x = Convert.ToInt32(globalSettings["locationX"]);
            }
            else
            {
                x = 0;
            }
            if (globalSettings.ContainsKey("locationY"))
            {
                y = Convert.ToInt32(globalSettings["locationY"]);
            }
            else
            {
                y = 0;
            }
            return new Point(x, y);
        }
        public int getOrder(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            int ret = Convert.ToInt32(db.getJ("order", index));
            return ret;
        }
        public string getLabel(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            string ret = db.getJ("label", index);
            return ret;
        }
        public string getBar(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            string ret = db.getJ("bar", index);
            return ret;
        }
        public bool getHighRefresh(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            bool ret = Convert.ToBoolean(Convert.ToInt32(db.getJ("highRefresh", index)));
            return ret;
        }
        public int getStartAngle(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            int ret = Convert.ToInt32(db.getJ("startAngle", index));
            return ret;
        }
        public int getBufferAngle(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            int ret = Convert.ToInt32(db.getJ("bufferAngle", index));
            return ret;
        }
        public int getSweepAngle(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            int ret = Convert.ToInt32(db.getJ("sweepAngle", index));
            return ret;
        }

        public int getBreatheInterval(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            int ret;
            try
            {
                ret = Convert.ToInt32(db.getJ("breatheInterval", index));
            }
            catch (System.FormatException e)
            {
                return 1;
            }
            return ret;
        }
        public string getBreatheType(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            string ret = db.getJ("breatheType", index);
            return ret;
        }
        public Color getColor1(int id, Color defaultColor)
        {
            int index = db.getI("id", Convert.ToString(id));
            string colorString = db.getJ("color1", index);
            if(colorString.Length<=0)
            {
                return defaultColor;
            }
            Color ret = ColorTranslator.FromHtml(colorString);
            return ret;
        }
        public Color getColor2(int id, Color defaultColor)
        {
            int index = db.getI("id", Convert.ToString(id));
            string colorString = db.getJ("color2", index);
            if (colorString.Length <= 0)
            {
                return defaultColor;
            }
            Color ret = ColorTranslator.FromHtml(colorString);
            return ret;
        }

        public Color getBreatheColor(int id, Color defaultColor)
        {
            int index = db.getI("id", Convert.ToString(id));
            string colorString = db.getJ("breatheColor", index);
            if (colorString.Length <= 0)
            {
                return defaultColor;
            }
            Color ret = ColorTranslator.FromHtml(colorString);
            return ret;
        }
        public string getTriggerParameter(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            string ret = db.getJ("triggerParameter", index);
            return ret;
        }
        public string getTriggerType(int id)
        {
            int index = db.getI("id", Convert.ToString(id));
            string ret = db.getJ("triggerType", index);
            return ret;
        }
        public bool triggered(int id, double triggerValueInput)
        {
            int index = db.getI("id", Convert.ToString(id));
            string triggerValueString = db.getJ("triggerValue", index);
            if(triggerValueString.Length<=0)
            {
                return true;
            }
            double triggerValueThreshold = Convert.ToDouble(triggerValueString);
            string triggerType = getTriggerType(id);
            switch(triggerType)
            {
                case "<": return triggerValueInput < triggerValueThreshold;
                case "<=": return triggerValueInput <= triggerValueThreshold;
                case ">": return triggerValueInput > triggerValueThreshold;
                case ">=": return triggerValueInput >= triggerValueThreshold;
                case "==": return triggerValueInput == triggerValueThreshold;
                case "!=": return triggerValueInput != triggerValueThreshold;
                default: return false;
            }
        }
        public List<int> getAllEnabled()
        {
            List<int> indexes = db.getAllI("enabled", "1");
            List<int> ret = new List<int>();
            foreach (int i in indexes)
            {
                Boolean enabled = Convert.ToBoolean(Convert.ToInt32(db.getJ("enabled", i)));
                if (enabled)
                {
                    ret.Add(Convert.ToInt32(db.getJ("id", i)));
                }
            }
            ret.Sort(sortIDsByOrder);
            return ret;
        }
        private int sortIDsByOrder(int x, int y)
        {
            int orderOfX = Convert.ToInt32(x);
            int orderOfY = Convert.ToInt32(y);
            if(orderOfX<orderOfY)
            {
                return -1;
            }
            else if (orderOfX > orderOfY)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}

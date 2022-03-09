using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace ClockPlus
{
    
    class DB
    {
        private string[] columnNames;

        List<string[]> db = new List<string[]>();
        
        public DB(string filename, string[] cn) 
        {
            db = new List<string[]>();
            columnNames = new string[cn.Length] ;
            Array.Copy(cn, columnNames, cn.Length);
            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    db.Add(values);
                }
            }
        }
        public DB(string filename)
        {
            db = new List<string[]>();
            using (var reader = new StreamReader(filename))
            {
                var line = reader.ReadLine();
                var values = line.Split(',');
                columnNames = values;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    values = line.Split(',');

                    db.Add(values);
                }
            }
        }
        public string getValue(int i,int j)
        {
            try
            {
                return db[i][j];
            }
            catch
            {
                return null;
            }
        }
        public int getI(string cs, string value)
        {
            int j = Array.IndexOf(columnNames, cs);
            for(int i=0;i<db.Count;i++)
            {
                if(getValue(i,j).ToLower().Equals(value.ToLower()))
                {
                    return i;
                }
            }
            return -1;
        }
        public List<int> getAllI(string cs, string value)
        {
            List<int> ret = new List<int>();
            int j = Array.IndexOf(columnNames, cs);
            for (int i = 0; i < db.Count; i++)
            {
                if (getValue(i, j).ToLower().Equals(value.ToLower()))
                {
                    ret.Add(i);
                }
            }
            return ret;
        }
      
        public string getJ(string cs, int i)
        {
            int j = Array.IndexOf(columnNames, cs);
            try
            {
                return getValue(i, j);
            }
            catch
            {
                return "";
            }
            
        }


    }
}

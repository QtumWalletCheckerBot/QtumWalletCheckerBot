using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace wallet_checker
{
    static public class strings
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        public enum eLanguageCode
        {
            En,
            Ko,
        };

        ///--------------------------------------------------------------------------------------------------------
        ///

        static private eLanguageCode languageCode = eLanguageCode.En;

        static private Dictionary<eLanguageCode, Dictionary<string, string>> dic = new Dictionary<eLanguageCode, Dictionary<string, string>>();

        static public eLanguageCode LanguageCode { get {return languageCode; } set { languageCode = value; }}

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public bool Load()
        {
            try
            {
                XDocument xdoc = XDocument.Load(@"strings.xml");                

                dic.Clear();

                dic.Add(eLanguageCode.En, new Dictionary<string, string>());
                dic.Add(eLanguageCode.Ko, new Dictionary<string, string>());

                var enDic = dic[eLanguageCode.En];
                
                IEnumerable<XElement> emps = xdoc.Root.Elements();
                foreach (var emp in emps)
                {
                    string strKey = emp.Attribute("name").Value.Replace("\\n", "\n");
                    string enStr = emp.Element("en").Value.Replace("\\n", "\n");

                    enDic.Add(strKey, enStr);
                }
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public bool Exsist(string key)
        {
            return dic.ContainsKey(languageCode) && dic[languageCode].ContainsKey(key);
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string GetString(string key)
        {
            if (Exsist(key))
                return dic[languageCode][key];

            return key;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string Format(string key, params object[] args)
        {
            return string.Format(GetString(key), args);
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

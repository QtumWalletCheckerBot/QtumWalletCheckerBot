using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wallet_checker
{
    static public class Config
    {
        ///--------------------------------------------------------------------------------------------------------
        ///

        public static string Language = "en";
        public static string TelegramApiId = "";
        public static string QtumWalletPath = "";
        public static string RPCUserName = "";
        public static string RPCPassword = "";
        public static bool StartupAutoStaking = true;

        ///--------------------------------------------------------------------------------------------------------
        ///
        public static bool Load()
        {
            try
            {
                using (StreamReader reader = new StreamReader("Config.txt"))
                {
                    char[] buffer = new char[1024 * 1024];
                    reader.ReadBlock(buffer, 0, buffer.Length);

                    string jsonStr = new string(buffer);

                    JObject json = JObject.Parse(jsonStr);
                    Language = json["Language"].ToString().ToLower();
                    TelegramApiId = json["Telegram API ID"].ToString();
                    QtumWalletPath = json["Qtum Wallet Path"].ToString();
                    RPCUserName = json["RPC UserName"].ToString();
                    RPCPassword = json["RPC Password"].ToString();
                    StartupAutoStaking = (bool)json["Startup Auto Staking"];

                    if (string.IsNullOrEmpty(QtumWalletPath))
                        QtumWalletPath = System.Environment.CurrentDirectory;

                    if (Language == "ko")
                        strings.LanguageCode = strings.eLanguageCode.Ko;

                    strings.Load();
                }
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString());
                return false;
            }

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

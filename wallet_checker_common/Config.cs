using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker_common
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
        public static string OtpSecretKey = "";
        public static string TimeZoneName = TimeZoneInfo.Local.StandardName;
        public static int[] MiniumVersion = new int[3];
        public static float MiniumPing = 1.0f;

        ///--------------------------------------------------------------------------------------------------------
        ///
        public static bool Load()
        {
            try
            {
                using (StreamReader reader = new StreamReader("Config/Config.txt"))
                {
                    char[] buffer = new char[1024 * 1024];
                    reader.ReadBlock(buffer, 0, buffer.Length);

                    string jsonStr = new string(buffer);

                    JObject json = JObject.Parse(jsonStr);
                    Language = json["Language"].ToString().ToLower();
                    TelegramApiId = json["Telegram API ID"].ToString();
                    OtpSecretKey = json["OtpSecretKey"].ToString();
                    QtumWalletPath = json["Qtum Wallet Path"].ToString();
                    RPCUserName = json["RPC UserName"].ToString();
                    RPCPassword = json["RPC Password"].ToString();
                    StartupAutoStaking = (bool)json["Startup Auto Staking"];
                    TimeZoneName = json["TimeZone"].ToString();

                    string versionStr = json["MiniumVersion"].ToString();
                    string[] verList = versionStr.Trim().Split('.');

                    for (int i = 0; i < 3; ++i)
                    {
                        int.TryParse(verList[i], out MiniumVersion[i]);
                    }

                    MiniumPing = (float)json["MiniumPing"];

                    try
                    {
                        if (TimeZoneInfo.FindSystemTimeZoneById(TimeZoneName) == null)
                        {
                            TimeZoneName = TimeZone.CurrentTimeZone.StandardName;
                        }
                    }
                    catch (Exception)
                    {
                        TimeZoneName = TimeZone.CurrentTimeZone.StandardName;
                    }

                    if (string.IsNullOrEmpty(TelegramApiId))
                    {
                        Logger.Log("Config 파일에 TelegramApiId 값이 비어있습니다. API 키를 입력해주세요.");
                        return false;
                    }

                    if (string.IsNullOrEmpty(QtumWalletPath))
                        QtumWalletPath = System.Environment.CurrentDirectory;

                    if (Language == "ko")
                        strings.LanguageCode = strings.eLanguageCode.Ko;

                    strings.Load();
                }
            }
            catch (Exception e)
            {
                Logger.Log("Failed Load Config" + e.ToString());
                return false;
            }

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public static bool Save()
        {
            try
            {
                using (StreamWriter file = new StreamWriter("Config/Config.txt"))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(file))
                    {
                        writer.Formatting = Formatting.Indented;

                        JObject json = new JObject(
                            new JProperty("Language", Language),
                            new JProperty("Telegram API ID", TelegramApiId),
                            new JProperty("OtpSecretKey", OtpSecretKey),
                            new JProperty("Qtum Wallet Path", QtumWalletPath),
                            new JProperty("RPC UserName", RPCUserName),
                            new JProperty("RPC Password", RPCPassword),
                            new JProperty("Startup Auto Staking", StartupAutoStaking),
                            new JProperty("TimeZone", TimeZoneName)
                            );

                        json.WriteTo(writer);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Failed Save Config : " + e.ToString());
                return false;
            }

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

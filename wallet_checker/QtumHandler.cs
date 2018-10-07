using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker
{
    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    public class QtumHandler
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        static Commmander commander = new Commmander();

        ///--------------------------------------------------------------------------------------------------------
        ///
        static private bool TryParseJson(string str, out JObject json)
        {
            json = null;

            try
            {
                json = JObject.Parse(str);

                return true;
            }
            catch(Exception)
            {
            }

            return false;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string GetHDMasterKeyId()
        {
            JObject json = null;

            string result = commander.Process("getwalletinfo");

            if (TryParseJson(result, out json))
            {
                try
                {
                    return json["hdmasterkeyid"].ToString();
                }
                catch (Exception)
                {

                }
            }
            else
            {
                Logger.Log(result);
            }

            return "";
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string GetInfo()
        {
            string result;

            try
            {
                bool isStakingState = false;

                JObject getStakingInfoJson = null;

                if(TryParseJson(commander.Process("getstakinginfo"), out getStakingInfoJson))
                {
                    try
                    {
                        isStakingState = (bool)getStakingInfoJson["staking"];
                    }
                    catch(Exception)
                    {
                    }
                }

                string getInfoResult = commander.Process("-getinfo");

                result = getInfoResult;

                JObject getInfoJson = null;

                if (TryParseJson(getInfoResult, out getInfoJson))
                {
                    string stakingStateStr = "OFF";

                    if((ulong)getInfoJson["unlocked_until"] != 0)
                    {
                        stakingStateStr = "ON";

                        if (isStakingState == false)
                            stakingStateStr += strings.GetString(" (숙성 대기 중)");
                    }

                    result = strings.Format(
@"
 코어 버전 : {0}
 지갑 버전 : {1}
 블록 : {2}
 잔고 : {3} Qtum
 스테이크 갯수 : {4} Qtum
 스테이크 상태 : {5}
 연결된 커넥션 : {6} 개
 주소 목록 :
"
                        , getInfoJson["version"].ToString()
                        , getInfoJson["walletversion"].ToString()
                        , getInfoJson["blocks"].ToString()
                        , getInfoJson["balance"].ToString()
                        , getInfoJson["stake"].ToString()
                        , stakingStateStr
                        , (uint)getInfoJson["connections"]
                    );

                    string listunspentResult = "{\n AddressList : " + commander.Process("listunspent") + "\n}";

                    JObject listunspentJson = null;

                    if (TryParseJson(listunspentResult, out listunspentJson))
                    {
                        Dictionary<string, double> balances = new Dictionary<string, double>();

                        JToken addressList = listunspentJson["AddressList"];
                        for (int i = 0; i < addressList.Count(); ++i)
                        {
                            string address = addressList[i]["address"].ToString();
                            string amount = addressList[i]["amount"].ToString();

                            double balance = 0;

                            balances.TryGetValue(address, out balance);

                            balance += Convert.ToDouble(amount);

                            balances[address] = balance;
                        }

                        int accountCount = 0;

                        foreach(var pair in balances)
                        {
                            string address = pair.Key;
                            double amount = pair.Value;

                            if (accountCount++ > 0)
                                result += "\n\n";

                            result += strings.Format("    - 주소 : {0}\n", address);
                            result += strings.Format("    - 수량 : {0} Qtum", amount);
                        }
                    }
                    else
                    {
                        result += listunspentResult + "\n";
                    }
                }
            }
            catch(Exception e)
            {
                result = e.ToString();
            }

            return result;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string StartSaking()
        {
            PasswordManager pwdManager = new PasswordManager();
            string pwd = pwdManager.GetPassword();

            if (pwd == null)
                return strings.GetString("패스워드 오류");

            string cmdResult = commander.Process(string.Format("walletpassphrase \"{0}\" 99999999 true", pwd));

            return cmdResult.Trim();
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string StopSaking()
        {
            PasswordManager pwdManager = new PasswordManager();
            string pwd = pwdManager.GetPassword();

            if (pwd == null)
                return strings.GetString("패스워드 오류");

            string cmdResult = commander.Process(string.Format("walletpassphrase \"{0}\" 0 true", pwd));

            return cmdResult.Trim();
        }
    }
    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

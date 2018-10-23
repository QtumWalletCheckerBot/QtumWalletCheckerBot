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
    public class QtumTxInfo
    {
        public string address = "";
        public string category = "";
        public double amount = 0;
        public string label = "";
        public double fee = 0;
        public long time = 0;
        public string txId = "";
        public string comment = "";

        public string GetString()
        {
            DateTime txTime = QtumHandler.BlockTimeToUtcTime(time);
            string str = string.Format("time : {0:yyyy/MM/dd HH:mm:ss}", DateTimeHandler.ToLocalTime(txTime));
            str += string.Format("\n tx id : {0}", Command.ICommand.GetTxLink(txId));
            str += string.Format("\n address : {0}", Command.ICommand.GetAddressLink(address));
            //notifyStr += string.Format("\n category : {0}", txInfo.category);
            str += string.Format("\n amount : {0}", amount);
            str += string.Format("\n fee : {0}", fee);
            str += string.Format("\n label : {0}", label);
            str += string.Format("\n comment : {0}", comment);
            str += string.Format("\n");

            return str;
        }
    }

    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    public class QtumPeerInfo
    {
        public string addr = "";
        public string version = "";
        public string subVersion = "";
        public double pingTime = 0;
    }

    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    public class QtumHandler
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        static CommmandLineController commandline = new CommmandLineController();

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
        static public string GetHDMasterKeyId(bool bWriteFailLog = true)
        {
            JObject json = null;

            string result = commandline.Process("getwalletinfo");

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
                if(bWriteFailLog)
                    Logger.Log(result);
            }

            return "";
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string GetInfo(out Dictionary<string, double> outBalances)
        {
            outBalances = new Dictionary<string, double>();

            string result;

            try
            {
                bool isStakingState = false;

                JObject getStakingInfoJson = null;

                if(TryParseJson(commandline.Process("getstakinginfo"), out getStakingInfoJson))
                {
                    try
                    {
                        isStakingState = (bool)getStakingInfoJson["staking"];
                    }
                    catch(Exception)
                    {
                    }
                }

                string getInfoResult = commandline.Process("-getinfo");

                result = getInfoResult;

                JObject getInfoJson = null;

                if (TryParseJson(getInfoResult, out getInfoJson))
                {
                    string stakingStateStr = "OFF";

                    if ((ulong)getInfoJson["unlocked_until"] != 0)
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
 TimeOffset : {7}
 주소 목록 :
"
                        , getInfoJson["version"].ToString()
                        , getInfoJson["walletversion"].ToString()
                        , getInfoJson["blocks"].ToString()
                        , getInfoJson["balance"].ToString()
                        , getInfoJson["stake"].ToString()
                        , stakingStateStr
                        , (uint)getInfoJson["connections"]
                        , getInfoJson["timeoffset"].ToString()
                    );

                    string listunspentResult = "{\n AddressList : " + commandline.Process("listunspent") + "\n}";

                    JObject listunspentJson = null;

                    if (TryParseJson(listunspentResult, out listunspentJson))
                    {
                        Dictionary<string, double> balances = outBalances;

                        JToken addressList = listunspentJson["AddressList"];
                        for (int i = 0; i < addressList.Count(); ++i)
                        {
                            string address = addressList[i]["address"].ToString();
                            string amount = addressList[i]["amount"].ToString();

                            double balance = Convert.ToDouble(amount);

                            if (balances.ContainsKey(address) == false)
                                balances[address] = 0;

                            balances[address] += balance;
                        }

                        int accountCount = 0;

                        if (balances.Count == 0)
                        {
                            result += "    - empty";
                        }
                        else
                        {
                            foreach (var pair in balances)
                            {
                                string address = Command.ICommand.GetAddressLink(pair.Key);
                                double amount = pair.Value;

                                if (accountCount++ > 0)
                                    result += "\n\n";

                                result += strings.Format("    - 주소 : {0}\n", address);
                                result += strings.Format("    - 수량 : {0} Qtum", amount);
                            }
                        }                        
                    }
                    else
                    {
                        result += listunspentResult + "\n";
                    }

                    string listAccountResult = "{\n AddressList : " + commandline.Process("getaddressesbyaccount \"\"") + "\n}";
                    JObject listAccountJson = null;

                    if (TryParseJson(listAccountResult, out listAccountJson))
                    {
                        JToken addressList = listAccountJson["AddressList"];
                        for (int i = 0; i < addressList.Count(); ++i)
                        {
                            string address = addressList[i].ToString();
                            if (outBalances.ContainsKey(address) == false)
                            {
                                outBalances[address] = 0;
                            }
                        }
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
        static public double GetBalance()
        {
            try
            {
                string resultStr = commandline.Process("getbalance \"\"").Trim();

                double balance = 0;

                if (double.TryParse(resultStr, out balance))
                    return balance;
            }
            catch (Exception e)
            {
                Logger.Log("failed GetBalance.\n" + e.ToString());
            }

            return 0;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string CreateNewAddress()
        {
            return commandline.Process("getnewaddress \"\" \"bech32\"").Trim();
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public bool IsValidateAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;

            try
            {
                string resultStr = commandline.Process(string.Format("validateaddress \"{0}\"", address)).Trim();

                JObject json = null;

                if (TryParseJson(resultStr, out json))
                {
                    return (bool)json["isvalid"];
                }
            }
            catch(Exception e)
            {
                Logger.Log("failed check validate adress. " + address + "\n" + e.ToString());

                return false;
            }

            return false;
        }

        static public bool IsStakingState()
        {
            bool isStakingState = false;

            try
            {
                JObject getStakingInfoJson = null;

                if (TryParseJson(commandline.Process("getstakinginfo"), out getStakingInfoJson))
                {
                    try
                    {
                        isStakingState = (bool)getStakingInfoJson["staking"];
                    }
                    catch (Exception)
                    {
                    }
                }

                if(isStakingState == false)
                {
                    string getInfoResult = commandline.Process("-getinfo");

                    JObject getInfoJson = null;

                    if (TryParseJson(getInfoResult, out getInfoJson))
                    {
                        if ((ulong)getInfoJson["unlocked_until"] != 0)
                            isStakingState = true;
                    }
                }
            }
            catch(Exception)
            {
                isStakingState = false;
            }            

            return isStakingState;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string StartSaking()
        {
            PasswordManager pwdManager = new PasswordManager();
            string pwd = pwdManager.GetPassword();

            if (pwd == null)
                return strings.GetString("패스워드 오류");

            string cmdResult = commandline.Process(string.Format("walletpassphrase \"{0}\" 99999999 true", pwd));

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

            string cmdResult = commandline.Process(string.Format("walletpassphrase \"{0}\" 0 true", pwd));

            return cmdResult.Trim();
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string Send(string myAddress, string destAddress, double amount)
        {
            if (amount < 0)
                return strings.GetString("수량이 부족합니다.");

            if (string.IsNullOrEmpty(myAddress) == false && IsValidateAddress(myAddress) == false)
                return strings.GetString("유효하지 않은 주소입니다.");

            if (IsValidateAddress(destAddress) == false)
                return strings.GetString("유효하지 않은 주소입니다.");

            PasswordManager pwdManager = new PasswordManager();
            string pwd = pwdManager.GetPassword();

            if (string.IsNullOrEmpty(pwd))
                return strings.GetString("패스워드 오류");

            bool bStaking = IsStakingState();

            commandline.Process(string.Format("walletpassphrase \"{0}\" 30 false", pwd));

            string cmdResult = "";

            if(string.IsNullOrEmpty(myAddress))
                cmdResult = commandline.Process(string.Format("sendtoaddress \"{0}\" {1} \"\" \"\" true", destAddress, amount));
            else
                cmdResult = commandline.Process(string.Format("sendtoaddress \"{0}\" {1} \"\" \"\" true null null \"\" \"{2}\"", destAddress, amount, myAddress));

            if (bStaking)
                commandline.Process(string.Format("walletpassphrase \"{0}\" 99999999 true", pwd));
            else
                commandline.Process(string.Format("walletpassphrase \"{0}\" 0 true", pwd));

            return cmdResult.Trim();
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string CommandLine(string commandLineStr)
        {
            string cmdResult = commandline.Process(commandLineStr);

            return cmdResult;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string BackupWallet()
        {
            string fileName = string.Format("{0:yyyy_MM_dd-HH_mm_ss}.dat", DateTimeHandler.GetTimeZoneNow());

            string cmdResult = commandline.Process(string.Format("backupwallet \"./backups/{0}\"", fileName));

            return cmdResult +"\n" + fileName;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string RestoreWallet(string filePath)
        {
            return "지갑 복원은 아직 구현되지 않았습니다.";
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public List<QtumTxInfo> GetTransactions(uint count = 1)
        {
            JObject json = null;
            JToken txJson = null;
            int i = 0;

            try
            {
                string resultStr = "{\n TxList : " + commandline.Process("listtransactions \"*\" " + count) + "\n}";
                

                if (TryParseJson(resultStr, out json))
                {
                    List<QtumTxInfo> list = new List<QtumTxInfo>();

                    JToken txListJson = json["TxList"];

                    for(i=0; i<txListJson.Count(); ++i)
                    {
                        txJson = txListJson[i];

                        if (txJson == null)
                            continue;

                        QtumTxInfo newInfo = new QtumTxInfo();

                        if(txJson["address"] != null)
                            newInfo.address = txJson["address"].ToString();

                        newInfo.category = txJson["category"].ToString();
                        newInfo.amount = Convert.ToDouble(txJson["amount"].ToString());

                        //if (txJson["blockhash"] == null)
                        //    continue;
                        //
                        //JObject blockInfoJson = GetBlockInfo(txJson["blockhash"].ToString());
                        //
                        //if (blockInfoJson == null || blockInfoJson["time"] == null)
                        //    continue;
                        //
                        //newInfo.time = Convert.ToInt64(blockInfoJson["time"].ToString());

                        newInfo.time = Convert.ToInt64(txJson["time"].ToString());

                        newInfo.txId = txJson["txid"].ToString();

                        if (txJson["fee"] != null)
                            newInfo.fee = Convert.ToDouble(txJson["fee"].ToString());

                        if (txJson["label"] != null)
                            newInfo.label = txJson["label"].ToString();

                        if (txJson["comment"] != null)
                            newInfo.label = txJson["comment"].ToString();

                        list.Add(newInfo);
                    }

                    return list;
                }
            }
            catch(Exception e)
            {
                if (txJson != null)
                    Logger.Log("faild parse txList {0}, {1}\n{2}", i, txJson.ToString(), e.ToString());
            }

            return null;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public JObject GetBlockInfo(string blockHash)
        {
            string blockInfoStr = commandline.Process(string.Format("getblock \"{0}\"", blockHash));

            JObject json = null;

            if (TryParseJson(blockInfoStr, out json))
                return json;

            return null;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public DateTime BlockTimeToUtcTime(long time)
        {
            DateTime tmp = new DateTime(1970, 1, 1);
            return new DateTime(time * TimeSpan.TicksPerSecond + tmp.Ticks, DateTimeKind.Utc);
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public List<QtumPeerInfo> GetPeerList()
        {
            List<QtumPeerInfo> list = new List<QtumPeerInfo>();

            string peerInfoStr = "{\n PeerList : " + commandline.Process("getpeerinfo") + "\n}";

            JObject json = null;

            if (TryParseJson(peerInfoStr, out json) == false)
                return list;
            
            JToken peerListJson = json["PeerList"];

            for (int i=0; i< peerListJson.Count(); ++i)
            {
                JToken peerInfo = peerListJson[i];

                QtumPeerInfo newInfo = new QtumPeerInfo();

                double ping = 0;
                double minPing = 0;

                if(peerInfo["pingwait"] != null)
                {
                    double.TryParse(peerInfo["pingwait"].ToString(), out ping);
                    minPing = ping;
                }
                else
                {
                    if(peerInfo["minping"] != null)
                        double.TryParse(peerInfo["minping"].ToString(), out minPing);

                    if(peerInfo["pingtime"] != null)
                        double.TryParse(peerInfo["pingtime"].ToString(), out ping);
                }                

                newInfo.pingTime = (ping + minPing) * 0.5;
                newInfo.addr = peerInfo["addr"].ToString();
                newInfo.version = peerInfo["version"].ToString();
                newInfo.subVersion = peerInfo["subver"].ToString();

                list.Add(newInfo);
            }

            return list;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public void BanPeer(QtumPeerInfo peer)
        {
            if (peer == null || string.IsNullOrEmpty(peer.addr))
                return;

            string address = peer.addr;

            if(address.IndexOf(":") != -1)
            {
                address = address.Substring(0, address.IndexOf(":"));
            }

            commandline.Process(string.Format("setban \"{0}\" \"add\" \"86400\"", address));
        }
    }
    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

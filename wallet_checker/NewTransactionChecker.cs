using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker
{
    static public class NewTransactionChecker
    {
        static private DateTime lastTxTime = DateTime.MinValue;

        static private DateTime lastRefreshTine = DateTime.MinValue;

        static public void Init()
        {
            LoadLastTime();
        }

        static public async void RefreshTransactionInfo()
        {
            if (lastTxTime == DateTime.MaxValue)
                return;

            if ((DateTime.Now - lastRefreshTine).Ticks / TimeSpan.TicksPerSecond < 10)
                return;

            lastRefreshTine = DateTime.Now;

            List<QtumTxInfo> list = QtumHandler.GetTransactions(1);

            if(list != null && list.Count > 0)
            {
                QtumTxInfo lastInfo = list[list.Count - 1];

                DateTime newLastTime = QtumHandler.BlockTimeToUtcTime(lastInfo.time);

                if(newLastTime > lastTxTime)
                {
                    list = QtumHandler.GetTransactions(100);

                    DateTime notiyStartTime = lastTxTime;

                    lastTxTime = DateTime.MaxValue;

                    await BroadcastTxNotify(notiyStartTime, SummarizeTxList(list));

                    lastTxTime = newLastTime;

                    SaveLastTime();
                }
            }
        }

        static private void LoadLastTime()
        {
            try
            {
                using (FileStream file = File.Open("txLastCheckTime.bin", FileMode.OpenOrCreate))
                {
                    using (StreamReader reader = new StreamReader(file))
                    {
                        try
                        {
                            long lastTick = Convert.ToInt64(reader.ReadLine());

                            if (lastTick == 0)
                                return;

                            lastTxTime = new DateTime(lastTick);
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }
            catch(Exception)
            {

            }
        }

        static private void SaveLastTime()
        {
            using (FileStream file = File.Open("txLastCheckTime.bin", FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(file))
                {
                    writer.WriteLine(lastTxTime.Ticks);
                }
            }            
        }

        static private List<QtumTxInfo> SummarizeTxList(List<QtumTxInfo> orgList)
        {
            if (orgList == null)
                return null;

            if (orgList.Count == 0)
                return orgList;

            List<QtumTxInfo> newList = new List<QtumTxInfo>();

            Dictionary<string, QtumTxInfo> infoByTxId = new Dictionary<string, QtumTxInfo>();

            foreach(var txInfo in orgList)
            {
                QtumTxInfo beforeInfo = null;
                if (infoByTxId.TryGetValue(txInfo.txId, out beforeInfo) == false)
                {
                    beforeInfo = txInfo;
                    infoByTxId.Add(txInfo.txId, beforeInfo);
                    continue;
                }

                beforeInfo.amount += txInfo.amount;
                beforeInfo.fee += txInfo.fee;
                beforeInfo.comment = txInfo.comment;
                beforeInfo.label = txInfo.label;
            }
            
            foreach(var txInfo in infoByTxId.Values)
            {
                newList.Add(txInfo);
            }
            
            newList.Sort((left,right) =>
                {
                    int cmp = left.time.CompareTo(right.time);
                    if (cmp == 0)
                    {
                        cmp = left.txId.CompareTo(right.txId);
                        if(cmp == 0)
                        {
                            cmp = left.address.CompareTo(right.address);
                        }
                    }

                    return cmp;
                }
            );

            return newList;
        }

        private static async Task BroadcastTxNotify(DateTime startTime, List<QtumTxInfo> txList)
        {
            if (txList == null)
                return;

            if (startTime == DateTime.MinValue)
                return;

            await UserList.ForeachSendMsg("---------------------------------");
            await UserList.ForeachSendMsg("A new transaction has occurred!\n");

            for (int i=txList.Count - 1; i >= 0; --i)
            {
                QtumTxInfo txInfo = txList[i];
                DateTime txTime = QtumHandler.BlockTimeToUtcTime(txInfo.time);

                if (txTime < startTime)
                    break;

                string notifyStr = txInfo.GetString();

                Logger.Log(notifyStr);
                Logger.Log("");

                await UserList.ForeachSendMsg(notifyStr);
            }

            await UserList.ForeachSendMsg("---------------------------------");
        }
    }
}

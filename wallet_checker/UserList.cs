using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace wallet_checker
{
    static public class UserList
    {
        ///--------------------------------------------------------------------------------------------------------
        ///

        class InvalidUserInfo
        {
            public DateTime lastTime = DateTime.Now;
            public uint accessCount = 0;
        }

        static private string savePath = "./userList.txt";

        static private HashSet<long> userIdList = new HashSet<long>();
        static private Dictionary<long, InvalidUserInfo> invalidUserList = new Dictionary<long, InvalidUserInfo>();

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public bool Exists(long id)
        {
            return userIdList.Contains(id);
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public void AddUser(long id)
        {
            if (id <= 0)
                return;

            if(userIdList.Add(id))
                Save();
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public async Task ForeachAsync(Func<long, Task> processor)
        {
            foreach (var userId in userIdList)
            {
                Task task = processor.DynamicInvoke(userId) as Task;
                await task;
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public async Task ForeachSendMsg(string msg, ParseMode parseMode = ParseMode.Html)
        {
            Logger.Log("\n@@@@@@@@@@");
            Logger.Log("[Boadcast]");
            Logger.Log(msg);
            Logger.Log("@@@@@@@@@@\n");

            async Task sendProcessor(long userId)
            {
                await TelegramBot.Bot.SendTextMessageAsync(userId, msg, parseMode);
            }

            await ForeachAsync(sendProcessor);
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static private void Save()
        {
            Logger.Log("Start Save UserIdList");
            try
            {
                using (FileStream fs = File.Open(savePath, FileMode.Create))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        foreach (var userId in userIdList)
                        {
                            writer.WriteLine(userId);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString());
                Logger.Log("Failed Save UserIdList");
                return;
            }

            Logger.Log("Success Save UserIdList");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public void Load()
        {
            Logger.Log("Start Load UserIdList");

            userIdList.Clear();

            if (File.Exists(savePath) == false)
                return;

            try
            {
                using (FileStream fs = File.Open(savePath, FileMode.Open))
                {
                    using (StreamReader writer = new StreamReader(fs))
                    {
                        while(writer.EndOfStream == false)
                            userIdList.Add(Convert.ToInt64(writer.ReadLine()));
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString());
                Logger.Log("Failed Load UserIdList");
                return;
            }

            Logger.Log("Success Load UserIdList");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public uint AddInvalidUser(long id)
        {
            InvalidUserInfo info = null;

            if( invalidUserList.TryGetValue(id, out info) == false)
            {
                info = new InvalidUserInfo();
                invalidUserList.Add(id, info);
            }

            info.lastTime = DateTime.Now;
            ++info.accessCount;

            return info.accessCount;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public void UpdateInvalidUserList()
        {
            List<long> deleteUserList = new List<long>();
            DateTime now = DateTime.Now;
            foreach (var pair in invalidUserList)
            {
                if((now - pair.Value.lastTime).Ticks / TimeSpan.TicksPerMinute > 1)
                {
                    deleteUserList.Add(pair.Key);
                }
            }

            foreach(var userId in deleteUserList)
            {
                invalidUserList.Remove(userId);
            }
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

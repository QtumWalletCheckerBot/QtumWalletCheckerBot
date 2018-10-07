using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker
{
    static public class UserList
    {
        ///--------------------------------------------------------------------------------------------------------
        ///

        static private string savePath = "./userList.txt";

        static private HashSet<long> userIdList = new HashSet<long>();

        public delegate void UserProcessor(long userId);

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
        static public async Task ForeachAsync(UserProcessor processor)
        {
            foreach( var userId in userIdList)
            {
                var task = Task.Run(()=>processor(userId));
                await task;
            }
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
    }
}

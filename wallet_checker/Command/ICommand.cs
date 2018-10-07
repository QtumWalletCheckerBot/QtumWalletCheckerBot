using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker.Command
{
    public enum eCommand
    {
        None,
        CheckState,
        StartStaking,
        StopStaking,
        RestartQtumWallet,
    }
    

    public abstract class ICommand
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        abstract public eCommand GetCommandType();

        abstract public string GetCommandName();

        abstract public string GetCommandDesc();

        protected virtual async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            await Task<bool>.Run(() => { });

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public async Task<bool> Process(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            UserList.AddUser(requesterId);

            try
            {
                if (requestTime.Kind != DateTimeKind.Local)
                {
                    TimeZoneInfo koreaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");
                    requestTime = TimeZoneInfo.ConvertTimeFromUtc(requestTime, koreaTimeZone);
                }
            }
            catch (Exception)
            {
                
            }

            LogStartCommand(GetCommandType().ToString(), requesterId, requesterName);
            
            return await OnStart(requesterId, requesterName, requestTime, args);
        }        

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected async Task SendMessage(long requesterId, string msg)
        {
            if (requesterId >= 0)
            {
                await TelegramBot.Bot.SendTextMessageAsync(requesterId, msg);
            }
            else
            {
                async void sendProcessor(long userId)
                {
                    await TelegramBot.Bot.SendTextMessageAsync(userId, msg);
                }

                await UserList.ForeachAsync(sendProcessor);
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected static void LogStartCommand(string cmdName, long requesterId, string requesterName)
        {
            DateTime recvTime = DateTime.Now;

            Logger.Log("///////////////////////////////////////////////////////////////////////////////////////");
            Logger.Log("//");
            Logger.Log("");
            Logger.Log(" 메세지 도착 : {0:yyyy/MM/dd HH:mm:ss}", recvTime);
            Logger.Log(" 명령 : {0}", cmdName);
            Logger.Log(string.Format(" 요청자 : {0}, {1}", requesterName, requesterId));
            Logger.Log("");
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace wallet_checker.Command
{
    public enum eCommand
    {
        None,
        CheckState,
        GetAddressList,
        CreateAddress,
        StartStaking,
        StopStaking,
        RestartQtumWallet,
        SendQtum,
        RemoteCommandLine,
        BackupWallet,
        RestoreWallet,
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
        private bool isCompleted = false;
        public bool IsCompleted {
            get
            {
                return isCompleted;
            }
            protected set
            {
                if (isCompleted == value)
                    return;

                isCompleted = value;

                if (isCompleted)
                    OnFinish();
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public async Task<bool> Process(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            IsCompleted = false;

            UserList.AddUser(requesterId);

            LogStartCommand(GetCommandType().ToString(), requesterId, requesterName);
            
            return await OnStart(requesterId, requesterName, requestTime, args);
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public virtual async Task OnUpdate() { await Task.Run(() => { }); }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected virtual void OnFinish() { }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public virtual async Task OnMessage(Telegram.Bot.Types.Message message) { await Task.Run(() => { }); }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected async Task SendMessage(long requesterId, string msg, ParseMode parseMode = ParseMode.Html)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            if (requesterId >= 0)
            {
                await TelegramBot.Bot.SendTextMessageAsync(requesterId, msg, parseMode);
            }
            else
            {
                await UserList.ForeachSendMsg(msg, parseMode);
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected static void LogStartCommand(string cmdName, long requesterId, string requesterName)
        {
            DateTime recvTime = DateTimeHandler.GetTimeZoneNow();

            Logger.Log("///////////////////////////////////////////////////////////////////////////////////////");
            Logger.Log("//");
            Logger.Log("");
            Logger.Log(" 메세지 도착 : {0:yyyy/MM/dd HH:mm:ss}", recvTime);
            Logger.Log(" 명령 : {0}", cmdName);
            Logger.Log(string.Format(" 요청자 : {0}, {1}", requesterName, requesterId));
            Logger.Log("");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public static string GetAddressLink(string address)
        {
            return string.Format("<a href=\"https://qtum.info/address/{0}/\">{1}</a>", address, address);
        }

        public static string GetTxLink(string tx)
        {
            return string.Format("<a href=\"https://qtum.info/tx/{0}\">{1}</a>", tx, tx);
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

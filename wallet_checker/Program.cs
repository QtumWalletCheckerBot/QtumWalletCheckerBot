using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

///텔레그램 dll using
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace wallet_checker
{
    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 
    /// </summary>
    class Program
    {
        ///--------------------------------------------------------------------------------------------------------
        /// 프로그램 도입부
        static void Main(string[] args)
        {
            Startup();

            Update();

            while(true)
            {
                Thread.Sleep(1000);
                UserList.UpdateInvalidUserList();
            }

            /// Recv Stop
            TelegramBot.Bot.StopReceiving();
        }

        static async void Startup()
        {

            if (Config.Load() == false)
            {
                Logger.Log("Config 파일이 손상되어 실행 할 수 없습니다.");
                Logger.Log("파일을 확인 하고 다시 실행 해 주세요.");
                System.Console.ReadLine();
                return;
            }

            OtpChecker.Init();

            if (await RestartQtumWallet() == false)
            {
                System.Console.ReadLine();
                return;
            }

            if (PasswordManager.RegisterPassword() == false)
            {
                System.Console.ReadLine();
                return;
            }

            UserList.Load();
            NewTransactionChecker.Init();

            await TelegramBot.Initialize(Bot_OnMessage, Bot_OnMessage, Bot_OnReceiveError);

            await BroadcastStartupNotify();

            await StartupAutoStaking();            
        }

        static async void Update()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (currentCommand != null && currentCommand.IsCompleted == false)
                {
                    await currentCommand.OnUpdate();
                }
                else
                {
                    NewTransactionChecker.RefreshTransactionInfo();
                }
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 
        private static async Task<bool> RestartQtumWallet()
        {
            bool result = await Command.RestartQtumWallet.Restart();
            
            if(result == false)
            {
                Logger.Log("퀀텀 월렛 실행에 실패했습니다. 프로그램을 종료합니다.");
                return false;
            }
            
            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 등록된 유저들에게 시작 알람을 보냅니다.
        private static async Task BroadcastStartupNotify()
        {
            async Task unban(long userId)
            {
                try
                {
                    await TelegramBot.Bot.UnbanChatMemberAsync(userId, Convert.ToInt32(userId));
                }
                catch(Exception)
                {

                }
            }

            await UserList.ForeachAsync(unban);
            await UserList.ForeachSendMsg("//////////////////////////////////////////////////////");
            await UserList.ForeachSendMsg(strings.Format("퀀텀 지갑이 구동되었습니다.  {0}", DateTimeHandler.GetTimeZoneNow()));
            await UserList.ForeachSendMsg(Command.CommandFactory.GetCommandHelpString());
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 기동 시 자동 스테이킹 시작
        private async static Task StartupAutoStaking()
        {
            if(Config.StartupAutoStaking)
            {
                Logger.Log("StartupAutoStaking");

                await UserList.ForeachSendMsg(strings.Format("자동 채굴 시작이 설정되어 있습니다. 채굴을 시작합니다."));

                Command.ICommand command = Command.CommandFactory.CreateCommand(Command.eCommand.StartStaking);

                if(command != null)
                {
                    await command.Process(-1, "", DateTimeHandler.GetTimeZoneNow());
                }
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        /// Recv Error
        private static void Bot_OnReceiveError(object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
        {
            Logger.Log("OnReceiveError : ");
            Logger.Log(e.ToString());

            Debugger.Break();
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 사용자로 부터 메세지를 받음
        private static async void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs messageEventArgs)
        {
            /// Message 객체
            var message = messageEventArgs.Message;

            /// 예외처리
            if (message == null || message.Type != MessageType.Text)
                return;

            if (UserList.Exists(message.Chat.Id) == false)
            {
                const int kickDurationMinute = 30;
                const uint accessCountMax = 1;
                uint accessCount = UserList.AddInvalidUser(message.Chat.Id);

                if (accessCount > accessCountMax)
                    return;

                try
                {
                    string alretMsg = string.Format(
                    "user name : {0}\nuser id : {1}\n you are not registered on user list.\n add the your user id to UserList.txt and restart the bot program."
                    , message.Chat.Username, message.Chat.Id);

                    await TelegramBot.Bot.SendTextMessageAsync(message.Chat.Id, alretMsg);

                    Logger.Log("Invalid Access User {0:yyyy/MM/dd HH:mm:ss} {1} {2}", DateTimeHandler.ToLocalTime(message.Date), message.Chat.Id, message.Chat.Username);
                }
                catch(Exception)
                {

                }                

                if (accessCount>= accessCountMax)
                {
                    try
                    {
                        DateTime now = DateTime.UtcNow;
                        await TelegramBot.Bot.RestrictChatMemberAsync(message.Chat.Id, message.From.Id, now.Add(new TimeSpan(0, kickDurationMinute, 0)));

                        Logger.Log("Kick User {0:yyyy/MM/dd HH:mm:ss} {1} {2}", DateTimeHandler.ToLocalTime(message.Date), message.Chat.Id, message.Chat.Username);
                    }
                    catch(Exception)
                    {
                    }
                }                

                return;
            }

            if(currentCommand != null && currentCommand.IsCompleted == false)
            {
                await currentCommand.OnMessage(message);

                return;
            }

            //string cmdStr = message.Text.Replace("-", "").Trim();
            string cmdStr = message.Text.Trim();

            Command.eCommand commandType = Command.CommandFactory.ParseCommand(cmdStr);

            Command.ICommand command = Command.CommandFactory.CreateCommand(commandType);

            if(command != null)
            {
                currentCommand = command;

                string[] args = cmdStr.Split(' ');

                bool success = await currentCommand.Process(message.Chat.Id, message.Chat.Username, DateTimeHandler.ToLocalTime(message.Date), args);
            }
            else
            {
                string helpStr = Command.CommandFactory.GetCommandHelpString();

                await TelegramBot.Bot.SendTextMessageAsync(message.Chat.Id, strings.Format(helpStr));
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 
        static private Command.ICommand currentCommand = null;

        ///--------------------------------------------------------------------------------------------------------
    }

    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
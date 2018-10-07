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
            if (Config.Load()== false)
            {
                Logger.Log("Config 파일이 손상되어 실행 할 수 없습니다.");
                Logger.Log("파일을 확인 하고 다시 실행 해 주세요.");
                System.Console.ReadLine();
                return;
            }

            if(RestartQtumWallet() == false)
            {
                System.Console.ReadLine();
                return;
            }

            if(PasswordManager.RegisterPassword() == false)
            {
                System.Console.ReadLine();
                return;
            }

            UserList.Load();

            TelegramBot.Initialize(Bot_OnMessage, Bot_OnMessage, Bot_OnReceiveError);

            BroadcastStartupNotify();

            StartupAutoStaking();

            while (true)
            {
                Thread.Sleep(500);
            }

            /// Recv Stop
            TelegramBot.Bot.StopReceiving();
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 
        private static bool RestartQtumWallet()
        {
            bool result = Command.RestartQtumWallet.Restart();

            if(result == false)
            {
                Logger.Log("퀀텀 월렛 실행에 실패했습니다. 프로그램을 종료합니다.");
                return false;
            }

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 등록된 유저들에게 시작 알람을 보냅니다.
        private static async void BroadcastStartupNotify()
        {
            string msg = strings.Format("퀀텀 지갑이 구동되었습니다.  {0}", DateTime.Now);
            msg += "\n\n";
            msg += Command.CommandFactory.GetCommandHelpString();

            UserList.UserProcessor processor = async (long userId) => { await TelegramBot.Bot.SendTextMessageAsync(userId, msg); };

            await UserList.ForeachAsync(processor);
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 기동 시 자동 스테이킹 시작
        private async static void StartupAutoStaking()
        {
            if(Config.StartupAutoStaking)
            {
                Logger.Log("StartupAutoStaking");

                UserList.UserProcessor processor = async (long userId) => {
                    await TelegramBot.Bot.SendTextMessageAsync(userId, strings.Format("자동 채굴 시작이 설정되어 있습니다. 채굴을 시작합니다."));
                };

                await UserList.ForeachAsync(processor);
                
                Command.ICommand command = Command.CommandFactory.CreateCommand(Command.eCommand.StartStaking);

                if(command != null)
                {
                    await command.Process(-1, "", DateTime.Now);
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

            string cmdStr = message.Text.Replace("-", "").Trim();

            Command.eCommand commandType = Command.CommandFactory.ParseCommand(cmdStr);

            Command.ICommand command = Command.CommandFactory.CreateCommand(commandType);

            if(command != null)
            {
                bool success = await command.Process(message.Chat.Id, message.Chat.Username, message.Date);
            }
            else
            {
                string helpStr = Command.CommandFactory.GetCommandHelpString();

                await TelegramBot.Bot.SendTextMessageAsync(message.Chat.Id, strings.Format(helpStr));
            }
        }

        ///--------------------------------------------------------------------------------------------------------
    }

    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
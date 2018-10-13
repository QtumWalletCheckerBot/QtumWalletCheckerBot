using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace wallet_checker
{
    public static class TelegramBot
    {
        public static TelegramBotClient Bot = null;

        public static async Task Initialize(EventHandler<MessageEventArgs> OnMessage, EventHandler<MessageEventArgs> OnMessageEdited, EventHandler<ReceiveErrorEventArgs> OnReceiveError)
        {
            Bot = new TelegramBotClient(Config.TelegramApiId);

            ///봇 이벤트 추가
            Bot.OnMessage += OnMessage;
            Bot.OnMessageEdited += OnMessageEdited;
            Bot.OnReceiveError += OnReceiveError;

            var getMe = Bot.GetMeAsync();

            var me = await getMe;

            Console.Title = me.Username;

            Logger.Log("//----------------------------------------------------------//");
            Logger.Log("");
            Logger.Log("   Start QtumWalletChecker");
            Logger.Log("   API KEY : {0}", Config.TelegramApiId);
            Logger.Log("   BotName : {0}", me.Username);
            Logger.Log("   BotId : {0}", me.Id);
            Logger.Log("");
            Logger.Log("//----------------------------------------------------------//");
            Logger.Log("\n\n\n");

            /// Recv Start
            Bot.StartReceiving();
        }
    }
}

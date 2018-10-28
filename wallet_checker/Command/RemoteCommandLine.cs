using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker.Command
{
    class RemoteCommandLine : ICommand
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        enum eCommandState
        {
            Ready,
            InputWaitOtp,
            InputWaitCommandLine,
        }

        private eCommandState commandState = eCommandState.Ready;
        long otpWaitUserId = 0;
        DateTime waitStartTime = DateTime.MinValue;

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override eCommand GetCommandType()
        {
            return eCommand.RemoteCommandLine;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandName()
        {
            return strings.GetString("원격 커맨드 라인");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandDesc()
        {
            return strings.GetString("원격으로 커맨드 라인을 입력합니다.");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            commandState = eCommandState.InputWaitOtp;

            otpWaitUserId = requesterId;

            waitStartTime = DateTime.Now;

            await SendMessage(requesterId, strings.GetString("Otp 인증 번호를 입력 하세요."));

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override void OnFinish()
        {
            base.OnFinish();

            Logger.Log("원격 커맨드 라인 응답 완료.\n");

            commandState = eCommandState.Ready;

            waitStartTime = DateTime.MinValue;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override async Task OnUpdate()
        {
            await base.OnUpdate();

            if (commandState == eCommandState.Ready)
                return;

            if (waitStartTime != DateTime.MinValue)
            {
                if ((DateTime.Now - waitStartTime).Ticks / TimeSpan.TicksPerSecond > 300.0f)
                {
                    Logger.Log("원격 커맨드 라인 응답 완료.\n");

                    IsCompleted = true;

                    await SendMessage(otpWaitUserId, strings.GetString("제한시간 초과"));
                }
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override async Task OnMessage(Telegram.Bot.Types.Message message)
        {
            await base.OnMessage(message);

            string msg = message.Text.Trim();

            long requesterId = message.Chat.Id;

            if (requesterId != otpWaitUserId)
                return;

            switch (commandState)
            {
                case eCommandState.InputWaitOtp:
                    {
                        string otpStr = message.Text.Trim();

                        if (OtpChecker.CheckOtp(otpStr))
                        {
                            commandState = eCommandState.InputWaitCommandLine;

                            waitStartTime = DateTime.Now;

                            await SendMessage(requesterId, strings.GetString("명령을 입력하세요.\n\n"));
                        }
                        else
                        {
                            IsCompleted = true;
                        
                            await SendMessage(requesterId, strings.GetString("Otp 인증에 실패 했습니다."));
                        }
                    }
                    break;

                case eCommandState.InputWaitCommandLine:
                    {
                        string commandLineStr = msg;

                        Logger.Log( "[" +message.Chat.Username + "] [" + message.Chat.Id + "] : " + commandLineStr);

                        string commandResult = QtumHandler.CommandLine(commandLineStr).Trim();

                        List<string> resultList = new List<string>();

                        await SendMessage(requesterId, "------------------------", Telegram.Bot.Types.Enums.ParseMode.Default);

                        if (string.IsNullOrEmpty(commandResult))
                        {
                            commandResult = "empty";

                            resultList.Add(commandResult);
                        }
                        else
                        {
                            // convert string to stream
                            byte[] byteArray = Encoding.UTF8.GetBytes(commandResult);
                            using (MemoryStream stream = new MemoryStream(byteArray))
                            {
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    while(reader.EndOfStream == false)
                                        resultList.Add(reader.ReadLine());
                                }
                            }
                        }

                        const int maxSendCount = 10;
                        const int onceListCount = 50;

                        int sendCount = 0;

                        for(int i=0; i<resultList.Count(); i+= onceListCount)
                        {
                            string responseMsg = "";

                            for(int j=0; j < onceListCount; ++j)
                            {
                                int idx = i + j;

                                if (idx >= resultList.Count())
                                    break;

                                responseMsg += resultList[idx] + "\n";
                            }

                            if(++sendCount >= maxSendCount)
                            {
                                await SendMessage(requesterId, "The result text is too long....", Telegram.Bot.Types.Enums.ParseMode.Default);
                                break;
                            }

                            await SendMessage(requesterId, responseMsg, Telegram.Bot.Types.Enums.ParseMode.Default);
                        }

                        await SendMessage(requesterId, "------------------------", Telegram.Bot.Types.Enums.ParseMode.Default);


                        IsCompleted = true;
                    }
                    break;
            }
        }
    }
}

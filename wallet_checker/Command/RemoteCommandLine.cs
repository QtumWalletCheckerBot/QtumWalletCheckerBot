using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                        string commandResult = QtumHandler.CommandLine(commandLineStr);

                        IsCompleted = true;

                        await SendMessage(requesterId, commandResult);
                    }
                    break;
            }
        }
    }
}

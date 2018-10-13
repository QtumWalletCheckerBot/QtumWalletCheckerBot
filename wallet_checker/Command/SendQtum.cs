using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace wallet_checker.Command
{
    public class SendQtum : ICommand
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        enum eSendCommandState
        {
            Ready,
            InputWaitOtp,
            InputWaitAddress,
            InputWaitAmount,
            InputWaitConfirm,
            Send,
        }

        private eSendCommandState commandState = eSendCommandState.Ready;
        long otpWaitUserId = 0;
        DateTime waitStartTime = DateTime.MinValue;
        string destAdress;
        double destAmount = 0;

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override eCommand GetCommandType()
        {
            return eCommand.SendQtum;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandName()
        {
            return strings.GetString("보내기");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandDesc()
        {
            return strings.GetString("퀀텀을 전송합니다.");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            if(QtumHandler.IsStakingState())
            {
                await SendMessage(requesterId, strings.GetString("채굴 상태에서는 전송 할 수 없습니다."));
                IsCompleted = true;
                return true;
            }

            commandState = eSendCommandState.InputWaitOtp;

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

            Logger.Log("보내기 응답 완료.\n");

            commandState = eSendCommandState.Ready;

            waitStartTime = DateTime.MinValue;

            //destAdress = "";
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override async Task OnUpdate() {

            await base.OnUpdate();

            if (commandState == eSendCommandState.Ready)
                return;
            
            if(waitStartTime != DateTime.MinValue)
            {
                if ((DateTime.Now - waitStartTime).Ticks / TimeSpan.TicksPerSecond > 60.0f)
                {
                    Logger.Log("보내기 응답 완료.\n");

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
                case eSendCommandState.InputWaitOtp:
                    {
                        string otpStr = message.Text.Trim();

                        if (OtpChecker.CheckOtp(otpStr))
                        {
                            commandState = eSendCommandState.InputWaitAddress;

                            waitStartTime = DateTime.Now;

                            await SendMessage(requesterId, strings.GetString("상대방의 퀀텀 주소를 입력하세요."));
                        }
                        else
                        {
                            IsCompleted = true;

                            await SendMessage(requesterId, strings.GetString("Otp 인증에 실패 했습니다."));                            
                        }
                    }
                    break;

                case eSendCommandState.InputWaitAddress:
                    {
                        if( QtumHandler.IsValidateAddress(msg))
                        {
                            destAdress = msg;

                            commandState = eSendCommandState.InputWaitAmount;

                            waitStartTime = DateTime.Now;

                            await SendMessage(requesterId, strings.GetString("보낼 수량을 입력하세요."));
                        }
                        else
                        {
                            IsCompleted = true;

                            await SendMessage(requesterId, strings.GetString("유효하지 않은 주소입니다."));
                        }
                    }
                    break;

                case eSendCommandState.InputWaitAmount:
                    {
                        double amount = 0;
                        if(double.TryParse( msg, out amount ) && QtumHandler.GetBalance() >= amount)
                        {
                            destAmount = amount;

                            commandState = eSendCommandState.InputWaitConfirm;

                            string str = strings.Format("받는 주소 : {0}", destAdress) + "\n" + strings.Format("보낼 수량 : {0}", destAmount);
                            str += "\n" + strings.Format("정말 진행 하시려면 숫자 1을 입력하세요.");

                            waitStartTime = DateTime.Now;

                            await SendMessage(requesterId, str);
                        }
                        else
                        {
                            IsCompleted = true;

                            await SendMessage(requesterId, strings.GetString("수량이 부족합니다."));
                        }
                    }
                    break;

                case eSendCommandState.InputWaitConfirm:
                    {
                        int num = 0;
                        if(int.TryParse(msg, out num) && num == 1)
                        {
                            commandState = eSendCommandState.Ready;

                            waitStartTime = DateTime.MinValue;

                            string resultErr = QtumHandler.Send(destAdress, destAmount);

                            if (string.IsNullOrEmpty(resultErr))
                            {
                                await SendMessage(requesterId, strings.GetString("보내기 응답 완료.\n"));
                            }
                            else
                            {
                                await SendMessage(requesterId, resultErr);
                                await SendMessage(requesterId, strings.GetString("보내기에 실패했습니다."));
                            }                            

                            IsCompleted = true;
                        }
                        else
                        {
                            IsCompleted = true;

                            await SendMessage(requesterId, strings.GetString("보내기가 취소되었습니다."));
                        }
                    }
                    break;
            }
        }
    }
}

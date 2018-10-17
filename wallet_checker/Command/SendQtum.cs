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
            InputWaitSendAddress,
            InputWaitReceiveAddress,
            InputWaitAmount,
            InputWaitConfirm,
            Send,
        }

        private eSendCommandState commandState = eSendCommandState.Ready;
        long otpWaitUserId = 0;
        DateTime waitStartTime = DateTime.MinValue;
        Dictionary<string, double> myAddressList = new Dictionary<string, double>();
        string myAddress;
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
            //if(QtumHandler.IsStakingState())
            //{
            //    await SendMessage(requesterId, strings.GetString("채굴 상태에서는 전송 할 수 없습니다."));
            //    IsCompleted = true;
            //    return true;
            //}            

            myAddress = "";
            destAdress = "";

            myAddressList.Clear();

            QtumHandler.GetInfo(out myAddressList);

            if (myAddressList.Count == 0 || QtumHandler.GetBalance() == 0)
            {
                IsCompleted = true;

                await SendMessage(requesterId, strings.GetString("지갑이 비어있습니다."));
            }
            else
            {
                commandState = eSendCommandState.InputWaitOtp;

                otpWaitUserId = requesterId;

                waitStartTime = DateTime.Now;

                await SendMessage(requesterId, strings.GetString("Otp 인증 번호를 입력 하세요."));
            }            

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
                            commandState = eSendCommandState.InputWaitSendAddress;

                            waitStartTime = DateTime.Now;

                            string sendMsg = strings.GetString("사용 할 본인의 주소를 번호로 선택하세요.");

                            sendMsg += "\n0. " + strings.GetString("자동으로 선택");

                            int num = 1;
                            foreach(var pair in myAddressList)
                            {
                                sendMsg += string.Format("\n{0}. {1} : {2}", num, pair.Key, pair.Value);
                                ++num;
                            }

                            await SendMessage(requesterId, sendMsg);
                        }
                        else
                        {
                            IsCompleted = true;

                            await SendMessage(requesterId, strings.GetString("Otp 인증에 실패 했습니다."));                            
                        }
                    }
                    break;

                case eSendCommandState.InputWaitSendAddress:
                    {
                        string[] args = msg.Split(' ');
                        int addressNum = 0;
                        if (args.Length <= 0 || int.TryParse(args[0], out addressNum) == false || addressNum < 0 || addressNum > myAddressList.Count)
                        {
                            IsCompleted = true;

                            await SendMessage(requesterId, strings.GetString("유효하지 않은 주소입니다."));
                        }
                        else
                        {
                            if(addressNum == 0)
                                myAddress = ""; // 자동이면 주소가 없습니다.
                            else
                                myAddress = myAddressList.Keys.ToArray()[addressNum - 1];

                            if (string.IsNullOrEmpty(myAddress) || QtumHandler.IsValidateAddress(myAddress))
                            {
                                commandState = eSendCommandState.InputWaitReceiveAddress;

                                waitStartTime = DateTime.Now;

                                await SendMessage(requesterId, strings.GetString("상대방의 퀀텀 주소를 입력하세요."));
                            }
                            else
                            {
                                IsCompleted = true;

                                await SendMessage(requesterId, strings.GetString("유효하지 않은 주소입니다."));
                            }
                        }                        
                    }
                    break;

                case eSendCommandState.InputWaitReceiveAddress:
                    {
                        bool invalidMyAddress = false;

                        double myBalance = 0;

                        if (string.IsNullOrEmpty(myAddress) == false && myAddressList.TryGetValue(myAddress, out myBalance) == false)
                        {
                            invalidMyAddress = true;
                        }

                        if(invalidMyAddress == false && QtumHandler.IsValidateAddress(msg))
                        {
                            destAdress = msg;

                            commandState = eSendCommandState.InputWaitAmount;

                            waitStartTime = DateTime.Now;                            

                            if(string.IsNullOrEmpty(myAddress) || myAddressList.TryGetValue(myAddress, out myBalance) == false)
                            {
                                myBalance = QtumHandler.GetBalance();
                            }
                            else
                            {
                                myBalance = myAddressList[myAddress];
                            }

                            string sendMsg = strings.GetString("보낼 수량을 입력하세요.");

                            sendMsg += "\n" + strings.Format("가능 수량 {0}", myAddress + " " + myBalance);

                            await SendMessage(requesterId, sendMsg);
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

                            string myAddressStr = myAddress;

                            if (string.IsNullOrEmpty(myAddressStr))
                                myAddressStr = strings.GetString("자동으로 선택");

                            string str = strings.Format("나의 주소 : {0}", myAddressStr) + "\n" + strings.Format("받는 주소 : {0}", destAdress) + "\n" + strings.Format("보낼 수량 : {0}", destAmount);
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

                            string result = QtumHandler.Send(destAdress, destAmount);

                            if (string.IsNullOrEmpty(result) == false)
                            {
                                await SendMessage(requesterId, "tx :\nhttps://explorer.qtum.org/tx/" + result);
                                await SendMessage(requesterId, strings.GetString("보내기 응답 완료.\n"));
                            }
                            else
                            {
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

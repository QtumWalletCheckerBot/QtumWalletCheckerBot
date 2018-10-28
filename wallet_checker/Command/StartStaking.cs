using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker.Command
{
    public class StartStaking : ICommand
    {
        ///--------------------------------------------------------------------------------------------------------
        /// 

        public override eCommand GetCommandType()
        {
            return eCommand.StartStaking;
        }

        public override string GetCommandName()
        {
            return strings.GetString("시작");
        }

        public override string GetCommandDesc()
        {
            return strings.GetString("채굴을 시작합니다.");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            Logger.Log("채굴을 시작합니다.");

            await SendMessage(requesterId, "");

            await SendMessage(requesterId, strings.Format("{0} 채굴을 시작합니다.", requesterName));

            bool invalidPassword = false;

            var taskResponse = Task<string>.Run(() => MakeStartStakingResponse(requestTime, out invalidPassword));

            string response = await taskResponse;

            await SendMessage(requesterId, response);
            
            if (invalidPassword)
            {
                System.IO.File.Delete(PasswordManager.passwordFile);
                await SendMessage(requesterId, strings.Format("잘못된 암호로 실패했습니다. 봇 프로그램에서 퀀텀 월렛 암호 설정을 확인 해주세요."));
            }
            
            Logger.Log("채굴 시작 응답 완료.\n");

            if (invalidPassword == false)
            {
                Command.ICommand command = Command.CommandFactory.CreateCommand(Command.eCommand.CheckState);

                if (command != null)
                {
                    await command.Process(-1, "", DateTimeHandler.GetTimeZoneNow());
                }
            }

            IsCompleted = true;

            return invalidPassword == false;
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 채굴 시작 요청에 대한 응답메세지 생성
        private static string MakeStartStakingResponse(DateTime requestTime, out bool invalidPassword)
        {
            string msgDateStr = "";

            invalidPassword = false;

            msgDateStr = string.Format("{0:yyyy/MM/dd HH:mm:ss}", requestTime);

            string resultStr = QtumHandler.StartSaking();

            var response = strings.Format(@"
 ---------------------------------
 요청 : {0}
 응답 : {1:yyyy/MM/dd HH:mm:ss}
 결과 : {2}
 ---------------------------------", msgDateStr, DateTimeHandler.GetTimeZoneNow(), resultStr.Count() == 0 ? "Success" : resultStr);

            Logger.Log(response);
            Logger.Log("");

            if (resultStr == strings.GetString("패스워드 오류"))
                invalidPassword = true;

            return response;
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

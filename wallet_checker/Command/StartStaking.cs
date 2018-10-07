using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            else
            {
                await SendMessage(requesterId, strings.GetString("프로그램에 등록된 패스워드가 퀀텀 지갑의 패스워드와 일치하지 않다면, 컨트롤에 실패하였을 수 있습니다.\n[1. 확인] 명령어로 정상적인 변경이 되었는지 확인 해 주세요.\n확인 후 정상적인 작동이 이뤄지지 않았다면, data.bin 파일을 지우고 올바른 패스워드를 다시 등록 해 주세요."));
            }

            Logger.Log("채굴 시작 응답 완료.\n");

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
 ---------------------------------", msgDateStr, DateTime.Now, resultStr.Count() == 0 ? "Success" : resultStr);

            Logger.Log(response);
            Logger.Log("");

            if (resultStr == strings.GetString("패스워드 오류"))
                invalidPassword = true;

            return response;
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

using OtpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker
{
    static public class OtpChecker
    {
        static private string lastCheckValue = "";
        static private Totp otp = null;
        static public void Init()
        {
            if (otp != null)
                return;

            if(string.IsNullOrEmpty(Config.OtpSecretKey))
            {
                Config.OtpSecretKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

                System.Console.WriteLine("Generated OtpKey: " + Config.OtpSecretKey);

                Config.Save();
            }

            otp = new Totp(Base32Encoding.ToBytes(Config.OtpSecretKey));
        }
        
        static public bool CheckOtp(string otpValue)
        {
            Logger.Log("Start CheckOtp");

            try
            {
                Init();

                if (lastCheckValue == otpValue)
                    return false;

                long timeStepMatched = 0;
                string computeValue = otp.ComputeTotp();

                var window = new VerificationWindow(previous: 1, future: 1);

                if (otp.VerifyTotp(otpValue, out timeStepMatched, window))
                {
                    lastCheckValue = otpValue;
                    return true;
                }
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString());
            }

            Logger.Log("Finish CheckOtp");

            return false;
        }
    }
}

using ICSharpCode.SharpZipLib.GZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker
{
    public class PasswordManager
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        static public readonly string passwordFile = Directory.GetCurrentDirectory() + @".\\Config\\data.bin";

        ///--------------------------------------------------------------------------------------------------------
        /// 패스워드가 등록되어 있는지 확인하고 패스워드를 등록 합니다.
        static public bool RegisterPassword()
        {
            PasswordManager pm = new PasswordManager();

            if (System.IO.File.Exists(PasswordManager.passwordFile) == true && string.IsNullOrEmpty(pm.GetPassword()) == false)
                return true;
            
            Logger.Log("패스워드가 등록되지 않았습니다. 퀀텀 지갑의 패스워드를 입력 해 주세요. 등록되지 않은 상태에서 최초 한번 설정합니다.");
            Logger.Log("패스워드는 현재 기기에 암호화 되어 저장되며 외부로 전송되지 않습니다.\n하지만 저장된 패스워드는 멀웨어 감염이나 기기 해킹등에 의해 보호받을 수 없으니,\n채굴 머신의 보안에 각별히 주의 해 주시기 바랍니다.\n");

            while (true)
            {
                Logger.Log("암호를 입력하고 엔터 : ");
                string newPw = System.Console.ReadLine();

                Logger.Log("다시 한번 입력 해 주세요. : ");
                string newPw2 = System.Console.ReadLine();

                Logger.Log("");

                if (newPw != newPw2)
                {
                    Logger.Log("입력하신 암호가 일치하지 않습니다.");
                    continue;
                }

                if (pm.SetPassword(newPw) == false)
                {
                    return false;
                }

                break;
            }

            Logger.Log("암호가 등록되었습니다.");

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public bool SetPassword(string pwd)
        {
            string hdmasterkeyid = QtumHandler.GetHDMasterKeyId();

            if (string.IsNullOrEmpty(hdmasterkeyid))
            {
                Logger.Log("퀀텀 지갑의 구동 상태를 확인 해 주세요.");
                return false;
            }

            try
            {
                byte[] pwdBytes = Encoding.UTF8.GetBytes(Encrypt(pwd, Encrypt(hdmasterkeyid, Config.RPCPassword)));
                
                using (FileStream fs = File.Open(passwordFile, FileMode.Create))
                {
                    using (GZipOutputStream gzs = new GZipOutputStream(fs))
                    {
                        using (MemoryStream c = new MemoryStream())
                        {
                            gzs.Write(pwdBytes, 0, pwdBytes.Length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                return false;
            }

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public string GetPassword()
        {
            string pwd = null;

            string hdmasterkeyid = QtumHandler.GetHDMasterKeyId();

            if (string.IsNullOrEmpty(hdmasterkeyid))
            {
                Logger.Log("퀀텀 지갑의 구동 상태를 확인 해 주세요.");
                return pwd;
            }

            try
            {
                if (File.Exists(passwordFile) == false)
                {
                    Logger.Log("파일 없음 : {0}", passwordFile);
                    return pwd;
                }

                using (FileStream fs = File.Open(passwordFile, FileMode.Open))
                {
                    using (GZipInputStream gzs = new GZipInputStream(fs))
                    {
                        const int count = 1024*1024;
                        byte[] dest = new byte[count];
                        int length = gzs.Read(dest, 0, count);
                        
                        pwd = Decrypt(Encoding.UTF8.GetString(dest, 0, length), Encrypt(hdmasterkeyid, Config.RPCPassword));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                return null;
            }

            return pwd;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        private static string Decrypt(string textToDecrypt, string key)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();

            rijndaelCipher.Mode = CipherMode.CBC;

            rijndaelCipher.Padding = PaddingMode.PKCS7;

            rijndaelCipher.KeySize = 128;

            rijndaelCipher.BlockSize = 128;

            byte[] encryptedData = Convert.FromBase64String(textToDecrypt);

            byte[] pwdBytes = Encoding.UTF8.GetBytes(key);

            byte[] keyBytes = new byte[16];

            int len = pwdBytes.Length;

            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }

            Array.Copy(pwdBytes, keyBytes, len);

            rijndaelCipher.Key = keyBytes;

            rijndaelCipher.IV = keyBytes;

            byte[] plainText = rijndaelCipher.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(plainText);
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        private static string Encrypt(string textToEncrypt, string key)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();

            rijndaelCipher.Mode = CipherMode.CBC;

            rijndaelCipher.Padding = PaddingMode.PKCS7;

            rijndaelCipher.KeySize = 128;

            rijndaelCipher.BlockSize = 128;

            byte[] pwdBytes = Encoding.UTF8.GetBytes(key);

            byte[] keyBytes = new byte[16];

            int len = pwdBytes.Length;

            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }

            Array.Copy(pwdBytes, keyBytes, len);

            rijndaelCipher.Key = keyBytes;

            rijndaelCipher.IV = keyBytes;

            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();

            byte[] plainText = Encoding.UTF8.GetBytes(textToEncrypt);

            return Convert.ToBase64String(transform.TransformFinalBlock(plainText, 0, plainText.Length));
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}

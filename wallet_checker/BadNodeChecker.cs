using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker
{
    static class BadNodeChecker
    {
        static DateTime lastUpdateTime = DateTime.MinValue;

        static public void Update()
        {
            if (((DateTime.Now - lastUpdateTime).Ticks / TimeSpan.TicksPerSecond) < 30)
            {
                return;
            }

            lastUpdateTime = DateTime.Now;

            try
            {
                List<QtumPeerInfo> peerList = QtumHandler.GetPeerList();

                for (int i = 0; i < peerList.Count; ++i)
                {
                    QtumPeerInfo info = peerList[i];

                    bool isBad = false;

                    if (info.pingTime >= Config.MiniumPing)
                    {
                        isBad = true;
                    }
                    else if (string.IsNullOrEmpty(info.subVersion) == false)
                    {
                        string subVersion = info.subVersion.Replace("/Satoshi:", "").Replace("/", "");

                        string[] subVersionStrList = subVersion.Split('.');

                        if (subVersionStrList.Length < 3)
                        {
                            isBad = true;
                        }
                        else
                        {
                            for (int n = 0; n < subVersionStrList.Length; ++n)
                            {
                                if (n >= 3)
                                    break;

                                int verNum = 0;
                                int.TryParse(subVersionStrList[n], out verNum);

                                if (Config.MiniumVersion[n] > verNum)
                                {
                                    isBad = true;
                                    break;
                                }

                            }
                        }
                    }

                    if (isBad)
                    {
                        QtumHandler.BanPeer(info);
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Log(e.ToString());
            }
        }
    }
}

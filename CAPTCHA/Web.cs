using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;

namespace CAPTCHA
{
    public static class Web
    {
        public static bool Connection()
        {
            IPStatus status = IPStatus.Unknown;
            try
            {
                status = new Ping().Send("google.com").Status;
            }
            catch (Exception)
            {
            }

            if (status == IPStatus.Success)
                return true;
            else
                return false;
        }

        public static bool IsValidUrl(string url)
        {
            Stream stream;
            HttpWebRequest webRequest;
            HttpWebResponse webResponse;

            try
            {
                webRequest = (HttpWebRequest)WebRequest.Create(url);
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                stream = webResponse.GetResponseStream();
                string read = new StreamReader(stream).ReadToEnd();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

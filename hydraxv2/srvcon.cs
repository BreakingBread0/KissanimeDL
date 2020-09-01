using Flurl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace hydraxv2 {
    public class srvcon {
        #region defaults
        public srvcon() {
            Cookies = new CookieCollection();
            //if (File.Exists("cookies.txt")) {
            //    Console.WriteLine("Using existing cookies");
            //    //Cookies = File.ReadAllText("cookies.txt");
            //    Cookies.Parse(File.ReadAllText("cookies.txt"));
            //}
        }
        public string Referer;

        public Dictionary<string, string> GetCookiesReturned(HttpWebResponse resp) {
            var ret = new Dictionary<string, string>();
            if (resp.Headers.AllKeys.Contains("Set-Cookie")) {
                foreach (var cookie in resp.Headers.GetValues("Set-Cookie")) {
                    var cook = cookie.Split(';')[0];
                    var cookiedata = cook.Split(new[] { '=' }, 2);
                    ret.Add(cookiedata[0], cookiedata[1]);
                }
            }
            return ret;
        }

        //static void HandleAddedCookies(WebHeaderCollection coll)
        //{
        //    if (coll.AllKeys.Contains("Set-Cookie"))
        //    {
        //        foreach (var cookie in coll.GetValues("Set-Cookie"))
        //        {
        //            var cook = cookie.Split(';')[0];
        //            Console.WriteLine("Handled new cokie: " + cook);

        //        }
        //        File.WriteAllText("cookies.txt", Cookies);
        //    }
        //}
        public class CookieCollection : Dictionary<string, string> {
            public void Parse(string cookies) {
                cookies = cookies.Replace(" ", string.Empty);
                foreach (var single in cookies.Split(new[] { ";" }, StringSplitOptions.None)) {
                    if (!single.Contains("=")) {
                        Console.WriteLine("Cookie format invalud! Cookie: " + single);
                    }
                    var cook = single.Split(new[] { '=' }, 2);
                    var cookieName = cook[0];
                    var cookieValue = cook[1];
                    ParseSingle(cookieName, cookieValue);
                }
            }
            public void ParseSingle(string cookie, string value) {
                lock (this) {
                    if (this.ContainsKey(cookie)) {
                        if (this[cookie] != value) {
                            Console.WriteLine($"Cookie value changed ({cookie}): {this[cookie]} -> {value}");
                            this[cookie] = value;
                        }
                    } else {
                        Console.WriteLine($"Cookie added ({cookie}): {value}");
                        this.Add(cookie, value);
                    }
                }
            }
            public void ParseCollection(WebHeaderCollection coll) {
                lock (this) {
                    if (coll.AllKeys.Contains("Set-Cookie")) {
                        foreach (var cookie in coll.GetValues("Set-Cookie")) {
                            var cook = cookie.Split(';')[0].Split(new[] { '=' }, 2);
                            var cookieName = cook[0];
                            var cookieValue = cook[1];
                            ParseSingle(cookieName, cookieValue);
                        }
                    }
                    File.WriteAllText("cookies.txt", GetSerialized());
                }
            }

            public string GetSerialized() {
                List<string> ret = new List<string>();
                foreach (var cookie in this) {
                    ret.Add($"{cookie.Key}={cookie.Value}");
                }
                return string.Join("; ", ret.ToArray());
            }
        }
        public CookieCollection Cookies;
        public static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.70 Safari/537.36";

        public void AddHeaders(ref HttpWebRequest req) {
            req.UserAgent = UserAgent;
            //req.Headers.Add("Origin", req.RequestUri.GetLeftPart(UriPartial.Authority));
            req.Referer = Referer;
            req.Host = req.RequestUri.Host;
            //Console.WriteLine(Cookies.GetSerialized());
            lock (Cookies)
                req.Headers.Add("Cookie", Cookies.GetSerialized());
            req.Timeout = int.MaxValue;
            req.Headers.Add("DNT", "1");

            req.Headers.Add("Sec-Fetch-Mode", "no-cors");
            req.Headers.Add("Sec-Fetch-Site", "cross-site");
        }

        public string POST(string url = null, string POSTData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            AddHeaders(ref req);
            if (POSTData != null) {
                byte[] buf = Encoding.Default.GetBytes(POSTData);
                req.ContentLength = buf.Length;
                req.GetRequestStream().Write(buf, 0, buf.Length);
            }
            try {
                var resp = req.GetResponse();
                Cookies.ParseCollection(resp.Headers);
                var code = ((HttpWebResponse)resp).StatusCode;
                return new StreamReader(resp.GetResponseStream()).ReadToEnd();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
            //Console.WriteLine("fail");
            return "fail";
        }
        public string GET(out bool success, string url = null, string GETData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create(GETData == null || string.IsNullOrWhiteSpace(GETData) ? $"{url}" : $"{url}?{GETData}");
            req.Method = "GET";
            AddHeaders(ref req);
            try {
                var resp = req.GetResponse();
                Cookies.ParseCollection(resp.Headers);
                success = true;
                return new StreamReader(resp.GetResponseStream()).ReadToEnd();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                success = false;
                return "";
            }
        }
        public HttpWebResponse GETRaw(string url = null, string GETData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create(GETData == null || string.IsNullOrWhiteSpace(GETData) ? $"{url}" : $"{url}?{GETData}");
            req.Method = "GET";
            req.Accept = "image/webp,image/apng,image/*,*/*;q=0.8";
            AddHeaders(ref req);
            try {
                var resp = req.GetResponse() as HttpWebResponse;
                Cookies.ParseCollection(resp.Headers);
                return resp;
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
        public void Save(string filename, string url = null, string GETData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create(GETData == null || string.IsNullOrWhiteSpace(GETData) ? $"{url}" : $"{url}?{GETData}");
            req.Method = "GET";
            AddHeaders(ref req);
            try {
                using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write)) {
                    req.GetResponse().GetResponseStream().CopyTo(stream);
                    stream.Flush();
                    stream.Close();
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        public byte[] Bytes(string url = null, string GETData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create(GETData == null || string.IsNullOrWhiteSpace(GETData) ? $"{url}" : $"{url}?{GETData}");
            req.Method = "GET";
            AddHeaders(ref req);
            try {
                using (MemoryStream stream = new MemoryStream()) {
                    var resp = req.GetResponse();
                    resp.GetResponseStream().CopyTo(stream);
                    Cookies.ParseCollection(resp.Headers);
                    return stream.ToArray();
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public static string POSTEncode(Dictionary<string, string> POSTData) {
            List<string> POST = new List<string>();
            foreach (var key in POSTData) {
                POST.Add($"{Url.Encode(key.Key)}={Url.Encode(key.Value, true)}");
            }
            return string.Join("&", POST.ToArray());
        }

        public static HttpWebRequest New(string URL) => (HttpWebRequest)HttpWebRequest.Create(URL);

        #endregion
    }
}

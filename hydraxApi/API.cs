using Flurl;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace hydrax
{
    public class API
    {
        static CookieContainer container;
        static string POST(string url = null, string POSTData = null, HttpWebRequest req = null)
        {
            if (req == null)
            {
                req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.70 Safari/537.36";
            }
            req.Method = "POST";
            req.Timeout = 10000;
            //req.CookieContainer = container;
            if (POSTData != null)
            {
                byte[] buf = Encoding.Default.GetBytes(POSTData);
                req.ContentLength = buf.Length;
                req.GetRequestStream().Write(buf, 0, buf.Length);
            }
            try
            {
                return new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return "";
            }
        }
        static string GET(string url = null, string GETData = null, HttpWebRequest req = null)
        {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create($"{url}?{GETData}");
            req.Method = "GET";
            try
            {
                return new StreamReader(req.GetResponse().GetResponseStream()).ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return "";
            }
        }
        static void Save(string filename, string url = null, string GETData = null, HttpWebRequest req = null)
        {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create($"{url}?{GETData}");
            req.Method = "GET";
            try
            {
                using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    req.GetResponse().GetResponseStream().CopyTo(stream);
                    stream.Flush();
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        static byte[] Bytes(string url = null, string GETData = null, HttpWebRequest req = null)
        {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create($"{url}?{GETData}");
            req.Method = "GET";
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    req.GetResponse().GetResponseStream().CopyTo(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        static string POSTEncode(Dictionary<string, string> POSTData)
        {
            List<string> POST = new List<string>();
            foreach (var key in POSTData)
            {
                POST.Add($"{Url.Encode(key.Key)}={Url.Encode(key.Value, true)}");
            }
            return string.Join("&", POST.ToArray());
        }

        static HttpWebRequest New(string URL) => (HttpWebRequest)HttpWebRequest.Create(URL);

        static string Explode(string input, string pattern, int index)
        {
            try
            {
                return Regex.Split(input, pattern)[index];
            }
            catch
            {
                return string.Empty;
            }
        }

        static string Between(string input, string before, string after)
        {
            return Explode(Explode(input, before, 1), after, 0);
        }

        static JObject GetHighest(JObject obj)
        {
            try
            {
                if (obj["origin"] != null)
                {
                    WriteConsole("Highest Quality: (original)");
                    return (JObject)obj["origin"];
                }
                if (obj["fullhd"] != null)
                {
                    WriteConsole("Highest Quality: Full HD");
                    return (JObject)obj["fullhd"];
                }
                if (obj["hd"] != null)
                {
                    WriteConsole("Highest Quality: HD");
                    return (JObject)obj["hd"];
                }
                if (obj["mhd"] != null)
                {
                    WriteConsole("Highest Quality: Mobile HD");
                    return (JObject)obj["mhd"];
                }
                if (obj["sd"] != null)
                {
                    WriteConsole("Highest Quality: SD");
                    return (JObject)obj["sd"];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return null;
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        static byte[] FixupAndGetB64Key(string key)
        {
            return Convert.FromBase64String(key + new string('=', key.Length % 4));
        }

        static byte[] Decrypt(byte[] input, byte[] key, byte[] IV)
        {
            Aes aes = new AesManaged();
            aes.Key = key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var decrypt = aes.CreateDecryptor();
            return decrypt.TransformFinalBlock(input, 0, input.Length);
        }

        public class DownloadOptions
        {
            public string HydraxURL { get; set; }
            public string UserAgent { get; set; }
            public bool skipFirst { get; set; }
            public string QualityDefault { get; set; }
            public string outputFilename { get; set; }
            public bool OnlyShowInfo { get; set; }
            public bool Verbose { get; set; }
        }

        static bool _verbose;
        static void WriteConsole(dynamic inhalt)
        {
            if (_verbose)
                Console.WriteLine(inhalt);
        }

        public static bool Download(DownloadOptions options)
        {
            try
            {
                _verbose = options.Verbose || options.OnlyShowInfo;
                if (!Uri.IsWellFormedUriString(options.HydraxURL, UriKind.Absolute))
                    throw new ArgumentException("Invalid URL");
                if (!options.HydraxURL.Contains("#slug=") && !options.HydraxURL.Contains("?v="))
                    throw new ArgumentException("Slug not found!");
                var breq = New(options.HydraxURL);
                breq.UserAgent = options.UserAgent;
                string site = GET(req: breq);
                Console.WriteLine(site);
                string key = Between(site, "key: \"", "\"");
                //SLUGS CAN NOW BE LONGER THAN 9!
                string slug = Explode(options.HydraxURL, "#slug=|\\?v=", 1)?.Split(new[] { '?' }, 2)[0];
                WriteConsole($"Key: {key}\nSlug: {slug}");

                var vreq = New("https://multi.idocdn.com/vip");
                vreq.UserAgent = options.UserAgent;
                vreq.Headers.Add("Origin", Url.GetRoot(options.HydraxURL));
                vreq.Headers.Add("Sec-Fetch-Mode", "cors");
                vreq.Headers.Add("Sec-Fetch-Site", "cross-site");
                vreq.ContentType = "application/x-www-form-urlencoded";
                var videoInfo = JObject.Parse(POST(POSTData: POSTEncode(new Dictionary<string, string>() {
                    { "key", key },
                    { "type", "slug" },
                    { "value", slug }
                }), req: vreq));
                //Console.WriteLine(videoInfo);
                var streamingServer = videoInfo["servers"]["stream"].ToString();
                WriteConsole("Stream Server: " + streamingServer);
                JObject bestStream;
                if (options.QualityDefault == "highest" || options.OnlyShowInfo)
                    bestStream = GetHighest(videoInfo);
                else if (videoInfo[options.QualityDefault] != null)
                    bestStream = (JObject)videoInfo[options.QualityDefault];
                else
                    throw new ArgumentException("Could not find quality in streams");

                var ids = bestStream["ids"].ToObject<string[]>();
                int idsLen = ids.Length;
                var sig = bestStream["sig"].ToObject<string>();
                var id = bestStream["id"].ToObject<string>();
                var ranges = bestStream["ranges"].ToObject<JArray>();
                //var duration = bestStream["duration"];
                var extinfs = bestStream["extinfs"].ToObject<string[]>();

                var IV = bestStream["iv"].ToObject<string>();
                var IV2 = StringToByteArray(IV.Replace("0x", string.Empty));
                var Key = bestStream["hash"].ToObject<string>();
                var Key2 = FixupAndGetB64Key(Key);
                var pingSite = videoInfo["ping"].ToObject<string>();

                //D:\old\tools\ffmpeg.exe

                WriteConsole($"ID count: {idsLen}\nSIG: {sig}\nID: {id}\nKey: {Key}\nIV: {IV}");

                if (options.OnlyShowInfo)
                    return true;

                FileStream stream = new FileStream(options.outputFilename, FileMode.Create, FileAccess.Write);
                
                /*
            //restraining to ranges.length == ids.length
            //h < ranges.length (15)
            //s == sig
            for (h = 0; h < l; h++) {
                //d = a[ranges.length] (15)
                for (d = a[h], p = 0; p < u[h].length; p++)
                    y = l <= h + p + 1 ? l <= p + 1 ? a[0] : a[p + 1] : a[h + p + 1],
                    "object" == typeof e.redirect ? i < e.redirect.length ? (c = e.redirect[i],
                    i++) : (i = 1,
                    c = e.redirect[0]) : c = e.redirect,
                    n += "#EXTINF:" + t.extinfs[r] + ",\n",
                    1 < u[h].length && (n += "#EXT-X-BYTERANGE:" + u[h][p] + "\n"),
                    //PROTOCOL :// STREAM_SERVER / SIG / ID / shit / dynid
                    n += N.protocol + "//" + c + "/redirect/" + s + "/" + o + "/" + d + "/" + y + "\n",
                    r++;
                l == h + 1 && (n += "#EXT-X-ENDLIST")
            }
                 */

                int counter = 0;
                for (int idCounter = 0; idCounter < idsLen; idCounter++)
                {
                    var currID = ids[idCounter];
                    var currRange = (JArray)ranges[idCounter]; //for byteranges
                    for (int shit = 0; shit < currRange.Count; shit++)
                    {
                        if (shit == 0 && idCounter == 0 && options.skipFirst)
                            continue;
                        // / SIG / ID / shit currid / dynid
                        var dynid = idsLen <= idCounter + shit + 1 ?
                            (idsLen <= shit + 1 ?
                                ids[0] :
                                ids[shit + 1]) :
                            ids[idCounter + shit + 1];
                        var constructed = $"https://{streamingServer}/redirect/{sig}/{id}/{currID}/{dynid}";
                        WriteConsole($"Opening: {constructed}");
                        byte[] bytes;
                        try {
                            //throw new Exception();
                            var m3ureq = New($"{constructed}");
                            m3ureq.UserAgent = options.UserAgent;
                            //var file = $"{extInfCounter}.ts";
                            bytes = Decrypt(Bytes(req: m3ureq), Key2, IV2);

                        } catch {
                            WriteConsole("Failed to open fragment, trying to open alternative link!");
                            try {
                                WriteConsole("Ping...");
                                var ping = New($"{pingSite}/{id}/ping");
                                ping.UserAgent = options.UserAgent;
                                ping.Headers.Add("Origin", Url.GetRoot(options.HydraxURL));
                                GET(req: ping);
                                WriteConsole("PING OK");

                                constructed = $"https://{streamingServer}/html/{sig}/{id}/{currID}/{dynid}.html?domain={breq.RequestUri.Host}";
                                WriteConsole($"Opening: {constructed}");

                                var m3ureq = New($"{constructed}");
                                m3ureq.UserAgent = options.UserAgent;
                                m3ureq.Headers.Add("Origin", Url.GetRoot(options.HydraxURL));
                                var newLink = JObject.Parse(GET(req: m3ureq));
                                if (!newLink["status"].ToObject<bool>())
                                    throw new Exception("Invalid status");
                                constructed = Encoding.Default.GetString(FixupAndGetB64Key(newLink["url"].ToObject<string>()));
                                WriteConsole("---> " + constructed);
                                m3ureq = New($"{constructed}");
                                m3ureq.UserAgent = options.UserAgent;
                                //var file = $"{extInfCounter}.ts";
                                bytes = Decrypt(Bytes(req: m3ureq), Key2, IV2);

                                //Console.ReadLine();
                            } catch {
                                WriteConsole("Failed to open alternative link!");
                                throw;
                            }
                        }
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush();

                        counter++;
                        Console.Title = $"{((float)counter/extinfs.Length)*100}%";
                    }
                }
                stream.Flush();
                stream.Close();

                WriteConsole("Finished");

                return true;
            } catch(Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                WriteConsole(ex.StackTrace);
            }
            return false;
        }
    }
}

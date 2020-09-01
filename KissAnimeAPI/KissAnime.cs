using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Flurl;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using OpenQA.Selenium.Firefox;
using System.Text.RegularExpressions;

namespace KissAnimeAPI {
    static class KissAnime {//POST https://kissanime.ru/Search/Anime
        public class SearchResult {
            public enum ShowState {
                Completed,
                NotYetAired,
                CurrentlyAiring,
                Other
            }
            public ShowState currentState;
            public string name;
            public byte[] Poster;
            public string Link;
        }

        #region defaults

        static KissAnime() {
            if (File.Exists("cookies.txt")) {
                Console.WriteLine("Using existing cookies");
                Cookies = File.ReadAllText("cookies.txt");
            }
            //Cookies = "__cfduid=d976e8eb630436901756cb19072fce4881572656042; idtz=80.110.125.31-967251095; _ga=GA1.2.726163535.1574970561; _gid=GA1.2.876595278.1574970561; ASP.NET_SessionId=lvxqlgzojaeyfginkbr5q4vu; usingHTML5V1=true; __atuvc=9%7C48; cf_clearance=c89be2050fec1ed4bfabe91876c7e74d263f1ac5-1575051516-0-150; _gat_gtag_UA_1712467_41=1; MarketGidStorage=%7B%220%22%3A%7B%22svspr%22%3A%22%22%2C%22svsds%22%3A15%2C%22TejndEEDj%22%3A%22YpyDTcAc%2B%22%7D%2C%22C757993%22%3A%7B%22page%22%3A1%2C%22time%22%3A1575051520144%7D%7D";
        }

        static void HandleAddedCookies(WebHeaderCollection coll) {
            if (coll.AllKeys.Contains("Set-Cookie")) {
                foreach (var cookie in coll.GetValues("Set-Cookie")) {
                    var cook = cookie.Split(';')[0];
                    Console.WriteLine("Handled new cokie: " + cook);
                    if (Cookies == string.Empty)
                        Cookies += cook;
                    else
                        Cookies += "; " + cook;
                }
                File.WriteAllText("cookies.txt", Cookies);
            }
        }
        static string Cookies = string.Empty;
        //static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.70 Safari/537.36";
        static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:71.0) Gecko/20100101 Firefox/71.0";
        public static void BypassCFExperimental() {
            FirefoxOptions options = new FirefoxOptions();
            //options.SetPreference("general.useragent.override", UserAgent);
            options.BrowserExecutableLocation = "Mozilla Firefox\\firefox.exe";
            FirefoxDriver driver = new FirefoxDriver(/*"D:\\Repos\\hydrax_dl\\Build", */options);

            driver.Manage().Window.Size = new System.Drawing.Size(1, 1);
            driver.Manage().Window.Position = new System.Drawing.Point(1, 1);
            driver.Navigate().GoToUrl("https://kissanime.ru/Search/Anime");
            while (new Uri(driver.Url) != new Uri("https://kissanime.ru"))
                Thread.Sleep(1000);

            Console.WriteLine("Finished bypassing Cloudflare");
            //Console.WriteLine("Site: " + driver.Url);
            var agent = driver.ExecuteScript("return navigator.userAgent");
            UserAgent = agent.ToString();

            Cookies = "";
            foreach (var cookie in driver.Manage().Cookies.AllCookies) {
                Cookies += $"{cookie.Name}={cookie.Value}; ";
            }

            //Console.WriteLine("UserAgent: " + agent.ToString());
            //Console.ReadLine();
            driver.Quit();
            //driver.Close();
        }

        //static CookieContainer container;
        static string POST(string url = null, string POSTData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create(url);
            req.Method = "POST";
            req.Proxy = null;
            req.UserAgent = UserAgent;
            //req.AllowAutoRedirect = false;
            req.Headers.Add("Origin", "https://kissanime.ru");
            req.Referer = "https://kissanime.ru/";
            req.Host = "kissanime.ru";
            req.ContentType = "application/x-www-form-urlencoded";
            req.KeepAlive = true;
            req.Expect = string.Empty;
            //req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            req.Headers.Add("Cookie", Cookies);
            //req.Timeout = 10000;
            //req.Headers.Add("Accept-Language", "de,en-US;q=0.9,en;q=0.8");
            //req.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            req.Headers.Add("DNT", "1");
            //req.Accept = "*/*";
            //req.CookieContainer = container;
            if (POSTData != null) {
                byte[] buf = Encoding.Default.GetBytes(POSTData);
                req.ContentLength = buf.Length;
                req.GetRequestStream().Write(buf, 0, buf.Length);
            }
            try {
                var resp = req.GetResponse();
                HandleAddedCookies(resp.Headers);
                var code = ((HttpWebResponse)resp).StatusCode;
                if (code == HttpStatusCode.Moved || code == HttpStatusCode.Redirect) {
                    Console.WriteLine("Site moved to " + resp.ResponseUri.AbsoluteUri);
                    return "moved";
                }
                return new StreamReader(resp.GetResponseStream()).ReadToEnd();
            } catch (WebException ex) {
                var code = ((HttpWebResponse)ex.Response).StatusCode;
                if (code == HttpStatusCode.ServiceUnavailable) {
                    BypassCFExperimental();

                    Console.WriteLine("Main Len: " + GET(req: New("https://kissanime.ru")).Length);

                    return "repeat";
                } else if (code == HttpStatusCode.Moved || code == HttpStatusCode.Redirect) {
                    //Console.WriteLine("Site Moved!");
                    return "moved";
                }
                throw new Exception(string.Empty, ex);
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
            //Console.WriteLine("fail");
            return "fail";
        }
        static string GET(string url = null, string GETData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create($"{url}?{GETData}");
            req.Method = "GET";
            req.Proxy = null;
            req.UserAgent = UserAgent;
            req.AllowAutoRedirect = false;
            req.Headers.Add("Origin", "https://kissanime.ru");
            req.Referer = "https://kissanime.ru/";
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            req.Headers.Add("Cookie", Cookies);
            //req.Timeout = 10000;
            try {
                var resp = req.GetResponse();
                HandleAddedCookies(resp.Headers);
                return new StreamReader(resp.GetResponseStream()).ReadToEnd();
            } catch (WebException ex) {
                var code = ((HttpWebResponse)ex.Response).StatusCode;
                if (code == HttpStatusCode.ServiceUnavailable) {
                    Console.WriteLine("CF Challenge found!");
                    //var resp = ex.Response as HttpWebResponse;
                    //var html = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                    //var uri = req.RequestUri.Host;
                    //HandleAddedCookies(ex.Response.Headers);
                    //var solution = ChallengeSolver.Solve(html, uri);
                    //var new_url = "https://" + Url.Combine(uri, solution.ClearanceQuery);
                    ////Console.WriteLine(new_url);

                    //Console.WriteLine("Waiting for timeout!");
                    //Thread.Sleep(5000);
                    ////Console.WriteLine("Requesting cookies!");

                    //var cf_req = New(new_url);
                    //cf_req.AllowAutoRedirect = false;
                    //cf_req.UserAgent = UserAgent;
                    //var response = GETRaw(req: cf_req);

                    //Cookies = string.Empty;

                    //HandleAddedCookies(response.Headers);

                    //if (!Cookies.Contains("cf_clearance") || !Cookies.Contains("__cfduid"))
                    //{
                    //    Console.WriteLine("Could not get cookies!");
                    //    return "cookie_fail";
                    //}

                    BypassCFExperimental();

                    Console.WriteLine("Main Len: " + GET(req: New("https://kissanime.ru")).Length);

                    return "repeat";

                    //HttpWebRequest new_req = New(req.RequestUri.)

                    //return POST(url, POSTData, );
                } else if (code == HttpStatusCode.Moved || code == HttpStatusCode.Redirect) {
                    //Console.WriteLine("Site Moved!");
                    return "moved";
                }
                throw new Exception(string.Empty, ex);
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return "";
            }
        }
        static HttpWebResponse GETRaw(string url = null, string GETData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create($"{url}?{GETData}");
            req.Method = "GET";
            req.Proxy = null;
            req.UserAgent = UserAgent;
            req.Headers.Add("Cookie", Cookies);
            try {
                return req.GetResponse() as HttpWebResponse;
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return null;
            }
        }
        static void Save(string filename, string url = null, string GETData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create($"{url}?{GETData}");
            req.Method = "GET";
            req.Proxy = null;
            req.UserAgent = UserAgent;
            req.Headers.Add("Cookie", Cookies);
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
        static byte[] Bytes(string url = null, string GETData = null, HttpWebRequest req = null) {
            if (req == null)
                req = (HttpWebRequest)HttpWebRequest.Create($"{url}?{GETData}");
            req.Method = "GET";
            req.Proxy = null;
            req.UserAgent = UserAgent;
            req.Headers.Add("Cookie", Cookies);
            try {
                using (MemoryStream stream = new MemoryStream()) {
                    req.GetResponse().GetResponseStream().CopyTo(stream);
                    return stream.ToArray();
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        static string POSTEncode(Dictionary<string, string> POSTData) {
            List<string> POST = new List<string>();
            foreach (var key in POSTData) {
                POST.Add($"{Url.Encode(key.Key)}={Url.Encode(key.Value, true)}");
            }
            return string.Join("&", POST.ToArray());
        }

        static HttpWebRequest New(string URL) => (HttpWebRequest)HttpWebRequest.Create(URL);

        #endregion

        public static List<SearchResult> Search(string keywords) {
            List<SearchResult> ret = new List<SearchResult>();
            try {
                //GET("https://kissanime.ru");
                HtmlDocument doc = new HtmlDocument();
                //var req = New("https://kissanime.ru/Anime/Saint-Seiya-The-Lost-Canvas-Dub");

                //string res = POST("https://kissanime.ru/AdvanceSearch", $"animeName={Url.Encode(keywords)}&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&genres=0&status=");
                string res = POST("https://kissanime.ru/Search/SearchSuggestx", $"type=Anime&keyword=" + Url.Encode(keywords));

                if (res == "repeat")
                    return Search(keywords);

                //res = string.Join(string.Empty, Regex.Split(res, @"(<span>|<\/span>)"));
                res = res.Replace("<span>", string.Empty).Replace("</span>", string.Empty);

                MatchCollection matches = Regex.Matches(res, "<a href=\"(.*?)\">(.*?)<\\/a>");
                
                foreach (Match match in matches) {
                    ret.Add(new SearchResult() {
                        currentState = SearchResult.ShowState.Other,
                        Link = match.Groups[1].Value,
                        name = match.Groups[2].Value,
                        Poster = null
                    });
                }
                
                //foreach (var html in doc.DocumentNode.SelectNodes("//tr")) {
                //    if (html.GetAttributeValue("class", "") != "head") {
                //        try {
                //            var internalDoc = new HtmlDocument();
                //            internalDoc.LoadHtml(html?.SelectSingleNode("td")?.GetAttributeValue("title", string.Empty) ?? string.Empty);

                //            var animeState = html?.SelectNodes("td")[1].InnerText.ToLower();
                //            var state = SearchResult.ShowState.Other;
                //            if (animeState.Contains("completed"))
                //                state = SearchResult.ShowState.Completed;
                //            else if (animeState.Contains("not yet aired"))
                //                state = SearchResult.ShowState.NotYetAired;
                //            else if (animeState.Contains("episode"))
                //                state = SearchResult.ShowState.CurrentlyAiring;

                //            ret.Add(new SearchResult() {
                //                currentState = state,
                //                Link = Url.Combine("https://kissanime.ru", internalDoc.DocumentNode.SelectSingleNode("//div/a").GetAttributeValue("href", "")),
                //                name = internalDoc.DocumentNode.SelectSingleNode("//div/a").InnerText,
                //                Poster = Bytes(req: New(internalDoc.DocumentNode.SelectSingleNode("//img").GetAttributeValue("src", "")))
                //            });
                //        } catch {
                //            //Console.WriteLine("err");
                //        }
                //    }
                //}
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }

            return ret;
        }

        public class EpisodeInfo {
            public string Link;
            public string EpisodeName;
        }

        static int GetFirstNonSpace(string str) {
            int counter = 0;
            while (true) {
                if (!Char.IsWhiteSpace(str[counter]))
                    return counter;
                counter++;
            }
        }

        public static List<EpisodeInfo> GetEpisodes(string episodeLink) {
            List<EpisodeInfo> ret = new List<EpisodeInfo>();
            try {
                var episodesHtml = new HtmlDocument();
                var sjot = GET(req: New(episodeLink));
                if (sjot == "repeat")
                    return GetEpisodes(episodeLink);
                episodesHtml.LoadHtml(sjot);
                foreach (var html in episodesHtml.DocumentNode.SelectNodes("//table/tr/td")) {
                    var node = html?.SelectSingleNode("a");
                    var link = node?.GetAttributeValue("href", string.Empty) ?? string.Empty;
                    if (link == string.Empty)
                        continue;
                    var title = node.InnerText.Replace("\n", String.Empty).Replace(Environment.NewLine, string.Empty);
                    title = title.Substring(GetFirstNonSpace(title));
                    link = Url.Combine("https://kissanime.ru", link + "&s=hydrax");

                    tryagain:
                    var episodePage = GET(req: New(link));
                    if (episodePage == "repeat")
                        goto tryagain;

                    var hydraxLink = Regex.Split(episodePage, "0px;\" src=\"")[1].Split('"')[0];

                    ret.Add(new EpisodeInfo() {
                        Link = hydraxLink,
                        EpisodeName = title
                    });
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
            return ret;
        }
    }
}

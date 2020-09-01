using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace hydraxv2 {
    public static class Extensions {
        public static async Task CopyToWithProgressAsync(this Stream source, Stream destination, int bufferSize = 100000, Action<long> progress = null) {
            var buffer = new byte[bufferSize];
            var total = 0L;
            int amtRead;
            do {
                amtRead = 0;
                while (amtRead < bufferSize) {
                    var numBytes = await source.ReadAsync(buffer,
                                                          amtRead,
                                                          bufferSize - amtRead);
                    if (numBytes == 0) {
                        break;
                    }
                    amtRead += numBytes;
                }
                total += amtRead;
                await destination.WriteAsync(buffer, 0, amtRead);
                if (progress != null) {
                    progress(total);
                }
            } while (amtRead == bufferSize);
        }
    }

    //public class ProgressBar : IDisposable, IProgress<double> {
    //    private const int blockCount = 30;
    //    private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
    //    private const string animation = @"|/-\";

    //    private readonly Timer timer;

    //    private double currentProgress = 0;
    //    private string currentText = string.Empty;
    //    private bool disposed = false;
    //    private int animationIndex = 0;

    //    public ProgressBar() {
    //        timer = new Timer(TimerHandler);

    //        // A progress bar is only for temporary display in a console window.
    //        // If the console output is redirected to a file, draw nothing.
    //        // Otherwise, we'll end up with a lot of garbage in the target file.
    //        if (!Console.IsOutputRedirected) {
    //            ResetTimer();
    //        }
    //    }

    //    public void Report(double value) {
    //        // Make sure value is in [0..1] range
    //        value = Math.Max(0, Math.Min(1, value));
    //        Interlocked.Exchange(ref currentProgress, value);
    //    }

    //    private void TimerHandler(object state) {
    //        lock (timer) {
    //            if (disposed) return;

    //            int progressBlockCount = (int)(currentProgress * blockCount);
    //            int percent = (int)(currentProgress * 100);
    //            string text = string.Format("[{0}{1}] {2,3}% {3}",
    //                new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
    //                percent,
    //                animation[animationIndex++ % animation.Length]);
    //            UpdateText(text);

    //            ResetTimer();
    //        }
    //    }

    //    private void UpdateText(string text) {
    //        // Get length of common portion
    //        int commonPrefixLength = 0;
    //        int commonLength = Math.Min(currentText.Length, text.Length);
    //        while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength]) {
    //            commonPrefixLength++;
    //        }

    //        // Backtrack to the first differing character
    //        StringBuilder outputBuilder = new StringBuilder();
    //        outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

    //        // Output new suffix
    //        outputBuilder.Append(text.Substring(commonPrefixLength));

    //        // If the new text is shorter than the old one: delete overlapping characters
    //        int overlapCount = currentText.Length - text.Length;
    //        if (overlapCount > 0) {
    //            outputBuilder.Append(' ', overlapCount);
    //            outputBuilder.Append('\b', overlapCount);
    //        }

    //        Console.Write(outputBuilder);
    //        currentText = text;
    //    }

    //    private void ResetTimer() {
    //        timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
    //    }

    //    public void Dispose() {
    //        lock (timer) {
    //            disposed = true;
    //            UpdateText(string.Empty);
    //        }
    //    }

    //}

    public class Hydrax {
        public static bool WaitForFile(string fullPath) {
            int numTries = 0;
            while (true) {
                ++numTries;
                try {
                    // Attempt to open the file exclusively.
                    using (FileStream fs = new FileStream(fullPath,
                        FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, 100)) {
                        fs.ReadByte();

                        // If we got this far the file is ready
                        break;
                    }
                } catch (Exception ex) {
                    if (numTries > 10) {
                        Console.WriteLine("Could not get exclusive lock!");
                        return false;
                    }

                    // Wait for the lock to be released
                    Thread.Sleep(500);
                }
            }

            return true;
        }

        public bool failed = false;
        public int retries = 0;
        public double progressPercentage;
        public string _link;

        public void DownloadFragmented() {
            if (File.Exists(outFileName))
                File.Delete(outFileName);
            
            FileStream outStream = new FileStream(outFileName + ".temp", FileMode.Create, FileAccess.Write);
            try {
                long currPos = 0;
            newPos:
                var req = srvcon.New(baseURL);
                req.AddRange(currPos);
                var res = con.GETRaw(req: req);

                if (res.GetResponseHeader("Content-Length") == "20") {
                    outStream.Dispose();
                    con = new srvcon();
                    Download(_link, outFileName);
                }

                //res.GetResponseStream().CopyTo(outStream);
                if (currPos != 0)
                    Console.WriteLine("---------------------------------------- Trying to regrab at position " + currPos);
                try {
                    res.GetResponseStream().CopyToWithProgressAsync(outStream, progress: (totalProgress) => {
                        double progPerc = (totalProgress + currPos) / double.Parse(Regex.Match(res.GetResponseHeader("Content-Range"), "/([0-9]*)").Groups[1].Value);
                        progressPercentage = progPerc;
                    }).Wait();
                } catch {
                    currPos = outStream.Position;
                    goto newPos;
                }

                //bar.Dispose();

                outStream.Flush();
                outStream.Close();
                outStream.Dispose();
                Thread.Sleep(1000);

                if (!WaitForFile(outFileName + ".temp")) {
                    throw new Exception("File could not be locked");
                }

                File.Move(outFileName + ".temp", outFileName);
            } catch (Exception ex) {
                Console.Error.WriteLine(ex);
                outStream.Dispose();
                if (File.Exists(outFileName))
                    File.Delete(outFileName);

                //bar.Dispose();
                retries++;
                if (retries > 4)
                    throw;
                else {
                    Console.WriteLine("error, retrying");
                    DownloadFragmented();
                }
            }
        }

        public string baseURL = "";
        public string outFileName = "";
        public srvcon con = new srvcon();

        public int Download(string link, string outFile) {
            //args = new[] {
            //    "https://playhydrax.com/?v=WCyhc3W6o",
            //    "test.mp4"
            //};
            try {
                _link = link;
                outFileName = outFile;
                //Console.Title = outFileName;
                //Console.WriteLine("Out File: " + outFileName);
                string slug = Regex.Match(link, "(\\?|&)v=(.*)(&){0,1}").Groups[2].Value;
                Console.WriteLine(slug);
                con.Referer = link;

                JObject obj = JObject.Parse(con.POST("https://ping.idocdn.com/", "slug=" + slug));

                if (!obj["status"].ToObject<bool>()) {
                    //Console.WriteLine("Could not get mirror URL");
                    failed = true;
                    return -1;
                }
                baseURL = "https://" + obj["url"].ToObject<string>() + "/";

                var cookieReq = srvcon.New(baseURL + "ping.gif");
                cookieReq.Accept = "image/webp,image/apng,image/*,*/*;q=0.8";
                con.GET(out bool sucfirst, req: cookieReq);
                if (sucfirst && con.Cookies.ContainsKey("hx_stream")) {
                    //Console.WriteLine("Got hxstream");

                    string a = con.POST(baseURL, "slug=" + slug);
                    if (a == "fail") {
                        //Console.WriteLine("Could not set pos");
                        failed = true;
                        return -1;
                    } else {
                        DownloadFragmented();
                    }
                } else {
                    //Console.WriteLine("Could not get initialization cookie!");
                    failed = true;
                    return -1;
                }
                progressPercentage = 1;
                return 0;
            } catch (Exception ex) {
                Console.WriteLine(ex);
                failed = true;
                return -2;
            }
        }
    }
}

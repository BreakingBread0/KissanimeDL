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
using Fclp;

namespace hydrax_dl
{
    class Program
    {

        const string iPadUserAgent = "Mozilla/5.0 (iPad; CPU OS 12_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.1 Mobile/15E148 Safari/604.1";
        static void Main(string[] args)
        {
            hydrax.API.Download(new hydrax.API.DownloadOptions() {
                //UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36",
                skipFirst = false,
                UserAgent = iPadUserAgent,
                OnlyShowInfo = false,
                HydraxURL = "https://playhydrax.com/?v=WCyhc3W6o ",
                outputFilename = "a.mp4",
                QualityDefault = "highest",
                Verbose = true
            });
            Console.ReadKey();
            return;


            //https://replay.watch/hydrax.html?vc=1#slug=QuLneXo4m
            //string baseURL = "https://replay.watch/hydrax.html?vc=1#slug=QuLneXo4m";
            //string baseURL = "https://replay.watch/hydrax.html?vc=1#slug=bvCL2VuAp";
            //string baseURL = "https://replay.watch/hydrax.html?vc=1#slug=7H5mEYDkI";
            var parser = new FluentCommandLineParser<hydrax.API.DownloadOptions>();

            parser.Setup(a => a.HydraxURL)
                .As('i', "input-file")
                .Required()
                .WithDescription("Sets the URL to download from (MUST include protocol, e.g. http://)");

            parser.Setup(a => a.Verbose)
                .As('v', "verbose")
                .SetDefault(false)
                .WithDescription("Sets if verbose output should be printed in the console. Default: False");

            parser.Setup(a => a.UserAgent)
                .As("user-agent")
                .SetDefault(iPadUserAgent)
                .WithDescription("Sets the user agent to use when calling Hydrax URLs.");

            parser.Setup(a => a.skipFirst)
                .As('s', "skip-first")
                .SetDefault(false)
                .WithDescription("Excludes the first MPEG containter from the download, mostly only used for intros. Default: False");

            parser.Setup(a => a.QualityDefault)
                .As('q', "quality")
                .SetDefault("highest")
                .WithDescription("Sets the prefered quality of the stream. Defaults to the highest avaliable.");

            parser.Setup(a => a.outputFilename)
                .As('o', "output-file")
                .SetDefault("out.mp4")
                .WithDescription("Sets the output filename. Default: out.mp4");

            parser.Setup(a => a.OnlyShowInfo)
                .As("only-info")
                .SetDefault(false)
                .WithDescription("Only shows the info of the file. Does not download any files. Default: False");

            parser.SetupHelp("h", "help", "?")
                .Callback(a => Console.WriteLine(a));

            var result = parser.Parse(args);
            if (result.HasErrors)
            {
                Console.WriteLine(result.ErrorText);
                Environment.Exit(-2);
            }

            if (!hydrax.API.Download(parser.Object))
                Environment.Exit(-1);

            //Console.ReadKey();
        }
    }
}

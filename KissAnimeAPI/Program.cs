using Flurl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
//using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System.Runtime.InteropServices;

namespace KissAnimeAPI
{
    class Program
    {
        //Process.Start("cmd", $"/K hydrax_dl.exe -i \"{episode.Link}\" -v -o \"{RemoveInvalidFilenameCharacters(episode.EpisodeName)}.mp4\"").WaitForExit();
        //Process.Start("hydrax_dl.exe", $"-i \"{episode.Link}\" -v -o \"{RemoveInvalidFilenameCharacters(episode.EpisodeName)}.mp4\"").WaitForExit();
        static void Download(KissAnime.EpisodeInfo episode) {
            Console.WriteLine("Downloading: " + episode.EpisodeName);
            Console.WriteLine(episode.Link);
            if (!File.Exists($"{RemoveInvalidFilenameCharacters(episode.EpisodeName)}.mp4")) {
                Process p = Process.Start("Hydraxv2", $"{episode.Link} \"{RemoveInvalidFilenameCharacters(episode.EpisodeName)}.mp4\"");
                

                Thread.Sleep(5000);
            } else {
                Console.WriteLine("Skipping");
            }
        }

        public static void DownloadAll(KissAnime.EpisodeInfo[] infos) {
            hydraxv2.Hydrax[] downloaders = new hydraxv2.Hydrax[infos.Length];
            Thread[] threads = new Thread[infos.Length];
            for (int i = 0; i < infos.Length; i++) {
                downloaders[i] = new hydraxv2.Hydrax();
                threads[i] = new Thread(() => {
                    string file = $"{RemoveInvalidFilenameCharacters(infos[i].EpisodeName)}.mp4";
                    if (!File.Exists(file))
                        downloaders[i].Download(infos[i].Link, file);
                    else
                        downloaders[i].progressPercentage = 1;
                });
                threads[i].Start();
                Thread.Sleep(100);
            }
            Console.WriteLine("Downloaders started.");
            int addTop = Console.CursorTop;
            int blockCount = 30;
            Console.CursorVisible = false;
            while (true) {
                bool finished = true;
                double addedProg = 0;
                for (int i = 0; i < infos.Length; i++) {
                    if (downloaders[i].progressPercentage != 1 && !downloaders[i].failed)
                        finished = false;
                    Console.SetCursorPosition(0, addTop + i);
                    double currentProgress = downloaders[i].progressPercentage;
                    if (downloaders[i].failed)
                        currentProgress = 1;
                    int progressBlockCount = (int)(currentProgress * blockCount);
                    int percent = (int)(currentProgress * 100);
                    addedProg += currentProgress;
                    string text = string.Format("[{0}{1}] {2,3}%  ", new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount), percent);
                    if (downloaders[i].failed)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (downloaders[i].progressPercentage == 1)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(text);
                    Thread.Sleep(50);
                }
                addedProg /= infos.Length;
                int progressBlockCount1 = (int)(addedProg * blockCount * 2);
                int percent1 = (int)(addedProg * 100);
                string text2 = string.Format("[{0}{1}] {2,3}%", new string('#', progressBlockCount1), new string('-', blockCount * 2 - progressBlockCount1), percent1);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.SetCursorPosition(0, addTop + infos.Length);
                Console.WriteLine(text2);

                Thread.Sleep(100);
                if (finished)
                    break;
            }
            foreach (var thread in threads) {
                thread.Join();
            }
        }

        public static string RemoveInvalidFilenameCharacters(string input) {
            return string.Join("", input.Split(Path.GetInvalidFileNameChars()));
        }

        static void Main(string[] args) {
            Console.Write("Search terms for series: ");
            string term = Console.ReadLine();
            KissAnime.SearchResult selectedAnime;
            if (term.ToLower().Equals("link") || term.ToLower().Equals("custom")) {
                Console.Write("Link: ");
                selectedAnime = new KissAnime.SearchResult();
                selectedAnime.Link = Console.ReadLine();
                selectedAnime.name = "Custom";
                selectedAnime.currentState = KissAnime.SearchResult.ShowState.Other;
            } //else if (Uri.IsWellFormedUriString(term, UriKind.RelativeOrAbsolute)) {
            //    selectedAnime = new KissAnime.SearchResult();
            //    selectedAnime.Link = term;
            //    selectedAnime.name = "Custom";
            //    selectedAnime.currentState = KissAnime.SearchResult.ShowState.Other;
            /*}*/ else {
                var animes = KissAnime.Search(term);

                for (int i = 0; i < animes.Count; i++) {
                    var anime = animes[i];
                    Console.WriteLine($"({i}) {anime.name}: {anime.currentState}\n-->{anime.Link}");
                }
                Console.Write("Choose anime: ");
                selectedAnime = animes[int.Parse(Console.ReadLine())];
            }
            Console.WriteLine($"\n{selectedAnime.name}:");

            var episodes = KissAnime.GetEpisodes(selectedAnime.Link);
            episodes.Reverse();

            for (int i = 0; i < episodes.Count; i++) {
                var episode = episodes[i];
                Console.WriteLine($"({i}) {episode.EpisodeName}: {episode.Link}");
            }
            Console.WriteLine($"Enter {episodes.Count} to download everything");
            Console.Write("Choose Episode(s): ");
            int selection = int.Parse(Console.ReadLine());
            if (selection == episodes.Count) {
                Console.WriteLine("Downloading all episodes");
                //foreach (var e in episodes) {
                //    Download(e);
                //}
                DownloadAll(episodes.ToArray());
            } else if (selection >= 0 && selection < episodes.Count) {
                Download(episodes[selection]);
            } else {
                Console.WriteLine("Invalid selection");
            }

            Console.WriteLine("Finished");
            Console.ReadLine();
        }
    }
}

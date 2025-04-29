using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Data;

namespace yt_dlp
{
    public enum VideoQuality : uint
    {
        p144 = 144,
        p240 = 240,
        p360 = 360,
        p480 = 480,
        p720 = 720,
        p1080 = 1080,
        p2k = 1440,
        p4k = 2160,
    }
    internal class DownloadControl
    {
        List<Dictionary<string, VideoQuality>> downloadList = new();
        Func<string[], bool, Process> execute_ytdlp;
        Process P;
        Thread downloadLoop;
        internal VideoQuality _tempQuality = VideoQuality.p1080;
        public EventHandler<string> OnDownloading;

        public string downloadDir { set; get; }
        public string ytdlp_Path { set; get; }
        public string ffmpeg_Path { set; get; }
        bool isDownloading = false;

        public DownloadControl(Func<string[], bool, Process> ytdlp)
        {
            execute_ytdlp = ytdlp;
            downloadLoop = new Thread(_Loop_Download);
        }
        public void StartLoop()
        {
            downloadLoop.Start();
        }
        void _Loop_Download()
        {
            while (true)
            {
                if (isDownloading)
                {
                    Thread.Sleep(1000);
                    if (P.HasExited)
                        isDownloading = false;
                }
                else if (!isDownloading && downloadList.Count > 0)
                {
                    isDownloading = true;
                    Dictionary<string, VideoQuality> downloadItem = downloadList[0];
                    if (downloadItem == null)
                        continue;
                    var URL = new List<string>(downloadItem.Keys)[0];
                    var Quality = downloadItem[URL];
                    P = execute_ytdlp(new string[] { ytdlp_Path, URL, "--ffmpeg-location", FindffmpegPath(), "-S", "\"height:" + (uint)Quality + ",ext:mp4:m4a\"", "-f", "\"bv*+ba/b\"", "-P", downloadDir }, true);
                    downloadList.RemoveAt(0);
                    OnDownloading.Invoke(this, URL);
                }
            }
        }
        public void SetQuality(VideoQuality quality)
        {
            _tempQuality = quality;
        }
        public void AddDownloadList(string URL)
        {
            AddDownloadList(URL, _tempQuality);
        }
        public void AddDownloadList(string URL, VideoQuality videoQuality)
        {
            downloadList.Add(new Dictionary<string, VideoQuality>() { { URL, videoQuality } });
        }
        public string FindffmpegPath()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            var directories = path.Split(';');
            if (ffmpeg_Path.Contains(":"))
                return ffmpeg_Path;

            foreach (var dir in directories)
            {
                var fullpath = Path.Combine(dir, ffmpeg_Path);
                if (File.Exists(fullpath)) return fullpath;
            }

            // filename does not exist in path
            return null;
        }
    }
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            VideoQuality enumValue = (VideoQuality)value;
            string result = enumValue.ToString().Remove(0, 1);
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

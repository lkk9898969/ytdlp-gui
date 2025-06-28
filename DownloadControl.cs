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
        List<List<object>> downloadList = new();
        Func<string[], bool, Process> execute_ytdlp;
        Process P;
        Thread downloadLoop;
        App.BindDataObject data => App.mainWindow.data;

        internal VideoQuality _tempQuality = VideoQuality.p1080;
        public EventHandler<string> OnDownloading;

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
                    var downloadItem = downloadList[0];
                    if (downloadItem == null)
                        continue;
                    var URL = downloadItem[0].ToString();
                    var Quality = downloadItem[1];
                    var StartTime = (int)downloadItem[2];
                    var EndTime = (int)downloadItem[3];
                    List<string> para;
                    if (Quality is VideoQuality)
                        para = new List<string> { data.ytdlp_Path, URL, "--ffmpeg-location", "\"" + FindffmpegPath() + "\"", "-S", "\"height:" + (uint)Quality + ",ext:mp4:m4a\"", "-f", "\"bv*+ba/b\"", "-P", "\"" + data.downloadDir + "\"" };
                    else
                        para = new List<string> { data.ytdlp_Path, URL, "--ffmpeg-location", "\"" + FindffmpegPath() + "\"", "-S", "\"ext:mp4:m4a\"", "-f", Quality.ToString(), "-P", "\"" + data.downloadDir + "\"" };
                    if (StartTime > 0 || EndTime > 0)
                    {
                        para.Add("-S");
                        para.Add("proto:https");
                        para.Add("--download-sections");
                        string s = "\"*";
                        if (StartTime > 0) s += StartTime.ToString();
                        s += "-";
                        if (EndTime > 0) s += EndTime.ToString();
                        s += "\"";
                        para.Add(s);
                    }
                    P = execute_ytdlp(para.ToArray(), true);
                    downloadList.RemoveAt(0);
                    OnDownloading.Invoke(this, URL);
                }
            }
        }
        public void SetQuality(VideoQuality quality)
        {
            _tempQuality = quality;
        }
        public void AddDownloadList(string URL, int startTime, int endTime)
        {
            AddDownloadList(URL, _tempQuality, startTime, endTime);
        }
        public void AddDownloadList(string URL, VideoQuality videoQuality, int startTime, int endTime)
        {
            downloadList.Add(new List<object> { URL, videoQuality, startTime, endTime });
        }
        public void AddDownloadList(string URL, string format_id, int startTime, int endTime)
        {
            downloadList.Add(new List<object> { URL, format_id, startTime, endTime });
        }
        public string FindffmpegPath()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            var directories = path.Split(';');
            if (data.ffmpeg_Path.Contains(":"))
                return data.ffmpeg_Path;

            foreach (var dir in directories)
            {
                var fullpath = Path.Combine(dir, data.ffmpeg_Path);
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

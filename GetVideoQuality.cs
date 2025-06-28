using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
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
    internal class VideoInfo
    {
        public readonly string format_id;
        public readonly string vcodec;
        public readonly string resolution;
        public readonly uint fps;
        public VideoInfo(string format_id, string vcodec, string resolution, uint fps)
        {
            this.format_id = format_id;
            this.vcodec = vcodec;
            this.resolution = resolution;
            this.fps = fps;
        }
    }
    internal class VideoInfoItem
    {
        public string quality { get; set; }
        public string format_id { get; set; }
    }
    internal class GetVideoQuality
    {
        App.BindDataObject data => App.mainWindow.data;


        public List<VideoInfo> Getformat_id(string URL)
        {
            List<string> para = new List<string> { data.ytdlp_Path, "-j", "\"" + URL + "\"" };
            Process p = new()
            {
                StartInfo = new()
                {
                    FileName = "cmd.exe",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = "/C " + ParseArgs(para)
                }
            };
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return Getformat_idFromJson(output);
        }

        List<VideoInfo> Getformat_idFromJson(string JsonString)
        {
            JsonNode json;
            List<VideoInfo> Results = new();
            try
            {
                json = JsonNode.Parse(JsonString);
                var formats = json["formats"];
                uint max_abr = 0;
                string audio_format_id = string.Empty;
                foreach (var format in formats.AsArray())
                {
                    var acodec = format["acodec"];
                    var jsonabr = format["abr"];
                    if (acodec != null && jsonabr != null)
                    {
                        var abr = (uint)float.Parse(jsonabr.ToString());
                        if (max_abr < abr)
                        {
                            max_abr = abr;
                            audio_format_id = "+" + (string)format["format_id"];
                        }
                    }
                    var vcodec = format["vcodec"];

                    if (!((string)vcodec).Contains("vp9"))
                        continue;
                    VideoInfo videoInfo = new((string)format["format_id"] + audio_format_id, (string)vcodec, (string)format["format_note"], (uint)format["fps"]);
                    Console.WriteLine("format_id: {0}, vcodec: {1}, quality: {2}, fps: {3}", videoInfo.format_id, videoInfo.vcodec, videoInfo.resolution, videoInfo.fps);
                    Results.Add(videoInfo);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new();
            }
            return Results;
        }

        string ParseArgs(List<string> args)
        {
            var Arguments = "";
            foreach (string arg in args)
            {
                Arguments += arg + " ";
            }
            return Arguments;
        }
    }
    public class QualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = string.Empty;
            if (typeof(VideoQuality).IsInstanceOfType(value))
            {
                VideoQuality enumValue = (VideoQuality)value;
                result = enumValue.ToString().Remove(0, 1);
            }
            else if (typeof(VideoInfoItem).IsInstanceOfType(value))
                result = ((VideoInfoItem)value).quality.Replace("1440p", "2k").Replace("2160p", "4k");
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

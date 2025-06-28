using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Nodes;

namespace yt_dlp
{
    internal class VideoInfo
    {
        public readonly uint format_id;
        public readonly string vcodec;
        public readonly uint height;
        public readonly uint fps;
        public VideoInfo(uint format_id, string vcodec, uint height, uint fps)
        {
            this.format_id = format_id;
            this.vcodec = vcodec;
            this.height = height;
            this.fps = fps;
        }
    }
    internal class GetVideoQuality
    {
        App.BindDataObject data => App.mainWindow.data;


        public List<VideoInfo> Getformat_id(string URL)
        {
            List<string> para = new List<string> { data.ytdlp_Path, "-j", URL };
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
                foreach (var format in formats.AsArray())
                {
                    var vcodec = format["vcodec"];

                    if (!((string)vcodec).Contains("vp9"))
                        continue;
                    VideoInfo videoInfo = new(uint.Parse((string)format["format_id"]), (string)vcodec, (uint)format["height"], (uint)format["fps"]);
                    Console.WriteLine("format_id: {0}, vcodec: {1}, height: {2}, fps: {3}", videoInfo.format_id, videoInfo.vcodec, videoInfo.height, videoInfo.fps);
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
}

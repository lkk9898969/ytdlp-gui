using Microsoft.Win32;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace yt_dlp
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        ProcessStartInfo startInfo;
        DownloadControl downloadControl;
        RegistryKey config;

        string ytdlp_Path
        {
            get => textbox_ytdlpPath.Text;
            set
            {
                textbox_ytdlpPath.Text = value;
                downloadControl.ytdlp_Path = value;
            }
        }
        string ffmpeg_Path
        {
            get => textbox_ffmpegPath.Text;
            set
            {
                textbox_ffmpegPath.Text = value;
                downloadControl.ffmpeg_Path = value;
            }
        }
        string URL
        {
            get => textbox_url.Text;
            set { textbox_url.Text = value; }
        }
        string Dir
        {
            get => textbox_dir.Text;
            set
            {
                textbox_dir.Text = value;
                downloadControl.downloadDir = value;
            }
        }

        public MainWindow()
        {

            downloadControl = new(ExecuteCommandPrompt);
            InitializeComponent();
            config = Registry.CurrentUser.CreateSubKey("Software\\yt-dlp-GUI", RegistryKeyPermissionCheck.ReadWriteSubTree);
            //config = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            //config = config.CreateSubKey("Software\\yt-dlp-GUI", RegistryKeyPermissionCheck.ReadWriteSubTree);
            ytdlp_Path = (string)config.GetValue("ytdlp_Path", "yt-dlp.exe");
            ffmpeg_Path = (string)config.GetValue("ffmpeg_Path", "ffmpeg.exe");
            Dir = (string)config.GetValue("download_Dir", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads");
            uint quality = Convert.ToUInt32(config.GetValue("quality", 1080u).ToString());
            downloadControl.SetQuality((VideoQuality)quality);
            downloadControl.StartLoop();
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            startInfo = new()
            {
                FileName = "cmd.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            select_Quality.ItemsSource = (uint[])Enum.GetValues(typeof(VideoQuality));
            select_Quality.SelectedValue = downloadControl._tempQuality;
            downloadControl.OnDownloading += OnDownloadStarted;
        }

        private Process ExecuteCommandPrompt(string[] args, bool clear = true)
        {
            Process p;
            if (clear)
                Dispatcher.Invoke(() => commandPrompt.Clear());
            string Arguments = "";
            foreach (string arg in args)
            {
                Arguments += arg + " ";
            }
            Dispatcher.Invoke(() => commandPrompt.AppendText(Arguments + Environment.NewLine + Environment.NewLine));
            p = new() { StartInfo = startInfo };
            p.StartInfo.Arguments = "/C " + Arguments;
            p.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Dispatcher.Invoke(() => commandPrompt.AppendText(e.Data + Environment.NewLine));
                }
            };
            p.Start();
            p.BeginErrorReadLine();
            Task.Run(() => OutputRedirected(p));
            return p;
        }

        private void OutputRedirected(Process P)
        {
            const int buffersize = 1023;
            int count, read;
            byte temp = 0;
            string text;
            byte[] bytes;
            while (P.HasExited == false)
            {
                bytes = new byte[buffersize + 1];
                bytes[0] = temp;
                temp = 0;
                count = 1;
                while ((read = P.StandardOutput.BaseStream.Read(bytes, count, 1)) != 0 && count < buffersize)
                {
                    count += read;
                    if (bytes[count - 1] == '\n') // newline
                        break;
                    else if (bytes[count - 2] == '\r') // carriage return
                    {
                        temp = bytes[count - 1];
                        bytes[count - 1] = 0;
                        Dispatcher.Invoke(() => commandPrompt.Undo());
                        break;
                    }
                }
                text = System.Text.Encoding.Default.GetString(bytes);
                //Console.WriteLine(bytes.Length);
                //Console.WriteLine(ToLiteral(text));
                Dispatcher.Invoke(() => delegate_UpdateText(text));
            }
        }
        private static string ToLiteral(string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }
        private void delegate_UpdateText(string text)
        {
            commandPrompt.AppendText(text);
            commandPrompt.ScrollToEnd();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (Uri.TryCreate(URL, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                Dispatcher.Invoke(() => Downdload_List.Items.Add(URL));
                downloadControl.AddDownloadList(URL);
            }
            else
                Dispatcher.Invoke(() => MessageBox.Show(this, "Invalid URL", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            URL = "";
        }
        void OnDownloadStarted(object sender, string e)
        {
            Dispatcher.Invoke(() => Downdload_List.Items.Remove(e));
        }

        private void Check_ytdlp_Click(object sender, RoutedEventArgs e)
        {
            if (!ytdlp_Path.Contains(":"))
                ExecuteCommandPrompt(new string[] { "where", ytdlp_Path });
            ExecuteCommandPrompt(new string[] { ytdlp_Path, "--version" }, false);
        }
        private void Check_ffmpeg_Click(object sender, RoutedEventArgs e)
        {
            if (!ytdlp_Path.Contains(":"))
                ExecuteCommandPrompt(new string[] { "where", ffmpeg_Path });
            ExecuteCommandPrompt(new string[] { ffmpeg_Path, "-version" }, false);
        }
        private void Select_ytdlp_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new OpenFileDialog
            {
                FileName = "yt-dlp", // Default file name
                DefaultExt = ".exe", // Default file extension
                Filter = "Executable files |*.exe|All files |*.*" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ytdlp_Path = filename;
            }
        }
        private void button_ffmpeg_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new OpenFileDialog
            {
                FileName = "ffmpeg", // Default file name
                DefaultExt = ".exe", // Default file extension
                Filter = "Executable files |*.exe|All files |*.*" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                ytdlp_Path = filename;
            }
        }
        private void Download_Directory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = Dir
            };
            var result = dialog.ShowDialog();
            if (result == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                Dir = dialog.FileName;
            }
        }

        private void Open_Directory_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", Dir);
        }
        private void select_Quality_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            downloadControl.SetQuality((VideoQuality)select_Quality.SelectedValue);
        }


        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            config.SetValue("ytdlp_Path", ytdlp_Path);
            config.SetValue("ffmpeg_Path", ffmpeg_Path);
            config.SetValue("download_Dir", Dir);
            config.SetValue("quality", (uint)downloadControl._tempQuality, RegistryValueKind.DWord);
            config.Close();
            Environment.Exit(0);
        }


    }
}

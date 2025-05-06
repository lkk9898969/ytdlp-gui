using Microsoft.Win32;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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
        public App.BindDataObject data = new();

        int startTime
        {
            get
            {
                Int32.TryParse(from_hours.Text, out int hour);
                Int32.TryParse(from_mins.Text, out int minute);
                Int32.TryParse(from_secs.Text, out int second);
                return hour * 60 * 60 + minute * 60 + second;
            }
        }
        int endTime
        {
            get
            {
                Int32.TryParse(to_hours.Text, out int hour);
                Int32.TryParse(to_mins.Text, out int minute);
                Int32.TryParse(to_secs.Text, out int second);
                return hour * 60 * 60 + minute * 60 + second;
            }
        }

        public MainWindow()
        {

            downloadControl = new(ExecuteCommandPrompt);
            this.DataContext = data;
            InitializeComponent();
            config = Registry.CurrentUser.CreateSubKey("Software\\yt-dlp-GUI", RegistryKeyPermissionCheck.ReadWriteSubTree);
            data.ytdlp_Path = (string)config.GetValue("ytdlp_Path", "yt-dlp.exe");
            data.ffmpeg_Path = (string)config.GetValue("ffmpeg_Path", "ffmpeg.exe");
            data.downloadDir = (string)config.GetValue("download_Dir", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads");
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
            if (Uri.TryCreate(data.URL, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                Dispatcher.Invoke(() => Downdload_List.Items.Add(data.URL));
                downloadControl.AddDownloadList(data.URL, startTime, endTime);
            }
            else
                Dispatcher.Invoke(() => MessageBox.Show(this, "Invalid URL", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            if (Autoreset.IsChecked ?? false)
            {
                Dispatcher.Invoke(() => { data.URL = ""; });
                ResetTimeRange();
            }
        }
        void OnDownloadStarted(object sender, string e)
        {
            Dispatcher.Invoke(() => Downdload_List.Items.Remove(e));
        }

        private void Check_ytdlp_Click(object sender, RoutedEventArgs e)
        {
            if (!data.ytdlp_Path.Contains(":"))
                ExecuteCommandPrompt(new string[] { "where", data.ytdlp_Path });
            ExecuteCommandPrompt(new string[] { data.ytdlp_Path, "--version" }, false);
        }
        private void Check_ffmpeg_Click(object sender, RoutedEventArgs e)
        {
            if (!data.ytdlp_Path.Contains(":"))
                ExecuteCommandPrompt(new string[] { "where", data.ffmpeg_Path });
            ExecuteCommandPrompt(new string[] { data.ffmpeg_Path, "-version" }, false);
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
                data.ytdlp_Path = filename;
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
                data.ytdlp_Path = filename;
            }
        }
        private void Download_Directory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                InitialDirectory = data.downloadDir
            };
            var result = dialog.ShowDialog();
            if (result == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                data.downloadDir = dialog.FileName;
            }
        }
        private void Open_Directory_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer", data.downloadDir);
        }
        private void select_Quality_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            downloadControl.SetQuality((VideoQuality)select_Quality.SelectedValue);
        }

        private void timeRange_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            char c = KeyboardProcessing.GetCharFromKey(e.Key);
            e.Handled = true;
            if (c >= '0' && c <= '9')
            {
                if (((TextBox)sender).Text.Length >= 2 && ((TextBox)sender).SelectionLength == 0)
                {
                    ((TextBox)sender).Text = "99";
                    return;
                }
                e.Handled = false;
            }
        }
        private void hours_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            char c = KeyboardProcessing.GetCharFromKey(e.Key);
            e.Handled = true;
            if (c >= '0' && c <= '9')
            {
                if (((TextBox)sender).Text.Length >= 3 && ((TextBox)sender).SelectionLength == 0)
                {
                    ((TextBox)sender).Text = "999";
                    return;
                }
                e.Handled = false;
            }
        }
        private void from_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            CauculateTimeChange(from_hours, from_mins, from_secs);
            CheckValidTimeRange();
        }
        private void to_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            CauculateTimeChange(to_hours, to_mins, to_secs);
            CheckValidTimeRange();
        }
        void CauculateTimeChange(TextBox hours, TextBox mins, TextBox secs)
        {
            int hour = 0, min, case_ = -1;
            // Sec
            Int32.TryParse(secs.Text, out int sec);
            if (sec >= 60)
            {
                Int32.TryParse(mins.Text, out min);
                min++;
                sec -= 60;
            }
            // Min
            Int32.TryParse(mins.Text, out min);
            if (min >= 60)
            {
                Int32.TryParse(hours.Text, out hour);
                hour++;
                min -= 60;
            }
            case_ = sec == 0 ? 2 : case_;
            case_ = min == 0 ? 1 : case_;
            case_ = hour == 0 ? 0 : case_;

            switch (case_)
            {
                case 0:
                case 1:
                    mins.Text = string.Format("{0:00}", min);
                    goto case 2;
                case 2:
                    secs.Text = string.Format("{0:00}", sec);
                    break;
            }
        }
        void CheckValidTimeRange()
        {
            if (startTime == 0 || endTime == 0)
                return;
            if (startTime > endTime)
            {
                string temp;
                temp = from_hours.Text;
                from_hours.Text = to_hours.Text;
                to_hours.Text = temp;
                temp = from_mins.Text;
                from_mins.Text = to_mins.Text;
                to_mins.Text = temp;
                temp = from_secs.Text;
                from_secs.Text = to_secs.Text;
                to_secs.Text = temp;
            }
        }
        private void resetTimeRange_Click(object sender, RoutedEventArgs e) { ResetTimeRange(); }
        void ResetTimeRange()
        {
            from_hours.Text = string.Empty;
            from_mins.Text = string.Empty;
            from_secs.Text = string.Empty;
            to_hours.Text = string.Empty;
            to_mins.Text = string.Empty;
            to_secs.Text = string.Empty;
        }
        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            config.SetValue("ytdlp_Path", data.ytdlp_Path);
            config.SetValue("ffmpeg_Path", data.ffmpeg_Path);
            config.SetValue("download_Dir", data.downloadDir);
            config.SetValue("quality", (uint)downloadControl._tempQuality, RegistryValueKind.DWord);
            config.Close();
            Environment.Exit(0);
        }

    }
}

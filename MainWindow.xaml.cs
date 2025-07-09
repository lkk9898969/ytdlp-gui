using Microsoft.Win32;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
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
        DownloadControl downloadControl;
        GetVideoQuality getvideoquality;

        ProcessStartInfo startInfo;
        RegistryKey config;
        public App.BindDataObject data = new();

        VideoQuality GeneralVideoQualitytemp;

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


        #region window init / deinit fuction
        public MainWindow()
        {
            downloadControl = new(ExecuteCommandPrompt);
            getvideoquality = new();

            this.DataContext = data;
            InitializeComponent();
            config = Registry.CurrentUser.CreateSubKey("Software\\yt-dlp-GUI", RegistryKeyPermissionCheck.ReadWriteSubTree);
            data.ytdlp_Path = (string)config.GetValue("ytdlp_Path", "yt-dlp.exe");
            data.ffmpeg_Path = (string)config.GetValue("ffmpeg_Path", "ffmpeg.exe");
            data.downloadDir = (string)config.GetValue("download_Dir", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads");
            GeneralVideoQualitytemp = VideoQuality.p1080;
            downloadControl.StartLoop();
        }
        private void window_Initialized(object sender, EventArgs e)
        {
            var uri = new Uri("PresentationFramework.Aero;V3.0.0.0;31bf3856ad364e35;component/themes/aero.normalcolor.xaml", UriKind.Relative);

            Resources.MergedDictionaries.Add(Application.LoadComponent(uri) as ResourceDictionary);
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
            Init_select_Quality();
            downloadControl.OnDownloading += (object sender, string e) => { Dispatcher.Invoke(() => Downdload_List.Items.Remove(e)); };
        }
        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            config.SetValue("ytdlp_Path", data.ytdlp_Path);
            config.SetValue("ffmpeg_Path", data.ffmpeg_Path);
            config.SetValue("download_Dir", data.downloadDir);
            config.Close();
            Environment.Exit(0);
        }
        #endregion


        #region redirect console output function
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
        #endregion


        #region control event callback
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (CheckValidURL())
            {
                if (select_Quality.SelectedValue == null)
                {
                    Dispatcher.Invoke(() => MessageBox.Show(this, "Invalid Video Quality", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                    return;
                }
                else if (typeof(VideoQuality).IsInstanceOfType(select_Quality.SelectedValue))
                    downloadControl.AddDownloadList(data.URL, (VideoQuality)select_Quality.SelectedValue, startTime, endTime);
                else
                    downloadControl.AddDownloadList(data.URL, select_Quality.SelectedValue.ToString(), startTime, endTime);
                Dispatcher.Invoke(() => Downdload_List.Items.Add(data.URL));
            }
            if (Autoreset.IsChecked ?? false)
            {
                Dispatcher.Invoke(() => { data.URL = ""; });
                ResetTimeRange();
            }
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
            Process.Start(new ProcessStartInfo()
            {
                FileName = "cmd",
                Arguments = "/c start \"\" \"" + data.downloadDir + "\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        private void select_Quality_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

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
        private void resetTimeRange_Click(object sender, RoutedEventArgs e) { ResetTimeRange(); }
        private async void GetQualityBtn_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                GetQualityBtn.IsEnabled = false;
                GetQualityBtn.Content = "Please wait...";
            });
            if (CheckValidURL())
            {
                Dispatcher.Invoke(clear_select_Quality);
                var result = await Task.Run(() => getvideoquality.Getformat_id(data.URL));
                if (result.Count != 0)
                {
                    ObservableCollection<VideoInfoItem> ItemsSource = new();
                    foreach (var item in result)
                    {
                        ItemsSource.Add(new VideoInfoItem() { format_id = item.format_id, quality = item.resolution });
                    }
                    select_Quality.ItemsSource = ItemsSource;
                    select_Quality.SelectedValuePath = "format_id";
                }
            }
            Dispatcher.Invoke(() =>
            {
                GetQualityBtn.IsEnabled = true;
                GetQualityBtn.Content = "Get Video Quality";
            });
        }
        private void textbox_url_TextChanged(object sender, TextChangedEventArgs e)
        {
            Dispatcher.Invoke(() => Init_select_Quality());
        }
        #endregion

        #region helper functions

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
        void ResetTimeRange()
        {
            from_hours.Text = string.Empty;
            from_mins.Text = string.Empty;
            from_secs.Text = string.Empty;
            to_hours.Text = string.Empty;
            to_mins.Text = string.Empty;
            to_secs.Text = string.Empty;
        }

        bool CheckValidURL()
        {
            if (Uri.TryCreate(data.URL, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                return true;
            else
                Dispatcher.Invoke(() => MessageBox.Show(this, "Invalid URL", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            return false;
        }
        #endregion

        #region Invoker fuction
        void Init_select_Quality()
        {
            select_Quality.SelectedValuePath = "";
            select_Quality.ItemsSource = (uint[])Enum.GetValues(typeof(VideoQuality));
            select_Quality.SelectedValue = GeneralVideoQualitytemp;
        }
        void clear_select_Quality()
        {
            if (typeof(VideoQuality).IsInstanceOfType(select_Quality.SelectedValue))
                GeneralVideoQualitytemp = (VideoQuality)select_Quality.SelectedValue;
            select_Quality.SelectedIndex = -1;
            select_Quality.ItemsSource = null;
        }
        private void delegate_UpdateText(string text)
        {
            commandPrompt.AppendText(text);
            commandPrompt.ScrollToEnd();
        }

        #endregion


    }
}

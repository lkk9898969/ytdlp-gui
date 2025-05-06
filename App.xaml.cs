using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace yt_dlp
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public static App This => (App)Application.Current;
        public static MainWindow mainWindow { get { return App.This.Dispatcher.Invoke<MainWindow>(() => (MainWindow)Application.Current.MainWindow); } }

        public class BindDataObject : INotifyPropertyChanged
        {
            private string _ytdlp_Path;
            private string _ffmpeg_Path;
            private string _URL;
            private string _downloadDir;
            public string ytdlp_Path
            {
                get { return _ytdlp_Path; }
                set { _ytdlp_Path = value; OnPropertyChanged(); }
            }
            public string ffmpeg_Path
            {
                get { return _ffmpeg_Path; }
                set { _ffmpeg_Path = value; OnPropertyChanged(); }
            }
            public string URL
            {
                get { return _URL; }
                set { _URL = value; OnPropertyChanged(); }
            }
            public string downloadDir
            {
                get { return _downloadDir; }
                set { _downloadDir = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public static class KeyboardProcessing
    {
        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                case 0:
                    break;
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }
    }

}

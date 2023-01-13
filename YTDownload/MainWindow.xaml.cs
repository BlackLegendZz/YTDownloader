using System;
using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace YTDownload
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GridMainContent.Visibility = Visibility.Collapsed;
            GridYTUrl.Visibility = Visibility.Collapsed;
            StackPanelFfmpegInfo.Visibility = Visibility.Visible;

            await DownloadFfmpeg();

            StackPanelFfmpegInfo.Visibility = Visibility.Collapsed;
            GridYTUrl.Visibility = Visibility.Visible;
            GridMainContent.Visibility = Visibility.Visible;
        }

        public async Task DownloadFfmpeg()
        {
            
            string[] ffmpegUrls = { 
                "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-win-64.zip",
                "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffprobe-4.4.1-win-64.zip"
            };
            string[] ffmpegZips = { 
                Path.Combine(App.localAppDataPath, "ffmpeg.zip"),
                Path.Combine(App.localAppDataPath, "ffprobe.zip")
            };
            string[] ffmpegExes = {
                Path.Combine(App.ffmpegPath, "ffmpeg.exe"),
                Path.Combine(App.ffmpegPath, "ffprobe.exe")
            };

            if (!Directory.Exists(App.localAppDataPath))
            {
                Directory.CreateDirectory(App.localAppDataPath);
            }

            if (!Directory.Exists(App.tempPath))
            {
                Directory.CreateDirectory(App.tempPath);
            }

            HttpClient client = new HttpClient();
            for (int i = 0; i < ffmpegUrls.Length; i++)
            {
                if (!File.Exists(Path.Combine(App.ffmpegPath, ffmpegExes[i])))
                {
                    await Utils.DownloadFileAsync(ffmpegUrls[i], ffmpegZips[i]);
                    ZipFile.ExtractToDirectory(ffmpegZips[i], Path.Combine(App.ffmpegPath));
                    File.Delete(ffmpegZips[i]);
                }
            }
        }

        private void ToggleMaximize()
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    WindowState = WindowState.Maximized;
                    break;
                case WindowState.Maximized:
                    WindowState = WindowState.Normal;
                    break;
                default:
                    break;
            }
        }

        /* This black magic which I don't understand handles all
         * the snapping and maximising and restoring and idk what
         * else of the window when dragging it acros the screen.
         */
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);
        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            /*if (e.ClickCount >= 2)
            {
                ToggleMaximize();
            }
            else
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                SendMessage(helper.Handle, 161, 2, 0);
            }*/
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SendMessage(helper.Handle, 161, 2, 0);
        }

        /* Update value of max size every time the mouse enters the window.
         * This should handle PCs with multiple monitors that have different
         * resolutions.
         
        private void StackPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 5;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth - 5;
        }
        */

        private void WindowClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void WindowMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void WindowMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }
    }
}

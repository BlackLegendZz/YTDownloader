﻿using System;
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
            string localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Simple YTDownloader");
            string ffmpegUrl = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-win-64.zip";
            string ffmpegZip = Path.Combine(localAppDataPath, "ffmpeg.zip");
            string ffmpegExe = Path.Combine(localAppDataPath, "ffmpeg", "ffmpeg.exe");

            if (!Directory.Exists(localAppDataPath))
            {
                Directory.CreateDirectory(localAppDataPath);
            }

            if (!File.Exists(ffmpegExe))
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(ffmpegUrl);
                response.EnsureSuccessStatusCode();

                using (var fs = new FileStream(ffmpegZip, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                ZipFile.ExtractToDirectory(ffmpegZip, Path.Combine(localAppDataPath, "ffmpeg"));
                File.Delete(ffmpegZip);
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

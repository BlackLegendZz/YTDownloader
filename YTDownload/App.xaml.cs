using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace YTDownload
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly string localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Simple_YTDownloader");
        public static readonly string ffmpegPath = Path.Combine(localAppDataPath, "ffmpeg");
        public static readonly string tempPath = Path.Combine(localAppDataPath, "temp");
    }
}

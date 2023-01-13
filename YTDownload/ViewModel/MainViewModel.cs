using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Xabe.FFmpeg;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YTDownload.Model;

namespace YTDownload.ViewModel
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        string url = "";
        [ObservableProperty]
        string statusMessage = "";
        [ObservableProperty]
        ObservableCollection<YTElement> videoCollection = new ObservableCollection<YTElement>();
        [ObservableProperty]
        Visibility metadataWindowVisibile = Visibility.Collapsed;
        [ObservableProperty]
        bool canAddYTVideo = true;
        [ObservableProperty]
        bool canDownload = false;

        // All metadata fields
        [ObservableProperty]
        string metadataTitle = "";
        [ObservableProperty]
        string metadataAlbum = "";
        [ObservableProperty]
        string metadataInterpreter = "";
        [ObservableProperty]
        string metadataYear = "";
        [ObservableProperty]
        string metadataTracknumber = "";

        private readonly YoutubeClient _youtube = new();
        private Dictionary<string, YTElement> videoList = new Dictionary<string, YTElement>();
        private YTElement? selectedYTElem = null;

        [RelayCommand]
        async Task FetchVideo()
        {
            CanAddYTVideo = false;
            Video video = await _youtube.Videos.GetAsync(Url);
            Thumbnail videoThumbnail = video.Thumbnails.GetWithHighestResolution();

            StreamManifest streamManifest = await _youtube.Videos.Streams.GetManifestAsync(Url);
            IStreamInfo[] streams = streamManifest
                .GetAudioStreams()
                .OrderByDescending(s => s.Bitrate)
                .ToArray();

            IStreamInfo bestAudioStream = streams[0];
            YTElement YTElem = new YTElement(bestAudioStream, video, videoThumbnail.Url);

            videoCollection.Add(YTElem);
            videoList.Add(YTElem.StreamUrl, YTElem);
            StatusMessage = "";
            Url = "";

            CanAddYTVideo = true;
            if (!CanDownload)
            {
                CanDownload= true;
            }
        }

        [RelayCommand]
        void RemoveVideo(object parameter)
        {
            YTElement videoToDelete = GetSelectedVideo(parameter);
            bool isSelectedElement = videoToDelete.Equals(selectedYTElem);
            string? btnUrl = parameter.ToString();
            
            if (btnUrl != null)
            {
                videoList.Remove(btnUrl);
                videoCollection.Remove(videoToDelete);
            }

            if (isSelectedElement)
            {
                MetadataTitle = "";
                MetadataAlbum = "";
                MetadataInterpreter = "";
                MetadataYear = "";
                MetadataTracknumber = "";
                MetadataWindowVisibile = Visibility.Collapsed;
            }
            if(videoCollection.Count == 0)
            {
                CanDownload = false;
            }
        }

        [RelayCommand]
        void EditMetadata(object parameter)
        {
            // Save previous changes in the ElementModel-Class before
            // loading in the values of the newly selected one.
            if (selectedYTElem != null)
            {
                selectedYTElem.MetadataTitle = MetadataTitle;
                selectedYTElem.MetadataAlbum = MetadataAlbum;
                selectedYTElem.MetadataInterpreter = MetadataInterpreter;
                selectedYTElem.MetadataYear = MetadataYear;
                selectedYTElem.MetadataTracknumber= MetadataTracknumber;
            }
            MetadataWindowVisibile = Visibility.Visible;

            try
            {
                selectedYTElem = GetSelectedVideo(parameter);
            }catch(Exception)
            {
                StatusMessage = "Oops! Cant Edit Metadata";
                return;
            }

            MetadataTitle = selectedYTElem.MetadataTitle;
            MetadataAlbum = selectedYTElem.MetadataAlbum;
            MetadataInterpreter = selectedYTElem.MetadataInterpreter;
            MetadataYear = selectedYTElem.MetadataYear;
            MetadataTracknumber = selectedYTElem.MetadataTracknumber;
        }

        YTElement GetSelectedVideo(object parameter)
        {
            string? btnUrl = parameter.ToString();

            YTElement? YTElem = null;
            if (btnUrl != null)
            {
                _ = videoList.TryGetValue(btnUrl, out YTElem);
            }
            if (YTElem == null)
            {
                throw new Exception("Failed getting currently selected video.");
            }
            return YTElem;
        }

        [RelayCommand]
        async Task DownloadAll()
        {
            // Save previous changes in the ElementModel-Class before
            // loading in the values of the newly selected one.
            if (selectedYTElem != null)
            {
                selectedYTElem.MetadataTitle = MetadataTitle;
                selectedYTElem.MetadataAlbum = MetadataAlbum;
                selectedYTElem.MetadataInterpreter = MetadataInterpreter;
                selectedYTElem.MetadataYear = MetadataYear;
                selectedYTElem.MetadataTracknumber = MetadataTracknumber;
            }

            string folderPath = "";
            FolderBrowserDialog openFileDlg = new FolderBrowserDialog();
            var result = openFileDlg.ShowDialog();
            if (result.ToString() != string.Empty)
            {
                folderPath = openFileDlg.SelectedPath;
            }

            int count = 1;
            foreach(YTElement YTElem in videoList.Values)
            {
                string randomName = Guid.NewGuid().ToString();

                string randomFilename = Utils.SanitizeFileName($"{randomName}.{YTElem.Stream.Container.Name}");
                string randomFilenameMP3 = Utils.SanitizeFileName($"{randomName}.mp3");
                string filenameMP3 = Utils.SanitizeFileName($"{YTElem.MetadataInterpreter} - {YTElem.MetadataTitle}.mp3");

                string tempFile = Path.Combine(App.tempPath, randomFilename);
                string tempFileMP3 = Path.Combine(App.tempPath, randomFilenameMP3);

                string finalFile = Path.Combine(folderPath, filenameMP3);
                // Set up progress reporting
                var progressHandler = new Progress<double>(p => StatusMessage = $"Downloading {count}/{videoList.Count}... {Math.Round(p*100,2)}%");

                await _youtube.Videos.Streams.DownloadAsync(YTElem.Stream, tempFile, progressHandler);

                string msg = StatusMessage;
                StatusMessage = msg + " Converting now. Please wait a bit.";
                
                await Convert(tempFile, tempFileMP3);

                if (File.Exists(finalFile))
                {
                    File.Delete(finalFile);
                }
                File.Move(tempFileMP3, finalFile);
                File.Delete(tempFile);

                EditFileMetadata(finalFile, YTElem);

                YTElem.IsFinishedDownloading = true;
                count++;
                StatusMessage = msg + " Done Converting.";
            }
            StatusMessage = "";
        }

        async Task Convert(string currentFile, string destinationFile)
        {
            FFmpeg.SetExecutablesPath(App.ffmpegPath);

            string msg = StatusMessage;
            IConversion c = FFmpeg.Conversions.New();
            c.OnProgress += (sender, args) =>
            {
                StatusMessage = msg + $" [{args.Duration}/{args.TotalLength}][{args.Percent}%]";
            };
            await c.Start($"-i {currentFile} -preset ultrafast {destinationFile}");
        }

        void EditFileMetadata(string filename, YTElement YTElem)
        {
            TagLib.File tagFile = TagLib.File.Create(filename);

            tagFile.Tag.Album = YTElem.MetadataAlbum;
            tagFile.Tag.Title = YTElem.MetadataTitle;
            tagFile.Tag.Track = uint.Parse(YTElem.MetadataTracknumber);
            tagFile.Tag.Year = uint.Parse(YTElem.MetadataYear);
            tagFile.Tag.Performers = new string[] { YTElem.MetadataInterpreter };
            
            tagFile.Save();
        }
    }
}

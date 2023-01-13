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

        /// <summary>
        /// Try to get the audio stream with the highest quality from the provided url
        /// and add the newly created YTElement to a list for display.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        async Task FetchVideo()
        {
            CanAddYTVideo = false;
            Video video;
            try
            {
                video = await _youtube.Videos.GetAsync(Url);
            }catch(ArgumentException ex)
            {
                CanAddYTVideo = true;
                StatusMessage = ex.Message;
                return;
            }
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

        /// <summary>
        /// Removes a YTElement entry from the list of YT videos.
        /// If the removed video is selected, hide the Metadata-Column in the UI as well.
        /// </summary>
        /// <param name="parameter"></param>
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

        /// <summary>
        /// Applies the users changes on the Metadata of the selected YTElement
        /// </summary>
        /// <param name="parameter"></param>
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

        /// <summary>
        /// Gets the reference of the users currently selected YTElement RadioButton
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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

        /// <summary>
        /// Goes through the list of all added YT videos, downloads them, 
        /// converts them to MP3 and applies the Metadata-Information.
        /// If the File already exists, the old one will get removed.
        /// </summary>
        /// <returns></returns>
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

                string msg = StatusMessage;
                try
                {
                    await _youtube.Videos.Streams.DownloadAsync(YTElem.Stream, tempFile, progressHandler);
                }catch(IOException)
                {
                    StatusMessage = "Download Failed because the internet connection got cut off for a moment. Please try again.";
                    return;
                }

                StatusMessage = msg + " Converting now. Please wait a bit.";

                await Convert(tempFile, tempFileMP3);

                if (File.Exists(finalFile))
                {
                    File.Delete(finalFile);
                }
                File.Move(tempFileMP3, finalFile);
                File.Delete(tempFile);

                await EditFileMetadata(finalFile, YTElem);

                count++;
                StatusMessage = msg + " Done Converting.";
            }
            StatusMessage = "";
        }

        /// <summary>
        /// Converts the current file to the destinationFile using FFmpeg with the 'ultrafast' preset.
        /// </summary>
        /// <param name="currentFile"></param>
        /// <param name="destinationFile"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Applies the Metadata-Informations made by the user to the convertet AudioFile
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="YTElem"></param>
        async Task EditFileMetadata(string filename, YTElement YTElem)
        {
            TagLib.Id3v2.Tag.DefaultVersion = 3; //Windows Media Player & Groove Music dont like other versions.
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;
            TagLib.File tagFile = TagLib.File.Create(filename);

            tagFile.Tag.Album = YTElem.MetadataAlbum;
            tagFile.Tag.Title = YTElem.MetadataTitle;
            tagFile.Tag.Track = uint.Parse(YTElem.MetadataTracknumber);
            tagFile.Tag.Year = uint.Parse(YTElem.MetadataYear);
            tagFile.Tag.Performers = new string[] { YTElem.MetadataInterpreter };

            //Get the YT thumbnail as the albumb cover
            string thumbnailPath = Path.Combine(App.tempPath, "thumb" + Guid.NewGuid().ToString() + ".jpg");
            await Utils.DownloadFileAsync(YTElem.ThumbnailUrl, thumbnailPath);
            TagLib.IPicture pic = new TagLib.Picture(thumbnailPath);
            tagFile.Tag.Pictures = new TagLib.IPicture[] { pic };
            if (File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
            }

            tagFile.Save();
        }
    }
}

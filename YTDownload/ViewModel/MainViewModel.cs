using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        ObservableCollection<YTElementModel> videoCollection = new ObservableCollection<YTElementModel>();
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
        private Dictionary<string, YTElementModel> videoList = new Dictionary<string, YTElementModel>();
        private YTElementModel? selectedYTEM = null;

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
            YTElementModel YTEM = new YTElementModel(bestAudioStream, video, videoThumbnail.Url);

            videoCollection.Add(YTEM);
            videoList.Add(YTEM.StreamUrl, YTEM);
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
            YTElementModel videoToDelete = GetSelectedVideo(parameter);
            bool isSelectedElement = videoToDelete.Equals(selectedYTEM);
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
            if (selectedYTEM != null)
            {
                selectedYTEM.MetadataTitle = MetadataTitle;
                selectedYTEM.MetadataAlbum = MetadataAlbum;
                selectedYTEM.MetadataInterpreter = MetadataInterpreter;
                selectedYTEM.MetadataYear = MetadataYear;
                selectedYTEM.MetadataTracknumber= MetadataTracknumber;
            }
            MetadataWindowVisibile = Visibility.Visible;

            try
            {
                selectedYTEM = GetSelectedVideo(parameter);
            }catch(Exception)
            {
                StatusMessage = "Oops! Cant Edit Metadata";
                return;
            }

            MetadataTitle = selectedYTEM.MetadataTitle;
            MetadataAlbum = selectedYTEM.MetadataAlbum;
            MetadataInterpreter = selectedYTEM.MetadataInterpreter;
            MetadataYear = selectedYTEM.MetadataYear;
            MetadataTracknumber = selectedYTEM.MetadataTracknumber;
        }

        YTElementModel GetSelectedVideo(object parameter)
        {
            string? btnUrl = parameter.ToString();

            YTElementModel? YTEM = null;
            if (btnUrl != null)
            {
                _ = videoList.TryGetValue(btnUrl, out YTEM);
            }
            if (YTEM == null)
            {
                throw new Exception("Failed getting currently selected video.");
            }
            return YTEM;
        }

        [RelayCommand]
        async Task DownloadAll()
        {
            int count = 1;
            foreach(YTElementModel YTEM in videoList.Values)
            {
                string filename = Utils.SanitizeFileName($"{YTEM.Author} - {YTEM.Title}.{YTEM.Stream.Container.Name}");
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), filename);

                // Set up progress reporting
                var progressHandler = new Progress<double>(p => StatusMessage = $"Downloading {count}/{videoList.Count}... {Math.Round(p*100,2)}%");

                await _youtube.Videos.Streams.DownloadAsync(YTEM.Stream, filePath, progressHandler);
                count++;
            }
            StatusMessage = "";
        }
    }
}

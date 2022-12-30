using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VideoLibrary;
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

        Dictionary<string, YTElementModel> videoList = new Dictionary<string, YTElementModel>();
        YouTube youTube = YouTube.Default;
        YTElementModel selectedYTEM = new YTElementModel();

        [RelayCommand]
        async Task FetchVideo()
        {
            if (videoList.ContainsKey(url))
            {
                StatusMessage = "The video was already added!";
                return;
            }
            StatusMessage = $"Collecting information about {Url}";
            IEnumerable<YouTubeVideo> videoInfos;
            try
            {
                videoInfos = await youTube.GetAllVideosAsync(Url);
            } 
            catch(Exception ex)
            {
                StatusMessage = $"Error occured during processing: {ex.Message}";
                return;
            }
            try
            {
                videoInfos.Count(); //Doing anything with a bad stream will throw an error so lets just check the count.
            }
            catch (Exception)
            {
                StatusMessage = "No video has been found.";
                return;
            }

            //Get video objects with the best audio and video quality
            YouTubeVideo videoBestAudio = videoInfos.First();
            YouTubeVideo videoBestVideo = videoInfos.First();
            int bestAudioBitrate = 0;
            int bestVideoResolution = 0;
            foreach (YouTubeVideo video in videoInfos)
            {
                if (video.AudioBitrate > bestAudioBitrate)
                {
                    bestAudioBitrate = video.AudioBitrate;
                    videoBestAudio = video;
                }
                if (video.Resolution > bestVideoResolution)
                {
                    bestVideoResolution = video.Resolution;
                    videoBestVideo = video;
                }
            }

            YouTubeVideo[] vidArr = { videoBestAudio, videoBestVideo };

            YTElementModel YTEM = new YTElementModel();
            YTEM.Title = videoBestVideo.Title;
            YTEM.Author = videoBestVideo.Info.Author;
            if (videoBestVideo.Info.LengthSeconds != null)
            {
                int length = (int)videoBestVideo.Info.LengthSeconds;
                int hours = length / 3600;
                int minutes = length / 60 - hours * 60;
                int seconds = length % 60;
                YTEM.Length = hours.ToString().PadLeft(2, '0') + ':' + minutes.ToString().PadLeft(2, '0') + ':' + seconds.ToString().PadLeft(2, '0');
            }
            YTEM.Url = Url;
            YTEM.EditMetadataCommand = EditMetadataCommand;
            YTEM.RemoveVideoCommand = RemoveVideoCommand;
            YTEM.Videos = vidArr;


            videoCollection.Add(YTEM);
            videoList.Add(Url, YTEM);
            StatusMessage = "";
            Url = "";
        }

        [RelayCommand]
        void RemoveVideo(object parameter)
        {
            GetSelectedVideo(parameter);

            string? btnUrl = parameter.ToString();
            if (btnUrl != null)
            {
                videoList.Remove(btnUrl);
                videoCollection.Remove(selectedYTEM);
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
            GetSelectedVideo(parameter);
            if (selectedYTEM == null)
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

        void GetSelectedVideo(object parameter)
        {
            string? btnUrl = parameter.ToString();

            if (btnUrl != null)
            {
                _ = videoList.TryGetValue(btnUrl, out selectedYTEM);
            }
        }
    }
}

using System;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YTDownload.Model
{
    public sealed class YTElement
    {
        public string Title { get; }
        public string Author { get; }
        public string Length { get; } = "00:00:00";
        public string StreamUrl { get; }
        public string ThumbnailUrl { get; }
        public IStreamInfo Stream { get; }
        public bool IsFinishedDownloading { get; set; } = false;

        public string MetadataTitle { get; set; }
        public string MetadataAlbum { get; set; }
        public string MetadataInterpreter { get; set; }
        public string MetadataYear { get; set; }
        public string MetadataTracknumber { get; set; }

        public YTElement(IStreamInfo stream, Video video, string thumbnailUrl)
        {
            Stream = stream;
            Title = video.Title;
            Author = video.Author.ChannelTitle;
            if (video.Duration != null)
            {
                TimeSpan length = (TimeSpan)video.Duration;
                Length = length.Hours.ToString().PadLeft(2, '0')
                    + ':' + length.Minutes.ToString().PadLeft(2, '0')
                    + ':' + length.Seconds.ToString().PadLeft(2, '0');
            }
            StreamUrl = stream.Url;
            ThumbnailUrl = thumbnailUrl;

            MetadataTracknumber = "1";
            MetadataYear = DateTime.Now.Year.ToString();
            MetadataAlbum = "";
            string[] titleSplits = Title.Split(" - ");
            if (titleSplits.Length == 1)
            {
                MetadataTitle = Title;
                MetadataInterpreter = Author.Replace(" - Topic", "");
            }
            else
            {
                MetadataTitle = titleSplits[1];
                MetadataInterpreter = titleSplits[0];
            }
        }
    }
}

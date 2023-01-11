namespace YTDownload.Model
{
    public sealed class YTElementModel
    {
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Length { get; set; } = "";
        public string StreamUrl { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public float DownloadProgress { get; set; } = 0.0f;

        public string MetadataTitle { get; set; } = "";
        public string MetadataAlbum { get; set; } = "";
        public string MetadataInterpreter { get; set; } = "";
        public string MetadataYear { get; set; } = "";
        public string MetadataTracknumber { get; set; } = "";

        public YTElementModel() { }
    }
}

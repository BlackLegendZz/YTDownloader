using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace YTDownload
{
    static class Utils
    {
        public static string SanitizeFileName(string fileName)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');

            return fileName;
        }

        public static async Task DownloadFileAsync(string url, string fileName)
        {
            HttpClient client = new HttpClient();
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                using (var fs = new FileStream(fileName, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.Assets
{
    public class LargeUploadRequest
    {
        public LargeUploadRequest()
        {

        }
        public LargeUploadRequest(string filename, string mediaType, long fileSize, byte[] fileContent, string contentHubHostName, string contentHubToken, string uploadConfiguration)
        {
            Filename = filename;
            MediaType = mediaType;
            FileSize = fileSize;
            FileContent = fileContent;
            ContentHubHostName = contentHubHostName;
            ContentHubToken = contentHubToken;
            UploadConfiguration = uploadConfiguration;
        }

        [Required]
        public string Filename { get; set; }
        [Required]
        public string MediaType { get; set; }
        [Required]
        [Range(1, long.MaxValue)]
        public long FileSize { get; set; }
        [Required]
        public byte[] FileContent { get; set; }
        [Required]
        public string ContentHubHostName { get; set; }
        [Required]
        public string ContentHubToken { get; set; }
        public string UploadConfiguration { get; set; }
    }
}

using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace BlackNight.MailDaemon
{
    public class Helpers
    {
        public static string GetMediaType(string fileName)
        {
            switch (Path.GetExtension(fileName).ToLower())
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                    return MediaTypeNames.Image.Jpeg;
                case ".gif":
                    return MediaTypeNames.Image.Gif;
                case ".tiff":
                    return MediaTypeNames.Image.Tiff;
                case ".pdf":
                    return MediaTypeNames.Application.Pdf;
                case ".zip":
                    return MediaTypeNames.Application.Zip;
                case ".rtf":
                    return MediaTypeNames.Application.Rtf;
                case ".txt":
                    return MediaTypeNames.Text.Plain;
                case ".html":
                    return MediaTypeNames.Text.Html;
                case ".xml":
                    return MediaTypeNames.Text.Xml;
                default:
                    return MediaTypeNames.Application.Octet;
            }
        }
    }
}

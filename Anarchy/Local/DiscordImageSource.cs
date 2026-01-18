using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Discord
{
    public static class DiscordImageSource
    {
        public static async Task<DiscordImage> FromUrl(string url)
        {
            using (var hc = new HttpClient())
            using (var response = await hc.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();

                var contentType = response.Content.Headers
                    .First(h => h.Key == "Content-Type")
                    .Value.First();

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    return FromStream(
                        stream,
                        DiscordImageMediaType.ToImageFormat(contentType)
                    );
                }
            }
        }

        public static DiscordImage FromFile(DiscordAttachmentFile file)
        {
            return FromBytes(
                file.Bytes,
                DiscordImageMediaType.ToImageFormat(file.MediaType)
            );
        }

        public static DiscordImage FromStream(Stream stream, ImageFormat format)
        {
            using (var temp = Image.FromStream(stream))
            {
                var bitmap = new Bitmap(temp);
                return new DiscordImage(bitmap, format);
            }
        }

        public static DiscordImage FromBytes(byte[] bytes, ImageFormat format)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return FromStream(stream, format);
            }
        }
    }
}

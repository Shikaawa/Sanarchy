using System;

namespace Discord
{
    public enum ImageFormat
    {
        Jpeg,
        Png,
        Gif
    }
    
    public static class DiscordImageMediaType
    {
        public static string ToMediaType(this ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.Jpeg:
                    return MediaTypeNames.Image.Jpeg;

                case ImageFormat.Png:
                    return "image/png";

                case ImageFormat.Gif:
                    return MediaTypeNames.Image.Gif;

                default:
                    throw new NotSupportedException("ImageFormat not supported.");
            }
        }

        public static ImageFormat ToImageFormat(string mediaType)
        {
            if (string.IsNullOrEmpty(mediaType))
                throw new ArgumentNullException("mediaType");

            string value = mediaType.Replace("image/", string.Empty);

            return (ImageFormat)Enum.Parse(
                typeof(ImageFormat),
                value,
                true
            );
        }

        public static bool IsSupportedImageFormat(string mediaType)
        {
            if (string.IsNullOrEmpty(mediaType))
                return false;

            return mediaType == MediaTypeNames.Image.Png
                || mediaType == MediaTypeNames.Image.Jpeg
                || mediaType == MediaTypeNames.Image.Gif;
        }
    }
}

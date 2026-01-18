using System;
using Newtonsoft.Json;

namespace Discord
{
    internal class ImageJsonConverter : JsonConverter
    {
        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DiscordImage);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            var image = (DiscordImage)value;

            string data = string.Format(
                "data:{0};base64,{1}",
                image.ImageFormat.ToMediaType(),
                Convert.ToBase64String(image.Bytes)
            );

            writer.WriteValue(data);
        }
    }
    
    [JsonConverter(typeof(ImageJsonConverter))]
    public class DiscordImage
    {
        public DiscordImage(byte[] bytes, ImageFormat format)
        {
            Bytes = bytes;
            ImageFormat = format;
        }

        public byte[] Bytes { get; private set; }

        public ImageFormat ImageFormat { get; private set; }

        public static implicit operator DiscordAttachmentFile(DiscordImage image)
        {
            return new DiscordAttachmentFile(image.Bytes, image.ImageFormat);
        }
    }
}

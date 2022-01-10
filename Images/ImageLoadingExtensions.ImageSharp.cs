using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using SixLabors.ImageSharp;

using EastFive.Extensions;
using EastFive.Linq;
using SixLabors.ImageSharp.Formats;
using EastFive.Text;

namespace EastFive.Images
{
    public static partial class ImageLoadingExtensions
    {
        public static async Task<TResult> TryReadImageAsync<TResult>(this Stream mediaContents,
            Func<Image, IImageFormat , TResult> onRead,
            Func<TResult> onFailure)
        {
            try
            {
                var image = Image.Load(mediaContents, out IImageFormat format);
                return onRead(image, format);
            } catch(ArgumentException)
            {
                mediaContents.Position = 0;
                return onFailure();
            }
        }

        public static bool TryReadImage(this byte [] mediaContents, out Image image, out IImageFormat format)
        {
            image = Image.Load(mediaContents, out format);
            return true;
        }

        public static Task SaveWithQualityAsync(this Image image, Stream outputStream,
            IImageFormat imageCodec, long encoderQuality = 80L)
        {
            return image.SaveAsync(outputStream, imageCodec);
        }

        public static async Task<IImageFormat> SaveAsync(this Image image, Stream outputStream,
            string encodingMimeType, long encoderQuality = 80L)
        {
            var format = encodingMimeType.ParseImageEncoder();
            await image.SaveAsync(outputStream, format);
            return format;
        }

        public static async Task<(byte [], IImageFormat)> GetBytesAsync(this Image image,
            string encodingMimeType = "image/jpeg", long encoderQuality = 80L)
        {
            using (var stream = new MemoryStream())
            {
                var format = encodingMimeType.ParseImageEncoder();
                await image.SaveAsync(stream, format);
                return (stream.ToArray(), format);
            }
        }

        public static (byte[], IImageFormat) Save(this Image image,
            string encodingMimeType = "image/jpeg", long encoderQuality = 80L)
        {
            using (var stream = new MemoryStream())
            {
                var format = encodingMimeType.ParseImageEncoder();
                image.Save(stream, format);
                return (stream.ToArray(), format);
            }
        }

        public static IImageFormat ParseImageEncoder(this string encodingMimeType)
        {
            return Configuration.Default.ImageFormats
                .Where(format => format.MimeTypes.Any(mt => mt.Equals(encodingMimeType, StringComparison.OrdinalIgnoreCase)))
                .Max(
                    format =>
                    {
                        if (format.DefaultMimeType.Equals(encodingMimeType, StringComparison.Ordinal))
                            return 4;

                        if (format.DefaultMimeType.Equals(encodingMimeType, StringComparison.OrdinalIgnoreCase))
                            return 3;

                        if (format.MimeTypes.Any(mt => mt.Equals(encodingMimeType, StringComparison.Ordinal)))
                            return 2;

                        if (format.MimeTypes.Any(mt => mt.Equals(encodingMimeType, StringComparison.OrdinalIgnoreCase)))
                            return 1;

                        return 0;
                    },
                    (format) =>
                    {
                        return format;
                    },
                    () =>
                    {
                        return Configuration.Default.ImageFormats
                            .Min(
                                format =>
                                {
                                    return format.MimeTypes
                                        .Append(format.DefaultMimeType)
                                        .Min(mt => mt.Levenshtein(encodingMimeType));
                                },
                                (format) =>
                                {
                                    return format;
                                },
                                () =>
                                {
                                    throw new Exception(
                                        "Default ImageSharp configuration does not have any formats loaded.");
                                });
                    });
        }

        public static bool TryParseImage(this string imageDataEncoding, out Image image)
        {
            if(!imageDataEncoding.TryParseImage(
                out byte [] data, out string contentType))
            {
                image = default;
                return false;
            }

            using (var stream = new MemoryStream(data))
            {
                image = Image.Load(data);
                return true;
            }
        }

        public static string Base64Encode(this Image image,
            string encodingMimeType = "image/jpeg", long encoderQuality = 80L)
        {
            var (mediaContents, imageCodecInfo) = image.Save(encodingMimeType: encodingMimeType);
            var contentType = imageCodecInfo.DefaultMimeType;
            return $"data:{contentType};base64,{mediaContents.ToBase64String()}";
        }

        public static string GetMimeType(this Image image)
        {
            return "image/jpeg";
        }
    }
}

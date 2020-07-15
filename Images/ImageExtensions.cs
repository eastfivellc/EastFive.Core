using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EastFive.Extensions;
using EastFive.Linq;

namespace EastFive.Images
{
    public static class ImageExtensions
    {
        public static Image ResizeImage(this Image image,
            int? width = default(int?), int? height = default(int?), bool? fill = default(bool?),
            Brush background = default)
        {
            if (!width.HasValue)
                if (!height.HasValue)
                    if (!fill.HasValue)
                        return image;
            
            var ratio = ((double)image.Size.Width) / ((double)image.Size.Height);
            var newWidth = (int)Math.Round(width.HasValue ?
                    width.Value
                    :
                    height.HasValue ?
                        height.Value * ratio
                        :
                        image.Size.Width);
            var newHeight = (int)Math.Round(height.HasValue ?
                    height.Value
                    :
                    width.HasValue ?
                        width.Value / ratio
                        :
                        image.Size.Width);

            var newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            //set the new resolution
            newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            //start the resizing
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                if(background.IsDefaultOrNull())
                    background = System.Drawing.Brushes.White;
                graphics.FillRectangle(background, 0, 0, newWidth, newHeight);

                //set some encoding specs
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                return newImage;
            }
        }

        public static Image SetBackground(this Image image,
            Brush background)
        {
            var newImage = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);

            //set the new resolution
            newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            //start the resizing
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.FillRectangle(background, 0, 0, image.Width, image.Height);

                //set some encoding specs
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                return newImage;
            }
        }

        public static void Save(this Image image, Stream outputStream,
            ImageCodecInfo imageCodec, long encoderQuality = 80L)
        {
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, encoderQuality);

            image.Save(outputStream, imageCodec, encoderParameters);
        }

        public static void Save(this Image image, Stream outputStream,
            out ImageCodecInfo imageCodecUsed,
            string encodingMimeType = "image/jpeg", long encoderQuality = 80L)
        {
            imageCodecUsed = ParseImageCodecInfo(encodingMimeType);
            image.Save(outputStream, imageCodecUsed, encoderQuality);
        }

        public static byte [] Save(this Image image,
            out ImageCodecInfo imageCodecUsed,
            string encodingMimeType = "image/jpeg", long encoderQuality = 80L)
        {
            using(var stream = new MemoryStream())
            {
                image.Save(stream, out imageCodecUsed,
                    encodingMimeType: encodingMimeType,
                    encoderQuality: encoderQuality);
                return stream.ToArray();
            }
        }

        public static ImageCodecInfo ParseImageCodecInfo(this string mimeType)
        {
            return ImageCodecInfo
                .GetImageEncoders()
                .First(
                    (encoder, next) =>
                    {
                        if (encoder.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase))
                            return encoder;
                        return next();
                    },
                    () => ImageCodecInfo.GetImageEncoders().First());
        }

        public static bool TryParseImage(this string imageDataEncoding, out Image image)
        {
            if(!imageDataEncoding.TryMatchRegex(
                "data:(?<contentType>[^;]+);(?<encoding>[^,]+),(?<data>[\\S\\s]+)",
                (contentType, encoding, data) => Tuple.Create(contentType, encoding, data),
                out Tuple<string, string, string> components))
            {
                image = default;
                return false;
            }

            var encoding = components.Item2;
            var dataEncoded = components.Item3;
            if (encoding.Equals("base64", StringComparison.OrdinalIgnoreCase))
            {
                var data = dataEncoded.FromBase64String();
                using (var stream = new MemoryStream(data))
                {
                    image = new Bitmap(stream);
                    return true;
                }
            }

            if (encoding.Equals("base58", StringComparison.OrdinalIgnoreCase))
            {
                var data = dataEncoded.Base58Decode();
                using (var stream = new MemoryStream(data))
                {
                    image = new Bitmap(stream);
                    return true;
                }
            }

            image = default;
            return false;
        }
        
        public static string Base64Encode(this Image image,
            string encodingMimeType = "image/jpeg", long encoderQuality = 80L)
        {
            var mediaContents = image.Save(out ImageCodecInfo imageCodecInfo,
                encodingMimeType:encodingMimeType, encoderQuality:encoderQuality);
            var contentType = imageCodecInfo.MimeType;
            return $"data:{contentType};base64,{mediaContents.ToBase64String()}";
        }

        public static Image Crop(this Image image, int x, int y, int w, int h)
        {
            var newImage = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            //set the new resolution
            newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            //start the resizing
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                var brush = System.Drawing.Brushes.Transparent;
                graphics.FillRectangle(brush, 0, 0, w, h);

                //set some encoding specs
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(image, 0, 0,
                    new System.Drawing.Rectangle(x, y, w, h),
                    GraphicsUnit.Pixel);
            }
            return newImage;
        }

        public static Image Crop(this Image image, double x, double y, double w, double h)
        {
            return image.Crop(
                (int)(x * image.Width),
                (int)(y * image.Height),
                (int)(w * image.Width),
                (int)(h * image.Height));
        }

        public static Image Scale(this Image image,
            int? width = default(int?), int? height = default(int?), bool? fill = default(bool?))
        {
            bool KeepOriginal()
            {
                if (width.HasValue || height.HasValue || fill.HasValue)
                    return false;
                return true;
            }
            if (KeepOriginal())
                return image;

            var ratio = ((double)image.Size.Width) / ((double)image.Size.Height);
            var newWidth = (int)Math.Round(width.HasValue ?
                    width.Value
                    :
                    height.HasValue ?
                        height.Value * ratio
                        :
                        image.Size.Width);
            var newHeight = (int)Math.Round(height.HasValue ?
                    height.Value
                    :
                    width.HasValue ?
                        width.Value / ratio
                        :
                        image.Size.Width);

            var newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            //set the new resolution
            newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            //start the resizing
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                var brush = System.Drawing.Brushes.Transparent;
                graphics.FillRectangle(brush, 0, 0, newWidth, newHeight);

                //set some encoding specs
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }

        public static Image MaxAspect(this Image image, double viewportAspect)
        {
            var imageAspect = image.AspectRatio();
            if(imageAspect < viewportAspect)
            {
                var newHeight = image.Width / viewportAspect;
                var heightCrop = (image.Height - newHeight) + 1.0;
                var newY = (int)(heightCrop / 2);
                return image.Crop(0, newY, image.Width, (int)(newHeight + 0.5));
            }
            if (imageAspect > viewportAspect)
            {
                var newWidth = image.Height * viewportAspect;
                var widthCrop = image.Width - newWidth;
                var newX = (int)(widthCrop / 2);
                return image.Crop(newX, 0, (int)(newWidth + 0.5), image.Height);
            }
            return image;
        }

        public static Image MinAspect(this Image image, double viewportAspect)
        {
            var imageAspect = image.AspectRatio();
            if (imageAspect > viewportAspect)
            {
                var newHeight = image.Width / viewportAspect;
                var heightMargin = (newHeight - image.Height) + 1.0;
                var newY = (int)(heightMargin / 2);
                return image.Crop(0, newY, image.Width, (int)(newHeight + 0.5));
            }
            if (imageAspect < viewportAspect)
            {
                var newWidth = image.Height * viewportAspect;
                var widthMargin = newWidth - image.Width;
                var newX = (int)(widthMargin / 2);
                return image.Crop(newX, 0, (int)(newWidth + 0.5), image.Height);
            }
            return image;
        }

        public static Image ScaleAspect(this Image image, double viewportAspect)
        {
            var imageAspect = image.AspectRatio();
            if (imageAspect > viewportAspect)
            {
                var newHeight = image.Width / viewportAspect;
                return image.Scale(image.Width, (int)(newHeight + 0.5), true);
            }
            if (imageAspect < viewportAspect)
            {
                var newWidth = image.Height * viewportAspect;
                return image.Scale((int)(newWidth + 0.5), image.Height, true);
            }
            return image;
        }

        public static double AspectRatio(this Image image)
        {
            var width = (double)image.Width;
            var height = (double)image.Height;
            return width / height;
        }

        public static string GetMimeType(this Image image)
        {
            return image.RawFormat.GetMimeType();
        }

        public static string GetMimeType(this ImageFormat imageFormat)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.First(codec => codec.FormatID == imageFormat.Guid).MimeType;
        }
    }
}

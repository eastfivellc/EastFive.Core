using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using SixLabors.ImageSharp;

using EastFive.Extensions;
using EastFive.Linq;
using System.Drawing;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

namespace EastFive.Images
{
    public static partial class ImageExifExtensions
    {
        //public static void FixOrientation(this Image image)
        //{
        //    var orientation = image.ExifGetRotateFlip();
        //    if (orientation == Orientation.rotated0)
        //        return;

        //    orientation.TryOrientationToFlipRotate(
        //        out RotateMode rotateMode, out FlipMode flipMode);
        //    image.Mutate(ctx => ctx.RotateFlip(rotateMode, flipMode));
        //    image.ExifSetRotateFlip(Orientation.rotated0);
        //    //return image;
        //}

        public static Func<IImageProcessingContext, IImageProcessingContext> FixOrientation(this Image image)
        {
            var orientation = image.ExifGetRotateFlip();
            if (orientation == Orientation.rotated0)
                return ctx => ctx;

            orientation.TryOrientationToFlipRotate(
                out RotateMode rotateMode, out FlipMode flipMode);
            image.ExifSetRotateFlip(Orientation.rotated0);
            return ctx => ctx.RotateFlip(rotateMode, flipMode);
        }

        public static Orientation Invert(this Orientation xform)
        {
            // 0
            if (xform == Orientation.rotated0)
                return Orientation.rotated180;
            // 1
            if (xform == Orientation.rotated90)
                return Orientation.rotated270;
            // 2
            if (xform == Orientation.rotated180)
                return Orientation.rotated0;
            // 3
            if (xform == Orientation.rotated270)
                return Orientation.rotated90;
            // 4
            if (xform == Orientation.rotated0Mirrored)
                return Orientation.rotated180Mirrored;
            // 5
            if (xform == Orientation.rotated90Mirrored)
                return Orientation.rotated270Mirrored;
            // 6
            if (xform == Orientation.rotated180Mirrored)
                return Orientation.rotated0Mirrored;
            // 7
            if (xform == Orientation.rotated270Mirrored)
                return Orientation.rotated90Mirrored;

            throw new ArgumentException($"'{xform}' is not a recognized transform");
        }

        public static Orientation ExifGetRotateFlip(this Image image)
        {
            if (!image.Metadata.ExifProfile.Values.Contains(
                    v => v.Tag == ExifTag.Orientation))
                return Orientation.rotated0;

            var orientation = image.Metadata.ExifProfile.Values
                .Where(item => item.Tag == ExifTag.Orientation)
                .First();

            var orientationEnum = (Orientation)orientation.GetValue();
            return orientationEnum;
        }

        public static Image ExifSetRotateFlip(this Image image, Orientation orientation)
        {
            return image.Metadata.ExifProfile.Values
                .Where(item => item.Tag == ExifTag.Orientation)
                .First(
                    (propertyItem, next) =>
                    {
                        propertyItem.TrySetValue(
                                (ushort)orientation);
                        return image;
                    },
                    () =>
                    {
                        image.Metadata.ExifProfile
                            .SetValue(ExifTag.Orientation,
                                (ushort)orientation);
                        return image;
                    });
        }

        public static bool TryOrientationToFlipRotate(this Orientation orientation,
            out RotateMode rotateMode, out FlipMode flipMode)
        {
            if(orientation == Orientation.rotated0)
            {
                rotateMode = RotateMode.None;
                flipMode = FlipMode.None;
                return true;
            }
            if (orientation == Orientation.rotated0Mirrored)
            {
                rotateMode = RotateMode.None;
                flipMode = FlipMode.Horizontal;
                return true;
            }
            if (orientation == Orientation.rotated90)
            {
                rotateMode = RotateMode.Rotate90;
                flipMode = FlipMode.None;
                return true;
            }
            if (orientation == Orientation.rotated90Mirrored)
            {
                rotateMode = RotateMode.Rotate90;
                flipMode = FlipMode.Horizontal;
                return true;
            }
            if (orientation == Orientation.rotated180)
            {
                rotateMode = RotateMode.Rotate180;
                flipMode = FlipMode.None;
                return true;
            }
            if (orientation == Orientation.rotated180Mirrored)
            {
                rotateMode = RotateMode.Rotate180;
                flipMode = FlipMode.Horizontal;
                return true;
            }
            if (orientation == Orientation.rotated270)
            {
                rotateMode = RotateMode.Rotate270;
                flipMode = FlipMode.None;
                return true;
            }
            if (orientation == Orientation.rotated270Mirrored)
            {
                rotateMode = RotateMode.Rotate270;
                flipMode = FlipMode.Horizontal;
                return true;
            }

            rotateMode = RotateMode.None;
            flipMode = FlipMode.None;
            return false;
        }
    }
}

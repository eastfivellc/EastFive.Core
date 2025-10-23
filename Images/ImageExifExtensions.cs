﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using EastFive.Extensions;
using EastFive.Linq;

namespace EastFive.Images
{
    public static partial class ImageExifExtensions
    {
        [SupportedOSPlatform("windows6.1")]
        public static void FixOrientation(this Image image)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("OS not supported");

            var orientation = image
                .ExifGetRotateFlip()
                .Invert();
            if (orientation == RotateFlipType.RotateNoneFlipNone)
                return;

            image.RotateFlip(orientation);
            image.ExifSetRotateFlip(RotateFlipType.RotateNoneFlipNone);
        }

        [SupportedOSPlatform("windows6.1")]
        public static RotateFlipType Invert(this RotateFlipType xform)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("OS not supported");

            // 0
            if (xform == RotateFlipType.RotateNoneFlipNone)
                return RotateFlipType.RotateNoneFlipNone;
            // 1
            if (xform == RotateFlipType.Rotate90FlipNone)
                return RotateFlipType.Rotate270FlipNone;
            // 2
            if (xform == RotateFlipType.Rotate180FlipNone)
                return RotateFlipType.Rotate180FlipNone;
            // 3
            if (xform == RotateFlipType.Rotate270FlipNone)
                return RotateFlipType.Rotate90FlipNone;
            // 4
            if (xform == RotateFlipType.RotateNoneFlipX)
                return RotateFlipType.RotateNoneFlipX;
            // 5
            if (xform == RotateFlipType.Rotate90FlipX)
                return RotateFlipType.Rotate270FlipX;
            // 6
            if (xform == RotateFlipType.RotateNoneFlipY)
                return RotateFlipType.RotateNoneFlipY;
            // 7
            if (xform == RotateFlipType.Rotate90FlipY)
                return RotateFlipType.Rotate270FlipY;

            throw new ArgumentException($"'{xform}' is not a recognized transform");
        }

        [SupportedOSPlatform("windows6.1")]
        public static RotateFlipType ExifGetRotateFlip(this Image image)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("OS not supported");

            if (!image.PropertyIdList.Contains(PropertyIdTags.PropertyTagOrientation))
                return RotateFlipType.RotateNoneFlipNone;

            #pragma warning disable CA1416
            var orientation = (Orientation)image.PropertyItems
                .Where(item => item.Id == PropertyIdTags.PropertyTagOrientation)
                .First()
                .GetIntValue();
            #pragma warning restore CA1416

            switch (orientation)
            {
                case Orientation.rotated0:
                    return RotateFlipType.RotateNoneFlipNone;
                case Orientation.rotated0Mirrored:
                    return RotateFlipType.RotateNoneFlipX;
                case Orientation.rotated90:
                    return RotateFlipType.Rotate90FlipNone;
                case Orientation.rotated90Mirrored:
                    return RotateFlipType.Rotate90FlipX;
                case Orientation.rotated180:
                    return RotateFlipType.Rotate180FlipNone;
                case Orientation.rotated180Mirrored:
                    return RotateFlipType.Rotate180FlipX;
                case Orientation.rotated270:
                    return RotateFlipType.Rotate270FlipNone;
                case Orientation.rotated270Mirrored:
                    return RotateFlipType.Rotate270FlipX;
            }
            return RotateFlipType.RotateNoneFlipNone;
        }

        [SupportedOSPlatform("windows6.1")]
        public static bool ExifSetRotateFlip(this Image image, RotateFlipType rotateFlip)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("OS not supported");

            #pragma warning disable CA1416
            var orientation = GetExifOrientation(rotateFlip);
            return image.PropertyItems
                   .Where(item => item.Id == PropertyIdTags.PropertyTagOrientation)
                   .First(
                        (propertyItem, next) =>
                        {
                            propertyItem.SetValue((int)orientation);
                            return true;
                        },
                        () =>
                        {
                            return false;
                        });
            #pragma warning restore CA1416

            Orientation GetExifOrientation(RotateFlipType orientation)
            {
                switch (orientation)
                {
                    case RotateFlipType.RotateNoneFlipNone:
                        return Orientation.rotated0;
                    case RotateFlipType.RotateNoneFlipX:
                        return Orientation.rotated0Mirrored;
                    case RotateFlipType.Rotate90FlipNone:
                        return Orientation.rotated90;
                    case RotateFlipType.Rotate90FlipX:
                        return Orientation.rotated90Mirrored;
                    case RotateFlipType.Rotate180FlipNone:
                        return Orientation.rotated180;
                    case RotateFlipType.Rotate180FlipX:
                        return Orientation.rotated180Mirrored;
                    case RotateFlipType.Rotate270FlipNone:
                        return Orientation.rotated270;
                    case RotateFlipType.Rotate270FlipX:
                        return Orientation.rotated270Mirrored;
                }
                return Orientation.rotated0;
            }
        }

        #region PropertyItem Value Extensions

        [SupportedOSPlatform("windows6.1")]
        public static void SetValue(this PropertyItem propertyItem, int value)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("OS not supported");

            if (propertyItem.Type == PropertyTypes.PropertyTagTypeShort)
            {
                propertyItem.Value = BitConverter.GetBytes((ushort)value);
                return;
            }
            if (propertyItem.Type == PropertyTypes.PropertyTagTypeSShort)
            {
                propertyItem.Value = BitConverter.GetBytes((short)value);
                return;
            }
            if (propertyItem.Type == PropertyTypes.PropertyTagTypeSByte)
            {
                sbyte s = (sbyte)value;
                unchecked // probably uncessary
                {
                    byte b = (byte)s;
                    propertyItem.Value = b.AsArray();
                }
                return;
            }
            if (propertyItem.Type == PropertyTypes.PropertyTagTypeByte)
            {
                var b = (byte)value;
                propertyItem.Value = b.AsArray();
                return;
            }
            throw new ArgumentException($"Cannot cast int to type `{propertyItem.Type}`");
        }

        [SupportedOSPlatform("windows6.1")]
        public static int GetIntValue(this PropertyItem propertyItem)
        {
            propertyItem.TryGetValue(out int value);
            return value;
        }

        [SupportedOSPlatform("windows6.1")]
        public static bool TryGetValue(this PropertyItem propertyItem, out int value)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("OS not supported");

            if (propertyItem.Type == PropertyTypes.PropertyTagTypeShort)
            {
                value = BitConverter.ToUInt16(propertyItem.Value, 0);
                return true;
            }
            if (propertyItem.Type == PropertyTypes.PropertyTagTypeSShort)
            {
                value = BitConverter.ToInt16(propertyItem.Value, 0);
                return true;
            }
            if (propertyItem.Type == PropertyTypes.PropertyTagTypeSByte)
            {
                value = ((sbyte)propertyItem.Value.First());
                return true;
            }
            if (propertyItem.Type == PropertyTypes.PropertyTagTypeByte)
            {
                value = propertyItem.Value.First();
                return true;
            }
            value = default;
            return false;
        }

        [SupportedOSPlatform("windows6.1")]
        public static bool TryGetValue(this PropertyItem propertyItem, out long value)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("OS not supported");

            if (propertyItem.Type == PropertyTypes.PropertyTagTypeSLONG)
            {
                value = BitConverter.ToInt64(propertyItem.Value, 0);
                return true;
            }
            #region can't call TryGetValue(out ulong) here because it will become an infinte loop
            if (propertyItem.Type == PropertyTypes.PropertyTagTypeLong)
            {
                ulong unsignedValue = BitConverter.ToUInt64(propertyItem.Value, 0);
                value = (long)unsignedValue;
                return true;
            }
            #endregion
            if (propertyItem.TryGetValue(out int intValue))
            {
                value = intValue;
                return true;
            }
            value = default;
            return false;
        }

        [SupportedOSPlatform("windows6.1")]
        public static bool TryGetValue(this PropertyItem propertyItem, out ulong value)
        {
            if (!OperatingSystem.IsWindows())
                throw new NotSupportedException("OS not supported");

            if (propertyItem.Type == PropertyTypes.PropertyTagTypeLong)
            {
                value = BitConverter.ToUInt64(propertyItem.Value, 0);
                return true;
            }
            if (propertyItem.TryGetValue(out long signedValue))
            {
                if (signedValue < 0)
                {
                    value = default;
                    return false;
                }
                value = (ulong)signedValue;
                return true;
            }
            value = default;
            return false;
        }

        #endregion

        #region Const classes

        public enum Orientation : UInt16
        {
            rotated0 = 1, //  0 degrees: the correct orientation, no adjustment is required.
            rotated0Mirrored = 2, // 0 degrees, mirrored: image has been flipped back-to-front.
            rotated180 = 3, // 180 degrees: image is upside down.
            rotated180Mirrored = 4, // 180 degrees, mirrored: image has been flipped back-to-front and is upside down.
            rotated270Mirrored = 5, // 90 degrees: image has been flipped back-to-front and is on its side.
            rotated270 = 6, //
            rotated90Mirrored = 7,
            rotated90 = 8
        }

        private static class PropertyTypes
        {
            public const int PropertyTagTypeByte = 1;
            public const int PropertyTagTypeASCII = 2;
            public const int  PropertyTagTypeShort = 3;
            public const int  PropertyTagTypeLong =  4;
            public const int  PropertyTagTypeRational  =  5;
            public const int  PropertyTagTypeSByte = 6;
            public const int  PropertyTagTypeUndefined = 7;
            public const int  PropertyTagTypeSShort =    8;
            public const int  PropertyTagTypeSLONG = 9;
            public const int  PropertyTagTypeSRational = 10;
            public const int  PropertyTagTypeFloat =     11;
            public const int PropertyTagTypeDouble = 12;
        }

        private static class PropertyIdTags
        {
            public const int PropertyTagExifIFD = 0x8769;
            public const int PropertyTagGpsIFD = 0x8825;

            public const int PropertyTagNewSubfileType = 0x00FE;
            public const int PropertyTagSubfileType = 0x00FF;
            public const int PropertyTagImageWidth = 0x0100;
            public const int PropertyTagImageHeight = 0x0101;
            public const int PropertyTagBitsPerSample = 0x0102;
            public const int PropertyTagCompression = 0x0103;
            public const int PropertyTagPhotometricInterp = 0x0106;
            public const int PropertyTagThreshHolding = 0x0107;
            public const int PropertyTagCellWidth = 0x0108;
            public const int PropertyTagCellHeight = 0x0109;
            public const int PropertyTagFillOrder = 0x010A;
            public const int PropertyTagDocumentName = 0x010D;
            public const int PropertyTagImageDescription = 0x010E;
            public const int PropertyTagEquipMake = 0x010F;
            public const int PropertyTagEquipModel = 0x0110;
            public const int PropertyTagStripOffsets = 0x0111;
            public const int PropertyTagOrientation = 0x0112;
            public const int PropertyTagSamplesPerPixel = 0x0115;
            public const int PropertyTagRowsPerStrip = 0x0116;
            public const int PropertyTagStripBytesCount = 0x0117;
            public const int PropertyTagMinSampleValue = 0x0118;
            public const int PropertyTagMaxSampleValue = 0x0119;
            public const int PropertyTagXResolution = 0x011A;
            public const int PropertyTagYResolution = 0x011B;
            public const int PropertyTagPlanarConfig = 0x011C;
            public const int PropertyTagPageName = 0x011D;
            public const int PropertyTagXPosition = 0x011E;
            public const int PropertyTagYPosition = 0x011F;
            public const int PropertyTagFreeOffset = 0x0120;
            public const int PropertyTagFreeByteCounts = 0x0121;
            public const int PropertyTagGrayResponseUnit = 0x0122;
            public const int PropertyTagGrayResponseCurve = 0x0123;
            public const int PropertyTagT4Option = 0x0124;
            public const int PropertyTagT6Option = 0x0125;
            public const int PropertyTagResolutionUnit = 0x0128;
            public const int PropertyTagPageNumber = 0x0129;
            public const int PropertyTagTransferFuncition = 0x012D; // Deliberate typo to match GDI+.
            public const int PropertyTagSoftwareUsed = 0x0131;
            public const int PropertyTagDateTime = 0x0132;
            public const int PropertyTagArtist = 0x013B;
            public const int PropertyTagHostComputer = 0x013C;
            public const int PropertyTagPredictor = 0x013D;
            public const int PropertyTagWhitePoint = 0x013E;
            public const int PropertyTagPrimaryChromaticities = 0x013F;
            public const int PropertyTagColorMap = 0x0140;
            public const int PropertyTagHalftoneHints = 0x0141;
            public const int PropertyTagTileWidth = 0x0142;
            public const int PropertyTagTileLength = 0x0143;
            public const int PropertyTagTileOffset = 0x0144;
            public const int PropertyTagTileByteCounts = 0x0145;
            public const int PropertyTagInkSet = 0x014C;
            public const int PropertyTagInkNames = 0x014D;
            public const int PropertyTagNumberOfInks = 0x014E;
            public const int PropertyTagDotRange = 0x0150;
            public const int PropertyTagTargetPrinter = 0x0151;
            public const int PropertyTagExtraSamples = 0x0152;
            public const int PropertyTagSampleFormat = 0x0153;
            public const int PropertyTagSMinSampleValue = 0x0154;
            public const int PropertyTagSMaxSampleValue = 0x0155;
            public const int PropertyTagTransferRange = 0x0156;

            public const int PropertyTagJPEGProc = 0x0200;
            public const int PropertyTagJPEGInterFormat = 0x0201;
            public const int PropertyTagJPEGInterLength = 0x0202;
            public const int PropertyTagJPEGRestartInterval = 0x0203;
            public const int PropertyTagJPEGLosslessPredictors = 0x0205;
            public const int PropertyTagJPEGPointTransforms = 0x0206;
            public const int PropertyTagJPEGQTables = 0x0207;
            public const int PropertyTagJPEGDCTables = 0x0208;
            public const int PropertyTagJPEGACTables = 0x0209;

            public const int PropertyTagYCbCrCoefficients = 0x0211;
            public const int PropertyTagYCbCrSubsampling = 0x0212;
            public const int PropertyTagYCbCrPositioning = 0x0213;
            public const int PropertyTagREFBlackWhite = 0x0214;

            public const int PropertyTagICCProfile = 0x8773;
            public const int PropertyTagGamma = 0x0301;
            public const int PropertyTagICCProfileDescriptor = 0x0302;
            public const int PropertyTagSRGBRenderingIntent = 0x0303;

            public const int PropertyTagImageTitle = 0x0320;
            public const int PropertyTagCopyright = 0x8298;

            public const int PropertyTagResolutionXUnit = 0x5001;
            public const int PropertyTagResolutionYUnit = 0x5002;
            public const int PropertyTagResolutionXLengthUnit = 0x5003;
            public const int PropertyTagResolutionYLengthUnit = 0x5004;
            public const int PropertyTagPrintFlags = 0x5005;
            public const int PropertyTagPrintFlagsVersion = 0x5006;
            public const int PropertyTagPrintFlagsCrop = 0x5007;
            public const int PropertyTagPrintFlagsBleedWidth = 0x5008;
            public const int PropertyTagPrintFlagsBleedWidthScale = 0x5009;
            public const int PropertyTagHalftoneLPI = 0x500A;
            public const int PropertyTagHalftoneLPIUnit = 0x500B;
            public const int PropertyTagHalftoneDegree = 0x500C;
            public const int PropertyTagHalftoneShape = 0x500D;
            public const int PropertyTagHalftoneMisc = 0x500E;
            public const int PropertyTagHalftoneScreen = 0x500F;
            public const int PropertyTagJPEGQuality = 0x5010;
            public const int PropertyTagGridSize = 0x5011;
            public const int PropertyTagThumbnailFormat = 0x5012;
            public const int PropertyTagThumbnailWidth = 0x5013;
            public const int PropertyTagThumbnailHeight = 0x5014;
            public const int PropertyTagThumbnailColorDepth = 0x5015;
            public const int PropertyTagThumbnailPlanes = 0x5016;
            public const int PropertyTagThumbnailRawBytes = 0x5017;
            public const int PropertyTagThumbnailSize = 0x5018;
            public const int PropertyTagThumbnailCompressedSize = 0x5019;
            public const int PropertyTagColorTransferFunction = 0x501A;
            public const int PropertyTagThumbnailData = 0x501B;

            public const int PropertyTagThumbnailImageWidth = 0x5020;
            public const int PropertyTagThumbnailImageHeight = 0x5021;
            public const int PropertyTagThumbnailBitsPerSample = 0x5022;
            public const int PropertyTagThumbnailCompression = 0x5023;
            public const int PropertyTagThumbnailPhotometricInterp = 0x5024;
            public const int PropertyTagThumbnailImageDescription = 0x5025;
            public const int PropertyTagThumbnailEquipMake = 0x5026;
            public const int PropertyTagThumbnailEquipModel = 0x5027;
            public const int PropertyTagThumbnailStripOffsets = 0x5028;
            public const int PropertyTagThumbnailOrientation = 0x5029;
            public const int PropertyTagThumbnailSamplesPerPixel = 0x502A;
            public const int PropertyTagThumbnailRowsPerStrip = 0x502B;
            public const int PropertyTagThumbnailStripBytesCount = 0x502C;
            public const int PropertyTagThumbnailResolutionX = 0x502D;
            public const int PropertyTagThumbnailResolutionY = 0x502E;
            public const int PropertyTagThumbnailPlanarConfig = 0x502F;
            public const int PropertyTagThumbnailResolutionUnit = 0x5030;
            public const int PropertyTagThumbnailTransferFunction = 0x5031;
            public const int PropertyTagThumbnailSoftwareUsed = 0x5032;
            public const int PropertyTagThumbnailDateTime = 0x5033;
            public const int PropertyTagThumbnailArtist = 0x5034;
            public const int PropertyTagThumbnailWhitePoint = 0x5035;
            public const int PropertyTagThumbnailPrimaryChromaticities = 0x5036 ;
            public const int PropertyTagThumbnailYCbCrCoefficients = 0x5037;
            public const int PropertyTagThumbnailYCbCrSubsampling=  0x5038;
            public const int PropertyTagThumbnailYCbCrPositioning = 0x5039;
            public const int PropertyTagThumbnailRefBlackWhite = 0x503A;
            public const int PropertyTagThumbnailCopyRight = 0x503B;

            public const int PropertyTagLuminanceTable = 0x5090;
            public const int PropertyTagChrominanceTable = 0x5091;

            public const int PropertyTagFrameDelay = 0x5100;
            public const int PropertyTagLoopCount = 0x5101;

            public const int PropertyTagGlobalPalette=0x5102;
            public const int PropertyTagIndexBackground=0x5103;
            public const int PropertyTagIndexTransparent=0x5104;

            public const int PropertyTagPixelUnit = 0x5110;
            public const int PropertyTagPixelPerUnitX = 0x5111;
            public const int PropertyTagPixelPerUnitY = 0x5112;
            public const int PropertyTagPaletteHistogram = 0x5113;

            public const int PropertyTagExifExposureTime = 0x829A;
            public const int PropertyTagExifFNumber = 0x829D;

            public const int PropertyTagExifExposureProg = 0x8822;
            public const int PropertyTagExifSpectralSense = 0x8824;
            public const int PropertyTagExifISOSpeed = 0x8827;
            public const int PropertyTagExifOECF = 0x8828;

            public const int PropertyTagExifVer = 0x9000;
            public const int PropertyTagExifDTOrig = 0x9003;
            public const int PropertyTagExifDTDigitized = 0x9004;

            public const int PropertyTagExifCompConfig = 0x9101;
            public const int PropertyTagExifCompBPP = 0x9102;

            public const int PropertyTagExifShutterSpeed = 0x9201;
            public const int PropertyTagExifAperture = 0x9202;
            public const int PropertyTagExifBrightness = 0x9203;
            public const int PropertyTagExifExposureBias = 0x9204;
            public const int PropertyTagExifMaxAperture = 0x9205;
            public const int PropertyTagExifSubjectDist = 0x9206;
            public const int PropertyTagExifMeteringMode = 0x9207;
            public const int PropertyTagExifLightSource = 0x9208;
            public const int PropertyTagExifFlash = 0x9209;
            public const int PropertyTagExifFocalLength = 0x920A;
            public const int PropertyTagExifSubjectArea = 0x9214;
            public const int PropertyTagExifMakerNote = 0x927C;
            public const int PropertyTagExifUserComment = 0x9286;
            public const int PropertyTagExifDTSubsec = 0x9290;
            public const int PropertyTagExifDTOrigSS = 0x9291;
            public const int PropertyTagExifDTDigSS = 0x9292;

            public const int PropertyTagExifFPXVer = 0xA000;
            public const int PropertyTagExifColorSpace = 0xA001;
            public const int PropertyTagExifPixXDim = 0xA002;
            public const int PropertyTagExifPixYDim = 0xA003;
            public const int PropertyTagExifRelatedWav = 0xA004;
            public const int PropertyTagExifInterop = 0xA005;
            public const int PropertyTagExifFlashEnergy = 0xA20B;
            public const int PropertyTagExifSpatialFR = 0xA20C;
            public const int PropertyTagExifFocalXRes = 0xA20E;
            public const int PropertyTagExifFocalYRes = 0xA20F;
            public const int PropertyTagExifFocalResUnit = 0xA210;
            public const int PropertyTagExifSubjectLoc = 0xA214;
            public const int PropertyTagExifExposureIndex = 0xA215;
            public const int PropertyTagExifSensingMethod = 0xA217;
            public const int PropertyTagExifFileSource = 0xA300;
            public const int PropertyTagExifSceneType = 0xA301;
            public const int PropertyTagExifCfaPattern = 0xA302;

            public const int PropertyTagExifCustomRendered = 0xA401;
            public const int PropertyTagExifExposureMode = 0xA402;
            public const int PropertyTagExifWhiteBalance = 0xA403;
            public const int PropertyTagExifDigitalZoomRatio = 0xA404;
            public const int PropertyTagExifFocalLengthIn35mmFilm = 0xA405;
            public const int PropertyTagExifSceneCaptureType = 0xA406;
            public const int PropertyTagExifGainControl = 0xA407;
            public const int PropertyTagExifContrast = 0xA408;
            public const int PropertyTagExifSaturation = 0xA409;
            public const int PropertyTagExifSharpness = 0xA40A;
            public const int PropertyTagExifDeviceSettingDesc = 0xA40B;
            public const int PropertyTagExifSubjectDistanceRange = 0xA40C;
            public const int PropertyTagExifUniqueImageID = 0xA420;

            public const int PropertyTagGpsVer = 0x0000;
            public const int PropertyTagGpsLatitudeRef = 0x0001;
            public const int PropertyTagGpsLatitude = 0x0002;
            public const int PropertyTagGpsLongitudeRef = 0x0003;
            public const int PropertyTagGpsLongitude = 0x0004;
            public const int PropertyTagGpsAltitudeRef = 0x0005;
            public const int PropertyTagGpsAltitude = 0x0006;
            public const int PropertyTagGpsGpsTime = 0x0007;
            public const int PropertyTagGpsGpsSatellites = 0x0008;
            public const int PropertyTagGpsGpsStatus = 0x0009;
            public const int PropertyTagGpsGpsMeasureMode = 0x00A;
            public const int PropertyTagGpsGpsDop = 0x000B;
            public const int PropertyTagGpsSpeedRef = 0x000C;
            public const int PropertyTagGpsSpeed = 0x000D;
            public const int PropertyTagGpsTrackRef = 0x000E;
            public const int PropertyTagGpsTrack = 0x000F;
            public const int PropertyTagGpsImgDirRef = 0x0010;
            public const int PropertyTagGpsImgDir = 0x0011;
            public const int PropertyTagGpsMapDatum = 0x0012;
            public const int PropertyTagGpsDestLatRef = 0x0013;
            public const int PropertyTagGpsDestLat = 0x0014;
            public const int PropertyTagGpsDestLongRef = 0x0015;
            public const int PropertyTagGpsDestLong = 0x0016;
            public const int PropertyTagGpsDestBearRef = 0x0017;
            public const int PropertyTagGpsDestBear = 0x0018;
            public const int PropertyTagGpsDestDistRef = 0x0019;
            public const int PropertyTagGpsDestDist = 0x001A;
            public const int PropertyTagGpsProcessingMethod = 0x001B;
            public const int PropertyTagGpsAreaInformation = 0x001C;
            public const int PropertyTagGpsDate = 0x001D;
            public const int PropertyTagGpsDifferential = 0x001E;
        }

        #endregion
    }
}

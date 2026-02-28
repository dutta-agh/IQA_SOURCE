using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using IQA_SOURCE.Models.Admin;
using System.Text.Json;
using ImageMetadata = IQA_SOURCE.Models.Admin.ImageMetadata;

namespace IQA_SOURCE.Services
{
    public class ImageMetadataService : IImageMetadataService
    {
        public async Task<ImageMetadata> ExtractMetadata(string filePath)
        {
            try
            {
                using var image = await Image.LoadAsync(filePath);
                var metadata = new ImageMetadata
                {
                    Width = image.Width,
                    Height = image.Height,
                    Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
                    ExifData = new Dictionary<string, object>()
                };

                // Extract resolution
                if (image.Metadata.HorizontalResolution > 0)
                    metadata.DpiX = (decimal)image.Metadata.HorizontalResolution;
                if (image.Metadata.VerticalResolution > 0)
                    metadata.DpiY = (decimal)image.Metadata.VerticalResolution;

                // Extract EXIF data
                var exifProfile = image.Metadata.ExifProfile;
                if (exifProfile != null)
                {
                    ExtractExifData(exifProfile, metadata);
                }

                // Extract pixel format information
                ExtractPixelFormatInfo(image, metadata);

                return metadata;
            }
            catch (Exception ex)
            {
                // Return basic metadata even if EXIF extraction fails
                return new ImageMetadata
                {
                    ExifData = new Dictionary<string, object>
                    {
                        { "Error", $"Failed to extract metadata: {ex.Message}" }
                    }
                };
            }
        }

        private void ExtractExifData(ExifProfile exifProfile, ImageMetadata metadata)
        {
            // Camera Make
            if (exifProfile.TryGetValue(ExifTag.Make, out var make) && make?.Value != null)
            {
                metadata.CameraMake = Convert.ToString(make.Value)?.Trim();
                metadata.ExifData["Make"] = metadata.CameraMake;
            }

            // Camera Model
            if (exifProfile.TryGetValue(ExifTag.Model, out var model) && model?.Value != null)
            {
                metadata.CameraModel = Convert.ToString(model.Value)?.Trim();
                metadata.ExifData["Model"] = metadata.CameraModel;
            }

            // Lens Model
            if (exifProfile.TryGetValue(ExifTag.LensModel, out var lensModel) && lensModel?.Value != null)
            {
                metadata.LensModel = Convert.ToString(lensModel.Value)?.Trim();
                metadata.ExifData["LensModel"] = metadata.LensModel;
            }

            // Focal Length
            if (exifProfile.TryGetValue(ExifTag.FocalLength, out var focalLength) && focalLength?.Value != null)
            {
                try
                {
                    if (focalLength.Value is Rational rational)
                    {
                        metadata.FocalLength = (decimal)rational.ToDouble();
                        metadata.ExifData["FocalLength"] = Convert.ToString(metadata.FocalLength);
                    }
                }
                catch
                {
                    metadata.ExifData["FocalLength"] = Convert.ToString(focalLength.Value);
                }
            }

            // Aperture (F-Number)
            if (exifProfile.TryGetValue(ExifTag.FNumber, out var fNumber) && fNumber?.Value != null)
            {
                try
                {
                    if (fNumber.Value is Rational fRational)
                    {
                        metadata.Aperture = $"f/{fRational.ToDouble():F1}";
                        metadata.ExifData["Aperture"] = metadata.Aperture;
                    }
                    else
                    {
                        metadata.ExifData["Aperture"] = Convert.ToString(fNumber.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["Aperture"] = Convert.ToString(fNumber.Value);
                }
            }

            // Shutter Speed (Exposure Time)
            if (exifProfile.TryGetValue(ExifTag.ExposureTime, out var exposureTime) && exposureTime?.Value != null)
            {
                try
                {
                    if (exposureTime.Value is Rational expRational)
                    {
                        var seconds = expRational.ToDouble();
                        metadata.ShutterSpeed = seconds >= 1 
                            ? $"{seconds}s" 
                            : $"1/{(int)(1 / seconds)}s";
                        metadata.ExifData["ShutterSpeed"] = metadata.ShutterSpeed;
                    }
                    else
                    {
                        metadata.ExifData["ShutterSpeed"] = Convert.ToString(exposureTime.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["ShutterSpeed"] = Convert.ToString(exposureTime.Value);
                }
            }

            // ISO
            if (exifProfile.TryGetValue(ExifTag.ISOSpeedRatings, out var iso) && iso?.Value != null)
            {
                try
                {
                    if (iso.Value is ushort[] isoArray && isoArray.Length > 0)
                    {
                        metadata.Iso = isoArray[0];
                        metadata.ExifData["ISO"] = Convert.ToString(metadata.Iso);
                    }
                    else
                    {
                        metadata.ExifData["ISO"] = Convert.ToString(iso.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["ISO"] = Convert.ToString(iso.Value);
                }
            }

            // Flash
            if (exifProfile.TryGetValue(ExifTag.Flash, out var flash) && flash?.Value != null)
            {
                metadata.Flash = Convert.ToString(flash.Value);
                metadata.ExifData["Flash"] = metadata.Flash;
            }

            // Exposure Mode
            if (exifProfile.TryGetValue(ExifTag.ExposureMode, out var exposureMode) && exposureMode?.Value != null)
            {
                metadata.ExposureMode = Convert.ToString(exposureMode.Value);
                metadata.ExifData["ExposureMode"] = metadata.ExposureMode;
            }

            // White Balance
            if (exifProfile.TryGetValue(ExifTag.WhiteBalance, out var whiteBalance) && whiteBalance?.Value != null)
            {
                metadata.WhiteBalance = Convert.ToString(whiteBalance.Value);
                metadata.ExifData["WhiteBalance"] = metadata.WhiteBalance;
            }

            // Date Taken
            if (exifProfile.TryGetValue(ExifTag.DateTimeOriginal, out var dateTaken) && dateTaken?.Value != null)
            {
                try
                {
                    var dateString = Convert.ToString(dateTaken.Value);
                    if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out var dateTime))
                    {
                        metadata.DateTaken = dateTime;
                        metadata.ExifData["DateTaken"] = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        metadata.ExifData["DateTaken"] = dateString;
                    }
                }
                catch
                {
                    metadata.ExifData["DateTaken"] = Convert.ToString(dateTaken.Value);
                }
            }

            // Orientation
            if (exifProfile.TryGetValue(ExifTag.Orientation, out var orientation) && orientation?.Value != null)
            {
                try
                {
                    if (orientation.Value is ushort orientValue)
                    {
                        metadata.Orientation = orientValue;
                        metadata.ExifData["Orientation"] = Convert.ToString(metadata.Orientation);
                    }
                    else
                    {
                        metadata.ExifData["Orientation"] = Convert.ToString(orientation.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["Orientation"] = Convert.ToString(orientation.Value);
                }
            }

            // Color Space
            if (exifProfile.TryGetValue(ExifTag.ColorSpace, out var colorSpace) && colorSpace?.Value != null)
            {
                metadata.ColorSpace = Convert.ToString(colorSpace.Value);
                metadata.ExifData["ColorSpace"] = metadata.ColorSpace;
            }

            // Compression (JPEG Quality)
            if (exifProfile.TryGetValue(ExifTag.Compression, out var compression) && compression?.Value != null)
            {
                try
                {
                    if (compression.Value is ushort compValue)
                    {
                        metadata.CompressionQuality = compValue;
                        metadata.ExifData["Compression"] = Convert.ToString(metadata.CompressionQuality);
                    }
                    else
                    {
                        metadata.ExifData["Compression"] = Convert.ToString(compression.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["Compression"] = Convert.ToString(compression.Value);
                }
            }

            // Software
            if (exifProfile.TryGetValue(ExifTag.Software, out var software) && software?.Value != null)
            {
                metadata.ExifData["Software"] = Convert.ToString(software.Value);
            }

            // Copyright
            if (exifProfile.TryGetValue(ExifTag.Copyright, out var copyright) && copyright?.Value != null)
            {
                metadata.ExifData["Copyright"] = Convert.ToString(copyright.Value);
            }

            // Artist
            if (exifProfile.TryGetValue(ExifTag.Artist, out var artist) && artist?.Value != null)
            {
                metadata.ExifData["Artist"] = Convert.ToString(artist.Value);
            }

            // GPS Info
            ExtractGpsData(exifProfile, metadata);

            // Additional EXIF tags
            ExtractAdditionalExifData(exifProfile, metadata);
        }

        private void ExtractGpsData(ExifProfile exifProfile, ImageMetadata metadata)
        {
            var gpsData = new Dictionary<string, object>();

            try
            {
                if (exifProfile.TryGetValue(ExifTag.GPSLatitude, out var latitude) && latitude?.Value != null &&
                    exifProfile.TryGetValue(ExifTag.GPSLatitudeRef, out var latRef) && latRef?.Value != null)
                {
                    gpsData["Latitude"] = $"{Convert.ToString(latitude.Value)} {Convert.ToString(latRef.Value)}";
                }

                if (exifProfile.TryGetValue(ExifTag.GPSLongitude, out var longitude) && longitude?.Value != null &&
                    exifProfile.TryGetValue(ExifTag.GPSLongitudeRef, out var lonRef) && lonRef?.Value != null)
                {
                    gpsData["Longitude"] = $"{Convert.ToString(longitude.Value)} {Convert.ToString(lonRef.Value)}";
                }

                if (exifProfile.TryGetValue(ExifTag.GPSAltitude, out var altitude) && altitude?.Value != null)
                {
                    gpsData["Altitude"] = Convert.ToString(altitude.Value);
                }

                if (gpsData.Count > 0)
                {
                    metadata.ExifData["GPS"] = gpsData;
                }
            }
            catch
            {
                // GPS data extraction failed, continue without it
            }
        }

        private void ExtractAdditionalExifData(ExifProfile exifProfile, ImageMetadata metadata)
        {
            // Exposure Program
            if (exifProfile.TryGetValue(ExifTag.ExposureProgram, out var exposureProgram) && exposureProgram?.Value != null)
            {
                metadata.ExifData["ExposureProgram"] = Convert.ToString(exposureProgram.Value);
            }

            // Metering Mode
            if (exifProfile.TryGetValue(ExifTag.MeteringMode, out var meteringMode) && meteringMode?.Value != null)
            {
                metadata.ExifData["MeteringMode"] = Convert.ToString(meteringMode.Value);
            }

            // Scene Capture Type
            if (exifProfile.TryGetValue(ExifTag.SceneCaptureType, out var sceneType) && sceneType?.Value != null)
            {
                metadata.ExifData["SceneCaptureType"] = Convert.ToString(sceneType.Value);
            }

            // Contrast
            if (exifProfile.TryGetValue(ExifTag.Contrast, out var contrast) && contrast?.Value != null)
            {
                metadata.ExifData["Contrast"] = Convert.ToString(contrast.Value);
            }

            // Saturation
            if (exifProfile.TryGetValue(ExifTag.Saturation, out var saturation) && saturation?.Value != null)
            {
                metadata.ExifData["Saturation"] = Convert.ToString(saturation.Value);
            }

            // Sharpness
            if (exifProfile.TryGetValue(ExifTag.Sharpness, out var sharpness) && sharpness?.Value != null)
            {
                metadata.ExifData["Sharpness"] = Convert.ToString(sharpness.Value);
            }

            // Digital Zoom Ratio
            if (exifProfile.TryGetValue(ExifTag.DigitalZoomRatio, out var digitalZoom) && digitalZoom?.Value != null)
            {
                try
                {
                    if (digitalZoom.Value is Rational dzRational)
                    {
                        metadata.ExifData["DigitalZoomRatio"] = Convert.ToString(dzRational.ToDouble());
                    }
                    else
                    {
                        metadata.ExifData["DigitalZoomRatio"] = Convert.ToString(digitalZoom.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["DigitalZoomRatio"] = Convert.ToString(digitalZoom.Value);
                }
            }

            // Exposure Bias
            if (exifProfile.TryGetValue(ExifTag.ExposureBiasValue, out var exposureBias) && exposureBias?.Value != null)
            {
                try
                {
                    // Fix: Handle both Rational and SignedRational
                    if (exposureBias.Value is SignedRational ebSignedRational)
                    {
                        metadata.ExifData["ExposureBias"] = Convert.ToString(ebSignedRational.ToDouble());
                    }
                    else
                    {
                        metadata.ExifData["ExposureBias"] = Convert.ToString(exposureBias.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["ExposureBias"] = Convert.ToString(exposureBias.Value);
                }
            }

            // Max Aperture
            if (exifProfile.TryGetValue(ExifTag.MaxApertureValue, out var maxAperture) && maxAperture?.Value != null)
            {
                try
                {
                    if (maxAperture.Value is Rational maRational)
                    {
                        metadata.ExifData["MaxAperture"] = Convert.ToString(maRational.ToDouble());
                    }
                    else
                    {
                        metadata.ExifData["MaxAperture"] = Convert.ToString(maxAperture.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["MaxAperture"] = Convert.ToString(maxAperture.Value);
                }
            }

            // Subject Distance
            if (exifProfile.TryGetValue(ExifTag.SubjectDistance, out var subjectDistance) && subjectDistance?.Value != null)
            {
                try
                {
                    if (subjectDistance.Value is Rational sdRational)
                    {
                        metadata.ExifData["SubjectDistance"] = Convert.ToString(sdRational.ToDouble());
                    }
                    else
                    {
                        metadata.ExifData["SubjectDistance"] = Convert.ToString(subjectDistance.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["SubjectDistance"] = Convert.ToString(subjectDistance.Value);
                }
            }

            // Light Source
            if (exifProfile.TryGetValue(ExifTag.LightSource, out var lightSource) && lightSource?.Value != null)
            {
                metadata.ExifData["LightSource"] = Convert.ToString(lightSource.Value);
            }

            // Brightness Value
            if (exifProfile.TryGetValue(ExifTag.BrightnessValue, out var brightness) && brightness?.Value != null)
            {
                try
                {
                    if (brightness.Value is SignedRational bRational)
                    {
                        metadata.ExifData["BrightnessValue"] = Convert.ToString(bRational.ToDouble());
                    }
                    else
                    {
                        metadata.ExifData["BrightnessValue"] = Convert.ToString(brightness.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["BrightnessValue"] = Convert.ToString(brightness.Value);
                }
            }

            // Image Description
            if (exifProfile.TryGetValue(ExifTag.ImageDescription, out var imageDesc) && imageDesc?.Value != null)
            {
                metadata.ExifData["ImageDescription"] = Convert.ToString(imageDesc.Value);
            }

            // User Comment
            if (exifProfile.TryGetValue(ExifTag.UserComment, out var userComment) && userComment?.Value != null)
            {
                metadata.ExifData["UserComment"] = Convert.ToString(userComment.Value);
            }

            // Sensitivity Type
            if (exifProfile.TryGetValue(ExifTag.SensitivityType, out var sensitivityType) && sensitivityType?.Value != null)
            {
                metadata.ExifData["SensitivityType"] = Convert.ToString(sensitivityType.Value);
            }

            // Lens Make
            if (exifProfile.TryGetValue(ExifTag.LensMake, out var lensMake) && lensMake?.Value != null)
            {
                metadata.ExifData["LensMake"] = Convert.ToString(lensMake.Value);
            }

            // Lens Serial Number
            if (exifProfile.TryGetValue(ExifTag.LensSerialNumber, out var lensSerial) && lensSerial?.Value != null)
            {
                metadata.ExifData["LensSerialNumber"] = Convert.ToString(lensSerial.Value);
            }

            // Body Serial Number
            if (exifProfile.TryGetValue(ExifTag.SerialNumber, out var bodySerial) && bodySerial?.Value != null)
            {
                metadata.ExifData["BodySerialNumber"] = Convert.ToString(bodySerial.Value);
            }

            // Resolution Unit
            if (exifProfile.TryGetValue(ExifTag.ResolutionUnit, out var resUnit) && resUnit?.Value != null)
            {
                metadata.ExifData["ResolutionUnit"] = Convert.ToString(resUnit.Value);
            }

            // X Resolution
            if (exifProfile.TryGetValue(ExifTag.XResolution, out var xRes) && xRes?.Value != null)
            {
                try
                {
                    if (xRes.Value is Rational xRational)
                    {
                        metadata.ExifData["XResolution"] = Convert.ToString(xRational.ToDouble());
                    }
                    else
                    {
                        metadata.ExifData["XResolution"] = Convert.ToString(xRes.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["XResolution"] = Convert.ToString(xRes.Value);
                }
            }

            // Y Resolution
            if (exifProfile.TryGetValue(ExifTag.YResolution, out var yRes) && yRes?.Value != null)
            {
                try
                {
                    if (yRes.Value is Rational yRational)
                    {
                        metadata.ExifData["YResolution"] = Convert.ToString(yRational.ToDouble());
                    }
                    else
                    {
                        metadata.ExifData["YResolution"] = Convert.ToString(yRes.Value);
                    }
                }
                catch
                {
                    metadata.ExifData["YResolution"] = Convert.ToString(yRes.Value);
                }
            }
        }

        private void ExtractPixelFormatInfo(Image image, ImageMetadata metadata)
        {
            try
            {
                // Try to determine bit depth from pixel format
                var pixelType = image.PixelType;
                
                if (pixelType.BitsPerPixel > 0)
                {
                    metadata.BitDepth = pixelType.BitsPerPixel;
                    metadata.ExifData["BitsPerPixel"] = Convert.ToString(metadata.BitDepth);
                }

                metadata.ExifData["PixelFormat"] = Convert.ToString(pixelType);
            }
            catch
            {
                // Pixel format extraction failed, continue without it
            }
        }
    }
}
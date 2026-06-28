using OpenCvSharp;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CvRect = OpenCvSharp.Rect;
using CvSize = OpenCvSharp.Size;

namespace DaerahRawanBanjir;

public static class FaceService
{
    private const int TemplateSize = 100;
    private const string CascadeFileName = "haarcascade_frontalface_default.xml";

    public static async Task<SKBitmap?> CreateTemplateFromFileAsync(
        Microsoft.Maui.Storage.FileResult? file)
    {
        if (file is null)
        {
            return null;
        }

        using Stream stream = await file.OpenReadAsync();
        using MemoryStream memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream);

        byte[] imageBytes = memoryStream.ToArray();

        using Mat sourceImage = Cv2.ImDecode(imageBytes, ImreadModes.Color);

        if (sourceImage.Empty())
        {
            return null;
        }

        Mat? faceTemplate = await CreateFaceTemplateWithOpenCvAsync(sourceImage);

        if (faceTemplate == null || faceTemplate.Empty())
        {
            faceTemplate?.Dispose();
            return null;
        }

        SKBitmap result = ConvertMatToSkBitmap(faceTemplate);

        faceTemplate.Dispose();

        return result;
    }

    public static double CalculateSimilarity(SKBitmap registeredFace, SKBitmap currentFace)
    {
        using Mat registeredMat = ConvertSkBitmapToMat(registeredFace);
        using Mat currentMat = ConvertSkBitmapToMat(currentFace);

        using Mat registeredTemplate = NormalizeFaceForComparison(registeredMat);
        using Mat currentTemplate = NormalizeFaceForComparison(currentMat);

        if (registeredTemplate.Empty() || currentTemplate.Empty())
        {
            return 0;
        }

        using Mat difference = new Mat();
        Cv2.Absdiff(registeredTemplate, currentTemplate, difference);

        Scalar meanDifference = Cv2.Mean(difference);

        double pixelSimilarity = 100.0 - ((meanDifference.Val0 / 255.0) * 100.0);

        using Mat matchResult = new Mat();

        Cv2.MatchTemplate(
            registeredTemplate,
            currentTemplate,
            matchResult,
            TemplateMatchModes.CCoeffNormed
        );

        Cv2.MinMaxLoc(
            matchResult,
            out _,
            out double maxCorrelation,
            out _,
            out _
        );

        double correlationSimilarity = ((maxCorrelation + 1.0) / 2.0) * 100.0;

        double finalSimilarity =
            (pixelSimilarity * 0.55) +
            (correlationSimilarity * 0.45);

        return Math.Clamp(finalSimilarity, 0, 100);
    }

    private static async Task<Mat?> CreateFaceTemplateWithOpenCvAsync(Mat sourceImage)
    {
        string cascadePath = await GetCascadeFilePathAsync();

        using CascadeClassifier faceCascade = new CascadeClassifier(cascadePath);

        if (faceCascade.Empty())
        {
            return null;
        }

        using Mat grayImage = new Mat();

        Cv2.CvtColor(sourceImage, grayImage, ColorConversionCodes.BGR2GRAY);
        Cv2.EqualizeHist(grayImage, grayImage);

        CvRect[] detectedFaces = faceCascade.DetectMultiScale(
            image: grayImage,
            scaleFactor: 1.1,
            minNeighbors: 5,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: new CvSize(60, 60)
        );

        if (detectedFaces.Length == 0)
        {
            return null;
        }

        CvRect largestFace = detectedFaces
            .OrderByDescending(face => face.Width * face.Height)
            .First();

        CvRect safeFaceArea = MakeSafeFaceArea(
            largestFace,
            grayImage.Width,
            grayImage.Height
        );

        using Mat faceRegion = new Mat(grayImage, safeFaceArea);

        Mat resizedFace = new Mat();

        Cv2.Resize(
            faceRegion,
            resizedFace,
            new CvSize(TemplateSize, TemplateSize)
        );

        Cv2.EqualizeHist(resizedFace, resizedFace);

        return resizedFace;
    }

    private static async Task<string> GetCascadeFilePathAsync()
    {
        string folderPath = Path.Combine(
            Microsoft.Maui.Storage.FileSystem.AppDataDirectory,
            "opencv"
        );

        Directory.CreateDirectory(folderPath);

        string targetPath = Path.Combine(
            folderPath,
            CascadeFileName
        );

        if (File.Exists(targetPath))
        {
            return targetPath;
        }

        using Stream cascadeStream =
            await Microsoft.Maui.Storage.FileSystem.OpenAppPackageFileAsync(CascadeFileName);

        using FileStream fileStream = File.Create(targetPath);

        await cascadeStream.CopyToAsync(fileStream);

        return targetPath;
    }

    private static CvRect MakeSafeFaceArea(CvRect face, int imageWidth, int imageHeight)
    {
        int paddingX = (int)(face.Width * 0.15);
        int paddingY = (int)(face.Height * 0.20);

        int x = Math.Max(face.X - paddingX, 0);
        int y = Math.Max(face.Y - paddingY, 0);

        int right = Math.Min(face.X + face.Width + paddingX, imageWidth);
        int bottom = Math.Min(face.Y + face.Height + paddingY, imageHeight);

        int width = Math.Max(right - x, 1);
        int height = Math.Max(bottom - y, 1);

        return new CvRect(x, y, width, height);
    }

    private static Mat NormalizeFaceForComparison(Mat source)
    {
        using Mat gray = new Mat();

        if (source.Channels() == 1)
        {
            source.CopyTo(gray);
        }
        else
        {
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
        }

        Mat resized = new Mat();

        Cv2.Resize(
            gray,
            resized,
            new CvSize(TemplateSize, TemplateSize)
        );

        Cv2.EqualizeHist(resized, resized);

        return resized;
    }

    private static SKBitmap ConvertMatToSkBitmap(Mat mat)
    {
        Cv2.ImEncode(".png", mat, out byte[] imageBytes);

        SKBitmap bitmap = SKBitmap.Decode(imageBytes);

        return bitmap;
    }

    private static Mat ConvertSkBitmapToMat(SKBitmap bitmap)
    {
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

        byte[] imageBytes = data.ToArray();

        Mat mat = Cv2.ImDecode(imageBytes, ImreadModes.Color);

        return mat;
    }

    public static SKBitmap CreateDemoFaceTemplate()
    {
        using Mat demoTemplate = new Mat(
            TemplateSize,
            TemplateSize,
            MatType.CV_8UC1,
            Scalar.All(120)
        );

        return ConvertMatToSkBitmap(demoTemplate);
    }
}
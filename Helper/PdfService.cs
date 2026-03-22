using System.IO;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using ImageMagick;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FunctionalitiesWebAPI.Helper
{
    public class PdfService
    {
        /// <summary>
        /// Splits a multi-page PDF into separate PDFs (one per page).
        /// </summary>
        public void SplitPdf(string inputPdfPath, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            using var pdfReader = new PdfReader(inputPdfPath);
            using var pdfDoc = new PdfDocument(pdfReader);

            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var outputPath = Path.Combine(outputFolder, $"page_{i}.pdf");
                using var writer = new PdfWriter(outputPath);
                using var newPdf = new PdfDocument(writer);
                pdfDoc.CopyPagesTo(i, i, newPdf);
            }
        }

        /// <summary>
        /// Converts each page of a PDF to separate PNG images.
        /// Requires Magick.NET
        /// </summary>
        public void ConvertPdfToImages(string inputPdf, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            //using var images = new MagickImageCollection(inputPdf);
            //int pageNum = 1;
            //foreach (var img in images)
            //{
            //    var outputPath = Path.Combine(outputFolder, $"page_{pageNum}.png");
            //    img.Write(outputPath);
            //    pageNum++;
            //}
        }

        /// <summary>
        /// Slices a large PNG into a grid of smaller images.
        /// </summary>
        public void SliceImage(string inputPng, int rows, int cols, string outputFolder)
        {
            Directory.CreateDirectory(outputFolder);

            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(inputPng);

            int width = image.Width / cols;
            int height = image.Height / rows;

            int count = 1;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var crop = image.Clone(ctx => ctx.Crop(new Rectangle(x * width, y * height, width, height)));
                    crop.Save(Path.Combine(outputFolder, $"slice_{count}.png"));
                    count++;
                }
            }
        }

        /// <summary>
        /// Converts multiple images to a single PDF.
        /// </summary>
        public void ConvertImagesToPdf(List<string> imagePaths, string outputPath)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                throw new ArgumentException("No images provided");

            var directory = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
            Directory.CreateDirectory(directory);

            using var writer = new PdfWriter(outputPath);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            foreach (var path in imagePaths)
            {
                if (!File.Exists(path))
                    continue;

                using var fs = File.OpenRead(path);
                using var ms = new MemoryStream();
                fs.CopyTo(ms);
                var imageData = ImageDataFactory.Create(ms.ToArray());
                var image = new iText.Layout.Element.Image(imageData).SetAutoScale(true);

                document.Add(image);
                document.Add(new AreaBreak());
            }
        }
    }
}
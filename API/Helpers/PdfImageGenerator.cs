using PDFiumSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace API.Helpers
{
    public static class PdfImageGenerator
    {
        public static async Task<List<string>> GeneratePdfPageImagesAsync(string pdfFilePath, string baseUrl)
        {
            //create images folder
            var imageFolder = Path.Combine(Path.GetDirectoryName(pdfFilePath) ?? string.Empty, pdfFilePath.Replace(" ", "_").Replace(".pdf",""));
            if (!Directory.Exists(imageFolder))
                Directory.CreateDirectory(imageFolder);

            var imageUrls = new List<string>();

            using var document = new PdfDocument(pdfFilePath);

            for (int i = 0; i < document.Pages.Count; i++)
            {
                using var page = document.Pages[i];

                int width = (int)page.Width;
                int height = (int)page.Height;

                // Render the PDF page to a native bitmap
                using var bitmap = new PDFiumSharp.PDFiumBitmap(width, height, true);
                page.Render(bitmap);

                // Copy the bitmap buffer to a managed byte array
                var buffer = new byte[width * height * 4];
                System.Runtime.InteropServices.Marshal.Copy(bitmap.Scan0, buffer, 0, buffer.Length);

                // Load the buffer into an ImageSharp image
                using var image = Image.LoadPixelData<Bgra32>(buffer, width, height);

                // Save the image to a file (e.g., PNG)
                var fileName = $"page_{i + 1}.png";
                var outputPath = Path.Combine(imageFolder, fileName);

                await image.SaveAsPngAsync(outputPath);
                // Generate the URL for the saved image
                var imageUrl = $"{baseUrl.TrimEnd('/')}/{Path.GetFileName(pdfFilePath).Replace(" ", "_").Replace(".pdf", "")}/{fileName}";
                imageUrls.Add(imageUrl);
            }

            return imageUrls;
        }
    }
}

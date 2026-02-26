using PDFtoImage;
using SkiaSharp;

namespace Backend.BLL.ParserModule;

public class PdfToImageConverter
{
    /// <summary>
    /// Конвертирует PDF в список картинок (байты PNG) в памяти.
    /// </summary>
    public List<byte[]> GetImages(byte[] pdfBytes, int dpi = 300)
    {
        var images = new List<byte[]>();
        int pageCount = Conversion.GetPageCount(pdfBytes);

        for (int i = 0; i < pageCount; i++)
        {
            var options = new RenderOptions { Dpi = dpi };

            using SKBitmap bitmap = Conversion.ToImage(pdfBytes, new Index(i), null, options);
                
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);

            images.Add(data.ToArray());
        }

        return images;
    }
}
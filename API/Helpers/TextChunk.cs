using iText.Kernel.Font;
using iText.Kernel.Geom;

namespace API.Helpers
{
    public class TextChunk
    {
        public string Text { get; set; }
        public Rectangle Rect { get; set; }
        public PdfFont Font { get; set; }
        public float FontSize { get; set; }

        public TextChunk(string text, Rectangle rect, PdfFont font, float fontSize)
        {
            Text = text;
            Rect = rect;
            Font = font;
            FontSize = fontSize;
        }
    }
}

using iText.Forms.Fields;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System.Text.RegularExpressions;
using iText.Kernel.Font;

namespace API.Helpers
{
    public static class PdfUtilityFunction
    {
        public static string GetButtonType(PdfButtonFormField btn)
        {
            var ff = btn.GetPdfObject().GetAsNumber(PdfName.Ff);
            long fieldFlags = ff != null ? ff.LongValue() : 0;

            const int RADIO_FLAG = 1 << 15;
            const int PUSHBUTTON_FLAG = 1 << 16;

            if ((fieldFlags & PUSHBUTTON_FLAG) != 0)
                return "button";

            if ((fieldFlags & RADIO_FLAG) != 0)
                return "radio";

            return "checkbox";
        }

        public static string GenerateLabelFromKey(string rawKey)
        {
            // Step 1: Split by dot
            var segments = rawKey.Split('.');

            // Step 2: Clean each part by removing [index]
            var cleaned = segments
                .Select(s =>
                {
                    var match = Regex.Match(s, @"^([^\[]+)(?:\[(\d+)\])?$");
                    var name = match.Groups[1].Value;
                    var index = match.Groups[2].Success ? $"_{match.Groups[2].Value}" : "";
                    return (Name: name, Index: index);
                })
                .ToList();

            // Step 3: Take the last two parts
            if (cleaned.Count < 2) return ToLabel(cleaned.Last().Name + cleaned.Last().Index);

            var secondLast = cleaned[^2];
            var last = cleaned[^1];

            // Step 4: Format
            var label = $"{ToLabel(secondLast.Name)}: {ToLabel(last.Name)}{last.Index}";
            return label;
        }

        public static string ToLabel(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            input = input.Replace("_", " ");

            // Add space before capital letters
            input = Regex.Replace(input, @"(?<!^)([A-Z])", " $1");

            // Collapse multiple spaces
            return Regex.Replace(input, @"\s+", " ").Trim();
        }

        public static iText.Kernel.Geom.Rectangle GetTextCoordinates(PdfPage page, string key, int occurrence, out PdfFont? fontName, out float? fontSize)
        {
            iText.Kernel.Geom.Rectangle rectangle = new iText.Kernel.Geom.Rectangle(0, 0, 0, 0);
            fontName = null;
            fontSize = null;

            var strategy = new ChunkTrackingStrategy();
            PdfTextExtractor.GetTextFromPage(page, strategy);

            int matchOccurance = 0;

            for (int k = 0; k < strategy.Chunks.Count; k++)
            {
                if (matchOccurance == occurrence)
                    break;

                var combined = "";
                var chunks = new List<TextChunk>();

                for (int j = k; j < strategy.Chunks.Count && combined.Length < key.Length; j++)
                {
                    combined += strategy.Chunks[j].Text;
                    chunks.Add(strategy.Chunks[j]);

                    if (combined == key || combined == (key.Replace(" ", "")))
                    {
                        ++matchOccurance;
                       
                        float x = chunks.Min(c => c.Rect.GetX());
                        float y = chunks.Max(c => c.Rect.GetY());
                        // Calculate the bounding rectangle for the matched chunks
                        float left = chunks.Min(c => c.Rect.GetLeft());
                        float right = chunks.Max(c => c.Rect.GetRight());

                        float width = right - left;//chunks.Sum(c => c.Rect.GetWidth());
                        float height = chunks[0].FontSize;

                        // Get font and size from first chunk
                        var matchedFont = chunks[0].Font;
                        var matchedSize = chunks[0].FontSize;

                        fontName = matchedFont;
                        fontSize = matchedSize;

                        rectangle = new iText.Kernel.Geom.Rectangle(x, y, width, height);
                        break;
                    }
                }
            }
            return rectangle;
        }
    }
}

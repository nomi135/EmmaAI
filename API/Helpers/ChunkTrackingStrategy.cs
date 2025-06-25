using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace API.Helpers
{
    public class ChunkTrackingStrategy : ITextExtractionStrategy
    {
        public List<TextChunk> Chunks { get; } = new();

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type != EventType.RENDER_TEXT) return;

            var renderInfo = (TextRenderInfo)data;
            var text = renderInfo.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            var rect = renderInfo.GetDescentLine().GetBoundingRectangle();
            var font = renderInfo.GetFont();
            var fontSize = renderInfo.GetFontSize();

            Chunks.Add(new TextChunk(text, rect, font, fontSize));
        }

        public ISet<EventType> GetSupportedEvents() => null!;
        public void BeginTextBlock() { }
        public void EndTextBlock() { }
        public string GetResultantText() => string.Join("", Chunks.Select(c => c.Text));

        ICollection<EventType> IEventListener.GetSupportedEvents()
        {
            return new HashSet<EventType>
            {
                EventType.RENDER_TEXT
            };
        }
    }

}

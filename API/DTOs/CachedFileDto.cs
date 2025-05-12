namespace API.DTOs
{
    public class CachedFileDto
    {
        public byte[] Content { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public string FileName { get; set; } = default!;
    }
}

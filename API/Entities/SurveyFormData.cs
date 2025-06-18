namespace API.Entities
{
    public class SurveyFormData
    {
        public int Id { get; set; }
        public required string Value { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public required string Path { get; set; }

        //Navigation properties
        public int SurveyFormDetailId { get; set; }
        public SurveyFormDetail SurveyFormDetail { get; set; } = null!;
        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
    }
}

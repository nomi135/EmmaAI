namespace API.Entities
{
    public class SurveyForm
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public ICollection<SurveyFormDetail> SurveyFormDetails { get; set; } = [];
    }
}

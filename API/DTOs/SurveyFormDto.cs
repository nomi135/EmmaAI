namespace API.DTOs
{
    public class SurveyFormDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ClientPath { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public List<SurveyFormDetailDto> SurveyFormDetails { get; set; } = [];

    }
}

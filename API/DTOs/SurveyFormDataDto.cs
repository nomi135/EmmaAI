namespace API.DTOs
{
    public class SurveyFormDataDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int SurveyFormDetailId { get; set; }
        public string Value { get; set; } = string.Empty;
        public SurveyFormDetailDto SurveyFormDetail { get; set; } = new SurveyFormDetailDto();  
        public DateTime DateCreated { get; set; }
        public string Path { get; set; } = string.Empty;
    }
}

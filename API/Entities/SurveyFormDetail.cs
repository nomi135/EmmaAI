namespace API.Entities
{
    public class SurveyFormDetail
    {
        public int Id { get; set; }
        public required string Key { get; set; }
        public required string Label { get; set; }
        public required string Type { get; set; }
        public required string PageNo { get; set; }
        public required string Left { get; set; }
        public required string Top { get; set; }
        public required string Width { get; set; }
        public required string Height { get; set; }
        public required string FontName { get; set; }
        public required string FontSize { get; set; }

        //Navigation properties
        public int SurveyFormId { get; set; }
        public SurveyForm SurveyForm { get; set; } = null!;
        public List<SurveyFormData> SurveyFormData { get; set; } = [];
    }
}
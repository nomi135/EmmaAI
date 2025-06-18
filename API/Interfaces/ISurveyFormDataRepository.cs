using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface ISurveyFormDataRepository
    {
        void AddSurveyFormData(List<SurveyFormData> surveyFormData);
        Task<List<SurveyFormDataDto>?> GetSurveyFormDataAsync(int surveyFormId, int? userId);
        void UpdateSurveyFormData(List<SurveyFormData> surveyFormData);
    }
}

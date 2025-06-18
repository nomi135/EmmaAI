using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface ISurveyFormRepository
    {
        void AddSurveyForm(SurveyForm surveyForm);
        Task<List<SurveyFormDto>> GetAllSurveyFormsAsync();
        Task<SurveyForm?> GetSurveyFormAsync(int id);
        Task<bool> CheckSurveyFormByTitleAsync(int id, string title);
        void UpdateSurveyForm(SurveyForm surveyForm);
        void DeleteSurveyForm(SurveyForm surveyForm);
    }
}

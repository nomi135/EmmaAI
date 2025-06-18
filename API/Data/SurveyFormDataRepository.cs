using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class SurveyFormDataRepository(DataContext context) : ISurveyFormDataRepository
    {
        public void AddSurveyFormData(List<SurveyFormData> surveyFormData)
        {
            context.SurveyFormData.AddRangeAsync(surveyFormData);
        }

        public async Task<List<SurveyFormDataDto>?> GetSurveyFormDataAsync(int surveyFormId, int? userId)
        {
            var query = context.SurveyFormData.AsQueryable().Where(s => s.SurveyFormDetail.SurveyFormId == surveyFormId);
            if (userId.HasValue)
            {
                query = query.Where(s => s.UserId == userId.Value);
            }
            return await query
                .Select(s => new SurveyFormDataDto
                {
                    Id = s.Id,
                    Value = s.Value,
                    DateCreated = s.Created,
                    SurveyFormDetailId = s.SurveyFormDetailId,
                    UserName = s.User.UserName.ToString(),
                    Path = s.Path,
                }).ToListAsync();
            /*
            return await context.SurveyFormData.Where(s => s.SurveyFormDetail.SurveyFormId == surveyFormId && s.UserId == userId)
                .Select(s => new SurveyFormDataDto
                {
                    Id = s.Id,
                    Value = s.Value,
                    DateCreated = s.Created,
                    SurveyFormDetailId = s.SurveyFormDetailId,
                    UserName = s.User.UserName.ToString()
                }).ToListAsync();
            */
        }

        public void UpdateSurveyFormData(List<SurveyFormData> surveyFormData)
        {
            foreach (var data in surveyFormData)
            {
                context.Entry(data).State = EntityState.Modified;
            }
        }
    }
}

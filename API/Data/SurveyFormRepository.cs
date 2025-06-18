using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using System;

namespace API.Data
{
    public class SurveyFormRepository(DataContext context, IMapper mapper) : ISurveyFormRepository
    {
        public void AddSurveyForm(SurveyForm surveyForm)
        {
            context.SurveyForms.Add(surveyForm);
        }

        public async Task<bool> CheckSurveyFormByTitleAsync(int id, string title)
        {
            if(id == 0)
            {
                return await context.SurveyForms.AnyAsync(x => x.Title.ToLower() == title.ToLower());
            }
            else
            {
                return await context.SurveyForms.Where(x => x.Title.ToLower() == title.ToLower() && x.Id != id).AnyAsync();
            }
        }

        public void DeleteSurveyForm(SurveyForm surveyForm)
        {
            context.SurveyForms.Remove(surveyForm);
        }

        public async Task<List<SurveyFormDto>> GetAllSurveyFormsAsync()
        {
            return await context.SurveyForms
                .Include(x => x.SurveyFormDetails)
                .ProjectTo<SurveyFormDto>(mapper.ConfigurationProvider).ToListAsync();
        }

        public async Task<SurveyForm?> GetSurveyFormAsync(int id)
        {
            return await context.SurveyForms.Include(f => f.SurveyFormDetails).FirstOrDefaultAsync(f => f.Id == id);
        }

        public void UpdateSurveyForm(SurveyForm surveyForm)
        {
            context.Entry(surveyForm).State = EntityState.Modified;
        }
    }
}

﻿using API.DTOs;
using API.Entities;
using AutoMapper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace API.Helpers
{
    public class AutoMapperProfiles: Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemberDto>();
            CreateMap<MemberUpdateDto, AppUser>();
            CreateMap<RegisterDto, AppUser>();
            CreateMap<ContactDto, Contact>();
            CreateMap<TextContent, AssistantMessageDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.GetType().Name)) // Extract type name
                .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text)); // Extract text
            CreateMap<ChatHistory, UserChatHistoryDto>();
            CreateMap<Reminder, ReminderDto>();
            CreateMap<ReminderDto, Reminder>();
            CreateMap<SurveyForm, SurveyFormDto>();
            CreateMap<SurveyFormDetail, SurveyFormDetailDto>();
            CreateMap<SurveyFormDto, SurveyForm>()
                .ForMember(dest => dest.SurveyFormDetails, opt => opt.MapFrom(src => src.SurveyFormDetails))
                .PreserveReferences(); // Optional for circular graphs
            CreateMap<SurveyFormDetailDto, SurveyFormDetail>();
            CreateMap<SurveyFormData, SurveyFormDataDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName));
            CreateMap<SurveyFormDataDto, SurveyFormData>()
                .ForMember(dest => dest.SurveyFormDetail, opt => opt.Ignore());
            CreateMap<string, DateOnly>().ConvertUsing(s => DateOnly.Parse(s));
        }
    }
}

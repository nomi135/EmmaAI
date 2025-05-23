﻿using API.DTOs;
using API.Entities;
using AutoMapper;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Globalization;

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
            CreateMap<string, DateOnly>().ConvertUsing(s => DateOnly.Parse(s));
        }
    }
}

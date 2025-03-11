﻿using API.Entities;

namespace API.DTOs
{
    public class MemberDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastActive { get; set; }
        public string? PrefferedLanguage { get; set; }
        public string? TimeZone { get; set; }
    }
}

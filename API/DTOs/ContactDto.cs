﻿using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class ContactDto
    {
        [Required] 
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required] 
        public string Subject { get; set; } = string.Empty;
        [Required]
        public string Message { get; set; } = string.Empty;

    }
}

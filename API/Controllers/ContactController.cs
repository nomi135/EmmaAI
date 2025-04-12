using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ContactController(IContactRepository contactRepository, IMapper mapper) : BaseApiController
    {
        [HttpPost]
        public async Task<ActionResult<ContactDto>> AddContact([FromBody]ContactDto contactDto)
        {
            var contact = mapper.Map<Contact>(contactDto);
            contactRepository.AddContact(contact);
            if (await contactRepository.SaveAllAsync())
                return NoContent();
            
            return BadRequest("Failed to submit message");
        }
    }
}

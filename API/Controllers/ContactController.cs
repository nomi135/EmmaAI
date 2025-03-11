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

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var contact = mapper.Map<Contact>(contactDto);
            contactRepository.AddContact(contact);
            if (await contactRepository.SaveAllAsync())
            {
                return Ok(mapper.Map<ContactDto>(contact));
            }
            return BadRequest("Failed to submit contact message");
        }
    }
}

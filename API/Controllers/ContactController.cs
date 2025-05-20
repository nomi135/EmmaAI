using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ContactController(IUnitOfWork unitOfWork, IMapper mapper) : BaseApiController
    {
        [HttpPost]
        public async Task<ActionResult<ContactDto>> AddContact([FromBody]ContactDto contactDto)
        {
            var contact = mapper.Map<Contact>(contactDto);
            unitOfWork.ContactRepository.AddContact(contact);
            if (await unitOfWork.Complete())
                return NoContent();
            
            return BadRequest("Failed to submit message");
        }
    }
}

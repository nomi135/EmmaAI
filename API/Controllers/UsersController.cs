using API.DTOs;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController(IUnitOfWork unitOfWork, IMapper mapper) : BaseApiController
    {

        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            var user = await unitOfWork.UserRepository.GetMemberAsync(username);

           if (user == null) return NotFound();

            return user;

        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {

            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(User.GetUsername());
            
            if (user == null) return BadRequest("Could not find user");

            if(user.FullName == memberUpdateDto.FullName && user.Country == memberUpdateDto.Country && user.City == memberUpdateDto.City && 
               user.PrefferedLanguage == memberUpdateDto.PrefferedLanguage && user.TimeZone == memberUpdateDto.TimeZone)
                return BadRequest("No Changes Detected");

            mapper.Map(memberUpdateDto, user);

            if (await unitOfWork.Complete()) return NoContent();
            
            return BadRequest("Failed to update the user");
        }

    }
}

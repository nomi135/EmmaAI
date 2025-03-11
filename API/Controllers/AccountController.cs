using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper) : BaseApiController
    {

        [HttpPost("register")]  //account/register
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.Username, null)) return BadRequest("Username is already taken");

            else if (await UserExists(null,registerDto.Email)) return BadRequest("Email is already taken");

            var user = mapper.Map<AppUser>(registerDto);
            user.UserName = registerDto.Username.ToLower();

            var result = await userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded) return BadRequest(result.Errors);

            return new UserDto
            {
                UserName = user.UserName,
                Token = await tokenService.CreateToken(user)
            };
        }

        [HttpPost("login")] //account/login
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await userManager.Users
                 .FirstOrDefaultAsync(x => x.NormalizedUserName == loginDto.Username.ToUpper());

            if (user == null || user.UserName == null) return Unauthorized("Invalid username");

            return new UserDto
            {
                UserName = user.UserName,
                Token = await tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string? username, string? email)
        {
            if (username != null)
                return await userManager.Users.AnyAsync(x => x.NormalizedUserName == username.ToUpper());
            else if (email != null)
                return await userManager.Users.AnyAsync(x => x.NormalizedEmail == email.ToUpper());
            return false;
        }
    }
}

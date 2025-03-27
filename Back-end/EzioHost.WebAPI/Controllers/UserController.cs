using System.Diagnostics;
using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class UserController(IUserService userService,ILogger<UserController> logger) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateUser([FromBody] UserCreateUpdateRequestDto userDto)
        {
            try
            {
                var user = await userService.GetUserByCondition(x => x.Email == userDto.Email || x.UserName == userDto.UserName);

                if (user is null)
                {
                    var newUser = new User()
                    {
                        Email = userDto.Email,
                        UserName = userDto.UserName,
                        FirstName = userDto.FirstName,
                        LastName = userDto.LastName
                    };
                    await userService.CreateNew(newUser);
                    return Created();
                }

                user.LastLogin = DateTime.UtcNow;
                await userService.UpdateUser(user);
                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}

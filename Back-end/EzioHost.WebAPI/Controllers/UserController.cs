using EzioHost.Core.Services.Interface;
using EzioHost.Domain.Entities;
using EzioHost.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace EzioHost.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class UserController(IUserService userService, ILogger<UserController> logger) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateUser([FromBody] UserCreateUpdateRequestDto userDto)
        {
            try
            {
                var user = await userService.GetUserByCondition(x => x.Email == userDto.Email || x.UserName == userDto.UserName || x.Id == userDto.Id);

                if (user is null)
                {
                    var newUser = new User()
                    {
                        Id = userDto.Id,
                        Email = userDto.Email,
                        UserName = userDto.UserName,
                        FirstName = userDto.FirstName,
                        LastName = userDto.LastName
                    };
                    var createUser = await userService.CreateNew(newUser);
                    userDto.Id = createUser.Id;
                    return Ok(newUser);
                }

                user.LastLogin = DateTime.UtcNow;
                await userService.UpdateUser(user);

                userDto.Id = user.Id;
                return Ok(userDto);
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}

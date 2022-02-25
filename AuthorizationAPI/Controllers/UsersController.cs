using AuthorizationAPI.Authorization;
using AuthorizationAPI.Entities;
using AuthorizationAPI.Models.Dto;
using AuthorizationAPI.Models.Users;
using AuthorizationAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("[action]")]
        public async Task<IActionResult> AuthenticateAsync(AuthenticateRequest model)
        {
            var response = await _userService.AuthenticateAsync(model);
            return Ok(response);
        }

        [Authorize(Role.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var users = await _userService.GetAll();
            return Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAsync(int id)
        {
            var currentUser = HttpContext.Items["User"] as User;
            if (id == currentUser.Id || currentUser.Role == Role.Admin)
            {
                var user = await _userService.Get(id);
                return Ok(user);
            }

            return Unauthorized(new { message = "Unauthorized" });
        }

        [Authorize(Role.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var res = await _userService.Delete(id);
            if (res)
                return Ok(res);
            return BadRequest();
        }

        [Authorize(Role.Admin)]
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody] UserDto user)
        {
            var res = await _userService.CreateUpdate(user);
            return Ok(res);
        }

        [Authorize(Role.Admin)]
        [HttpPut]
        public async Task<IActionResult> UpdateAsync([FromBody] UserDto user)
        {
            var res = await _userService.CreateUpdate(user);
            return Ok(res);
        }
    }
}

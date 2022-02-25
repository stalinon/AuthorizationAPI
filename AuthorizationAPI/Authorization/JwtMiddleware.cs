using AuthorizationAPI.Helpers;
using AuthorizationAPI.Services;
using Microsoft.Extensions.Options;

namespace AuthorizationAPI.Authorization
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthSettings _AuthSettings;

        public JwtMiddleware(RequestDelegate next, IOptions<AuthSettings> AuthSettings)
        {
            _next = next;
            _AuthSettings = AuthSettings.Value;
        }

        public async Task Invoke(HttpContext context, IUserService userService, IJwtUtils jwtUtils)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var userId = jwtUtils.ValidateJwtToken(token);
            if (userId != null)
            {
                // jwt удачно - крепим пользователя к контексту
                context.Items["User"] = userService.Get(userId.Value);
            }

            await _next(context);
        }
    }
}

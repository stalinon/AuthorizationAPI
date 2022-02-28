using AuthorizationAPI.DataAccess;
using AuthorizationAPI.Helpers;
using Microsoft.Extensions.Options;

namespace AuthorizationAPI.Authorization
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthSettings _appSettings;

        public JwtMiddleware(RequestDelegate next, IOptions<AuthSettings> appSettings)
        {
            _next = next;
            _appSettings = appSettings.Value;
        }

        public async Task Invoke(HttpContext context, DataContext dataContext, IJwtUtils jwtUtils)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var accountId = jwtUtils.ValidateJwtToken(token);
            if (accountId != null)
            {
                // attach account to context on successful jwt validation
                context.Items["Account"] = await dataContext.Accounts.FindAsync(accountId.Value);
            }

            await _next(context);
        }
    }
}

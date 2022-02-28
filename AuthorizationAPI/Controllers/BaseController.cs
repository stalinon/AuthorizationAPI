using AuthorizationAPI.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationAPI.Controllers
{
    [Controller]
    public abstract class BaseController : ControllerBase
    {
        // returns the current authenticated account (null if not logged in)
        public Account Account => HttpContext.Items["Account"] as Account;
    }
}

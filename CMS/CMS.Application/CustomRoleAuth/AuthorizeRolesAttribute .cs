using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace CMS.Application.CustomRoleAuth;

public class AuthorizeRolesAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public AuthorizeRolesAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        System.Security.Claims.ClaimsPrincipal user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? false)
        {
            context.Result = new RedirectToActionResult("Login", "Users", null);
            return;
        }

        bool hasRequiredRole = _roles.Any(role => user.IsInRole(role));
        if (!hasRequiredRole)
        {
            context.Result = new ViewResult { ViewName = "AccessDenied" };
        }
    }
}

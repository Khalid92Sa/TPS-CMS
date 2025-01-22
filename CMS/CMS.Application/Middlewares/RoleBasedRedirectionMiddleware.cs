using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMS.Application.Middlewares
{
    public class RoleBasedRedirectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RoleBasedRedirectionMiddleware(RequestDelegate next, IHttpContextAccessor httpContextAccessor)
        {
            _next = next;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task Invoke(HttpContext context, UserManager<IdentityUser> userManager)
        {
            string currentPath = context.Request.Path.ToString().ToLower();
            string queryString = context.Request.QueryString.ToString();

            if (context.Request.Method == HttpMethods.Get)
            {
                if (context.User.Identity.IsAuthenticated)
                {
                    IQueryCollection queryParams = context.Request.Query;

                    if (queryParams.ContainsKey("pageNumber"))
                    {
                        await _next(context);
                        return;
                    }

                    string sessionPath = _httpContextAccessor.HttpContext.Session.GetString("Path");

                    if (sessionPath is null || currentPath != sessionPath)
                    {
                        if (currentPath.Contains("/dashboard") || currentPath.Contains("/interviews/myinterviews"))
                        {
                            await _next(context);
                            return;
                        }

                        IdentityUser user = await userManager.GetUserAsync(context.User);
                        IList<string> roles = await userManager.GetRolesAsync(user);

                        if ((roles.Contains("General Manager") || roles.Contains("Admin") || roles.Contains("HR Manager"))
                            && (currentPath.Contains("/dashboard") || currentPath.Equals("/")))
                        {
                            context.Response.Redirect($"/dashboard{queryString}");
                            return;
                        }
                        else if ((roles.Contains("Solution Architecture") || roles.Contains("Interviewer"))
                            && (currentPath.Contains("/interviews/myInterviews") || currentPath.Equals("/")))
                        {
                            context.Response.Redirect($"/interviews/myInterviews{queryString}");
                            return;
                        }
                        else
                        {
                            _httpContextAccessor.HttpContext.Session.SetString("Path", currentPath);
                            context.Response.Redirect(currentPath + queryString);
                            return;
                        }
                    }
                }

                if (!context.User.Identity.IsAuthenticated && !currentPath.Contains("/users/login"))
                {
                    context.Response.Redirect("/users/login");
                    return;
                }

                if (currentPath.Contains("/users/logout"))
                {
                    await _next(context);
                    return;
                }
            }

            await _next(context);
        }
    }
}
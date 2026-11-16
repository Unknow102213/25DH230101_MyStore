using System.Web.Mvc;

namespace MyStore.Infrastructure
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            if (httpContext == null || !httpContext.User.Identity.IsAuthenticated) return false;
            return httpContext.Session != null && (httpContext.Session["UserRole"] as string) == "A";
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(
                new { area = "", controller = "Account", action = "Login" }));
        }
    }
}

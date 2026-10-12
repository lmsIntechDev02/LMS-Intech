using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

using System;
namespace LeaveON.Helper
{




  public class InactivityTimeoutAttribute : ActionFilterAttribute
  {
    public const string LastActivityKey = "LastActivityTimeUtc";
    public const string RememberMeKey = "RememberMe";

    private   int TimeoutMinutes = 180;

    public override void OnActionExecuting(
        ActionExecutingContext filterContext)
    {
      var context = filterContext.HttpContext;

      if (context.User?.Identity?.IsAuthenticated != true)
      {
        base.OnActionExecuting(filterContext);
        return;
      }

      string controller = filterContext.ActionDescriptor
          .ControllerDescriptor.ControllerName;

      string action = filterContext.ActionDescriptor.ActionName;

      // These actions must not trigger the inactivity redirect.
      if (controller.Equals("Account",
              StringComparison.OrdinalIgnoreCase) &&
          (action.Equals("Login",
               StringComparison.OrdinalIgnoreCase) ||
           action.Equals("Resume",
               StringComparison.OrdinalIgnoreCase) ||
           action.Equals("LogOff",
               StringComparison.OrdinalIgnoreCase)))
      {
        base.OnActionExecuting(filterContext);
        return;
      }

      // Count normal MVC page requests, not background AJAX polling.
      if (context.Request.IsAjaxRequest())
      {
        base.OnActionExecuting(filterContext);
        return;
      }

      var session = context.Session;

      if (session == null)
      {
        SignOutAndRedirect(filterContext);
        return;
      }

      object value = session[LastActivityKey];

      if (!(value is DateTime lastActivity))
      {
        // A missing timestamp requires the resume/login flow.
        HandleTimeout(filterContext);
        return;
      }
      bool rememberMe =
          context.Session?[RememberMeKey] is bool remembered
          && remembered;

      TimeoutMinutes = rememberMe == true ? 10080 : 160;

      if (DateTime.UtcNow - lastActivity >=
          TimeSpan.FromMinutes(TimeoutMinutes))
      {
        HandleTimeout(filterContext);
        return;
      }

      session[LastActivityKey] = DateTime.UtcNow;

      base.OnActionExecuting(filterContext);
    }

    private static void HandleTimeout(
        ActionExecutingContext filterContext)
    {
      var context = filterContext.HttpContext;

      bool rememberMe =
          context.Session?[RememberMeKey] is bool remembered
          && remembered;

      string requestedUrl = context.Request.RawUrl;

      // Never pass an external URL to the resume endpoint.
      var urlHelper = new UrlHelper(filterContext.RequestContext);

      if (rememberMe)
      {
        string resumeUrl = urlHelper.Action(
            "Resume",
            "Account",
            new { returnUrl = requestedUrl });

        filterContext.Result = new System.Web.Mvc.RedirectResult(resumeUrl);
        return;
      }

      SignOutAndRedirect(filterContext);
    }

    private static void SignOutAndRedirect(
        ActionExecutingContext filterContext)
    {
      var context = filterContext.HttpContext;

      context.GetOwinContext().Authentication.SignOut(
          DefaultAuthenticationTypes.ApplicationCookie);

      if (context.Session != null)
      {
        context.Session.Remove(LastActivityKey);
        context.Session.Remove(RememberMeKey);
      }

      filterContext.Result = new System.Web.Mvc.RedirectResult(
          VirtualPathUtility.ToAbsolute(
              "~/Account/Login?reason=timeout"));
    }
  }
}

using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using Owin;
using LeaveON.Models;
using LeaveON.Providers;

namespace LeaveON
{
  public partial class Startup
  {
    static Startup()
    {
      PublicClientId = "web";

      OAuthOptions = new OAuthAuthorizationServerOptions
      {
        TokenEndpointPath = new PathString("/Token"),
        AuthorizeEndpointPath = new PathString("/Account/Authorize"),
        Provider = new ApplicationOAuthProvider(PublicClientId),
        AccessTokenExpireTimeSpan = TimeSpan.FromDays(14),
        AllowInsecureHttp = true
      };
    }

    public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }
    public static string PublicClientId { get; private set; }

    public void ConfigureAuth(IAppBuilder app)
    {
      app.CreatePerOwinContext(ApplicationDbContext.Create);
      app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
      app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,

        // FIXED: Changed from /Account/Login to /Account/WindowsLogin
        // /Account/Login was causing infinite redirect loop because:
        // OWIN redirects to LoginPath → LoginPath requires auth → redirects again → loop
        // /Account/WindowsLogin is [AllowAnonymous] so OWIN can reach it safely
        // WindowsLogin then redirects to AuthLogin which triggers the Windows popup
        LoginPath = new PathString("/Account/WindowsLogin"),

        ExpireTimeSpan = TimeSpan.FromMinutes(60),
        SlidingExpiration = true,

        Provider = new CookieAuthenticationProvider
        {
          OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
              validateInterval: TimeSpan.FromMinutes(20),
              regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
        }
      });

      app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
      app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));
      app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
      app.UseOAuthBearerTokens(OAuthOptions);
    }
  }
}

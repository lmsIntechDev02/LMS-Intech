using LeaveON.Controllers;
using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace LeaveON
{
  public class MvcApplication : HttpApplication
  {
    protected void Application_Start()
    {
      AreaRegistration.RegisterAllAreas();
      GlobalConfiguration.Configure(WebApiConfig.Register);
      FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
      RouteConfig.RegisterRoutes(RouteTable.Routes);
      BundleConfig.RegisterBundles(BundleTable.Bundles);
    }


    protected void Application_BeginRequest()
    {
      if (!Request.IsLocal && !Request.IsSecureConnection)
      {
        string httpsUrl = "https://" + Request.Url.Host + Request.RawUrl;
        Response.RedirectPermanent(httpsUrl, true);
      }
    }



    protected void Application_Error(object sender, EventArgs e)
  {
    var exception = Server.GetLastError();
    // Log the exception if necessary

    Response.Clear();
    Server.ClearError();

    var routeData = new RouteData();
    routeData.Values["controller"] = "Error";
    routeData.Values["action"] = "General";
    routeData.Values["exception"] = exception;

    // Optional: Pass additional data
    if (exception is HttpException httpException)
    {
      routeData.Values["statusCode"] = httpException.GetHttpCode();
    }
    else
    {
      routeData.Values["statusCode"] = 500;
    }

    IController errorController = new ErrorController();
    errorController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
  }
}
}

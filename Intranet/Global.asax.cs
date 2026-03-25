using LeaveON.UtilityClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Serilog;
using System.Threading.Tasks;

namespace Intranet
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ConfigureLogging();
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            //new ScheduledTasks().Experiment1();
         //  new ScheduledTasks().InitTimerForScheduleTasks();
          new ScheduledTasks().SyncAppWithAD("Global Call");
           // StartBackgroundTasks();
        }

        //private static void StartBackgroundTasks()
        //{
        //    Task.Run(() =>
        //    {
        //        try
        //        {
        //            ScheduledTasks tasks = new ScheduledTasks();
        //            //tasks.InitTimerForScheduleTasks();
        //             tasks.SyncAppWithAD("Global Call");
        //        }
        //        catch (Exception ex)
        //        {
        //            new ScheduledTasks().InsertSyncLog("Global Call","error",0,0,0, ex.Message, "Global Task",null);
        //            // log error
        //        }
        //    });
        //}
        
        private void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    path: Server.MapPath("~/App_Data/logs/log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .WriteTo.Console()
                .CreateLogger();
        }

        protected void Application_End()
        {
            Log.CloseAndFlush();
        }
    }
}

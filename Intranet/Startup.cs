using Hangfire;
using LeaveON.UtilityClasses;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Intranet
{
    public class Startup
    {
        [assembly: OwinStartup(typeof(Intranet.Startup))]
        public void Configuration(IAppBuilder app)
        {
            // Configure Hangfire storage
            GlobalConfiguration.Configuration
               .UseSqlServerStorage("DefaultConnection");

            // Start Hangfire server
           // app.UseHangfireServer();

            // Enable dashboard
          // app.UseHangfireDashboard("/hangfire");

            // Schedule your AD sync job
            // comment for testing
            //RecurringJob.AddOrUpdate(
            //    "AD-Sync-Job",
            //    () => new ScheduledTasks().SyncAppWithAD("Hangfire Job"),
            //    Cron.MinuteInterval(10));   // Change schedule if needed
        }
    }
}
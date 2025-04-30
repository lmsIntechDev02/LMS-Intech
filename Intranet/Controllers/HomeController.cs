using Intranet.UtilityClasses;
using LeaveON.UtilityClasses;
using Repository.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Web.Mvc;

namespace Intranet.Controllers
{
    public class HomeController : Controller
    {
        private LeaveONEntities db = new LeaveONEntities();
        public ActionResult Index(string ReturnUrl)
        {
            GetLog();
            List<string> loginsList = new List<string>();
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
            UserPrincipal currentUser = UserPrincipal.FindByIdentity(ctx, User.Identity.Name);

            loginsList.Add(currentUser.UserPrincipalName);

            var path = Server.MapPath(@"~/myLog.txt");
            System.IO.File.AppendAllLines(path, loginsList);

            string ReturnUrlValue = "/";

            if (string.IsNullOrEmpty(ReturnUrl)) ReturnUrl = "/";

            string ADUserValue = currentUser.UserPrincipalName;


            //currently using
          return Redirect("http://lms-stage.intechww.com/Account/Login?ReturnUrl=" + ReturnUrl + "&ADUser=" + ADUserValue);//this for production *new test
          //  return Redirect("https://localhost:44380/Account/Login?ReturnUrl=" + ReturnUrl + "&ADUser=" + ADUserValue);//this for production *new test

        }
        public ActionResult Sync()
        {
            ScheduledTasks scheduledTasks = new ScheduledTasks();
            scheduledTasks.SyncAppWithAD();
            ViewBag.Message = "Your application description page.";
            
            return View();
        }

        public void GetLog()
        {
            var path = Server.MapPath(@"~/UsersAndProperties.txt");
            List<string> userprops = new List<string>();
            try
            {
                DirectoryEntry root = new DirectoryEntry("LDAP://RootDSE");
                root = new DirectoryEntry("LDAP://" + root.Properties["defaultNamingContext"][0]);
                DirectorySearcher search = new DirectorySearcher(root);
                search.Filter = "(&(objectClass=user)(objectCategory=person))";

                SearchResultCollection results = search.FindAll();
                if (results != null)
                {
                    foreach (SearchResult result in results)
                    {
                        foreach (DictionaryEntry property in result.Properties)
                        {
                            //Debug.Write(property.Key + ": ");
                            userprops.Add(property.Key + ": ");
                            foreach (var val in (property.Value as ResultPropertyValueCollection))
                            {
                                //Debug.Write(val + "; ");
                                userprops.Add(val + "; ");
                            }
                            //Debug.WriteLine("");
                            userprops.Add(Environment.NewLine + "");
                        }
                        userprops.Add(Environment.NewLine + "------------------------------------");
                    }
                }
                System.IO.File.WriteAllLines(path, userprops);
            }
            catch (Exception ex)
            {

            }
        }
         public ActionResult CheckDbConnection()
        {
            try
            {
                using (var connection = db.Database.Connection)
                {
                    connection.Open();
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        return Json(new { success = true, message = "Database connection is successful." }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Database connection failed: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, message = "Unknown error while connecting to the database." }, JsonRequestBehavior.AllowGet);
        }
    }
}
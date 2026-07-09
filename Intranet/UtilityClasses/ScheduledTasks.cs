//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;

using System;
using System.Collections.Generic;
using System.Linq;
using Repository.Models;

using System.Web.Configuration;
using System.Timers;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Reflection;
using System.Globalization;
using System.Web.Mvc;
using System.Web;
using System.IO;
using Serilog;
using Log = Serilog.Log;
using System.Threading.Tasks;
using System.Configuration;
using System.Data.SqlClient;

namespace LeaveON.UtilityClasses
{

    public class ScheduledTasks// : Controller
    {
        //static bool IsSecheduleTaskRunning = false;
        // Added this class visible variable to hold the timer interval so it's not gotten from the
        // web.config file on each Elapsed event of the timer

        private static double TimerIntervalInMilliseconds = 600000;//10min
        private static Timer _timer;
        private static readonly object _lock = new object();
        //Convert.ToDouble(WebConfigurationManager.AppSettings["TimerIntervalInMilliseconds"]);

        private LeaveONEntities db = new LeaveONEntities();

        public void InitTimerForScheduleTasks()
        {
            if (_timer == null)
            {
                lock (_lock)
                {
                    if (_timer == null)
                    {
                        _timer = new Timer(TimerIntervalInMilliseconds);
                        _timer.AutoReset = true;
                        _timer.Elapsed += timer_Elapsed;
                        _timer.Start();
                    }
                }
            }
        }
        //public void InitTimerForScheduleTasks()
        //{
        //    if (MyGlobalClass.MyGlobalBool == false)
        //    {
        //        MyGlobalClass.MyGlobalBool = true;
        //        // This will raise the Elapsed event every 'x' millisceonds (whatever you set in the
        //        // Web.Config file for the added TimerIntervalInMilliseconds AppSetting
        //        Timer timer = new Timer(TimerIntervalInMilliseconds);

        //        timer.Enabled = true;

        //        // Setup Event Handler for Timer Elapsed Event
        //        timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);

        //        timer.Start();
        //    }
        //}
        // Added the following procedure:
        //void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    // Get the TimerStartTime web.config value
        //    DateTime MyScheduledRunTime = DateTime.Parse(WebConfigurationManager.AppSettings["TimerStartTime"]);

        //    // Get the current system time
        //    DateTime CurrentSystemTime = DateTime.Now;

        //    Debug.WriteLine(string.Concat("Timer Event Handler Called: ", CurrentSystemTime.ToString()));

        //    // This makes sure your code will only run once within the time frame of (Start Time) to
        //    // (Start Time+Interval). The timer's interval and this (Start Time+Interval) must stay in sync
        //    // or your code may not run, could run once, or may run multiple times per day.
        //    DateTime LatestRunTime = MyScheduledRunTime.AddMilliseconds(TimerIntervalInMilliseconds);

        //    // If within the (Start Time) to (Start Time+Interval) time frame - run the processes
        //    if ((CurrentSystemTime.CompareTo(MyScheduledRunTime) >= 0) && (CurrentSystemTime.CompareTo(LatestRunTime) <= 0))
        //    {
        //        Debug.WriteLine(String.Concat("Timer Event Handling MyScheduledRunTime Actions: ", DateTime.Now.ToString()));
        //        // RUN YOUR PROCESSES HERE
        //        //Experiment();
        //        //Experiment1();
        //        SyncAppWithAD();

        //    }
        //}
        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                /// SyncAppWithAD("Task Scheduler call");
            }
            catch (Exception ex)
            {
                InsertSyncLog("Task Scheduler Call", "error", 0, 0, 0, ex.Message, "Task Scheduler", null);
            }

        }

        public void SyncAppWithAD(string jobName)


        {

            string filePath = Path.Combine(HttpRuntime.AppDomainAppPath, "SyncLog.txt");
            System.IO.File.AppendAllText(filePath, DateTime.Now.ToString() + Environment.NewLine);
            try
            {
                List<string> userIds = new List<string>
{
  "1128",
  "1435",
  "1427",
  "1209",
  "1757",
  "2333"

};

                using (var context = new PrincipalContext(ContextType.Domain, "intechww.com"))// "tenf.loc"))
                {
                    var userFilter = new UserPrincipal(context);

                    byte empFound = 0;
                    int counter = 0;
                    int insertedEmp = 0;
                    int UpdatedEmp = 0;
                    //List<string> loginsList = new List<string>();
                    //var path = @"D:\LeaveON - AD\Intranet\ADUserList.txt";
                    InsertSyncLog(jobName, "Start User sync Job", 0, 1, 0, "", "Insert", null);
                    using (var searcher = new PrincipalSearcher(userFilter))
                    {

                        /////////////find in app database
                        ///
//                        var AllIntechUsers = searcher.FindAll()
//.Cast<Principal>()
//.Select(p => new
//{
//    Auth = p as AuthenticablePrincipal,
//    De = p.GetUnderlyingObject() as DirectoryEntry
//})
//.Where(x =>
//  x.Auth != null &&
//  !string.IsNullOrEmpty(x.Auth.UserPrincipalName) &&
//  //x.Auth.Enabled == true && // only active users
//  x.De != null &&
//  x.De.Properties["facsimileTelephoneNumber"].Value != null &&  // has BioStar
//   x.De.Properties["facsimileTelephoneNumber"]
//           .Cast<object>()
//            .Any(v => userIds.Contains(v.ToString().Trim())) &&
//        (
//             !(x.De.Properties["distinguishedName"].Value?.ToString() ?? "")
//            .Contains("OU=Disabled Objects")
//        &&
//           !(x.De.Properties["distinguishedName"].Value?.ToString() ?? "").Contains("OU=Disable Objects")
//        )

//)
//.ToList();
//                        ///
                        var AllActiveIntechUsers = searcher.FindAll()
    .Cast<Principal>()
    .Select(p => new
    {
        Auth = p as AuthenticablePrincipal,
        De = p.GetUnderlyingObject() as DirectoryEntry
    })
    .Where(x =>
        x.Auth != null &&
     //x.Auth.Enabled == true &&

     //x.Auth.EmployeeId !="" &&
      x.De != null &&
      IsActive(x.De) &&
      x.De.Properties["facsimileTelephoneNumber"].Count > 0 &&

       //x.De.Properties["facsimileTelephoneNumber"]
       //     .Cast<object>()
       //      .Any(v => userIds.Contains(v.ToString().Trim())) &&
        (
             !(x.De.Properties["distinguishedName"].Value?.ToString() ?? "")
            .Contains("OU=Disabled Objects")
         &&
            !(x.De.Properties["distinguishedName"].Value?.ToString() ?? "").Contains("OU=Disable Objects")


        )
    )
    //.Select(x => new
    //{
    //    Name = x.Auth.Name,
    //    Email = x.Auth.UserPrincipalName,
    //    Enabled = x.Auth.Enabled,
    //    DistinguishedName = x.De.Properties["distinguishedName"].Value?.ToString()
    //})
    .ToList();

                        var inactiveUserinAd = searcher.FindAll()
    .Cast<Principal>()
    .Select(p => new
    {
        Auth = p as AuthenticablePrincipal,
        De = p.GetUnderlyingObject() as DirectoryEntry
    })
    .Where(x =>
        x.Auth != null &&
        x.De != null &&
        // x.Auth.Enabled != true &&
         
       
         
        x.De.Properties["facsimileTelephoneNumber"].Count > 0 &&
         //x.De.Properties["facsimileTelephoneNumber"]
         //    .Cast<object>()
         //  .Any(v => userIds.Contains(v.ToString().Trim()))
           
         //    &&
        (

            (x.De.Properties["distinguishedName"].Value?.ToString() ?? "")
               .Contains("OU=Disabled Objects")
         ||
         (x.De.Properties["distinguishedName"].Value?.ToString() ?? "")
         .Contains("OU=Disable Objects")
        )
    )
    .Select(x => new

    {
        Name = x.Auth.Name,
        Email = x.Auth.UserPrincipalName,
        Enabled = x.Auth.Enabled,

        DistinguishedName = x.De.Properties["distinguishedName"].Value?.ToString(),
        FaxNumber = x.De.Properties["facsimileTelephoneNumber"].Value?.ToString()
    })
    .ToList();




                        List<AspNetUser> userList = db.AspNetUsers.Where(x => x.IsDeleted != true).ToList<AspNetUser>();
                        //  return;
                        List<AspNetUser> inActiveUserList = userList.Where(x => x.BioStarEmpNum.HasValue && x.IsDeleted != true && inactiveUserinAd.Any(k => k.FaxNumber == x.BioStarEmpNum.ToString())).ToList<AspNetUser>();
                        UpdateInactiveADuserinLeaveonUser(inActiveUserList);

                        
                        AuthenticablePrincipal auth;
                        List<string> departmentsList = new List<string>();
                        List<string> countriesList = new List<string>();

                        foreach (var result in AllActiveIntechUsers)
                        {

                            try
                            {
                                DirectoryEntry de = result.De;
                                auth = result.Auth;
                                bool siacc = IsActive(de);
                                int bioStarValue = 0;
                                var rawValue = de.Properties["facsimileTelephoneNumber"].Value;
                                if (rawValue != null && long.TryParse(rawValue.ToString(), out long val))
                                {
                                    if (val >= int.MinValue && val <= int.MaxValue)
                                    {
                                        bioStarValue = (int)val;
                                    }
                                }
                                AspNetUser aspNetUser = userList.FirstOrDefault(x => x.BioStarEmpNum == bioStarValue && !(x.IsDeleted == true) && x.UserName.Replace(" ", "").ToUpper() == auth.UserPrincipalName.Replace(" ", "").ToUpper());
                                if ((aspNetUser == null && auth.Enabled == false) || bioStarValue == 0)
                                {
                                    continue;
                                }
                                departmentsList.Add(Convert.ToString(de.Properties["department"].Value));
                                // countriesList.Add(Convert.ToString(de.Properties["co"].Value));
                                string countryname = Convert.ToString(de.Properties["co"].Value);

                                if (aspNetUser == null && auth.Enabled != false)
                                {//Insert

                                    //it means if user is created before "01/01/2019" then totaDays will be in minus. so not add very old users. only add new users. which are after "01/01/2019"
                                    //this is just to fast the process
                                    //if (TimeDifference.TotalDays < 0) continue;
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(countryname))
                                        { UpdateCountry(countryname); }
                                        insertedEmp += 1;
                                        InsertEmployee(de, userList);
                                        // InsertSyncLog(jobName, "Completed", 0, 1, 0, "", "Insert", de);

                                    }
                                    catch (Exception ex)
                                    {

                                        //InsertSyncLog(jobName, "Error", 0, 1, 0, ex.Message, "Insert", de);
                                        // Skip this record and move to next
                                        Log.Error("Insert new user from {@ADUser}", new
                                        {
                                            Name = de.Properties["cn"].Value,
                                            Department = de.Properties["department"]?.Value,
                                            Fax = de.Properties["facsimileTelephoneNumber"]?.Value,
                                            Country = de.Properties["co"]?.Value,
                                        });
                                        Console.WriteLine($"Skipping record due to exception: {ex.Message}");
                                        continue;
                                    }

                                }
                                else
                                {//Update
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(countryname))
                                        { UpdateCountry(countryname); }
                                        var olddbUser = db.AspNetUsers.FirstOrDefault(x => x.Id == aspNetUser.Id && !(x.IsDeleted == true));
                                        UpdateEmployee(olddbUser, de, countryname, userList);
                                        //// InsertSyncLog(jobName, "Completed", 0, 0,1, "", "Update", de);
                                        ////}
                                    }
                                    catch (Exception ex)
                                    {
                                        InsertSyncLog(jobName, "Error", 0, 0, 1, ex.Message, "Update", de);
                                        // Logged the errored data
                                        Log.Error("Updating AD user {@ADUser}", new
                                        {
                                            Name = aspNetUser.UserName,
                                            Department = de.Properties["department"]?.Value,
                                            Fax = de.Properties["facsimileTelephoneNumber"]?.Value,
                                            Country = de.Properties["co"]?.Value,
                                        });
                                        Console.WriteLine($"Skipping record due to exception: {ex.Message}");
                                        //throw new Exception($"Error in SyncAppWithAD: {ex.Message}", ex);
                                        continue;
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                InsertSyncLog(jobName, "Loop iteration", 0, 1, 0, ex.Message, "Loop iteration", null);
                                Log.Error("Error processing AD record", ex);
                                continue;
                            }

                        }

                     //   return;
                        var dbDaprtmentList = db.DepartmentNames.ToList();
                        //-----------add department name which does not exist in LMS-DB------------
                        List<string> distinctDepartmentNames = departmentsList.Distinct().ToList();

                        foreach (string itm in distinctDepartmentNames)
                        {
                            //if(String.IsNullOrEmpty(itm))
                            //    continue;
                            DepartmentName departmentName = db.DepartmentNames.FirstOrDefault(x => x.Name == itm);
                            if (departmentName == null && !string.IsNullOrEmpty(itm.Trim()))
                            {
                                InsertSyncLog(jobName, "Insert", 0, 0, 1, itm, "DepartmentName", null);
                                departmentName = new DepartmentName() { Name = itm };
                                db.DepartmentNames.Add(departmentName);
                            }
                        }

                        //-------------remove department name which does not exist in AD-------------
                        foreach (var itm in dbDaprtmentList)
                        {
                            string foundName = distinctDepartmentNames.FirstOrDefault(x => x == itm.Name);
                            if (string.IsNullOrEmpty(foundName))
                            {
                                InsertSyncLog(jobName, "Delete", 0, 0, 1, itm.Name, "DepartmentName", null);
                                db.DepartmentNames.Remove(itm);
                            }
                        }

                        //-----------add country name which does not exist in LMS-DB------------
                        List<string> distinctCountriesNames = countriesList.Distinct().ToList();
                        foreach (string itm in distinctCountriesNames)
                        {

                            CountryName countryName = db.CountryNames.FirstOrDefault(x => x.Name == itm);
                            if (countryName == null && !string.IsNullOrEmpty(itm.Trim()))
                            {
                                countryName = new CountryName() { Name = itm };
                                db.CountryNames.Add(countryName);
                            }
                        }

                        db.SaveChanges();
                        InsertSyncLog(jobName, "End User sync Job", 0, 1, 0, "", "Insert", null);
                        //System.IO.File.WriteAllLines(path, loginsList);
                        ////////////////////////////now find in AD

                        //foreach (Employee item in _LstEmployees)
                        //{
                        //    foreach (var result in AllIntechUsers)
                        //    {
                        //        DirectoryEntry de = result.GetUnderlyingObject() as DirectoryEntry;
                        //        empFound = false;
                        //        if (item.EmployeeName == Convert.ToString(de.Properties["userPrincipalName"].Value))
                        //        {
                        //            empFound = true;
                        //            break;
                        //        }
                        //    }
                        //    if (empFound == false)
                        //    {
                        //        DeleteEmployee(item.EmployeeCode);
                        //    }
                        //}

                    }



                    //if (insertedEmp > 0 || UpdatedEmp > 0)
                    //{
                    //    MessageBox.Show(insertedEmp + " new user(s) found." + System.Environment.NewLine + UpdatedEmp + " user(s) status updated." + System.Environment.NewLine + "Please close and reopen this form to view updated data");
                    //}
                    //else
                    //{
                    //    MessageBox.Show("Data is up-to-date");
                    //}


                }
            }
            catch (Exception ex)
            {
                // Log the error in the SyncLog.txt file
                InsertSyncLog(jobName, "Error in SyncAppWithAD", 0, 1, 0, ex.Message, "Error in SyncAppWithAD", null);
                throw new Exception($"Error in SyncAppWithAD: {ex.Message}", ex);
            }

        }

        private bool IsActive(DirectoryEntry de)
        {
            if (de.NativeGuid == null) return false;

            int flags = (int)de.Properties["userAccountControl"].Value;

            return !Convert.ToBoolean(flags & 0x0002);
        }
        private void InsertEmployee(DirectoryEntry de, List<AspNetUser> LstAspNetUsers)
        {
            //return;
            AspNetUser emp = new AspNetUser();

            int? bioStarValue = 0;
            var rawValue = de.Properties["facsimileTelephoneNumber"].Value;

            if (rawValue != null && long.TryParse(rawValue.ToString(), out long val))
            {
                if (val >= int.MinValue && val <= int.MaxValue)
                {
                    bioStarValue = (int)val;
                }
                else
                {
                    Console.WriteLine($"Out of range value: {val}");
                }
            }


            DateTime? whenCreated = de.Properties["whenCreated"].Value != null
                ? (DateTime?)de.Properties["whenCreated"].Value
               : null;

            emp.Email = Convert.ToString(de.Properties["userPrincipalName"].Value);

            emp.EmpolyeeName = Convert.ToString(de.Properties["name"].Value);
            if (string.IsNullOrEmpty(emp.EmpolyeeName))
            {
                emp.EmpolyeeName = emp.Email.Split('@')[0].Replace('.', ' ');
            }


            emp.UserName = Convert.ToString(de.Properties["userPrincipalName"].Value);

            emp.Id = Guid.NewGuid().ToString();
            //emp.BioStarEmpNum = Convert.ToInt32(de.Properties["facsimileTelephoneNumber"].Value);//null; //0000;
            emp.BioStarEmpNum = bioStarValue;
            emp.EmailConfirmed = false;
            emp.PasswordHash = "ABaTT1CcvSEzwTzDXHnXFm+9cJ3Zaa65Z6QMZ4ZygNVyX8TIvSevNuJGKX7k81VQVQ==";
            emp.SecurityStamp = "e93564e2-08f0-47cd-a822-4b99ca4c08d2";
            emp.PhoneNumberConfirmed = false;
            emp.TwoFactorEnabled = false;
            emp.LockoutEnabled = true;
            emp.AccessFailedCount = 0;
            emp.DateCreated = DateTime.Now;
            emp.IsNew = true;
            emp.DepartmentName = Convert.ToString(de.Properties["department"].Value);
            //emp.CntryName = Convert.ToString(de.Properties["co"].Value);
            emp.IsActive = IsActive(de);
            emp.Gender = Convert.ToString(de.Properties["gender"].Value) == "Male" ? true : false;
            emp.JoiningDate = whenCreated;
            string dn = de.Properties["manager"].Value != null ? de.Properties["manager"].Value.ToString() : string.Empty;

            if (!string.IsNullOrEmpty(dn))
            {


                int startIndex = dn.IndexOf("CN=") + 3;
                int endIndex = dn.IndexOf(",", startIndex);
                string managerNameFromAD = endIndex > 0 ? dn.Substring(startIndex, endIndex - startIndex) : dn.Substring(startIndex);
                if (!String.IsNullOrEmpty(managerNameFromAD))
                {

                    // var aDmanagerIDs = LstAspNetUsers.Where(u => !string.IsNullOrEmpty(u.ManagerName) && u.ManagerName.Trim().ToLower() == managerNameFromAD.Trim().ToLower()).Select(o => o.ManagerID).ToList();
                    //var admanagerData = db.AspNetUsers
                    //    .AsEnumerable()
                    //    .FirstOrDefault(u => u.IsActive == true &&
                    //                         u.IsDeleted != true && aDmanagerIDs != null &&
                    //                         aDmanagerIDs.Any(k => k != null && u.Id.Trim() == k.ToString().Trim()));
                    string normalizedManagerName = managerNameFromAD.Trim();

                    if (normalizedManagerName == "huseyn.tarek")
                    { normalizedManagerName = "ht"; }
                    switch (normalizedManagerName)
                    {
                        case ("husyen.tarek"):
                            normalizedManagerName = "ht";
                            break;
                        case "muhammad.rehan.afgan":
                            normalizedManagerName = "muhammad afgan";
                            break;
                        case "abdul.rehman.arif":
                            normalizedManagerName = "abdul rehman";
                            break;
                        case "muhammad.sannan.asif":
                            normalizedManagerName = "sannan asif";
                            break;
                    }
                    var admanagerData = LstAspNetUsers
                                .AsEnumerable()
                                .FirstOrDefault(u => u.IsActive == true &&
                                                     u.IsDeleted != true
                                                    && !string.IsNullOrEmpty(u.EmpolyeeName) && u.EmpolyeeName.Trim().ToLower() == normalizedManagerName.Trim().ToLower()
                                                     // && aDmanagerIDs != null &&
                                                     //aDmanagerIDs.Any(k => k!= null && u.Id.Trim() == k.ToString().Trim())
                                                     );
                    //string normalizedManagerName = managerNameFromAD
                    //        .Trim()
                    //        .ToLower()
                    //        .Replace(" ", ".");

                    //var managerData = db.AspNetUsers
                    //    .AsEnumerable()
                    //    .FirstOrDefault(u =>
                    //        !string.IsNullOrEmpty(u.UserName) &&
                    //        u.UserName.Split('@')[0].ToLower() == normalizedManagerName);

                    if (admanagerData != null)
                    {
                        emp.ManagerName = managerNameFromAD;
                        emp.ManagerEmail = admanagerData.UserName;
                        emp.ManagerID = admanagerData.Id;
                    }
                }
            }

            db.AspNetUsers.Add(emp);
            db.SaveChanges();

        }

        private void UpdateEmployee(AspNetUser oldemp, DirectoryEntry de, string countryname, List<AspNetUser> LstAspNetUsers)
        {

            //return;0.
            //AspNetUser emp;
            //emp = new AspNetUser();
            //emp.IsActive = IsActive(de);

            db = new LeaveONEntities();

            var dbUser = db.AspNetUsers.FirstOrDefault(x => x.Id == oldemp.Id && !(x.IsDeleted == true));
            if (dbUser != null)
            {
                string managerNameFromAD = string.Empty;
                dbUser.IsActive = IsActive(de);
                if (!String.IsNullOrEmpty(countryname))
                { dbUser.CntryName = countryname; }
                else
                {
                    dbUser.CntryName = null;
                }
                dbUser.DepartmentName = Convert.ToString(de.Properties["department"].Value);
                dbUser.BioStarEmpNum = Convert.ToInt32(de.Properties["facsimileTelephoneNumber"].Value);
                dbUser.DateModified = DateTime.Now;
                string dn = de.Properties["manager"]?.Value?.ToString();
                if (dn != null)
                {
                    int startIndex = dn.IndexOf("CN=") + 3;
                    int endIndex = dn.IndexOf(",", startIndex);
                    managerNameFromAD = endIndex > 0 ? dn.Substring(startIndex, endIndex - startIndex) : dn.Substring(startIndex);


                    if (!string.IsNullOrEmpty(managerNameFromAD))
                    {
                        string normalizedManagerName = managerNameFromAD.Trim();

                        //string normalizedManagerName = managerNameFromAD
                        //        .Trim()
                        //        .ToLower()
                        //        .Replace(" ", ".");
                        if (normalizedManagerName == "huseyn.tarek")
                        { normalizedManagerName = "ht"; }
                        switch (normalizedManagerName)
                        {
                            case ("husyen.tarek"):
                                normalizedManagerName = "ht";
                                break;
                            case "muhammad.rehan.afgan":
                                normalizedManagerName = "muhammad afgan";
                                break;
                            case "abdul.rehman.arif":
                                normalizedManagerName = "abdul rehman";
                                break;
                            case "muhammad.sannan.asif":
                                normalizedManagerName = "sannan asif";
                                break;
                        }


                        // var aDmanagerIDs = LstAspNetUsers.Where(u => u.IsActive == true && !string.IsNullOrEmpty(u.ManagerName) && u.ManagerName.Trim().ToLower() == normalizedManagerName.Trim().ToLower()).Select(o => o.ManagerID).ToList();


                        var admanagerData = LstAspNetUsers
                            .AsEnumerable()
                            .FirstOrDefault(u => u.IsActive == true &&
                                                 u.IsDeleted != true
                                                && !string.IsNullOrEmpty(u.EmpolyeeName) && u.EmpolyeeName.Trim().ToLower() == normalizedManagerName.Trim().ToLower()
                                                 // && aDmanagerIDs != null &&
                                                 //aDmanagerIDs.Any(k => k!= null && u.Id.Trim() == k.ToString().Trim())
                                                 );

                        //set  name from AD
                        // dbUser.ManagerName = managerNameFromAD;
                        if (admanagerData != null && dbUser.ManagerID != admanagerData.Id)
                        {
                            dbUser.ManagerID = admanagerData.Id;
                            dbUser.ManagerEmail = admanagerData.UserName;

                            dbUser.ManagerName = managerNameFromAD;
                            //dbUser.ManagerName = admanagerData.EmpolyeeName;
                        }
                    }
                }

                // Retrieve "whenCreated" from DirectoryEntry
                DateTime? whenCreated = de.Properties["whenCreated"].Value != null
                    ? (DateTime?)de.Properties["whenCreated"].Value
                    : null;
                // Assign "JoiningDate" if it hasn't been set already
                if (dbUser.JoiningDate == null)
                {
                    dbUser.JoiningDate = whenCreated;
                }
                string empolyeeName = Convert.ToString(de.Properties["name"].Value);
                if (string.IsNullOrEmpty(empolyeeName))
                {
                    dbUser.EmpolyeeName = dbUser.Email.Split('@')[0].Replace('.', ' ');
                }
                else

                    dbUser.EmpolyeeName = empolyeeName;
            }
            dbUser.IsNew = true;
            db.Entry(dbUser).Property(x => x.IsActive).IsModified = true;
            db.Entry(dbUser).Property(x => x.DepartmentName).IsModified = true;
            db.Entry(dbUser).Property(x => x.CntryName).IsModified = true;
            db.Entry(dbUser).Property(x => x.BioStarEmpNum).IsModified = true;
            db.Entry(dbUser).Property(x => x.DateModified).IsModified = true;
            db.Entry(dbUser).Property(x => x.JoiningDate).IsModified = true;
            db.Entry(dbUser).Property(x => x.JoiningDate).IsModified = true;
            db.Entry(dbUser).Property(x => x.ManagerName).IsModified = true;
            db.Entry(dbUser).Property(x => x.ManagerID).IsModified = true;
            db.Entry(dbUser).Property(x => x.EmpolyeeName).IsModified = true;
            db.Entry(dbUser).Property(x => x.IsNew).IsModified = true;
            db.SaveChanges();









            //db.AspNetUsers.Attach(oldEmp);


            //db.Entry(oldEmp).Property(x => x.IsActive).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.EmpolyeeName).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.DepartmentName).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.CntryName).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.BioStarEmpNum).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.DateModified).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.JoiningDate).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.ManagerName).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.ManagerID).IsModified = true;
            //db.Entry(oldEmp).Property(x => x.ManagerEmail).IsModified = true;
            ////db.SaveChangesAsync();
            //db.SaveChanges();
            //db.Entry(emp).State = EntityState.Modified;
            //db.SaveChangesAsync();

        }

        private void UpdateCountry(String name)
        {
            LeaveONEntities dbcontext = new LeaveONEntities();
            var counntry = dbcontext.CountryNames.FirstOrDefault(k => !string.IsNullOrEmpty(k.Name) && k.Name.Trim().ToLower() == name.Trim().ToLower());
            if (counntry == null)
            {
                CountryName cntry = new CountryName();
                cntry.Name = name;
                dbcontext.CountryNames.Add(cntry);
                dbcontext.SaveChanges();

            }

        }
        private void UpdateInactiveADuserinLeaveonUser(List<AspNetUser> aduserList)
        {
            using (LeaveONEntities dbcontext = new LeaveONEntities())
            {
                if (aduserList.Any())
                {
                    var empNums = aduserList
                        .Select(x => x.BioStarEmpNum)
                        .ToList();

                    var oldList = dbcontext.AspNetUsers
                        .Where(x => empNums.Contains(x.BioStarEmpNum))
                        .ToList();

                    oldList.ForEach(x => x.IsActive = false);

                    dbcontext.SaveChanges();
                }
            }
        }
        public void InsertSyncLog(string jobName, string status, int totalRecords,
                      int inserted, int updated, string errorMessage, string taskType, DirectoryEntry de)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            int? bioStarValue = 0;
            if (de != null)
            {
                var rawValue = de.Properties["facsimileTelephoneNumber"].Value;

                if (rawValue != null && long.TryParse(rawValue.ToString(), out long val))
                {
                    if (val >= int.MinValue && val <= int.MaxValue)
                    {
                        bioStarValue = (int)val;
                    }
                    else
                    {
                        Console.WriteLine($"Out of range value: {val}");
                    }
                }
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            INSERT INTO SyncJobLogs
            (JobName, StartTime, EndTime, Status, TotalRecords,
             InsertedRecords, UpdatedRecords, ErrorMessage,TaskType,EmployeeID)
            VALUES
            (@JobName, @StartTime, @EndTime, @Status, @TotalRecords,
             @InsertedRecords, @UpdatedRecords, @ErrorMessage,@TaskType,@EmployeeID)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@JobName", jobName);
                    cmd.Parameters.AddWithValue("@StartTime", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EndTime", DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@TotalRecords", totalRecords);
                    cmd.Parameters.AddWithValue("@InsertedRecords", inserted);
                    cmd.Parameters.AddWithValue("@UpdatedRecords", updated);
                    cmd.Parameters.AddWithValue("@TaskType", taskType);
                    cmd.Parameters.AddWithValue("@EmployeeID", bioStarValue);
                    cmd.Parameters.AddWithValue("@ErrorMessage",
                        string.IsNullOrEmpty(errorMessage) ? (object)DBNull.Value : errorMessage);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public void Experiment()
        {
            var context = new DirectoryContext(DirectoryContextType.Forest, "intechww.com");
            List<string> Lstabc1 = new List<string>();
            using (var schema = System.DirectoryServices.ActiveDirectory.ActiveDirectorySchema.GetSchema(context))
            {
                var userClass = schema.FindClass("user");

                foreach (ActiveDirectorySchemaProperty property in userClass.GetAllProperties())
                {
                    var abc = property.Name;
                    if (property.Name.ToLower().Contains("region"))
                    {

                        Lstabc1.Add(property.Name);
                    }
                    // property.Name is what you're looking for
                }
            }
        }
    }
    public static class MyGlobalClass
    {
        public static bool MyGlobalBool { get; set; }
    }

}

//public void LeavePolicyValues()
//{
//  string LoggedInUserId = User.Identity.GetUserId();
//  int LoggedInUserLeavePolicyId = db.AspNetUsers.FirstOrDefault(x => x.Id == LoggedInUserId).UserLeavePolicyId.Value;
//  UserLeavePolicy userLeavePolicy = db.UserLeavePolicies.FirstOrDefault(x => x.Id == LoggedInUserLeavePolicyId);
//  List<UserLeavePolicyDetail> LoggedInUserLeavePolicyDetails = db.UserLeavePolicyDetails.Where(x => x.UserLeavePolicyId == LoggedInUserLeavePolicyId).ToList();
//  List<LeaveBalance> LoggedInUserLeaveBalances = db.LeaveBalances.Where(x => x.UserId == LoggedInUserId).ToList();
//  foreach (UserLeavePolicyDetail leavePolicy in LoggedInUserLeavePolicyDetails)
//  {
//    foreach (LeaveBalance leaveBalance in LoggedInUserLeaveBalances)
//    {
//      //          status 1 mean values reset
//      //staus 0 mean values has to reset
//      //date kay sath 1 or 0 ka check lagay ga
//    }
//  }
//}

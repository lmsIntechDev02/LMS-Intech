using LeaveON.Services;
using LeaveON.UtilityClasses;
using Repository.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LeaveON.Services.BreakHoursService;

namespace LeaveOnTaskRunner
{
    class Program
    {
        //static void Main(string[] args)
        static async Task Main(string[] args)

        {
            //// Calculate 'yesterday'
            DateTime yesterday = DateTime.Today.AddDays(-2);
            DateTime today = DateTime.Today;

            //// Initialize the service
            //AttendanceService service = new AttendanceService();  // Make sure any dependencies are resolved

            //// Call the method with 'yesterday' for both startDate and endDate
            //service.ConnectToDBandFillAttendanceData(yesterday, today);

            // Define specific start and end dates
            /*   DateTime startDate = new DateTime(DateTime.Today.Year - 1, 6, 1);  
               DateTime endDate = new DateTime(DateTime.Today.Year - 1, 6, 30);  */


            //DateTime startDate = new DateTime(DateTime.Today.Year, 1, 1);
            //DateTime endDate = new DateTime(DateTime.Today.Year, 1, 2);
            // DateTime startDate = new DateTime(2026, 01, 01);
            //DateTime endDate = new DateTime(2026, 02, 11);

            // testing
          DateTime startDate = new DateTime(2026, 03, 09);
         DateTime endDate = new DateTime(2026, 03, 31);

       //   DateTime startDate = yesterday;
         //  DateTime endDate = today;



            // Initialize the service
            //  AttendanceService service = new AttendanceService();  // Make sure any dependencies are resolved
            AttendanceService4 service = new AttendanceService4();
            BreakHoursService breakHours = new BreakHoursService();
            // Call the method with 'startDate' and 'endDate'
            // await service.ConnectToDBandFillAttendanceData(startDate, endDate);
            // await service.ConnectToDBandFillAttendanceData(startDate, endDate);

            //  for (int i = 12; i < 28; i++)//9 copm
            //{
            //   DateTime startDate = new DateTime(2026, 3, i);
            //  DateTime endDate = startDate; // same date, no need to recreate
           // var builder = new ConfigurationBuilder()
           //.SetBasePath(Directory.GetCurrentDirectory())
           //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

           // IConfiguration config = builder.Build();

             string connection = ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString; ;// "Data Source=10.1.10.28;Initial Catalog=BioStarTA;User Id=sa;Password=@Intech#123;";
            var userLsit=  await service.GetUserActiveList();
            //  userLsit = userLsit.Where(k => k.BioStarEmpNum == 2696).ToList();

            SqlConnection con = new SqlConnection(connection);
            con.Open();




            List<TimeData> finalList = new List<TimeData>();
            List<PunchLog> punchLogs = new List<PunchLog>();
            List<BreakHour> breakeHourList = new List<BreakHour>();


             

                InsertSyncLog("Sync User Atendance", "", userLsit.Count(),
                           1, 0, "Start Job", "Sync User Atendance", 0);

          //  startDate = new DateTime(2026, 4, 01);
          //  endDate = new DateTime(2026, 4, 03);

          //  for (DateTime date = lsstartDate; date <= lsendDate; date = date.AddDays(1))
           // {
               // Console.WriteLine(date.ToString("yyyy-MM-dd"));

                // Your logic here

               // startDate = date;
             //   endDate = date.AddDays(2);
            foreach (var user in userLsit)
            {

                List<TimeData> userData;
                try
                {
                    //(service.GetUserAttendancData(startDate, endDate, con, user, out userData)==1)
                    //   finalList.AddRange(userData);


                   userData =  await service.GetEmployeeAttendacne(startDate, endDate, user);
                    finalList.AddRange(userData);


                }
                catch (Exception ex)
                {
                    InsertSyncLog("Sync User Atendance", "", userLsit.Count(),
                          0, 0, ex.Message.ToString(), "Attendance job", 0);
                }
                try
                {
                    breakeHourList = breakHours.GetBreakHoursForUser(user, startDate, endDate, con);
                }
                catch (Exception ex)
                {
                    InsertSyncLog("Sync User Atendance", "", userLsit.Count(),
                           0, 0, ex.Message.ToString(), "Breake job", 0);
                }
            
            }

            con.Close();
            if (finalList.Count() > 0)
            {

                await service.ConnectToDBandReturnAttendanceData(finalList);
            }
            //Console.WriteLine("Running Break Hours Service..." +i);
            if (breakeHourList.Count() > 0)
            {
                await breakHours.ConnectToDBandFillBreakHours(breakeHourList);
            }
           //  }



            //Console.WriteLine("Attendance data processed for: " + yesterday.ToShortDateString());
        }

        public static void InsertSyncLog(string jobName, string status, int totalRecords,
                          int inserted, int updated, string errorMessage, string taskType, int? bioStarValue)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

             

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
    }
}

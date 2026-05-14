using LeaveON.Services;
using LeaveON.UtilityClasses;
using Repository.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
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
            DateTime today = DateTime.Today;//.AddDays(-1);  

            // testing
            DateTime startDate = new DateTime(2026, 03, 16);
            DateTime endDate = new DateTime(2026, 03, 25);

             // DateTime startDate = yesterday;
             //DateTime endDate = today;

            List<string> ids = new List<string>
{
"2304",
"3320",
"2135",
"1938",
"2021",
"2298",
"3326",
"2013",
"2553",
"2465",
"3327",
"1781",
"3324",
"2590"
};

            //Initialize the service
            //AttendanceService service = new AttendanceService();  
            //Make sure any dependencies are resolved
            AttendanceService4 service = new AttendanceService4();
            BreakHoursService breakHours = new BreakHoursService();
            string connection = ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString; ;// "Data Source=10.1.10.28;Initial Catalog=BioStarTA;User Id=sa;Password=@Intech#123;"
            var userList = await service.GetUserActiveList();
            //  userList = userList.Where(k => ids.Any(j=> j.ToString()==k.BioStarEmpNum.ToString())).ToList();

             userList = userList.Where(k => k.BioStarEmpNum == 2696).ToList();

            SqlConnection con = new SqlConnection(connection);
            con.Open();
             
            List<TimeData> finalList = new List<TimeData>();
            List<PunchLog> punchLogs = new List<PunchLog>();
            List<BreakHour> breakeHourList = new List<BreakHour>();




            InsertSyncLog("Sync User Atendance", "", userList.Count(),
                       1, 0, "Start Job", "Sync User Atendance", 0);


            foreach (var user in userList.OrderBy(k => k.BioStarEmpNum).ToList())
            {

                List<TimeData> userData;
                try
                {

                    userData = await service.GetEmployeeAttendacne(startDate, endDate, user, con);
                    if (userData.Count() > 0)
                    {
                        finalList.AddRange(userData);
                    }
                }
                catch (Exception ex)
                {
                    InsertSyncLog("Sync User Atendance", "", userList.Count(),
                          0, 0, ex.Message.ToString(), "Attendance job", 0);
                }
                try
                {
                    breakeHourList = breakHours.GetBreakHoursForUser(user, startDate, endDate, con);

                }
                catch (Exception ex)
                {
                    InsertSyncLog("Sync User Atendance", "", userList.Count(),
                           0, 0, ex.Message.ToString(), "Breake job", 0);
                }

            }

            con.Close();
            if (finalList.Count() > 0)
            {
                string leaveString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                //  await service.ConnectToDBandReturnAttendanceData(finalList);
                await service.SaveAttendance(finalList, leaveString);
            }
            //Console.WriteLine("Running Break Hours Service..." +i);
            if (breakeHourList.Count() > 0)
            {
                await breakHours.ConnectToDBandFillBreakHours(breakeHourList);
            }
            // }



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

using LeaveON.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaveOnTaskRunner
{
    class Program
    {
        //static void Main(string[] args)
        static async Task Main(string[] args)

        {
            //// Calculate 'yesterday'
            DateTime yesterday = DateTime.Today.AddDays(-1);
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
            DateTime startDate = new DateTime(2022, 12, 01);
            DateTime endDate = new DateTime(2023, 01, 01);
            // DateTime startDate = yesterday;
            // DateTime endDate = today;



            // Initialize the service
            //  AttendanceService service = new AttendanceService();  // Make sure any dependencies are resolved
            BreakHoursService breakHours = new BreakHoursService();
            AttendanceService4 service = new AttendanceService4();
            // Call the method with 'startDate' and 'endDate'
            // await service.ConnectToDBandFillAttendanceData(startDate, endDate);

            // await service.ConnectToDBandFillAttendanceData(startDate, endDate);
            // await breakHours.ConnectToDBandFillBreakHours(startDate, endDate);


             await service.ConnectToDBandReturnAttendanceData(startDate, endDate);

            //Console.WriteLine("Attendance data processed for: " + yesterday.ToShortDateString());
        }
    }
}

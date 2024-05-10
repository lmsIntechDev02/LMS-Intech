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
        static void Main(string[] args)
        {
            // Calculate 'yesterday'
            DateTime yesterday = DateTime.Today.AddDays(-1);
            DateTime today = DateTime.Today;

            // Initialize the service
            AttendanceService service = new AttendanceService();  // Make sure any dependencies are resolved

            // Call the method with 'yesterday' for both startDate and endDate
            service.ConnectToDBandFillAttendanceData(yesterday, today);

            Console.WriteLine("Attendance data processed for: " + yesterday.ToShortDateString());
        }
    }
}

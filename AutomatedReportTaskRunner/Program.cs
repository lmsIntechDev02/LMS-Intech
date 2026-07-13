using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using LeaveON.Services;

namespace AutomatedReportTaskRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("LOADING");

            // Show message
            Console.WriteLine("Currently service is not available.");
            // Wait for 5 seconds (5000 milliseconds)
            Thread.Sleep(5000);
            bool isHODRepot = true;
            Console.WriteLine("Exiting application...");
            // Stop execution immediately (out ho jaay)
            //return;
            DateTime today = DateTime.Today;
            DateTime previousMonthDate = today.AddMonths(-1);
            int month = previousMonthDate.Month;
            int year = previousMonthDate.Year;
            //int month = today.Month;
            //int year = today.Year;
            // Initialize the service
            AutomatedReportService service = new AutomatedReportService();  // Make sure any dependencies are resolved
            if (DateTime.Today.Day >= 26)
             {
                if (!isHODRepot)
                {
                    service.GetMonthlyReportData(today.Month, today.Year, false);
                }
                else
                {
                    service.GetHODDeparmentReport(today.Month, today.Year);
                }
             }
            else
            {
                if (!isHODRepot)
                    service.GetMonthlyReportData(month, year, false);
                else
                service.GetHODDeparmentReport(month, year);
                //service.GetMonthlyReportData(01, 2025, false);

                //   Console.WriteLine("Report will be generated on the last day of the month.");
            }
            Console.WriteLine("Automated Report Made");
        }
    }
}

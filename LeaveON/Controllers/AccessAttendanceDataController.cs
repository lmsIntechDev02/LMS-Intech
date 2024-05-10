using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LeaveON.Models;
using LeaveON.UtilityClasses;
using Repository.Models;

namespace LeaveON.Controllers
{
  public class AccessAttendanceDataController : Controller
  {
    LeaveONEntities dbLeaveOn = new LeaveONEntities();
    //private Task<List<TimeData>> ConnectToDBandReturnAbsentReport(int userID, DateTime startDate, DateTime endDate)
    //{
    //  var absentRecords = dbLeaveOn.AttendanceDatas.Where(x => x.EmployeeID == userID && (x.CreatedDate <= startDate && x.CreatedDate
    //   >= endDate) && x.IsAbsent == true).ToList();

    //  absentRecords.Co

      
    //}
  }
}

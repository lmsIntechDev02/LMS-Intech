using System;
using Repository.Models;
using System.Data;
using System.Linq;
using iTextSharp.text;
using System.IO;
using iTextSharp.text.pdf; // Adjust based on actual namespace that includes AttendanceData and LeaveONEntities
using System.Net.Mail;
using System.Collections.Generic;
using System.Threading.Tasks;
using iTextSharp.text.pdf.draw;
using System.Globalization;
using System.Data.SqlClient;

namespace LeaveON.Services
{

  public class AutomatedReportService
  {
    public const string LeavON_Email = "LMS@intechww.com";
    public const string LeavON_Password = "Pakistan12345678*";
    public class EmployeeReportData
    {
      public int? EmployeeID { get; set; }
      public string EmployeeName { get; set; }
      public string Department { get; set; }
      public long? TotalWorkHours { get; set; }
      public long? TotalBreakHours { get; set; }
      public int LateArrivals { get; set; }
      public int EarlyDepartures { get; set; }
      public int AbsentDays { get; set; }
      public int AvailedLeave { get; set; }
      public int LeaveDays { get; set; }
      public int WorkFromHomeDays { get; set; }
      public int OfficialDaysOff { get; set; }
      public string AverageTimeIn { get; set; }
      public string AverageTimeOut { get; set; }
      public string CountryName { get; set; }
      public string ManagerEmail { get; set; }
      public string Manager2Email { get; set; }
      public int TotalDays { get; set; }
      public string AverageTimeInOffice { get; set; }
      public string AssignedLeaveQuota { get; set; }
      public string BalanceLeave { get; set; }
      public string AvailableLeave { get; set; }
      public string CompensatoryLeave { get; set; }
      public string ShortHoursInMonth { get; set; }
      public string OffcialDaysOff { get; set; }
      public string CasualLeaves { get; set; }
      public string AnnualLeaves { get; set; }

    }
    public class EmailAndIDs
    {
      public int? userId { get; set; }
      public string email { get; set; }
      public int? userLeavePolicyID { get; set; }
      public string UserID { get; set; }
      public string EmployeeName { get; set; }
      public Nullable<System.DateTime> JoiningDate { get; set; }
    }
    public List<EmailAndIDs> GetUserEmailsAndIDs(string managerEmail)
    {
      using (var context = new LeaveONEntities())
      {
        //  var users = context.AspNetUsers.Where(y => y.CntryName != "Pakistan" && (y.ManagerID.ToLower() == managerEmail.ToLower() || y.Manager2ID.ToLower() == managerEmail.ToLower()))
        // users = users.Where(k => k.UserID == "2840417a-7247-44bf-bf71-0e98ae6bb956").ToList();
        //test
        var users = context.AspNetUsers.Where(y => y.ManagerID.ToLower() == managerEmail.ToLower() && y.IsActive == true && (y.Id == "dc70c0b4-e445-43b5-8c24-1ab960c3f431"))
      //live
      // var users = context.AspNetUsers.Where(y => y.ManagerID.ToLower() == managerEmail.ToLower() && y.IsActive == true)
      .Select(x => new EmailAndIDs
      {
        userId = x.BioStarEmpNum.Value,
        email = x.Email,
        userLeavePolicyID = x.UserLeavePolicyId,
        UserID = x.Id,
        JoiningDate = x.JoiningDate,
        EmployeeName = x.EmpolyeeName
      })
      .ToList();

        return users;
      }

    }

    public List<EmailAndIDs> GetUsersWithLeavePolicyByManager(string managerEmail)
    {
      using (var context = new LeaveONEntities())
      {
        var validPolicyIds = new[] { 1047, 1048 };

        var users = context.AspNetUsers
            .Where(user =>
                (user.ManagerID.ToLower() == managerEmail.ToLower() ||
                 user.Manager2ID.ToLower() == managerEmail.ToLower()) &&
                 user.IsActive == true &&
                user.UserLeavePolicyId.HasValue &&
                validPolicyIds.Contains(user.UserLeavePolicyId.Value))
            .Select(x => new EmailAndIDs
            {
              userId = x.BioStarEmpNum.Value,
              email = x.Email,
              userLeavePolicyID = x.UserLeavePolicyId,
              UserID = x.Id,
            })
            .ToList();

        return users;
      }
    }

    public List<EmailAndIDs> GetLegitemacyChckers()
    {
      using (var context = new LeaveONEntities())
      {
        int[] legitimacyCheckers = new int[1] { 2696 }; // these are the biostar numbers of legitimacy checkers

        var users = context.AspNetUsers
                           .Where(y => y.BioStarEmpNum.HasValue && legitimacyCheckers.Contains(y.BioStarEmpNum.Value))
                           .Select(x => new EmailAndIDs
                           {
                             userId = x.BioStarEmpNum.Value,
                             email = x.Email,
                             userLeavePolicyID = x.UserLeavePolicyId
                           })
                           .ToList();

        return users;
      }
    }
    public static string GetMonthName(int monthNumber)
    {
      if (monthNumber < 1 || monthNumber > 12)
        throw new ArgumentOutOfRangeException("monthNumber", "Month number must be between 1 and 12.");

      return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNumber);
    }

    // Get Monthly report data

    public Task GetMonthlyReportData(int month, int year, bool legitimacyCheckForReports)
    {
      string monthName = GetMonthName(month);
      List<DepartmentName> departmentList = GetDepartmentList();
      var legitimacyCheckers = GetLegitemacyChckers();
      List<ManagerDto> managerEmails = new List<ManagerDto>();


      managerEmails = GetManagersIDs();


      List<string> managerids = new List<string> {
"75aedf88-2b2e-43a0-99fc-78cd00a8aa71"


      };

      managerEmails = GetManagersListById(managerids);

      foreach (var managerEmail in managerEmails)
      {
        using (var context = new LeaveONEntities())
        {
          var users = GetUserEmailsAndIDs(managerEmail.Id);

          if (users == null || users.Count == 0)
            continue;

          DateTime startOfMonth = new DateTime(year, month, 1);
          DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

          var userIds = users.Select(u => u.UserID).ToList();
          var policyIds = users.Where(u => u.userLeavePolicyID.HasValue)
                               .Select(u => u.userLeavePolicyID.Value)
                               .Distinct()
                               .ToList();

          var attendanceMonth = context.AttendanceDatas
              .Where(a => userIds.Contains(a.UserID)
                       && a.CreatedDate >= startOfMonth
                       && a.CreatedDate <= endOfMonth)
              .ToList();

          var attendanceYear = context.AttendanceDatas
              .Where(a => userIds.Contains(a.UserID)
                       && a.CreatedDate.HasValue)
              .ToList();

          var leaves = context.Leaves
              .Where(l => userIds.Contains(l.UserId)
                       && l.IsAccepted1 == 1
                       && l.IsAccepted2 == 1)
              .ToList();

          var leavePolicies = context.UserLeavePolicies
              .Where(p => policyIds.Contains(p.Id))
              .ToList();

          var leavePolicyDetails = context.UserLeavePolicyDetails
              .Where(p => policyIds.Contains((int)p.UserLeavePolicyId))
              .ToList();

          var holidays = context.AnnualOffDays
              .Where(o => policyIds.Contains((int)o.UserLeavePolicyId))
              .ToList();

          List<EmployeeReportData> reportList = new List<EmployeeReportData>();



          foreach (var user in users)
          {
            if (!user.userLeavePolicyID.HasValue)
              continue;

            int userIdInt = Convert.ToInt32(user.userId);
            var policy = leavePolicies.First(p => p.Id == user.userLeavePolicyID.Value);

            DateTime fiscalStart = policy.FiscalYearStart.Value;
            DateTime fiscalEnd = policy.FiscalYearEnd.Value;

            var userAttendanceMonth = attendanceMonth
                .Where(a => a.BioStarEmpNum == userIdInt && a.CreatedDate >= user.JoiningDate)
                .ToList();


            List<DateTime> workingMonthDateList = GetWorkingDayByMonth(year, month, user.userLeavePolicyID);
            List<DateTime> missedMonthAttendanceDateList = new List<DateTime>();
            if (workingMonthDateList.Count > userAttendanceMonth.Count)
            {
              var list = workingMonthDateList.Where(date => !userAttendanceMonth.Any(j => j.CreatedDate.Value.Date == date.Date)).ToList();

              missedMonthAttendanceDateList.AddRange(list);
            }

            //if (userAttendanceMonth.Count() == 0)
            //  continue;
            //if (missedMonthAttendanceDateList.Count > 0)
            //{
            //  AttendanceData attand = new AttendanceData();
            //  foreach(var item in missedMonthAttendanceDateList)
            //  {
            //    attand = new AttendanceData();
            //    attand.BioStarEmpNum = userIdInt;
            //    attand.CreatedDate = item;
            //    //userAttendanceMonth.Add(attand);
            //    attendanceYear.Add(attand);
            //  }
            //}
           
            var userAttendanceYTD = attendanceYear
                .Where(a => a.BioStarEmpNum == userIdInt

                         && a.CreatedDate >= fiscalStart
                         && a.CreatedDate <= fiscalEnd && a.CreatedDate >= user.JoiningDate)
                .ToList();


            DateTime invalidDate = new DateTime(1, 1, 1);

            var validPunchIns = userAttendanceMonth
                .Where(a => a.FirstPunchIn.HasValue && a.FirstPunchIn.Value != invalidDate)
                .Select(a => a.FirstPunchIn.Value.TimeOfDay.TotalSeconds);

            var validPunchOuts = userAttendanceMonth
                .Where(a => a.LastPunchOut.HasValue && a.LastPunchOut.Value != invalidDate)
                .Select(a => a.LastPunchOut.Value.TimeOfDay.TotalSeconds);

            TimeSpan avgTimeIn = validPunchIns.Any()
                ? TimeSpan.FromSeconds(validPunchIns.Average())
                : TimeSpan.Zero;

            TimeSpan avgTimeOut = validPunchOuts.Any()
                ? TimeSpan.FromSeconds(validPunchOuts.Average())
                : TimeSpan.Zero;

            var publicHolidays = holidays
                .Where(h => h.UserLeavePolicyId == policy.Id
                         && h.OffDay >= fiscalStart
                         && h.OffDay <= fiscalEnd)
                .Select(h => h.OffDay.Value)
                .ToHashSet();

            var userLeavesYTD = leaves
                  .Where(l => l.UserId == user.UserID
                           && (l.LeaveTypeId == 1 || l.LeaveTypeId == 2)
                           && l.IsAccepted1 == 1
                           && l.IsAccepted2 == 1
                           && l.StartDate >= fiscalStart
                           && l.EndDate <= fiscalEnd)
                  .ToList();

            var absenteesYTD = leaves
                  .Where(l => l.UserId == user.UserID
                           && l.IsAccepted1 == 1
                           && l.IsAccepted2 == 1
                           && l.StartDate >= fiscalStart
                           && l.EndDate <= fiscalEnd).ToList();

            int absentYTD = userAttendanceYTD.Count(a =>
                a.IsAbsent == true
                && !publicHolidays.Contains(a.CreatedDate.Value.Date)
                && !absenteesYTD.Any(l => a.CreatedDate >= l.StartDate && a.CreatedDate <= l.EndDate));

            decimal availedLeaveYTD = userLeavesYTD.Sum(l => (decimal)l.TotalDays);

            int shortHoursYTD = (int)context.Leaves
                .Where(l => l.UserId == user.UserID
                         && l.IsShortLeave == true
                           && l.IsAccepted1 == 1
                           && l.IsAccepted2 == 1
                         && l.StartDate >= fiscalStart
                         && l.EndDate <= fiscalEnd)
                .AsEnumerable()
                .Sum(l => (l.EndDate - l.StartDate).TotalHours);
            int shortDaysToCausalLeave = 0;

            if (shortHoursYTD >= 8)
            {
              shortDaysToCausalLeave = (int)shortHoursYTD / 8;
              shortHoursYTD = shortHoursYTD % 8;


              availedLeaveYTD = availedLeaveYTD + shortDaysToCausalLeave;
            }


            // availedLeaveYTD = availedLeaveYTD + shortDaysToCausalLeave;  //comment becasue only  pick leave type ID=2 and 1
            // if short hour >8  then go to absent
            //if (shortDaysToCausalLeave >= 8)
            //{


            //  availedLeaveYTD = availedLeaveYTD + shortDaysToCausalLeave;
            //}

            int totalWorkDays = GetWorkingDays(year, month, user.userLeavePolicyID);

            //  this if user not mark attensance on  userAttendanceMonth then it will as absent
            if (totalWorkDays > userAttendanceMonth.Count())
            {
              absentYTD += (totalWorkDays - userAttendanceMonth.Count());
            }



            double totalWorkSeconds = userAttendanceMonth
                .Where(a => a.TotalWorkHours.HasValue && a.TotalWorkHours.Value > 0)
                .Select(a => a.TotalWorkHours.Value)
                .Distinct()
                .Sum();

            int workingDays = userAttendanceMonth
                .Where(a => a.TotalWorkHours.HasValue && a.TotalWorkHours.Value > 0)
                .Select(a => a.CreatedDate.Value.Date)
                .Distinct()
                .Count();

            TimeSpan avgTimeInOffice = workingDays > 0
                ? TimeSpan.FromSeconds(totalWorkSeconds / workingDays)
                : TimeSpan.Zero;

            DateTime joiningDate = user.JoiningDate ?? fiscalStart;
            DateTime effectiveStart = joiningDate > fiscalStart ? joiningDate : fiscalStart;

            int workedMonths = joiningDate > fiscalEnd ? 0 :
                ((fiscalEnd.Year - effectiveStart.Year) * 12)
                + fiscalEnd.Month - effectiveStart.Month + 1;

            if (workedMonths > 12 || effectiveStart == fiscalStart) workedMonths = 12;

            var policyDetail = leavePolicyDetails
                .Where(p => p.UserLeavePolicyId == policy.Id)
                .ToList();
            int casual = 0;
            int annual = 0;
            if (policyDetail.Where(p => p.LeaveTypeId == 1).Count() > 0)
              casual = (int)policyDetail.Where(p => p.LeaveTypeId == 1)?.Select(p => p.Allowed)?.FirstOrDefault();
            if (policyDetail.Where(p => p.LeaveTypeId == 2).Count() > 0)
              annual = (int)policyDetail.Where(p => p.LeaveTypeId == 2)?.Select(p => p.Allowed)?.FirstOrDefault();

            if (workedMonths < 12)
            {
              casual = (int)Math.Ceiling((casual / 12.0) * workedMonths);
              annual = (int)Math.Ceiling((annual / 12.0) * workedMonths);
            }

            int assignedLeaveQuota = casual + annual;
            int availableLeave = assignedLeaveQuota - (int)availedLeaveYTD;

            reportList.Add(new EmployeeReportData
            {
              EmployeeID = user.userId,
              EmployeeName = userAttendanceMonth.Count > 0 ? userAttendanceMonth.First().UserName : user.EmployeeName,
              Department = userAttendanceMonth.Count > 0 ? userAttendanceMonth.First().DepartmentName : String.Empty,
              CountryName = userAttendanceMonth.Count > 0 ? userAttendanceMonth.First().CountryName : String.Empty,
              ManagerEmail = managerEmail.Email,

              TotalDays = totalWorkDays,
              AbsentDays = absentYTD, // Absents (YTD)
              AvailedLeave = (int)availedLeaveYTD, // Availed Leave (YTD)

              TotalWorkHours = userAttendanceMonth.Count > 0 ? userAttendanceMonth.Sum(a => a.TotalWorkHours) : 0,
              TotalBreakHours = userAttendanceMonth.Count > 0 ? userAttendanceMonth.Sum(a => a.BreakHours) : 0,

              LateArrivals = userAttendanceMonth.Count > 0 ? userAttendanceMonth.Count(a => a.IsLateArrival == true) : 0,
              EarlyDepartures = userAttendanceMonth.Count > 0 ? userAttendanceMonth.Count(a => a.IsEarlyDeparture == true) : 0,

              AverageTimeIn = avgTimeIn.ToString(@"hh\:mm"),
              AverageTimeOut = avgTimeOut.ToString(@"hh\:mm"),
              AverageTimeInOffice = avgTimeInOffice.ToString(@"hh\:mm"),

              ShortHoursInMonth = shortHoursYTD > 0 ? shortHoursYTD.ToString() : "0", // Short Hours (YTD)

              AssignedLeaveQuota = assignedLeaveQuota.ToString(),
              AvailableLeave = availableLeave > 0 ? availableLeave.ToString() : "0",
              CasualLeaves = casual.ToString(),
              AnnualLeaves = annual.ToString()
            });
          }
          string hRBPEmail = String.Empty;
          if (managerEmail != null && !string.IsNullOrEmpty(managerEmail.DepartmentName))
          {
            hRBPEmail = departmentList.FirstOrDefault(k => !string.IsNullOrEmpty(k.Name) && k.Name.ToLower() == managerEmail.DepartmentName.ToLower()).HRBPEmail;
          }

          GeneratePDFManager1(
              reportList,
              reportList.Count > 0 ? reportList[0].TotalDays : 0,
              monthName,
              year,
              managerEmail,
              legitimacyCheckForReports,
              legitimacyCheckers, hRBPEmail);
        }
      }

      return Task.CompletedTask;
    }


    //public Task GetMonthlyReportData(int month, int year, bool legetimacyCheckForReports)
    //{
    //  string monthName = GetMonthName(month);
    //  var legitimacyCheckers = GetLegitemacyChckers();
    //  var managerEmails = GetManagersIDs();

    //  //var manageremails = getpolicywisemanagerids();

    //  //var managerEmails = new List<string>
    //  //    {
    //  //        "c6d11b88-5ad0-4d7b-8c95-d5cd797e6e27",
    //  //        "7baffeb6-7cad-46ad-9418-493d86e1da75",
    //  //        "cec06760-2ff1-4b68-92ee-f53ae43fa4ea",
    //  //        "6c75398c-4f4c-4ff5-baed-814c75138588"
    //  //    };
    //  foreach (var managerEmail in managerEmails)
    //  {

    //    var usersAgainstManagers = GetUserEmailsAndIDs(managerEmail);
    //   // var usersAgainstManagers = GetUsersWithLeavePolicyByManager(managerEmail);

    //    if (!usersAgainstManagers.Any())
    //    {
    //      Console.WriteLine($"No users found under manager: {managerEmail}");
    //      continue;
    //    }


    //    List<EmployeeReportData> managerReportOfUsersList = new List<EmployeeReportData>();
    //    var totalWorkDays = 0;
    //    foreach (var user in usersAgainstManagers)
    //    {

    //      totalWorkDays = GetWorkingDays(year, month, user.userLeavePolicyID);

    //      using (var context = new LeaveONEntities())
    //      // test DB LeaveONEntitiesTarget
    //     //  using (var context = new LeaveONEntitiesTarget())
    //      {
    //        DateTime invalidDate = new DateTime(0001, 01, 01);

    //        // For opening balance leave
    //        var fullYearAttendanceData = context.AttendanceDatas
    //              .Where(a => a.BioStarEmpNum == user.userId
    //                       && a.CreatedDate.HasValue
    //                       && a.CreatedDate.Value.Year == year)
    //              .ToList();

    //        var attendanceData = context.AttendanceDatas.Distinct()
    //        .Where(a => a.BioStarEmpNum == user.userId && a.CreatedDate.Value.Month == month && a.CreatedDate.Value.Year == year &&
    //        a.IsLeave != true)
    //        .AsEnumerable();

    //        if (attendanceData.Any())
    //        {
    //          // Filter out entries with invalid FirstPunchIn or LastPunchOut times
    //          var validPunchIns = attendanceData
    //          .Where(x => x.FirstPunchIn.HasValue && x.FirstPunchIn.Value != invalidDate)
    //          .Select(x => x.FirstPunchIn.Value.TimeOfDay.TotalSeconds);

    //          var validPunchOuts = attendanceData
    //          .Where(x => x.LastPunchOut.HasValue && x.LastPunchOut.Value != invalidDate)
    //          .Select(x => x.LastPunchOut.Value.TimeOfDay.TotalSeconds);

    //          // Compute averages only if there are valid entries
    //          double averageTimeInSecondsIn = validPunchIns.Any() ? validPunchIns.Average() : 0;
    //          double averageTimeInSecondsOut = validPunchOuts.Any() ? validPunchOuts.Average() : 0;

    //          if (averageTimeInSecondsIn == 0 && averageTimeInSecondsOut == 0)
    //          {
    //            Console.WriteLine($"Data not exist against this {user.email.Split('@')[0].Replace('.', ' ')}.");
    //            continue;
    //          }

    //          if (user.userLeavePolicyID == null)
    //          {
    //            continue;
    //          }
    //          var userPolicy = context.UserLeavePolicies.FirstOrDefault(x => x.Id == user.userLeavePolicyID);
    //          DateTime fiscalStart = (DateTime)userPolicy.FiscalYearStart;
    //          DateTime fiscalEnd = (DateTime)userPolicy.FiscalYearEnd;

    //          int? policyId = user.userLeavePolicyID;
    //          // Get the start of the selected month
    //          DateTime startOfCurrentMonth = new DateTime(year, month, 1);
    //          DateTime endOfSelectedMonth = startOfCurrentMonth.AddMonths(1).AddDays(-1);

    //          // Get public holidays list for this month
    //          var holidayDates = context.AnnualOffDays
    //              .Where(o => o.UserLeavePolicyId == policyId
    //                      && o.OffDay >= startOfCurrentMonth
    //                       && o.OffDay <= endOfSelectedMonth)
    //              .Select(o => o.OffDay)
    //              .ToHashSet();

    //          // Get all approved leaves for this user in current month
    //          var approvedLeaves = context.Leaves
    //              .Where(l => l.UserId == user.UserID
    //                       && l.IsAccepted1 == 1
    //                       && l.IsAccepted2 == 1
    //                       && l.StartDate >= fiscalStart
    //                       && l.EndDate <= fiscalEnd)
    //              .ToList();

    //          var approvedLeavsList_CurrentMonth = context.Leaves
    //              .Where(l => l.UserId == user.UserID
    //                      && (l.LeaveTypeId == 1 || l.LeaveTypeId == 2)
    //                      && l.IsAccepted1 == 1
    //                      && l.IsAccepted2 == 1
    //                      && l.StartDate >= startOfCurrentMonth
    //                      && l.EndDate <= endOfSelectedMonth).ToList();

    //          var totalTakenLeaveOfCurrentMonth = approvedLeavsList_CurrentMonth.Sum(x => x.TotalDays);


    //          // Absent count excluding public holidays + approved leaves
    //          var absentCount = attendanceData
    //              .Where(x => x.IsAbsent == true
    //                       && !holidayDates.Contains(x.CreatedDate.Value.Date)   // exclude public holidays
    //                       && !approvedLeaves.Any(l => x.CreatedDate >= l.StartDate && x.CreatedDate <= l.EndDate)
    //                      )
    //              .Count();

    //          // Convert average seconds to TimeSpan
    //          TimeSpan averageTimeIn = TimeSpan.FromSeconds(averageTimeInSecondsIn);
    //          TimeSpan averageTimeOut = TimeSpan.FromSeconds(averageTimeInSecondsOut);

    //          // Format TimeSpan to 12-hour format with AM/PM
    //          string formattedAverageTimeIn = new DateTime(averageTimeIn.Ticks).ToString("hh:mm tt");
    //          string formattedAverageTimeOut = new DateTime(averageTimeOut.Ticks).ToString("hh:mm tt");

    //          long? TotalWorkHours = attendanceData.Sum(x => x.TotalWorkHours);
    //          long? TotalBreakHours = attendanceData.Sum(x => x.BreakHours);

    //          //double totalWorkSeconds = attendanceData.Sum(x => x.TotalWorkHours ?? 0);
    //          double totalWorkSeconds = (double)attendanceData
    //                                    .Where(a =>
    //                                        a.TotalWorkHours > 0 &&
    //                                        a.CreatedDate.HasValue &&
    //                                        a.CreatedDate >= startOfCurrentMonth &&
    //                                        a.CreatedDate < endOfSelectedMonth.AddDays(1))
    //                                    .Select(a => new
    //                                    {
    //                                      a.BioStarEmpNum,
    //                                      a.CreatedDate,
    //                                      a.TotalWorkHours
    //                                    })
    //                                    .Distinct()
    //                                    .Sum(a => a.TotalWorkHours);
    //          double totalDays = attendanceData.Count(); // only for days with attendance

    //          int totalWorkingDays = attendanceData
    //                                .Where(a =>
    //                                    a.TotalWorkHours > 0 &&
    //                                    a.CreatedDate.HasValue &&
    //                                    a.CreatedDate >= startOfCurrentMonth &&
    //                                    a.CreatedDate < endOfSelectedMonth.AddDays(1))
    //                                .GroupBy(a => a.CreatedDate.Value.Date)
    //                                .Count();


    //          var totalWorkDaysExcludingAbsent = totalWorkDays - (absentCount + totalTakenLeaveOfCurrentMonth);





    //          // Calculate average time in office
    //          TimeSpan averageTimeInOffice1 = averageTimeOut - averageTimeIn;

    //          TimeSpan averageTimeInOffice = totalWorkingDays > 0
    //              ? TimeSpan.FromSeconds(totalWorkSeconds / totalWorkingDays)
    //              : TimeSpan.Zero;


    //          //TimeSpan averageTimeInOffice = averageActualWorkTime; // without breaks



    //          DateTime joiningDate = user.JoiningDate ?? fiscalStart;
    //          DateTime effectiveStart = (joiningDate > fiscalStart) ? joiningDate : fiscalStart;

    //          // Calculate worked months within the fiscal year
    //          int workedMonths;
    //          if (joiningDate > fiscalEnd)
    //          {
    //            workedMonths = 0;
    //          }
    //          else
    //          {
    //            workedMonths = ((fiscalEnd.Year - effectiveStart.Year) * 12 + fiscalEnd.Month - effectiveStart.Month + 1);
    //          }

    //          // Get total allowed leave (LeaveType 1 and 2)
    //          var fullAssignedLeaveQuota = user.userLeavePolicyID != null
    //              ? context.UserLeavePolicyDetails
    //                  .Where(lb => lb.UserLeavePolicyId == user.userLeavePolicyID &&
    //                               (lb.LeaveTypeId == 1 || lb.LeaveTypeId == 2)):null;

    //          // Apply prorated formula
    //          if (workedMonths > 12 || effectiveStart == fiscalStart)
    //          {
    //            workedMonths = 12;
    //          }

    //          double? proratedLeaves = (fullAssignedLeaveQuota?.Sum(lb => lb.Allowed) / 12.0) * (workedMonths);


    //          int? casualLeaves = fullAssignedLeaveQuota.Where(c => c.LeaveTypeId == 1).Select(cl => cl.Allowed).FirstOrDefault();
    //          int? annualLeaves = fullAssignedLeaveQuota.Where(c => c.LeaveTypeId == 2).Select(cl => cl.Allowed).FirstOrDefault();
    //          // Split the prorated value into casual and annual
    //          if (workedMonths < 12)
    //          {
    //            casualLeaves = (int?)Math.Ceiling((double)((casualLeaves / 12.0) * workedMonths));
    //            annualLeaves = (int?)Math.Ceiling((double)((annualLeaves / 12.0) * workedMonths));
    //          }

    //          // Filter leaves taken before the current month from attendanceData
    //          int leavesTakenBeforeCurrentMonth1 = attendanceData
    //              .Where(a => (a.IsLeave == true || a.IsAbsent == true) 
    //                       && a.CreatedDate.Value < startOfCurrentMonth)
    //              .Count();
    //          // Get leaves taken by user before selected month in the same fiscal year
    //          decimal? leavesTakenBeforeCurrentMonth = context.Leaves
    //              .Where(l => l.UserId == user.UserID &&
    //                          (l.LeaveTypeId == 1 || l.LeaveTypeId == 2) &&
    //                          l.IsAccepted1 == 1 &&
    //                          l.IsAccepted2 == 1 &&
    //                          l.StartDate >= fiscalStart &&
    //                          l.StartDate < startOfCurrentMonth)
    //              .Select(l => (int?)l.TotalDays)
    //              .DefaultIfEmpty(0)
    //              .Sum();

    //          decimal leavesTakenBeforeCurrentMonth2 = context.Leaves
    //                    .Where(l => l.UserId == user.UserID &&
    //                                (l.LeaveTypeId == 1 || l.LeaveTypeId == 2) &&
    //                                l.IsAccepted1 == 1 &&
    //                                l.IsAccepted2 == 1 &&
    //                                l.EndDate < startOfCurrentMonth) // Include only fully past leaves
    //                    .AsEnumerable()
    //                    .Sum(l =>
    //                    {
    //                      DateTime effectiveStart1 = l.StartDate < fiscalStart ? fiscalStart : l.StartDate;
    //                      DateTime effectiveEnd = l.EndDate < startOfCurrentMonth ? l.EndDate : startOfCurrentMonth.AddDays(-1);
    //                      return (decimal)(effectiveEnd - effectiveStart1).TotalDays + 1;
    //                    });


    //          // Get balance leave from LeaveBalance 
    //          int? balanceLeave = context.LeaveBalances
    //                        .Where(lb => lb.UserId == user.UserID && lb.UserLeavePolicyId == user.userLeavePolicyID && (lb.LeaveTypeId == 1 || lb.LeaveTypeId == 2))
    //                        .Select(lb => (int?)lb.Taken)
    //                        .DefaultIfEmpty(0)
    //                        .Sum();

    //          // Date range for the current month
    //          DateTime startSelectedMonth = new DateTime(year, month, 1);

    //          // Get public holidays in the selected month for this user
    //          var publicHolidaysSet = context.AnnualOffDays
    //              .Where(o => o.UserLeavePolicyId == user.userLeavePolicyID &&
    //                           o.OffDay >= startSelectedMonth &&
    //                          o.OffDay <= endOfSelectedMonth)
    //              .Select(o => o.OffDay)
    //              .ToHashSet();

    //          decimal availedLeaves = context.Leaves
    //              .Where(l => l.UserId == user.UserID &&
    //                          (l.LeaveTypeId == 1 || l.LeaveTypeId == 2) &&
    //                          l.IsAccepted1 == 1 &&
    //                          l.IsAccepted2 == 1 &&
    //                          l.StartDate >= fiscalStart &&
    //                          l.EndDate <= fiscalEnd).Sum(l  => (decimal?)l.TotalDays) ?? 0;


    //          //.Sum(l =>
    //          //{
    //          //  DateTime effectiveStarts = l.StartDate < fiscalStart ? fiscalStart : l.StartDate;
    //          //  DateTime effectiveEnd = l.EndDate > fiscalEnd ? fiscalEnd : l.EndDate;

    //          //  int workingDays = 0;
    //          //  for (DateTime date = effectiveStarts.Date; date <= effectiveEnd.Date; date = date.AddDays(1))
    //          //  {
    //          //    // Skip weekends
    //          //    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
    //          //                    continue;

    //          //    // Skip public holidays
    //          //    if (publicHolidaysSet.Contains(date))
    //          //                    continue;

    //          //                  workingDays++;
    //          //                }

    //          //  return (decimal)workingDays;
    //          //});



    //          DateTime extendedStart = startOfCurrentMonth.AddMonths(-1); // previous month start
    //          DateTime extendedEnd = endOfSelectedMonth.AddMonths(1);   // next month end
    //          //var casualLeaves = 0;
    //          //var annualLeaves = 0;
    //          //if (policyId != null)
    //          //{
    //          //  // Assuming your DbContext is named "db"
    //          //  var leaveDetails = context.UserLeavePolicyDetails
    //          //                       .Where(x => x.UserLeavePolicyId == policyId)
    //          //                       .ToList();

    //          //   casualLeaves = leaveDetails
    //          //                        .FirstOrDefault(x => x.LeaveTypeId == 1)?.Allowed ?? 0;

    //          //   annualLeaves = leaveDetails
    //          //                        .FirstOrDefault(x => x.LeaveTypeId == 2)?.Allowed ?? 0;

    //          //}

    //          int? openingBalanceLeave = fullAssignedLeaveQuota.Sum(lb => lb.Allowed) - (int)leavesTakenBeforeCurrentMonth;

    //          var totalTakenLeave = context.LeaveBalances
    //                                .Where(lb => lb.UserId == user.UserID &&
    //                                             lb.UserLeavePolicyId == user.userLeavePolicyID &&
    //                                             (lb.LeaveTypeId == 1 || lb.LeaveTypeId == 2))
    //                                .Select(lb => lb.Taken ?? 0)
    //                                .DefaultIfEmpty(0)
    //                                .Sum();


    //          //int availiableBalance = (int)assignedLeaveQuota - (int)totalTakenLeaveOfCurrentMonth;
    //            int? availiableBalance = fullAssignedLeaveQuota.Sum(lb => lb.Allowed) - (int)availedLeaves;

    //          //int? totalBalanceLeave = openingBalanceLeave - absentCount;


    //          // Get available leave days from LeaveBalance 
    //          int? availableLeaveDays = context.LeaveBalances
    //                        .Where(lb => lb.UserId == user.UserID && lb.UserLeavePolicyId == user.userLeavePolicyID && (lb.LeaveTypeId == 1 || lb.LeaveTypeId == 2))
    //                        .Select(lb => (int?)lb.Balance)
    //                        .DefaultIfEmpty(0)
    //                        .Sum();

    //          // Get compensatory leaves from LeaveBalances where LeaveTypeId is 0
    //          int? compensatoryLeaves = context.LeaveBalances
    //              .Where(lb => lb.UserId == user.UserID && lb.UserLeavePolicyId == user.userLeavePolicyID && lb.LeaveTypeId == 0)
    //              .Select(lb => (int?)lb.Balance)
    //              .FirstOrDefault();

    //          // Get short hours in month from LeaveBalance table (using HoursTaken column)

    //          var shortHoursInMonth = context.Leaves
    //                    .Where(lb =>
    //                        lb.UserId == user.UserID && lb.IsShortLeave == true &&
    //                        lb.UserLeavePolicyID == user.userLeavePolicyID &&
    //                        lb.StartDate >= startOfCurrentMonth &&
    //                        lb.EndDate <= endOfSelectedMonth)
    //                    .AsEnumerable() // switch to in-memory for TimeSpan calculation
    //                    .Sum(lb => (lb.EndDate - lb.StartDate).TotalHours);

    //          var daysInMonth = DateTime.DaysInMonth(year, month);
    //          //int? policyId = user.userLeavePolicyID;
    //          int publicHolidays = 0;
    //          if (policyId != null)
    //          {

    //            DateTime startOfMonth = new DateTime(year, month, 1);
    //            DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

    //            // Query AnnualOffDays for public holidays for this policy in the given month/year
    //            publicHolidays = context.AnnualOffDays
    //                                .Where(o => o.UserLeavePolicyId == policyId
    //                                 && o.OffDay >= startOfMonth
    //                                 && o.OffDay <= endOfMonth)
    //                                .Count();
    //          }


    //          TimeSpan averageActualWorkTime = totalWorkDaysExcludingAbsent > 0
    //              ? TimeSpan.FromSeconds(totalWorkSeconds /(double)totalWorkDaysExcludingAbsent)
    //              : TimeSpan.Zero;

    //          var reportData = new EmployeeReportData
    //          {
    //            EmployeeID = user.userId,
    //            EmployeeName = attendanceData.First().UserName,
    //            Department = attendanceData.First().DepartmentName,
    //            TotalWorkHours = attendanceData.Sum(x => x.TotalWorkHours),
    //            TotalBreakHours = attendanceData.Sum(x => x.BreakHours),
    //            LateArrivals = attendanceData.Count(x => x.IsLateArrival == true),
    //            EarlyDepartures = attendanceData.Count(x => x.IsEarlyDeparture == true),
    //            AbsentDays = absentCount,
    //            AvailedLeave = (int)availedLeaves,
    //            LeaveDays = attendanceData.Count(x => x.IsLeave == true),
    //            AverageTimeIn = averageTimeIn.ToString(@"hh\:mm"),
    //            AverageTimeOut = averageTimeOut.ToString(@"hh\:mm"),
    //            AverageTimeInOffice = averageTimeInOffice.ToString(@"hh\:mm"),
    //            WorkFromHomeDays = attendanceData.Count(x => x.LeaveTypeID == 10),
    //            OfficialDaysOff = attendanceData.Count(x => x.LeaveTypeID == 8 || x.LeaveTypeID == 9),
    //            CountryName = attendanceData.First().CountryName,
    //            ManagerEmail = managerEmail,
    //            TotalDays = totalWorkDays,
    //            //assignedLeaveQuote
    //            AssignedLeaveQuota = fullAssignedLeaveQuota.Sum(lb => lb.Allowed)?.ToString() ?? "0",
    //            //OpeningLeaveBalance
    //            BalanceLeave = openingBalanceLeave.HasValue ? (openingBalanceLeave.Value < 0 ? "0" : openingBalanceLeave.Value.ToString()) : "0",
    //            //AvailiableLeaveBalacne
    //            AvailableLeave = availiableBalance > 0 ? availiableBalance.ToString() : "0",
    //            CompensatoryLeave = compensatoryLeaves.HasValue ? (compensatoryLeaves.Value < 0 ? "0" : compensatoryLeaves.Value.ToString()) : "0",
    //            //ShortHoursInMonth = shortHoursInMonth,
    //            ShortHoursInMonth = shortHoursInMonth > 0 ? shortHoursInMonth.ToString() : "0",
    //            OffcialDaysOff = publicHolidays.ToString(),
    //            CasualLeaves = casualLeaves.ToString(),
    //            AnnualLeaves = annualLeaves.ToString()
    //          };
    //          managerReportOfUsersList.Add(reportData);
    //          if (legetimacyCheckForReports)//make it true again, false is for testing
    //          {
    //            foreach (EmailAndIDs legitChecker in legitimacyCheckers)
    //            {
    //          //    GeneratePDFIndividuals(reportData, legitChecker.email, totalWorkDays, monthName); // Pass the user's email to the PDF generation and sending function
    //            }
    //          }
    //          else
    //          {
    //         //   GeneratePDFIndividuals(reportData, user.email, totalWorkDays, monthName); // Pass the user's email to the PDF generation and sending function
    //          }
    //        }
    //      else
    //      {

    //          Console.WriteLine($"Data not exist against this {user.email.Split('@')[0].Replace('.', ' ')}");

    //        //var reportData = new EmployeeReportData
    //        //{
    //        //  EmployeeID = user.userId,
    //        //  EmployeeName = user.email.Split('@')[0].Replace('.', ' '),
    //        //  Department = context.AspNetUsers
    //        //.Where(u => u.Email == user.email)
    //        //.Select(u => u.DepartmentName)
    //        //.FirstOrDefault() ?? "N/A",
    //        //  TotalWorkHours = 0,
    //        //  TotalBreakHours = 0,
    //        //  LateArrivals = 0,
    //        //  EarlyDepartures = 0,
    //        //  AbsentDays = totalWorkDays,
    //        //  LeaveDays = 0,
    //        //  AverageTimeIn = null,
    //        //  AverageTimeOut = null,
    //        //  WorkFromHomeDays = 0,
    //        //  OfficialDaysOff = 0,
    //        //  CountryName = "N/A",
    //        //  ManagerEmail = managerEmail,
    //        //};

    //          //managerReportOfUsersList.Add(reportData);
    //        }
    //      }
    //    }
    //  //  GeneratePDFManager(managerReportOfUsersList, totalWorkDays, monthName, legetimacyCheckForReports, legitimacyCheckers, sentEmails);
    //    GeneratePDFManager1(managerReportOfUsersList, totalWorkDays, monthName, year, managerEmail, legetimacyCheckForReports, legitimacyCheckers);
    //  }

    //  return null;
    //}  

    public void GeneratePDFIndividuals(EmployeeReportData reportData, string userEmail, int totalWorkDays, string monthName)
    {
      MailMessage mail = new MailMessage();
      SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com");
      smtpServer.UseDefaultCredentials = false;
      smtpServer.Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password);
      smtpServer.Port = 587;
      smtpServer.EnableSsl = true;

      try
      {
        mail.From = new MailAddress(LeavON_Email);
        //mail.Bcc.Add(new MailAddress("saeed.dev125@gmail.com"));
        //  mail.To.Add(new MailAddress(userEmail));
        mail.Subject = $"Monthly Attendance Report";
        mail.Body = $"Dear {reportData.EmployeeName},\n\nPlease find the attached attendance report for your review. If you have any questions or need further clarification, please feel free to reach out. \n\nBest regards,\n";
        Console.WriteLine($"Sending email to => {userEmail}");
        Console.WriteLine($"Email Subject: {mail.Subject}");
        Console.WriteLine($"Email Body: {mail.Body}");


        using (MemoryStream memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A4, 50, 50, 25, 25);
          PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
          document.Open();

          PdfPTable table = new PdfPTable(2); // Two columns
          table.WidthPercentage = 100; // Table size is set to 100% of the page

          // Define a single-cell header
          PdfPCell header = new PdfPCell(new Phrase("Your Monthly Report", new Font(Font.FontFamily.HELVETICA, 14, Font.BOLD, BaseColor.WHITE)));
          header.Colspan = 2;
          header.HorizontalAlignment = Element.ALIGN_CENTER;
          header.BackgroundColor = new BaseColor(0, 51, 102); // Light blue background
          header.Border = Rectangle.BOTTOM_BORDER; // Only bottom border
          header.PaddingBottom = 10;
          table.AddCell(header);

          // Helper method to create a cell with specific styles
          void AddStyledCell(string content, int colspan = 1, bool isHeader = false)
          {
            PdfPCell cell = new PdfPCell(new Phrase(content, new Font(Font.FontFamily.HELVETICA, 12, isHeader ? Font.BOLD : Font.NORMAL, isHeader ? BaseColor.WHITE : BaseColor.BLACK)));
            cell.Colspan = colspan;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.Padding = 5;
            cell.Border = Rectangle.BOTTOM_BORDER;
            if (isHeader)
            {
              cell.BackgroundColor = new BaseColor(0, 51, 102); // Slightly darker blue for header cells
            }
            table.AddCell(cell);
          }
          Font dataFont = new Font(Font.FontFamily.HELVETICA, 15, Font.NORMAL);

          //// Adding dynamic data
          AddStyledCell($"Number working days in {monthName}: {totalWorkDays}", 2, true);
          AddStyledCell(reportData.EmployeeName.ToUpper() + " (" + reportData.EmployeeID + ")", 2, true);
          AddStyledCell("Average Entry Time:", 1, true);
          AddStyledCell(reportData.AverageTimeIn, 1);
          AddStyledCell("Average Exit Time:", 1, true);
          AddStyledCell(reportData.AverageTimeOut, 1);
          AddStyledCell("Working days of Employee", 1, true);
          AddStyledCell((totalWorkDays - ((reportData.AbsentDays))).ToString(), 1);
          AddStyledCell("Absents (Casual/Annual)", 1, true);
          AddStyledCell(reportData.AbsentDays.ToString(), 1);
          AddStyledCell("Work from home", 1, true);
          AddStyledCell(reportData.WorkFromHomeDays.ToString(), 1);
          AddStyledCell("Official Days off", 1, true);
          AddStyledCell(reportData.OfficialDaysOff.ToString(), 1);

          document.Add(table);
          document.Close();

          // Convert the memory stream to an array of bytes
          byte[] bytes = memoryStream.ToArray();

          // Attach the PDF as an email attachment
          mail.Attachments.Add(new Attachment(new MemoryStream(bytes), "MonthlyReport.pdf", "application/pdf"));
        }

        // Try to send the email and capture any exceptions
        try
        {
          smtpServer.Send(mail);
          Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
          Console.WriteLine("Email sending failed: {ex.Message}");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error sending email: " + ex.Message);
      }
    }
    public static int GetWorkingDays(int year, int month, int? userLeavePolicyID)
    {
      DateTime startOfMonth = new DateTime(year, month, 1);
      DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

      int workingDays = 0;
      for (DateTime date = startOfMonth; date <= endOfMonth; date = date.AddDays(1))
      {
        if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
        {
          workingDays++;
        }
      }

      var context = new LeaveONEntities();
      // Get all off days for the user that fall within the specified month and year
      var annualOffDays = context.AnnualOffDays
                                  .Where(x => x.UserLeavePolicyId == userLeavePolicyID
                                          && x.OffDay >= startOfMonth
                                          && x.OffDay <= endOfMonth)
                                  .ToList();


      // Subtract off days that are weekdays
      workingDays = workingDays - annualOffDays.Count();
      //foreach (var offDay in annualOffDays)
      //{
      //  workingDays--;
      //}

      return workingDays;
    }


    public static List<DateTime> GetWorkingDayByMonth(int year, int month, int? userLeavePolicyID)
    {
      DateTime startOfMonth = new DateTime(year, month, 1);
      DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

      List<DateTime> workingDates = new List<DateTime>();


      for (DateTime date = startOfMonth; date <= endOfMonth; date = date.AddDays(1))
      {
        if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
        {
          workingDates.Add(date);
        }
      }

      var context = new LeaveONEntities();
      // Get all off days for the user that fall within the specified month and year
      var annualOffDays = context.AnnualOffDays
                                  .Where(x => x.UserLeavePolicyId == userLeavePolicyID
                                          && x.OffDay >= startOfMonth
                                          && x.OffDay <= endOfMonth).Select(k => k.OffDay)
                                  .ToList();


      // Subtract off days that are weekdays
      workingDates.RemoveAll(date => annualOffDays.Contains(date));

      return workingDates;
    }
    // Generate Manager Report

    public void GeneratePDFManager1(
        List<EmployeeReportData> reportData,
        int totalWorkDays,
        string monthName,
        int year,
        ManagerDto managerDetail,
        bool legitimacyCheckForReports,
        List<EmailAndIDs> legitimacyCheckers, string hrBpEmail)
    {
      if (reportData == null || reportData.Count == 0)
      {
        Console.WriteLine("No report data available. Skipping email generation.");
        return;
      }

      using (var context = new LeaveONEntities())
      {
        //string managerEmailName = context.AspNetUsers
        //.Where(u => u.Id == managerEmail)
        //.Select(u => u.Email)
        //.FirstOrDefault();

        if (string.IsNullOrEmpty(managerDetail.Email))
          return;

        string managerName = managerDetail.Email.Split('@')[0].Replace('.', ' ');

        SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com")
        {
          UseDefaultCredentials = false,
          Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password),
          Port = 587,
          EnableSsl = true
        };

        MailMessage mail = new MailMessage
        {
          From = new MailAddress(LeavON_Email),
          Subject = string.Format("Monthly Attendance Summary Report - {0}", monthName),
          Body = string.Format(
                "Dear {0},\n\nPlease find the attached attendance report for your review.\n\nBest regards,\n",
                CultureInfo.CurrentCulture.TextInfo.ToTitleCase(managerName))
        };

        //if (legitimacyCheckForReports)
        //{
        //  foreach (var checker in legitimacyCheckers)
        //    mail.To.Add(checker.email);
        //}
        //else
        //{
        // mail.Bcc.Add("laiba.khan@intechww.com");
        // TO
        if (!String.IsNullOrEmpty(managerDetail.Email))
        {
          //mail.To.Add("laiba.khan@intechww.com");



          mail.To.Add("esswaqas@hotmail.com");
        }
        // CC
        if (!String.IsNullOrEmpty(hrBpEmail))
        {
          mail.CC.Add("esswaqas@hotmail.com");
        }

        // BCC
        // mail.Bcc.Add("laiba.khan@intechww.com");
        //mail.Bcc.Add("esswaqas@hotmail.com");

        //}

        using (var memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A3, 50, 50, 25, 25);
          PdfWriter.GetInstance(document, memoryStream);
          document.Open();

          // ================= HEADER TABLE (UNCHANGED LAYOUT) =================

          PdfPTable territoryTable = new PdfPTable(3);
          territoryTable.WidthPercentage = 100;

          Font titleFont = new Font(Font.FontFamily.HELVETICA, 20, Font.BOLD, BaseColor.WHITE);
          int monthNo = DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture).Month;
          int monthDays = DateTime.DaysInMonth(year, monthNo);
          string shortYear = string.Format("'{0:00}", year % 100);

          PdfPCell titleCell = new PdfPCell(new Phrase(
              string.Format("Attendance Report - {0} {1} (Total Days - {2})", monthName, shortYear, monthDays),
              titleFont))
          {
            Colspan = 9,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 10
          };
          territoryTable.AddCell(titleCell);

          Font headerFont = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD, BaseColor.WHITE);
          PdfPCell headerCell = new PdfPCell(new Phrase("Country-wise Working Days & Official Holidays", headerFont))
          {
            Colspan = 9,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 9
          };
          territoryTable.AddCell(headerCell);

          string[] territoryHeaders = { "Territory", "Working days", "Public / Official Holidays" };
          Font thFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);

          foreach (string h in territoryHeaders)
          {
            territoryTable.AddCell(new PdfPCell(new Phrase(h, thFont))
            {
              HorizontalAlignment = Element.ALIGN_CENTER,
              BackgroundColor = BaseColor.GRAY,
              Padding = 5
            });
          }

          var countryList = context.CountryNames
              .Where(c => c.Name != "United Kingdom" && c.Name != "Singapore" && c.Name != "Qatar" && c.Name != "China" && c.Name != "São Tomé and Príncipe" && c.Name != "Gabon")
              .OrderByDescending(c => c.Name == "Pakistan")
              .ToList();

          Font tdFont = new Font(Font.FontFamily.HELVETICA, 12);

          foreach (var country in countryList)
          {
            int monthNumber = monthNo;
            DateTime mStart = new DateTime(year, monthNumber, 1);
            DateTime mEnd = mStart.AddMonths(1).AddDays(-1);

            int? policyId = context.AspNetUsers
                .Where(u => u.CntryName == country.Name)
                .Select(u => u.UserLeavePolicyId)
                .FirstOrDefault();

            int holidayCount = 0;
            if (policyId.HasValue)
            {
              holidayCount = context.AnnualOffDays.Count(o =>
                  o.UserLeavePolicyId == policyId && o.OffDay >= mStart && o.OffDay <= mEnd);
            }

            int workingDays = GetWorkingDaysByCountry(year, monthNumber, country.Name) - holidayCount;

            territoryTable.AddCell(new PdfPCell(new Phrase(country.Name, tdFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            territoryTable.AddCell(new PdfPCell(new Phrase(workingDays.ToString(), tdFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            territoryTable.AddCell(new PdfPCell(new Phrase(holidayCount.ToString(), tdFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
          }

          document.Add(territoryTable);
          territoryTable.SpacingAfter = 20f;

          // ================= MAIN ATTENDANCE TABLE =================

          PdfPTable table = new PdfPTable(new float[]
              {
                  3f,   // ID
                  8f,   // Name
                  4.7f, // Casual Leave
                  4.9f, // Annual Leave
                  6f,   // Assigned Leave Quota
                  5f,   // Availed Leave (YTD)
                  6f,   // Available Leave Balance
                  6.2f, // Absents (YTD)
                  5f,   // Short Hours (YTD)
                  5.5f, // Avg. Entry Time
                  5.3f, // Avg. Exit Time
                  5.5f  // Avg. Time In Office
              });
          table.WidthPercentage = 100;
          table.SpacingBefore = 20f;

          PdfPCell managerCell = new PdfPCell(new Phrase(
              string.Format("Manager - {0}", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(managerName)), headerFont))
          {
            Colspan = 12,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 9,
            BorderWidthTop = 3f,
            BorderWidthLeft = 3f,
            BorderWidthRight = 3f
          };
          table.AddCell(managerCell);

          string[] headers = {
                "\n ID", "\n Name", "Casual \n Leave", "Annual \n Leave", "Assigned\n Leave\n Quota",
                "Availed Leave (YTD)", "Available\n Leave\n Balance", "Absents (YTD)",
                "Short Hours (YTD)", " Avg. Entry Time", " Avg. Exit Time", " Avg. Time\n In Office"
            };

          Font hf = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
          foreach (string h in headers)
          {
            table.AddCell(new PdfPCell(new Phrase(h, hf))
            {
              HorizontalAlignment = Element.ALIGN_CENTER,
              BackgroundColor = BaseColor.GRAY,
              Padding = 6
            });
          }

          Font dataFont = new Font(Font.FontFamily.HELVETICA, 12);
          string currentDept = null;

          foreach (var d in reportData.OrderBy(x => x.Department).ThenBy(x => x.EmployeeName))
          {
            if (currentDept != d.Department)
            {
              currentDept = d.Department;
              table.AddCell(new PdfPCell(new Phrase(currentDept, dataFont))
              {
                Colspan = 12,
                BackgroundColor = new BaseColor(200, 200, 200),
                Padding = 5,
                BorderWidthLeft = 3f,
                BorderWidthRight = 3f
              });
            }

            table.AddCell(new PdfPCell(new Phrase(d.EmployeeID.ToString(), dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BorderWidthLeft = 3f });
            table.AddCell(new PdfPCell(new Phrase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(d.EmployeeName), dataFont)));
            table.AddCell(new PdfPCell(new Phrase(d.CasualLeaves, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AnnualLeaves, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AssignedLeaveQuota, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AvailedLeave.ToString(), dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AvailableLeave, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AbsentDays.ToString(), dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.ShortHoursInMonth, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AverageTimeIn, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AverageTimeOut, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
            table.AddCell(new PdfPCell(new Phrase(d.AverageTimeInOffice, dataFont)) { HorizontalAlignment = Element.ALIGN_CENTER, BorderWidthRight = 3f });
          }

          document.Add(table);
          document.Close();

          mail.Attachments.Add(new Attachment(new MemoryStream(memoryStream.ToArray()),
              string.Format("ManagerReport_{0}.pdf", monthName), "application/pdf"));
        }

        try
        {
          smtpServer.Send(mail);
          Console.WriteLine("Email sent successfully ...");
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.Message);
        }
      }
    }


    //public void GeneratePDFManager1(List<EmployeeReportData> reportData, int totalWorkDays, string monthName, int year, string managerEmail, bool legitimacyCheckForReports, List<EmailAndIDs> legitimacyCheckers)
    //{
    //  if (reportData == null || reportData.Count == 0)
    //  {
    //    Console.WriteLine("No report data available. Skipping email generation.");
    //    return; // Exit the method if no data to process
    //  }

    //  LeaveONEntities context = new LeaveONEntities();
    //  // test DB LeaveONEntitiesTarget
    //  // LeaveONEntitiesTarget context = new LeaveONEntitiesTarget();

    //  string managerEmailName = context.AspNetUsers
    //     .Where(user => user.Id == managerEmail) // Check if the user Id matches the provided managerId
    //     .Select(user => user.Email) // Select the corresponding email
    //     .FirstOrDefault(); // Get the first match or null if no match

    //  string MangerName = managerEmailName.Split('@')[0].Replace('.', ' ');

    //  SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com")
    //  {
    //    UseDefaultCredentials = false,
    //    Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password),
    //    Port = 587,
    //    EnableSsl = true
    //  };

    //  MailMessage mail = new MailMessage
    //  {
    //    From = new MailAddress(LeavON_Email),
    //    Subject = $"Monthly Attendance Summary Report - {monthName}",
    //    Body = $"Dear {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(managerEmailName.Split('@')[0].Replace('.', ' '))},\n\nPlease find the attached attendance report for your review. If you have any questions or need further clarification, please feel free to reach out. \n\nBest regards,\n"
    //  };

    //  // Check if legitimacy checks are needed
    //  if (legitimacyCheckForReports)
    //  {
    //    foreach (var checker in legitimacyCheckers)
    //    {
    //      mail.To.Add(new MailAddress(checker.email));
    //    }
    //  }
    //  else
    //  {
    //    string mangerEmail = managerEmailName.ToLower();

    //    Console.WriteLine($"Email Subject: {mail.Subject}");
    //    Console.WriteLine($"Email Body: {mail.Body}");
    //    Console.WriteLine($"MangerName => {mangerEmail}");
    //    // Uncomment or adjust the following as needed
    //    //mail.To.Add("laiba.khan@intechww.com");
    //    //mail.Bcc.Add("yiwiyoj961@bdnets.com");

    //    // mail.To.Add("nouman.sial@intechww.com");
    //    // mail.To.Add("somia.waseem@acme-one.com");
    //    //mail.Bcc.Add("saeed.dev125@gmail.com");
    //    mail.Bcc.Add("salman.nazir@acme-one.com");

    //    //mail.To.Add(mangerEmail);
    //  }

    //  using (MemoryStream memoryStream = new MemoryStream())
    //  {
    //    Document document = new Document(PageSize.A3, 50, 50, 25, 25);
    //    PdfWriter.GetInstance(document, memoryStream);
    //    document.Open();

    //    // *****************************************
    //    // New Table: Territory, Total days, Working days, Public Holidays
    //    // *****************************************
    //    PdfPTable territoryTable = new PdfPTable(3); // 4 columns
    //    territoryTable.WidthPercentage = 100;

    //    // Title row above attendance table
    //    Font headTitleFont = new Font(Font.FontFamily.HELVETICA, 20, Font.BOLD, BaseColor.WHITE);
    //    int monthNo = DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture).Month;
    //    var monthDays = DateTime.DaysInMonth(year, monthNo);
    //    string shortYear = $"'{year % 100:D2}";

    //    PdfPCell headTitleCell = new PdfPCell(new Phrase($"Attendance Report - {monthName} {shortYear} (Total Days - {monthDays})", headTitleFont))
    //    {
    //      Colspan = 9, // Spanning all 9 columns
    //      HorizontalAlignment = Element.ALIGN_CENTER,
    //      BackgroundColor = new BaseColor(0, 51, 102),
    //      Padding = 10
    //    };
    //    territoryTable.AddCell(headTitleCell);

    //    Font headHeaderFont = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD, BaseColor.WHITE);
    //    //PdfPCell headHeaderCell2 = new PdfPCell(new Phrase($"Manager - {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(MangerName.ToLower())}", headHeaderFont))
    //    PdfPCell headHeaderCell2 = new PdfPCell(new Phrase($"Country-wise Working Days & Official Holidays", headHeaderFont))

    //    {
    //      Colspan = 9,
    //      HorizontalAlignment = Element.ALIGN_CENTER,
    //      BackgroundColor = new BaseColor(0, 51, 102),
    //      Padding = 9
    //    };
    //    territoryTable.AddCell(headHeaderCell2);

    //    // Define header cells for the new table
    //    string[] territoryHeaders = { "Territory", "Working days", "Public / Official Holidays" };
    //    Font territoryHeaderFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
    //    foreach (var header in territoryHeaders)
    //    {
    //      PdfPCell headerCell = new PdfPCell(new Phrase(header, territoryHeaderFont))
    //      {
    //        HorizontalAlignment = Element.ALIGN_CENTER,
    //        BackgroundColor = BaseColor.GRAY,
    //        Padding = 5
    //      };
    //      territoryTable.AddCell(headerCell);
    //    }

    //    // Query the CountryNames table from your database
    //    var countryData = context.CountryNames.ToList();

    //    // Filter out unwanted countries
    //    var filteredCountries = countryData
    //        .Where(c => c.Name != "United Kingdom" && c.Name != "Singapore" && c.Name != "Qatar" && c.Name != "China" && c.Name != "São Tomé and Príncipe")
    //        .ToList();

    //    // Find and move Pakistan to top
    //    var pakistan = filteredCountries.FirstOrDefault(c => c.Name == "Pakistan");
    //    if (pakistan != null)
    //    {
    //      filteredCountries.Remove(pakistan);
    //      filteredCountries.Insert(0, pakistan);
    //    }

    //    Font territoryDataFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL);
    //    foreach (var country in filteredCountries)
    //    {
    //      int monthNumber = DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture).Month;

    //      var daysInMonth = DateTime.DaysInMonth(year, monthNumber);
    //      var policyId = context.AspNetUsers
    //        .Where(u => u.CntryName == country.Name)
    //        .Select(u => u.UserLeavePolicyId)
    //        .FirstOrDefault();
    //            int publicHolidayCount = 0;

    //            if (policyId != null)
    //            {

    //              DateTime startOfMonth = new DateTime(year, monthNumber, 1);
    //              DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

    //              // Query AnnualOffDays for public holidays for this policy in the given month/year
    //              publicHolidayCount = context.AnnualOffDays
    //                                  .Where(o => o.UserLeavePolicyId == policyId
    //                                   && o.OffDay >= startOfMonth
    //                                   && o.OffDay <= endOfMonth)
    //                                  .Count();
    //            }
    //      // Adjust property names if they differ in your model
    //      PdfPCell cell = new PdfPCell(new Phrase(country.Name, territoryDataFont))
    //      {
    //        HorizontalAlignment = Element.ALIGN_CENTER
    //      };
    //      territoryTable.AddCell(cell);

    //      //int monthNumber = DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture).Month;

    //      //var daysInMonth = DateTime.DaysInMonth(year, monthNumber);

    //       // Total days 
    //      //cell = new PdfPCell(new Phrase(daysInMonth.ToString(), territoryDataFont))
    //      //{
    //      //  HorizontalAlignment = Element.ALIGN_CENTER
    //      //};

    //      //territoryTable.AddCell(cell);

    //      int totalWorkingDaysInMonth = GetWorkingDaysByCountry(year, monthNumber, country.Name);

    //      int wokringDays = totalWorkingDaysInMonth - publicHolidayCount;

    //      cell = new PdfPCell(new Phrase(wokringDays.ToString(), territoryDataFont))
    //      {
    //        HorizontalAlignment = Element.ALIGN_CENTER
    //      };
    //      territoryTable.AddCell(cell);

    //      cell = new PdfPCell(new Phrase(publicHolidayCount.ToString(), territoryDataFont))
    //      {
    //        HorizontalAlignment = Element.ALIGN_CENTER
    //      };
    //      territoryTable.AddCell(cell);
    //    }

    //    // Add the territory table to the document (it will appear above the main attendance table)
    //    document.Add(territoryTable);

    //    territoryTable.SpacingAfter = 20f;

    //    // *****************************************
    //    // Existing Attendance Table
    //    // *****************************************

    //    //PdfPTable table = new PdfPTable(new float[] { 2, 3, 2, 2, 2, 2, 3, 2.5f, 2.5f });
    //    //table.WidthPercentage = 100;
    //    //table.SpacingBefore = 20f;

    //    // Title row above attendance table
    //    //Font titleFont = new Font(Font.FontFamily.HELVETICA, 20, Font.BOLD, BaseColor.WHITE);
    //    //PdfPCell titleCell = new PdfPCell(new Phrase($"Attendance Report - {monthName} {year}", titleFont))
    //    //{
    //    //  Colspan = 9, // Spanning all 9 columns
    //    //  HorizontalAlignment = Element.ALIGN_CENTER,
    //    //  BackgroundColor = new BaseColor(0, 51, 102),
    //    //  Padding = 10
    //    //};
    //    //table.AddCell(titleCell);

    //    //Font headerFont = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD, BaseColor.WHITE);
    //    //PdfPCell headerCell2 = new PdfPCell(new Phrase($"Manager: {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(MangerName.ToLower())}", headerFont))
    //    //{
    //    //  Colspan = 9,
    //    //  HorizontalAlignment = Element.ALIGN_CENTER,
    //    //////////  BackgroundColor = new BaseColor(0, 51, 102),
    //    //  Padding = 9
    //    //};
    //    //table.AddCell(headerCell2);

    //    // Column headers for attendance table
    //    //string[] headers = { "Employee ID", "Employee\n Name", "Average \nEntry Time", "Average \nExit Time", "Total\n Days", "Working Days", "Absent/Leaves \nDays", "Work From \nHome Days", "Official Days \nOff" };
    //    // Create a 12-column table; adjust the float array as needed for column widths
    //    PdfPTable table = new PdfPTable(new float[] { 3f, 8f, 4.7f, 4.9f, 6f, 5f, 6f, 4.9f , 5f, 5.5f, 5.3f, 5.5f });
    //    table.WidthPercentage = 100;
    //    table.SpacingBefore = 20f;
    //    table.SplitLate = false;
    //    string shortMonthName = monthName.Substring(0, 3);

    //    PdfPCell managerHeaderCell = new PdfPCell(new Phrase($"Manager - {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(MangerName.ToLower())}", headHeaderFont))
    //    {
    //      Colspan = 12, // Match your column count
    //      HorizontalAlignment = Element.ALIGN_CENTER,
    //      BackgroundColor = new BaseColor(0, 51, 102),
    //      Padding = 9,
    //      BorderWidthTop = 3f,
    //      BorderWidthLeft = 3f,
    //      BorderWidthRight = 3f,
    //      BorderColorTop = BaseColor.BLACK,
    //      BorderColorLeft = BaseColor.BLACK,
    //      BorderColorRight = BaseColor.BLACK,
    //    };
    //    table.AddCell(managerHeaderCell);


    //    // Define header cells for the attendance table
    //    string[] headers = { "\n ID", "\n Name", "Casual \n Leave", "Annual \n Leave", "Assigned\n Leave\n Quota", $"Availed Leave", "Available\n Leave\n Balance","Absent \n Leave",  "Short\n Hours\n In Month", " Avg. Entry Time", " Avg. Exit Time", " Avg. Time\n In Office" };
    //    Font headerFont2 = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
    //    //foreach (var header in headers)
    //    //{
    //    //  PdfPCell colHeaderCell = new PdfPCell(new Phrase(header, headerFont2))
    //    //  {
    //    //    HorizontalAlignment = Element.ALIGN_CENTER,
    //    //    BackgroundColor = BaseColor.GRAY,
    //    //    Padding = 5,
    //    //  };
    //    //  table.AddCell(colHeaderCell);
    //    //}

    //    for (int i = 0; i < headers.Length; i++)
    //    {
    //      PdfPCell colHeaderCell = new PdfPCell(new Phrase(headers[i], headerFont2))
    //      {
    //        HorizontalAlignment = Element.ALIGN_CENTER,
    //        BackgroundColor = BaseColor.GRAY,
    //        Padding = 5,
    //        PaddingTop = 10f,
    //        BorderWidthTop = 3f,
    //        UseAscender = true,
    //        UseDescender = true
    //      };

    //      // Apply bold border

    //      if(i == 0)
    //      {
    //          colHeaderCell.BorderWidthLeft = 3f;
    //          colHeaderCell.BorderWidthTop = 0f;
    //      } else if (i == 4 || i == 10)
    //      {
    //        colHeaderCell.BorderWidthLeft = 1.5f;
    //        colHeaderCell.BorderWidthTop = 0f;
    //      } else if (i == 3 || i == 9)
    //      {
    //        colHeaderCell.BorderWidthRight = 1.5f;
    //        colHeaderCell.BorderWidthTop = 0f;
    //      } else if (i == 12)
    //      {
    //          colHeaderCell.BorderWidthRight = 3f;
    //          colHeaderCell.BorderWidthTop = 0f;
    //      } else 
    //      {
    //        colHeaderCell.BorderWidthTop = 0f;
    //      }

    //      table.AddCell(colHeaderCell);
    //    }

    //    // Data rows for attendance table

    //    Font dataFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL);
    //    Font nameFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
    //    string currentDepartment = null;
    //    Font departmnetFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.BLACK);
    //    BaseColor departmentRowColor = new BaseColor(200, 200, 200); // Light gray background for department rows

    //    // Iterate through sorted reportData (sorting logic remains unchanged)
    //    var orderedData = reportData.OrderBy(d => d.Department).ThenBy(d => d.EmployeeName).ToList();
    //    var firstEmployee = orderedData.First();
    //    var lastEmployee = orderedData.Last();

    //    foreach (var data in orderedData)
    //    {
    //      // Add a new row for department change if needed
    //      if (currentDepartment != data.Department)
    //      {
    //        currentDepartment = data.Department;
    //        PdfPCell departmentCell = new PdfPCell(new Phrase(currentDepartment, departmnetFont))
    //        {
    //          Colspan = 12, // Span across all 12 columns
    //          HorizontalAlignment = Element.ALIGN_LEFT,
    //          BackgroundColor = departmentRowColor,
    //          Padding = 5,
    //          PaddingLeft = 10f,
    //          BorderWidthLeft = 3f,
    //          BorderWidthRight = 3f,
    //          UseAscender = true,
    //          UseDescender = true
    //        };
    //        table.AddCell(departmentCell);
    //      }

    //      PdfPCell cell;

    //      // Employee ID
    //      cell = new PdfPCell(new Phrase(data.EmployeeID.ToString(), dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER,
    //        BorderWidthLeft = 3f,
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee)
    //      {
    //        cell.BorderWidthBottom = 3f;
    //        cell.PaddingBottom = 7f;  // adjust as needed
    //      }
    //      table.AddCell(cell);

    //      // Employee Name
    //      cell = new PdfPCell(new Phrase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(data.EmployeeName.ToLower()), dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_LEFT,
    //        BorderWidthRight = 1.5f
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Casual Leaves
    //      cell = new PdfPCell(new Phrase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(data.CasualLeaves), dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER,
    //        BorderWidthRight = 1.5f
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Annual Leaves
    //      cell = new PdfPCell(new Phrase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(data.AnnualLeaves), dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER,
    //        BorderWidthRight = 1.5f
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Assigned Leave Quota (dummy data)
    //      cell = new PdfPCell(new Phrase(data.AssignedLeaveQuota, dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER,
    //        BorderWidthLeft = 1.5f,
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      //Opening Balance Leave
    //      //cell = new PdfPCell(new Phrase(data.BalanceLeave, dataFont))
    //      //{
    //      //  HorizontalAlignment = PdfPCell.ALIGN_CENTER
    //      //};
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      //if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      //table.AddCell(cell);

    //      // Availed Leaves Days (existing)
    //      cell = new PdfPCell(new Phrase(data.AvailedLeave.ToString(), dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Available Leaves Balance 
    //      cell = new PdfPCell(new Phrase(data.AvailableLeave, dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Compensatory Leaves (dummy data)
    //      //cell = new PdfPCell(new Phrase(data.CompensatoryLeave, dataFont))
    //      //{
    //      //  HorizontalAlignment = PdfPCell.ALIGN_CENTER
    //      //};
    //      ////if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      //if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      //table.AddCell(cell);

    //      // Official Days Off (existing)
    //      //cell = new PdfPCell(new Phrase(data.OffcialDaysOff.ToString(), dataFont))
    //      //{
    //      //  HorizontalAlignment = PdfPCell.ALIGN_CENTER
    //      //};
    //      //table.AddCell(cell);

    //      // Absent Leaves
    //      cell = new PdfPCell(new Phrase(data.AbsentDays.ToString(), dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER,
    //      };
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Short Hours In Month (dummy data)
    //      cell = new PdfPCell(new Phrase(data.ShortHoursInMonth, dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER,
    //        BorderWidthRight = 1.5f
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Average Entry Time (existing)
    //      cell = new PdfPCell(new Phrase(data.AverageTimeIn, dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER,
    //        BorderWidthLeft = 1.5f
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Average Exit Time (existing)
    //      cell = new PdfPCell(new Phrase(data.AverageTimeOut, dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);

    //      // Average Time In Office (dummy data, update if you have actual data)
    //      cell = new PdfPCell(new Phrase(data.AverageTimeInOffice, dataFont))
    //      {
    //        HorizontalAlignment = PdfPCell.ALIGN_CENTER,
    //        BorderWidthRight = 3f
    //      };
    //      //if (data == firstEmployee) cell.BorderWidthTop = 3f;
    //      if (data == lastEmployee) cell.BorderWidthBottom = 3f;
    //      table.AddCell(cell);
    //    }


    //    // Add the attendance table to the document
    //    document.Add(table);
    //    document.Close();
    //    mail.Attachments.Add(new Attachment(new MemoryStream(memoryStream.ToArray()), $"ManagerReport_{monthName}.pdf", "application/pdf"));
    //  }

    //  try
    //  {
    //    smtpServer.Send(mail);
    //    Console.WriteLine("Email sent successfully ...");
    //  }
    //  catch (Exception ex)
    //  {
    //    Console.WriteLine($"Failed to send email to {(legitimacyCheckForReports ? "legitimacy checkers" : "")}: {ex.Message}");
    //  }
    //}


    public void GeneratePDFManager(List<EmployeeReportData> reportData, int totalWorkDays, string monthName, bool legitimacyCheckForReports, List<EmailAndIDs> legitimacyCheckers, HashSet<string> sentEmails)
    {

      if (reportData == null || reportData.Count == 0)
      {
        Console.WriteLine("No report data available. Skipping email generation.");
        return; // Exit the method if no data to process
      }
      SmtpClient smtpServer = new SmtpClient("mail.smtp2go.com")
      {
        UseDefaultCredentials = false,
        Credentials = new System.Net.NetworkCredential(LeavON_Email, LeavON_Password),
        Port = 587,
        EnableSsl = true
      };
      var groupedByManager = reportData
     .GroupBy(data => string.IsNullOrEmpty(data.ManagerEmail) ? data.Manager2Email : data.ManagerEmail)
     .ToList();

      //   var groupedByManager = reportData
      //.GroupBy(data => data.ManagerEmail)
      //.ToList();
      //  var groupedByDepartment = reportData.GroupBy(emp => emp.Department.Trim()).ToList();


      foreach (var group in groupedByManager)
      {
        string managerEmail = group.Key;

        if (string.IsNullOrEmpty(managerEmail))
        {
          Console.WriteLine($"No email found for manager of these employees.");
          continue;
        }


        // Skip if email already processed
        if (sentEmails.Contains(managerEmail.ToLower()))
        {
          Console.WriteLine($"Skipping email for {managerEmail}, already processed.");
          continue;
        }


        // Add to processed emails
        sentEmails.Add(managerEmail.ToLower());

        MailMessage mail = new MailMessage
        {
          From = new MailAddress(LeavON_Email),
          Subject = $"Monthly Attendance Report",
          Body = $"Dear {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(managerEmail.Split('@')[0].Replace('.', ' '))},\n\nPlease find the attached attendance report for your review. If you have any questions or need further clarification, please feel free to reach out. \n\nBest regards,\n"
        };

        // Check if legitimacy checks are needed
        if (legitimacyCheckForReports)
        {
          foreach (var checker in legitimacyCheckers)
          {
            mail.To.Add(new MailAddress(checker.email));
          }
        }
        else
        {
          mail.To.Add(new MailAddress(managerEmail));
          //mail.To.Add(new MailAddress("laiba.khan @intechww.com"));

        }

        using (MemoryStream memoryStream = new MemoryStream())
        {
          Document document = new Document(PageSize.A3, 50, 50, 25, 25);
          PdfWriter.GetInstance(document, memoryStream);
          document.Open();

          //PdfPTable table = new PdfPTable(9); // Assuming 8 columns as before
          PdfPTable table = new PdfPTable(new float[] { 3, 2, 2, 2, 2, 2, 2, 2, 2 });
          table.WidthPercentage = 100;

          // Header
          Font headerFont = new Font(Font.FontFamily.HELVETICA, 15, Font.BOLD, BaseColor.WHITE);
          PdfPCell headerCell = new PdfPCell(new Phrase($"Number of working days in {monthName}: {totalWorkDays}", headerFont))
          {
            Colspan = 9,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 8
          };
          table.AddCell(headerCell);
          string MangerName = managerEmail.Split('@')[0].Replace('.', ' ');
          PdfPCell headerCell2 = new PdfPCell(new Phrase($"Manager: {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(MangerName.ToLower())}", headerFont))
          {
            Colspan = 9,
            HorizontalAlignment = Element.ALIGN_CENTER,
            BackgroundColor = new BaseColor(0, 51, 102),
            Padding = 9
          };
          table.AddCell(headerCell2);

          // Column headers
          string[] headers = { "Employee Name", "Employee ID", "Department", "Average Entry Time", "Average Exit Time", "Total Working Days", "Absent Days", "Work From Home Days", "Official Days Off" };
          Font headerFont2 = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE);
          foreach (string header in headers)
          {
            PdfPCell colHeaderCell = new PdfPCell(new Phrase(header, headerFont2))
            {
              BackgroundColor = new BaseColor(0, 76, 153),
              HorizontalAlignment = Element.ALIGN_CENTER,
              Padding = 5
            };
            table.AddCell(colHeaderCell);
          }

          // Data rows
          Font dataFont = new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL);
          Font nameFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
          BaseColor yellowColor = new BaseColor(255, 255, 0);

          foreach (var data in group)
          {
            PdfPCell cell;

            //cell = new PdfPCell(new Phrase(data.EmployeeName, nameFont))
            cell = new PdfPCell(new Phrase(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(data.EmployeeName.ToLower()), nameFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.EmployeeID.ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.Department, dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.AverageTimeIn, dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.AverageTimeOut, dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase((totalWorkDays - data.AbsentDays).ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.AbsentDays.ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER,
              BackgroundColor = data.AbsentDays >= 3 ? yellowColor : BaseColor.WHITE
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.WorkFromHomeDays.ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);

            cell = new PdfPCell(new Phrase(data.OfficialDaysOff.ToString(), dataFont))
            {
              HorizontalAlignment = PdfPCell.ALIGN_CENTER
            };
            table.AddCell(cell);
          }

          document.Add(table);
          document.Close();

          // Attach the PDF
          mail.Attachments.Add(new Attachment(new MemoryStream(memoryStream.ToArray()), $"TeamAttendanceReport_{managerEmail}.pdf", "application/pdf"));
        }

        try
        {
          smtpServer.Send(mail);
          Console.WriteLine("Email send successfully ...");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Failed to send email to {(legitimacyCheckForReports ? "legitimacy checkers" : "")}: {ex.Message}");
        }
      }
    }


    public static string ConvertSecondsToReadableTime(long totalSeconds)
    {
      long hours = totalSeconds / 3600;
      long minutes = (totalSeconds % 3600) / 60;
      long seconds = totalSeconds % 60;

      return $"{hours} hours, {minutes} minutes, {seconds} seconds";
    }
    private List<string> GetPolicyWiseManagerIDs()
    {
      // return new List<string> { "c6d11b88-5ad0-4d7b-8c95-d5cd797e6e27" };

      using (var context = new LeaveONEntities())
      {
        var targetPolicyIDs = new[] { 1047, 1048 };

        // Step 1: Get all unique ManagerIDs and Manager2IDs from users
        var referencedManagerIds = context.AspNetUsers
            .SelectMany(user => new[] { user.ManagerID, user.Manager2ID })
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
           .ToList();

        // Step 2: From those referenced managers, filter those who have the required UserLeavePolicyId
        var validManagerIds = context.AspNetUsers
            .Where(manager =>
                referencedManagerIds.Contains(manager.Id) &&
                manager.UserLeavePolicyId.HasValue &&
                targetPolicyIDs.Contains(manager.UserLeavePolicyId.Value))
            .Select(manager => manager.Id)
            .Distinct()
            .ToList();

        // Optional: Debug output
        foreach (var managerID in validManagerIds)
        {
          Console.WriteLine($"ManagerID => {managerID}");
        }

        return validManagerIds;
      }
    }

    private List<DepartmentName> GetDepartmentList()
    {
      using (var context = new LeaveONEntities())
      {
        return context.DepartmentNames.ToList();
      }
    }
    private List<ManagerDto> GetManagersListById(List<string> list)
    {
      using (var context = new LeaveONEntities())
      {
        // return context.AspNetUsers.Where(u => list.Any(k=>k== u.Id )).ToList();
        return context.AspNetUsers
        .Where(u => list.Contains(u.Id))
        .Select(u => new ManagerDto
        {
          Id = u.Id,
          UserName = u.UserName,
          Email = u.Email,
          PhoneNumber = u.PhoneNumber,
          ManagerName = u.ManagerName,
          DepartmentName = u.DepartmentName
        })
        .ToList();
      }
    }
    private List<ManagerDto> GetManagersIDs()
    {
      using (var context = new LeaveONEntities())
      // using (var context = new LeaveONEntitiesTarget())
      {
        //var allowedDepartments = new[] { "Human Resource", "Finance", "IS&T", "iCSG" };
        var allowedDepartments = new[] { "IS&T", "Human Resource" };

        // Fetch all users who have either ManagerID or Manager2ID
        var managersIDs = context.AspNetUsers
                      .Where(u => !string.IsNullOrEmpty(u.ManagerID)).Distinct().Select(u => new ManagerDto
                      {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        ManagerName = u.ManagerName,
                        DepartmentName = u.DepartmentName
                      })
        .ToList();

        //var managersIDs = context.AspNetUsers
        //  .Where(user => !string.IsNullOrEmpty(user.ManagerID) || !string.IsNullOrEmpty(user.Manager2ID))
        //  .SelectMany(user => new[] { user.ManagerID, user.Manager2ID }) // Select both IDs
        //    .Where(id => !string.IsNullOrEmpty(id)) // Filter out null or empty IDs
        //    .Distinct() // Ensure unique IDs
        //    .ToList();

        //    var managersIDs = context.AspNetUsers
        //.Where(user =>
        //    allowedDepartments.Contains(user.DepartmentName) &&
        //    (!string.IsNullOrEmpty(user.ManagerID) || !string.IsNullOrEmpty(user.Manager2ID)))
        //.SelectMany(user => new[] { user.ManagerID, user.Manager2ID }) // Select both IDs
        //.Where(id => !string.IsNullOrEmpty(id)) // Filter out null or empty IDs
        //.Distinct() // Ensure unique IDs
        //.ToList();


        // var managersIDs = context.AspNetUsers
        //.Where(user => !string.IsNullOrEmpty(user.ManagerID) || !string.IsNullOrEmpty(user.Manager2ID)) // Filter out null or empty ManagerIDs
        //.Select(user => !string.IsNullOrEmpty(user.ManagerID) ? user.ManagerID : user.Manager2ID) // Select only ManagerID
        //.Distinct() // Ensure unique IDs (optional)
        //.ToList();

        // Manager ID to exclude
        var excludedManagerID = "708ada81-4409-48a0-905b-769b7b0da6b0";

        // Exclude the specific manager ID
        var filteredManagersIDs = managersIDs
            .Where(h => h.Id != excludedManagerID)
            .ToList();


        foreach (var managerID in managersIDs)
        {
          Console.WriteLine($"managerID => { managerID.Id}");
        }
        return filteredManagersIDs;
      }
    }

    private List<string> GetManagerEmailByDepartment(string department)
    {
      using (var context = new LeaveONEntities())
      {
        var managers = context.AspNetUserClaims.Where(x => x.ClaimType == department && x.isReportEmail == true)
        .Select(x => x.UserId).ToList();

        var managerEmails = new List<string>();
        foreach (var manager in managers)
        {
          // Fetch the email of the manager, make sure only one email is added
          var email = context.AspNetUsers
          .Where(x => x.Id == manager)
          .Select(x => x.Email)
          .FirstOrDefault(); // Ensures you get a single result or null, not a collection

          if (!string.IsNullOrEmpty(email))
          {
            managerEmails.Add(email);
          }
        }

        return managerEmails;
      }
    }


    public static int GetWorkingDaysByCountry(int year, int month, string countryName)
    {
      DateTime startOfMonth = new DateTime(year, month, 1);
      DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

      HashSet<DayOfWeek> weekendDays = GetWeekendDaysForCountry(countryName);

      int workingDays = 0;
      for (DateTime date = startOfMonth; date <= endOfMonth; date = date.AddDays(1))
      {
        if (!weekendDays.Contains(date.DayOfWeek))
        {
          workingDays++;
        }
      }

      return workingDays;
    }

    private static HashSet<DayOfWeek> GetWeekendDaysForCountry(string countryName)
    {
      switch (countryName.Trim().ToLower())
      {
        case "egypt":
        case "iraq":
        case "oman":
        case "qatar":
        case "saudi arabia":
          return new HashSet<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday };

        case "united arab emirates":
          return new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday }; // changed in Jan 2022

        // All others follow standard Saturday–Sunday
        case "angola":
        case "germany":
        case "kazakhstan":
        case "nigeria":
        case "pakistan":
        case "singapore":
        case "united kingdom":
        case "united states":
        default:
          return new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };
      }
    }

  }
  public class ManagerDto
  {
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string ManagerName { get; set; }
    public string DepartmentName { get; set; }
  }
}


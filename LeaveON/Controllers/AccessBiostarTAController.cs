
using LeaveON.UtilityClasses;
using Microsoft.AspNet.Identity;
using Repository.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using TimeManagement.Models;
using System.Globalization;
using LeaveON.Models;

namespace LeaveON.Controllers
{
  [Authorize(Roles = "Admin,Manager,User")]
  public class AccessBiostarTAController : Controller
  {
    private BioStarEntities dbBioStar = new BioStarEntities();
    LeaveONEntities dbLeaveOn = new LeaveONEntities();

    //enum ReadersIn {538600343, 38677, 538595648, 35816,540093375,540093369,540093374 , 547241993  ,540133115 , 538848767 }

    List<int> LstCardReadersIn = new List<int> { 540099805, 543726490, 38677, 538595648, 35816, 540093375, 540093369, 540093374, 547241993, 540133115, 538848767, 540095692, 540130033, 540130042 };

    private Task<List<TimeData>> ConnectToDBandReturnAbsentees(string startDate, string endDate, List<int> UserIds)
    {

      //string ReqMonthYear = "07-2019";

      //List<string> dateAttr = ReqMonthYear.Split('-').ToList();

      //string connection = @"Data Source=10.1.10.28;Initial Catalog=BiostarAC;User Id=sa;Password=@Intech#123;";
      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      SqlCommand cmd;
      SqlDataReader dr;
      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan TotalTime = new TimeSpan();
      TimeSpan TotalWorkingHours = new TimeSpan();

      DateTime reqDate1 = DateTime.ParseExact(startDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
      DateTime reqDate2 = DateTime.ParseExact(endDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
      reqDate2 = reqDate2.AddDays(1);//otherwise it wont show last day data.
      string mm;
      string yy;

      con.Open();

      foreach (int Id in UserIds)
      {
        int UserId = Id;

        cmd = new SqlCommand("select user_id,devdt,bsevtdt,DEVID,devnm from punchlog where USER_ID ='" + UserId + "' and devdt BETWEEN '" + reqDate1.ToString("yyyy-MM-dd") + "' AND '" + reqDate2.ToString("yyyy-MM-dd") + "' order by devdt", con);//last good

        dr = cmd.ExecuteReader();
        DataTable dt = new DataTable();

        //------
        string UserName = string.Empty;
        string timeZone = string.Empty;
        string countryName = string.Empty;
        AspNetUser aspNetUser = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId);
        UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
        string depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
        string userLeavePolicyDescription = string.Empty;
        if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
        if (aspNetUser.IsRelocated == true)
        {
          //in case relocate
          timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp).TimeZone;
          countryName = aspNetUser.CntryNameTemp;
        }
        else
        {
          timeZone = aspNetUser.CountryName.TimeZone;
          countryName = aspNetUser.CntryName;
        }
        //---------

        //////////////////
        //Creating dummy datatable for testing

        DataColumn dc = new DataColumn("USER_ID", typeof(String));
        dt.Columns.Add(dc);

        dc = new DataColumn("devdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("bsevtdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("DEVID", typeof(Int32));
        dt.Columns.Add(dc);
        dc = new DataColumn("DETAILS", typeof(string));
        dt.Columns.Add(dc);
        //devnm
        dc = new DataColumn("devnm", typeof(string));
        dt.Columns.Add(dc);
        while (dr.Read())
        {
          // for each row from the database, add the retrieved table name to the list
          DataRow dtrw = dt.NewRow();
          //Convert.ToDateTime(ConvertToCountryTimeZone(dt, k, timeZone)).Day
          dtrw[0] = dr["USER_ID"];
          dtrw[1] = ConvertToCountryTimeZoneNew((DateTime)dr["devdt"], timeZone);
          //dtrw[2] = ConvertToCountryTimeZoneNew((DateTime)dr["bsevtdt"], timeZone);
          dtrw[2] = dr["bsevtdt"];
          dtrw[3] = dr["DEVID"];

          if (LstCardReadersIn.Contains(Convert.ToInt32(dr["DEVID"])))
          {
            dtrw[4] = "IN";
          }
          else
          {
            dtrw[4] = "OUT";
          }
          dtrw[5] = dr["devnm"];
          dt.Rows.Add(dtrw);//this will add the row at the end of the datatable          
        }
        dr.Close();
        DataView view = dt.DefaultView;
        view.Sort = "devdt ASC";
        dt = view.ToTable();



        //dt.Load(dr);

        int rowsCount = dt.Rows.Count;
        if (rowsCount <= 0) continue;


        DateTime firstDateTime = (DateTime)dt.Rows[0]["devdt"];//ConvertToCountryTimeZone(dt, 0, timeZone);//(DateTime)dt.Rows[0]["SRVDT"];
        DateTime lastDateTime = (DateTime)dt.Rows[rowsCount - 1]["devdt"];//ConvertToCountryTimeZone(dt, rowsCount - 1, timeZone);//(DateTime)dt.Rows[rowsCount - 1]["SRVDT"];
        int firstDay = firstDateTime.Day;
        int lastDay = lastDateTime.Day;
        int dayCounter = 1;
        List<int> LstEmptyDays = new List<int>();
        //---
        TimeData attendance;
        DateTime timeIn;
        DateTime timeOut;
        DateTime firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
        DateTime lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
        DateTime blankDateTime = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//DateTime.Today;
        int EmptyDays = 0;
        bool IsCardIn = false;
        bool IsCardOut = false;
        bool lastsemiIn = false;
        int ThisMonthTotalDays;
        TimeSpan ThidDayWorkingHours = new TimeSpan();
        //string depName = string.Empty;
        int k = 0;//-1;
                  //for (int k = dayCounter; k < thisDay; k++)
                  //{
                  //  LstEmptyDays.Add(k);
                  //}

        for (int j = 0; j <= rowsCount - 1; j++)
          {

            //if (k >= j)             //                                |
            //{                           //                                |
            //  continue;   // Skip the remainder of this iteration. -----+
            //}
            firstDateTime = (DateTime)dt.Rows[j]["devdt"];//ConvertToCountryTimeZone(dt, j, timeZone);//(DateTime)dt.Rows[j]["SRVDT"];

          if (firstDateTime.Day != firstDay)
          {//its mean new date started. so add all previois date calcuation here and add to list

            if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
            {
              TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
              //UserName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId).UserName;
              //UserName = UserName.Substring(0, UserName.IndexOf('@')).Replace(".", " ");
              //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
              attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn) };

              LstTimeData.Add(attendance);
              TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);

            }

            //------reIntiallize variables to next date calculations
            firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
            lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
            ThidDayWorkingHours = new TimeSpan();
            IsCardIn = false; IsCardOut = false;

            //--- this loop is to cater empty days. for example time data started from 7th days. so to cater fist 6 days this loop is required
            //for (int k = dayCounter; k < firstDay; k++)
            //{
            //  LstEmptyDays.Add(k);
            //}

            //dayCounter = firstDay + 1;
            firstDay = firstDateTime.Day;


          }

          k = j + 1;
          //while ((k <= rowsCount - 1) && LstCardReadersIn.Contains((int)dt.Rows[k]["DEVUID"]))//find last semi in
          //{
          //  k++; //Last semi in
          //  lastsemiIn = true;
          //}
          //if (lastsemiIn==true)
          //{
          //  while ((k <= rowsCount - 1) && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVUID"]))//find last semi out
          //  {
          //    k++; 
          //    lastsemiIn = false;
          //  }
          //  k--;//Last semi out
          //}


          //------get actual working hour of this date--------
          //if ((j + 1 <= rowsCount - 1) && (int)dt.Rows[j]["TNAKEY"] == 1 && (int)dt.Rows[j + 1]["TNAKEY"] == 2 &&
          if ((k <= rowsCount - 1) && LstCardReadersIn.Contains((int)dt.Rows[j]["DEVID"]) && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]) &&
          Convert.ToDateTime(dt.Rows[j]["devdt"]).Day == firstDay && Convert.ToDateTime(dt.Rows[k]["devdt"]).Day == firstDay)
          {
            if (IsCardIn == false)
            {//get first time in
              firsTimeIn = (DateTime)dt.Rows[j]["devdt"];//ConvertToCountryTimeZone(dt, j, timeZone);//(DateTime)dt.Rows[j]["SRVDT"];
              lastTimeOut = (DateTime)dt.Rows[k]["devdt"];//ConvertToCountryTimeZone(dt, k, timeZone);
              IsCardIn = true;
              //UserId = Convert.ToInt32(dt.Rows[j]["USRID"]);
            }

            timeIn = (DateTime)dt.Rows[j]["devdt"];//ConvertToCountryTimeZone(dt, j, timeZone);//(DateTime)dt.Rows[j]["SRVDT"];
            timeOut = (DateTime)dt.Rows[k]["devdt"];//ConvertToCountryTimeZone(dt, k, timeZone);//(DateTime)dt.Rows[j + 1]["SRVDT"];
            TimeSpan workingHour = (timeOut - timeIn);
            ThidDayWorkingHours = ThidDayWorkingHours.Add(workingHour);

            //IsCardIn = true; IsCardOut = true;

            //if (IsCardIn == true && (int)dt.Rows[j]["TNAKEY"] == 2)
            if (IsCardIn == true && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]))
            {//get last time out
              IsCardOut = true;
              lastTimeOut = (DateTime)dt.Rows[k]["devdt"];//ConvertToCountryTimeZone(dt, k, timeZone);//(DateTime)dt.Rows[j]["SRVDT"];
            }
          }


        }//datetime for end here

        //add last date to list here as loop ended

        if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
        {
          TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
          //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
          //set data 
          attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn) };

          LstTimeData.Add(attendance);
          TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);
        }

        //dayCounter = firstDay + 1;
        //for (int k = dayCounter; k <= ThisMonthTotalDays; k++)
        //{
        //  LstEmptyDays.Add(k);
        //}



        //----------------------
        //int LastDayOfMonth = DateTime.DaysInMonth(reqDate.Year, reqDate.Month);
        //IQueryable<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.StartDate.Day >= 1 && x.EndDate.Day <= LastDayOfMonth && x.StartDate.Year == reqDate.Year && x.UserId == users[0].Id).AsQueryable<Leave>();
        //IQueryable<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.StartDate.Day >= 1 && x.EndDate.Day <= LastDayOfMonth && x.StartDate.Month == reqDate.Month && x.StartDate.Year == reqDate.Year && x.UserId == users[0].Id).AsQueryable<Leave>();
        ThisMonthTotalDays = DateTime.DaysInMonth(firstDateTime.Year, firstDateTime.Month);
        //List<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.StartDate.Day >= 1 && x.EndDate.Day <= ThisMonthTotalDays && x.StartDate.Month == reqDate.Month && x.StartDate.Year == reqDate.Year && x.UserId == aspNetUser.Id).ToList<Leave>();
        List<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.StartDate.Day >= 1 && x.EndDate.Day <= ThisMonthTotalDays && x.StartDate.Month == firstDateTime.Month && x.StartDate.Year == firstDateTime.Year && x.UserId == aspNetUser.Id).ToList<Leave>();
        //List<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.UserId == UserGuidId).ToList<Leave>();
        List<TimeData> offDays = new List<TimeData>();

        //------get Leave days
        //string empName = users[0].UserName;
        //string biostarEmpName = "";//LstEmpData[0].EmployeeName;
        int iEmpNum = aspNetUser.BioStarEmpNum.Value;
        if (aspNetUser.UserLeavePolicyId != null)
        {
          int UserLeavePolicyId = aspNetUser.UserLeavePolicyId.Value;
          foreach (Leave leave in thisMonthsLeaves)
          {
            for (int i = 0; i < leave.TotalDays; i++)
            {
              TimeData leaveDay = new TimeData
              {
                EmployeeName = UserName,
                EmployeeNumber = leave.AspNetUser.BioStarEmpNum.Value,
                TimeZone = countryName,
                Department = depName,
                Policy = userLeavePolicyDescription,
                Date = leave.StartDate.AddDays(i),
                Day = leave.StartDate.AddDays(i).ToString("dddd"),
                Status = leave.Reason
              };
              offDays.Add(leaveDay);
            }
          }

          //------get natioanl holidays
          foreach (AnnualOffDay annualHoliday in dbLeaveOn.AnnualOffDays.Where(x => x.OffDay.Value.Month == firstDateTime.Month && x.OffDay.Value.Year == firstDateTime.Year && x.UserLeavePolicyId == UserLeavePolicyId).ToList<AnnualOffDay>())
          {         
            TimeData annualOff = new TimeData
            {
              EmployeeName = UserName,
              EmployeeNumber = iEmpNum,
              TimeZone = countryName,
              Department = depName,
              Policy = userLeavePolicyDescription,
              Date = annualHoliday.OffDay.Value,
              Day = annualHoliday.OffDay.Value.ToString("dddd"),
              Status = annualHoliday.Description
            };
            //dayCntr += 1;
            offDays.Add(annualOff);
          }

          //------- get weekEndDays



          //foreach (TimeData offday in leaveNholidays.ToList())
          //{
          //  int? delThisDay = LstEmptyDays.FirstOrDefault(x => x == offday.Date.Day);

          //  if (delThisDay != null && delThisDay > 0)
          //  {
          //    LstEmptyDays.Remove(delThisDay.Value);
          //  }

          //}


        }
        //----------------------

        //foreach (int emptyday in LstEmptyDays)
        //{
        //  blankDateTime = DateTime.ParseExact(dateAttr[1] + "/" + dateAttr[0] + "/" + emptyday.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        //  attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, Date = blankDateTime, Day = blankDateTime.DayOfWeek.ToString(), Status = "Absent" };
        //  LstTimeData.Add(attendance);
        //}
        List<int> LstThisMonthsWeekEnds = new List<int>();
        if (aspNetUser.UserLeavePolicy == null || string.IsNullOrEmpty(aspNetUser.UserLeavePolicy.WeeklyOffDays))
        {
          LstThisMonthsWeekEnds = GetWeekEndList(firstDateTime.Year, firstDateTime.Month, "6,0");
        }
        else
        {
          LstThisMonthsWeekEnds = GetWeekEndList(firstDateTime.Year, firstDateTime.Month, aspNetUser.UserLeavePolicy.WeeklyOffDays);
        }


        //foreach (int weekEndDay in LstThisMonthsWeekEnds)
        //{
        //  TimeData thisWeekEnd = LstTimeData.FirstOrDefault(x => x.Date.Day == weekEndDay);
        //  if (thisWeekEnd != null)
        //  {
        //    thisWeekEnd.Status = "Weekend";
        //  }
        //  else
        //  {
        //    //DateTime weekEndDate = DateTime.ParseExact(dateAttr[1] + "/" + dateAttr[0] + "/" + weekEndDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        //    mm = firstDateTime.Month.ToString("00");
        //    yy = firstDateTime.Year.ToString();

        //    DateTime weekEndDate = DateTime.ParseExact(yy + "/" + mm + "/" + weekEndDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        //    TimeData weekEndOffDate = new TimeData
        //    {
        //      EmployeeName = UserName,
        //      EmployeeNumber = iEmpNum,
        //      TimeZone = countryName,
        //      Policy = userLeavePolicyDescription,
        //      Date = weekEndDate,
        //      Day = weekEndDate.ToString("dddd"),
        //      Status = "Weekend"
        //    };
        //    //dayCntr += 1;
        //    offDays.Add(weekEndOffDate);
        //  }
        //}
        //LstTimeData.AddRange(offDays);

        //--------------------------------------------------------

        //ThisMonthTotalDays = DateTime.DaysInMonth(firstDateTime.Year, firstDateTime.Month);
        //for (int absentDay = 1; absentDay <= ThisMonthTotalDays; absentDay++)
        //{
        //  if (LstTimeData.FirstOrDefault(x => x.Date.Day == absentDay) == null)
        //  {
        //    mm = firstDateTime.Month.ToString("00");
        //    yy = firstDateTime.Year.ToString();
        //    blankDateTime = DateTime.ParseExact(yy + "/" + mm + "/" + absentDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        //    //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
        //    attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = blankDateTime, Day = blankDateTime.DayOfWeek.ToString(), Status = "Absent" };
        //    LstTimeData.Add(attendance);
        //  }
        //  //LstEmptyDays.Add(k);
        //}

        //-----------------------------------

        foreach (DateTime day in EachDay(reqDate1, reqDate2.AddDays(-1)))
        {
          if (LstTimeData.FirstOrDefault(x => x.Date == day.Date && x.EmployeeNumber == UserId) == null && day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday)
          {
           var lastExistingDate = LstTimeData
                       .OrderByDescending(x => x.Date)
                       .Select(x => x.Date.Date)
                       .FirstOrDefault();

            if (day > lastExistingDate)
            {
              break;
            }
            mm = firstDateTime.Month.ToString("00");
            yy = firstDateTime.Year.ToString();
            blankDateTime = day;
            //DateTime.ParseExact(yy + "/" + mm + "/" + absentDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
            //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
            //set Absent Data
            attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = blankDateTime, Day = blankDateTime.DayOfWeek.ToString(), Status = "Absent" };
            LstTimeData.Add(attendance);
          }
        }
        // print it or whatever



      }


      //-----------to avaid showing current month all data which is not happend yet
      foreach (var itm in LstTimeData.ToList())
      {
        if (itm.Date > DateTime.Now.Date)
        {
          LstTimeData.Remove(itm);
        }
      }

      ViewBag.TotalHours = TotalTime.TotalHours.ToString("N2");
      ViewBag.TotalWorkingHours = TotalWorkingHours.TotalHours.ToString("N2");
      con.Close();
      LstTimeData.RemoveAll(x => x.Status == null);
      return Task.FromResult(LstTimeData);
    }


    private Task<List<TimeData>> GetAbsenteesData(string formattedStartDate, string formattedEndDate, List<int> UserIds)
    {
      DateTime startDate = DateTime.ParseExact(formattedStartDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      DateTime endDate = DateTime.ParseExact(formattedEndDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan totalWorkingHoursAllUsers = TimeSpan.Zero;
      TimeSpan totalTimeAllUsers = TimeSpan.Zero;

      // Fetch attendance data for each user
      foreach (int UserId in UserIds)
      {
        // Fetch attendance records for this user within the provided date range
        var attendanceRecords = dbLeaveOn.AttendanceDatas
            .Where(a => a.BioStarEmpNum == UserId && a.CreatedDate >= startDate && a.CreatedDate <= endDate)
            .GroupBy(a => new { a.BioStarEmpNum, Date = DbFunctions.TruncateTime(a.CreatedDate) })
            .Select(g => g.FirstOrDefault()) // Take only the first record for each day per user
            .OrderBy(a => a.CreatedDate)
            .ToList();

        if (!attendanceRecords.Any()) continue; // If no records found, skip to the next user

        // Process each attendance record
        foreach (var record in attendanceRecords)
        {
          // Skip weekends
          if (record.CreatedDate.HasValue &&
              (record.CreatedDate.Value.DayOfWeek == DayOfWeek.Saturday || record.CreatedDate.Value.DayOfWeek == DayOfWeek.Sunday))
          {
            continue;
          }
          if (record.IsAbsent == true)
          { 
          DateTime? timeInNullable = record.FirstPunchIn;
          DateTime? timeOutNullable = record.LastPunchOut;
          DateTime timeIn = timeInNullable ?? DateTime.MinValue;
          DateTime timeOut = timeOutNullable ?? (timeInNullable ?? DateTime.MinValue);
          TimeSpan totalTime = timeIn != DateTime.MinValue && timeOut != DateTime.MinValue ? timeOut - timeIn : TimeSpan.Zero;
          TimeSpan totalWorkingHours = record.TotalWorkHours.HasValue && record.TotalWorkHours > 0
              ? TimeSpan.FromSeconds((double)record.TotalWorkHours)
              : TimeSpan.Zero;
          string day = record.CreatedDate.HasValue ? record.CreatedDate.Value.ToString("dddd") : "N/A";

          totalWorkingHoursAllUsers += totalWorkingHours;
          totalTimeAllUsers += totalTime;

          // Map the data directly from AttendanceData and determine Status based on IsAbsent
          LstTimeData.Add(new TimeData()
          {
            EmployeeName = record.UserName,
            EmployeeNumber = record.BioStarEmpNum ?? 0,
            Department = record.DepartmentName,
            TimeZone = record.CountryName,
            Policy = record.UserLeavePolicyID,
            Date = record.CreatedDate ?? DateTime.MinValue,
            Day = day,
            TimeIn = timeIn,
            TimeOut = timeOut,
            TotalTime = totalTime,
            WorkingHours = totalWorkingHours,
            Status = "Absent",
          });
        }
       }
      }

      ViewBag.TotalWorkingHours = totalWorkingHoursAllUsers.TotalHours.ToString("N2");
      ViewBag.TotalHours = totalTimeAllUsers.TotalHours.ToString("N2");

      return Task.FromResult(LstTimeData);
    }

    private Task<List<TimeData>> ConnectToDBandReturnCountriesData(string startDate, string endDate, List<int> UserIds)
    {

      //string ReqMonthYear = "07-2019";

      //List<string> dateAttr = ReqMonthYear.Split('-').ToList();

      //string connection = @"Data Source=10.1.10.28;Initial Catalog=BiostarAC;User Id=sa;Password=@Intech#123;";
      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      SqlCommand cmd;
      SqlDataReader dr;
      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan TotalTime = new TimeSpan();
      TimeSpan TotalWorkingHours = new TimeSpan();

      DateTime reqDate1 = DateTime.ParseExact(startDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
      DateTime reqDate2 = DateTime.ParseExact(endDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture);
      reqDate2 = reqDate2.AddDays(1);//otherwise it wont show last day data.
      string mm;
      string yy;

      con.Open();
      //UserIds = UserIds.Where(x => x == 2205).ToList();
      foreach (int Id in UserIds)
      {
        int UserId = Id;
        //cmd = new SqlCommand("select * from T_LG202106 where USRID = '9919' and SRVDT>= '2021-06-01' AND SRVDT<= '2021-06-30' order by EVTLGUID", con);
        //cmd = new SqlCommand("select * from T_LG201901 where USRID = '2205' order by EVTLGUID", con);


        //cmd = new SqlCommand("select USRID,SRVDT,DEVDT,DEVUID from T_LG" + dateAttr[1] + dateAttr[0] + " where USRID ='" + UserId + "' and TNAKEY <> 0 order by DEVDT", con);//last good
        //cmd = new SqlCommand("select USRID,SRVDT,DEVDT,DEVUID from T_LG" + dateAttr[1] + dateAttr[0] + " where USRID ='" + UserId + "' order by DEVDT", con);//last good
        //cmd = new SqlCommand("select user_id,devdt,bsevtdt,DEVID,devnm from punchlog where USER_ID ='" + UserId + "' and devdt BETWEEN '" + dateAttr[1] + "-" + dateAttr[0] + "-01' AND '" + toYear + "-" + toMonth + "-" + "1" + "' order by devdt", con);//last good
        cmd = new SqlCommand("select user_id,devdt,bsevtdt,DEVID,devnm from punchlog where USER_ID ='" + UserId + "' and devdt BETWEEN '" + reqDate1.ToString("yyyy-MM-dd") + "' AND '" + reqDate2.ToString("yyyy-MM-dd") + "' order by devdt", con);//last good

        //cmd = new SqlCommand("select USRID,SRVDT,DEVDT,DEVUID from T_LG" + "2022" + "08" + " where USRID ='" + 2064 + "' and SRVDT BETWEEN '2022/08/31' AND '2022/09/01' order by DEVDT", con);

        dr = cmd.ExecuteReader();
        DataTable dt = new DataTable();

        //------
        string UserName = string.Empty;
        string timeZone = string.Empty;
        string countryName = string.Empty;
        AspNetUser aspNetUser = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId);
        UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
        string depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
        string userLeavePolicyDescription = string.Empty;
        if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
        if (aspNetUser.IsRelocated == true)
        {
          //in case relocate
          timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp).TimeZone;
          countryName = aspNetUser.CntryNameTemp;
        }
        else
        {
          timeZone = aspNetUser.CountryName.TimeZone;
          countryName = aspNetUser.CntryName;
        }
        //---------

        //////////////////
        //Creating dummy datatable for testing

        DataColumn dc = new DataColumn("USER_ID", typeof(String));
        dt.Columns.Add(dc);

        dc = new DataColumn("devdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("bsevtdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("DEVID", typeof(Int32));
        dt.Columns.Add(dc);
        dc = new DataColumn("DETAILS", typeof(string));
        dt.Columns.Add(dc);
        //devnm
        dc = new DataColumn("devnm", typeof(string));
        dt.Columns.Add(dc);
        while (dr.Read())
        {
          // for each row from the database, add the retrieved table name to the list
          DataRow dtrw = dt.NewRow();
          //Convert.ToDateTime(ConvertToCountryTimeZone(dt, k, timeZone)).Day
          dtrw[0] = dr["USER_ID"];
          dtrw[1] = (DateTime)dr["devdt"];
          //dtrw[2] = ConvertToCountryTimeZoneNew((DateTime)dr["bsevtdt"], timeZone);
          dtrw[2] = dr["bsevtdt"];
          dtrw[3] = dr["DEVID"];

          if (LstCardReadersIn.Contains(Convert.ToInt32(dr["DEVID"])))
          {
            dtrw[4] = "IN";
          }
          else
          {
            dtrw[4] = "OUT";
          }
          dtrw[5] = dr["devnm"];
          dt.Rows.Add(dtrw);//this will add the row at the end of the datatable          
        }
        dr.Close();
        DataView view = dt.DefaultView;
        view.Sort = "devdt ASC";
        dt = view.ToTable();



        //dt.Load(dr);

        int rowsCount = dt.Rows.Count;
        if (rowsCount <= 0) continue;


        DateTime firstDateTime = (DateTime)dt.Rows[0]["devdt"];//ConvertToCountryTimeZone(dt, 0, timeZone);//(DateTime)dt.Rows[0]["SRVDT"];
        DateTime lastDateTime = (DateTime)dt.Rows[rowsCount - 1]["devdt"];//ConvertToCountryTimeZone(dt, rowsCount - 1, timeZone);//(DateTime)dt.Rows[rowsCount - 1]["SRVDT"];
        int firstDay = firstDateTime.Day;
        int lastDay = lastDateTime.Day;
        int dayCounter = 1;
        List<int> LstEmptyDays = new List<int>();
        //---
        TimeData attendance;
        DateTime timeIn;
        DateTime timeOut;
        DateTime firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
        DateTime lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
        DateTime blankDateTime = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//DateTime.Today;
        int EmptyDays = 0;
        bool IsCardIn = false;
        bool IsCardOut = false;
        bool lastsemiIn = false;
        int ThisMonthTotalDays;
        TimeSpan ThidDayWorkingHours = new TimeSpan();
        //string depName = string.Empty;
        int k = 0;//-1;
                  //for (int k = dayCounter; k < thisDay; k++)
                  //{
                  //  LstEmptyDays.Add(k);
                  //}

        for (int j = 0; j <= rowsCount - 1; j++)
        {

          //if (k >= j)             //                                |
          //{                           //                                |
          //  continue;   // Skip the remainder of this iteration. -----+
          //}
          string shortCountryName = dt.Rows[j]["devnm"].ToString().Substring(0,3).Trim();
          switch (shortCountryName)
          {
            case "PK":
              timeZone = "Pakistan Standard Time";
              countryName = "Pakistan";
              break;
            case "PAK":
              timeZone = "Pakistan Standard Time";
              countryName = "Pakistan";
              break;
            case "UAE":
              timeZone = "Arab Standard Time";
              countryName = "United Arab Emirates";
              break;
            case "KSA":
              timeZone = "Arab Standard Time";
              countryName = "Saudi Arabia";
              break;
            case "GBR":
              timeZone = "GMT Standard Time";
              countryName = "United Kingdom";
              break;
            case "USA":
              timeZone = "Central Standard Time";
              countryName = "United States";
              break;
            case "NGA":
              timeZone = "W. Central Africa Standard Time";
              countryName = "Nigeria";
              break;
            case "EGY":
              timeZone = "Egypt Standard Time";
              countryName = "Egypt";
              break;
            case "IRQ":
              timeZone = "Arabic Standard Time";
              countryName = "Iraq";
              break;
            case "OMN":
              timeZone = "Arabian Standard Time";
              countryName = "Oman";
              break;
            case "QAT":
              timeZone = "Arab Standard Time";
              countryName = "Qatar";
              break;
            case "AGO":
              timeZone = "W. Central Africa Standard Time";
              countryName = "Angola";
              break;
            case "KAZ":
              timeZone = "West Asia Standard Time";
              countryName = "Kazakhstan";
              break;
            // Add more cases for other countries
            default:
              timeZone = aspNetUser.CountryName.TimeZone; // Or handle the default case based on your requirements
              countryName = aspNetUser.CountryName.Name;
              break;
          }
          firstDateTime = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);//ConvertToCountryTimeZone(dt, j, timeZone);//(DateTime)dt.Rows[j]["SRVDT"];

          if (firstDateTime.Day != firstDay)
          {//its mean new date started. so add all previois date calcuation here and add to list

            if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
            {
              TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
              //UserName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId).UserName;
              //UserName = UserName.Substring(0, UserName.IndexOf('@')).Replace(".", " ");
              //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
              attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn) };

              LstTimeData.Add(attendance);
              TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);

            }

            //------reIntiallize variables to next date calculations
            firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
            lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
            ThidDayWorkingHours = new TimeSpan();
            IsCardIn = false; IsCardOut = false;

            //--- this loop is to cater empty days. for example time data started from 7th days. so to cater fist 6 days this loop is required
            //for (int k = dayCounter; k < firstDay; k++)
            //{
            //  LstEmptyDays.Add(k);
            //}

            //dayCounter = firstDay + 1;
            firstDay = firstDateTime.Day;


          }

          k = j + 1;
          //while ((k <= rowsCount - 1) && LstCardReadersIn.Contains((int)dt.Rows[k]["DEVUID"]))//find last semi in
          //{
          //  k++; //Last semi in
          //  lastsemiIn = true;
          //}
          //if (lastsemiIn==true)
          //{
          //  while ((k <= rowsCount - 1) && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVUID"]))//find last semi out
          //  {
          //    k++; 
          //    lastsemiIn = false;
          //  }
          //  k--;//Last semi out
          //}


          //------get actual working hour of this date--------
          //if ((j + 1 <= rowsCount - 1) && (int)dt.Rows[j]["TNAKEY"] == 1 && (int)dt.Rows[j + 1]["TNAKEY"] == 2 &&
          if ((k <= rowsCount - 1) && LstCardReadersIn.Contains((int)dt.Rows[j]["DEVID"]) && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]) &&
          Convert.ToDateTime(dt.Rows[j]["devdt"]).Day == firstDay && Convert.ToDateTime(dt.Rows[k]["devdt"]).Day == firstDay)
          {
            if (IsCardIn == false)
            {//get first time in
              firsTimeIn = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);//ConvertToCountryTimeZone(dt, j, timeZone);//(DateTime)dt.Rows[j]["SRVDT"];
              lastTimeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone); ;//ConvertToCountryTimeZone(dt, k, timeZone);
              IsCardIn = true;
              //UserId = Convert.ToInt32(dt.Rows[j]["USRID"]);
            }

            timeIn = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);
            timeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
            TimeSpan workingHour = (timeOut - timeIn);
            ThidDayWorkingHours = ThidDayWorkingHours.Add(workingHour);

            //IsCardIn = true; IsCardOut = true;

            //if (IsCardIn == true && (int)dt.Rows[j]["TNAKEY"] == 2)
            if (IsCardIn == true && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]))
            {//get last time out
              IsCardOut = true;
              lastTimeOut = (DateTime)dt.Rows[k]["devdt"];//ConvertToCountryTimeZone(dt, k, timeZone);//(DateTime)dt.Rows[j]["SRVDT"];
            }
          }


        }//datetime for end here

        //add last date to list here as loop ended

        if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
        {
          TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
          //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
          attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn) };

          LstTimeData.Add(attendance);
          TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);
        }

        //dayCounter = firstDay + 1;
        //for (int k = dayCounter; k <= ThisMonthTotalDays; k++)
        //{
        //  LstEmptyDays.Add(k);
        //}



        //----------------------
        //int LastDayOfMonth = DateTime.DaysInMonth(reqDate.Year, reqDate.Month);
        //IQueryable<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.StartDate.Day >= 1 && x.EndDate.Day <= LastDayOfMonth && x.StartDate.Year == reqDate.Year && x.UserId == users[0].Id).AsQueryable<Leave>();
        //IQueryable<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.StartDate.Day >= 1 && x.EndDate.Day <= LastDayOfMonth && x.StartDate.Month == reqDate.Month && x.StartDate.Year == reqDate.Year && x.UserId == users[0].Id).AsQueryable<Leave>();
        ThisMonthTotalDays = DateTime.DaysInMonth(firstDateTime.Year, firstDateTime.Month);
        //List<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.StartDate.Day >= 1 && x.EndDate.Day <= ThisMonthTotalDays && x.StartDate.Month == reqDate.Month && x.StartDate.Year == reqDate.Year && x.UserId == aspNetUser.Id).ToList<Leave>();
        List<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.StartDate.Day >= 1 && x.EndDate.Day <= ThisMonthTotalDays && x.StartDate.Month == firstDateTime.Month && x.StartDate.Year == firstDateTime.Year && x.UserId == aspNetUser.Id).ToList<Leave>();
        //List<Leave> thisMonthsLeaves = dbLeaveOn.Leaves.Where(x => x.UserId == UserGuidId).ToList<Leave>();
        List<TimeData> offDays = new List<TimeData>();

        //------get Leave days
        //string empName = users[0].UserName;
        //string biostarEmpName = "";//LstEmpData[0].EmployeeName;
        int iEmpNum = aspNetUser.BioStarEmpNum.Value;
        if (aspNetUser.UserLeavePolicyId != null)
        {
          int UserLeavePolicyId = aspNetUser.UserLeavePolicyId.Value;
          foreach (Leave leave in thisMonthsLeaves)
          {
            for (int i = 0; i < leave.TotalDays; i++)
            {
              TimeData leaveDay = new TimeData
              {
                EmployeeName = UserName,
                EmployeeNumber = leave.AspNetUser.BioStarEmpNum.Value,
                TimeZone = countryName,
                Department = depName,
                Policy = userLeavePolicyDescription,
                Date = leave.StartDate.AddDays(i),
                Day = leave.StartDate.AddDays(i).ToString("dddd"),
                Status = leave.Reason
              };
              offDays.Add(leaveDay);
            }
          }

          //------get natioanl holidays
          foreach (AnnualOffDay annualHoliday in dbLeaveOn.AnnualOffDays.Where(x => x.OffDay.Value.Month == firstDateTime.Month && x.OffDay.Value.Year == firstDateTime.Year && x.UserLeavePolicyId == UserLeavePolicyId).ToList<AnnualOffDay>())
          {
            TimeData annualOff = new TimeData
            {
              EmployeeName = UserName,
              EmployeeNumber = iEmpNum,
              TimeZone = countryName,
              Department = depName,
              Policy = userLeavePolicyDescription,
              Date = annualHoliday.OffDay.Value,
              Day = annualHoliday.OffDay.Value.ToString("dddd"),
              Status = annualHoliday.Description
            };
            //dayCntr += 1;
            offDays.Add(annualOff);
          }

          //------- get weekEndDays



          //foreach (TimeData offday in leaveNholidays.ToList())
          //{
          //  int? delThisDay = LstEmptyDays.FirstOrDefault(x => x == offday.Date.Day);

          //  if (delThisDay != null && delThisDay > 0)
          //  {
          //    LstEmptyDays.Remove(delThisDay.Value);
          //  }

          //}


        }
        //----------------------

        //foreach (int emptyday in LstEmptyDays)
        //{
        //  blankDateTime = DateTime.ParseExact(dateAttr[1] + "/" + dateAttr[0] + "/" + emptyday.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        //  attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, Date = blankDateTime, Day = blankDateTime.DayOfWeek.ToString(), Status = "Absent" };
        //  LstTimeData.Add(attendance);
        //}
        List<int> LstThisMonthsWeekEnds = new List<int>();
        if (aspNetUser.UserLeavePolicy == null || string.IsNullOrEmpty(aspNetUser.UserLeavePolicy.WeeklyOffDays))
        {
          LstThisMonthsWeekEnds = GetWeekEndList(firstDateTime.Year, firstDateTime.Month, "6,0");
        }
        else
        {
          LstThisMonthsWeekEnds = GetWeekEndList(firstDateTime.Year, firstDateTime.Month, aspNetUser.UserLeavePolicy.WeeklyOffDays);
        }


        //foreach (int weekEndDay in LstThisMonthsWeekEnds)
        //{
        //  TimeData thisWeekEnd = LstTimeData.FirstOrDefault(x => x.Date.Day == weekEndDay);
        //  if (thisWeekEnd != null)
        //  {
        //    thisWeekEnd.Status = "Weekend";
        //  }
        //  else
        //  {
        //    //DateTime weekEndDate = DateTime.ParseExact(dateAttr[1] + "/" + dateAttr[0] + "/" + weekEndDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        //    mm = firstDateTime.Month.ToString("00");
        //    yy = firstDateTime.Year.ToString();

        //    DateTime weekEndDate = DateTime.ParseExact(yy + "/" + mm + "/" + weekEndDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        //    TimeData weekEndOffDate = new TimeData
        //    {
        //      EmployeeName = UserName,
        //      EmployeeNumber = iEmpNum,
        //      TimeZone = countryName,
        //      Policy = userLeavePolicyDescription,
        //      Date = weekEndDate,
        //      Day = weekEndDate.ToString("dddd"),
        //      Status = "Weekend"
        //    };
        //    //dayCntr += 1;
        //    offDays.Add(weekEndOffDate);
        //  }
        //}
        LstTimeData.AddRange(offDays);

        //--------------------------------------------------------

        //ThisMonthTotalDays = DateTime.DaysInMonth(firstDateTime.Year, firstDateTime.Month);
        //for (int absentDay = 1; absentDay <= ThisMonthTotalDays; absentDay++)
        //{
        //  if (LstTimeData.FirstOrDefault(x => x.Date.Day == absentDay) == null)
        //  {
        //    mm = firstDateTime.Month.ToString("00");
        //    yy = firstDateTime.Year.ToString();
        //    blankDateTime = DateTime.ParseExact(yy + "/" + mm + "/" + absentDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
        //    //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
        //    attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = blankDateTime, Day = blankDateTime.DayOfWeek.ToString(), Status = "Absent" };
        //    LstTimeData.Add(attendance);
        //  }
        //  //LstEmptyDays.Add(k);
        //}

        //-----------------------------------
        TimeData timeData = null;
        string abc = "abc";
        if (UserId == 2205)
        {
          abc = "";
        }
        foreach (DateTime day in EachDay(reqDate1, reqDate2.AddDays(-1)))
        {
          timeData = LstTimeData.FirstOrDefault(x => x.Date == day.Date && x.EmployeeNumber == UserId);
          if (timeData == null && day.DayOfWeek != DayOfWeek.Saturday && day.DayOfWeek != DayOfWeek.Sunday)
          {
            mm = firstDateTime.Month.ToString("00");
            yy = firstDateTime.Year.ToString();
            blankDateTime = day;//DateTime.ParseExact(yy + "/" + mm + "/" + absentDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
                                //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
            attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = blankDateTime, Day = blankDateTime.DayOfWeek.ToString(), Status = "Absent" };
            LstTimeData.Add(attendance);
          }
          else if (timeData != null && timeData.EmployeeNumber == UserId && (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday))
          {
            timeData.Status = "Weekend";
            //mm = firstDateTime.Month.ToString("00");
            //yy = firstDateTime.Year.ToString();
            //blankDateTime = day;//DateTime.ParseExact(yy + "/" + mm + "/" + absentDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
            ////depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
            //attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = blankDateTime, Day = blankDateTime.DayOfWeek.ToString(), Status = "Weekend" };
            //LstTimeData.Add(attendance);
          }
          //else if (LstTimeData.FirstOrDefault(x => x.Date == day.Date) != null && (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday))
          //{
          //  mm = firstDateTime.Month.ToString("00");
          //  yy = firstDateTime.Year.ToString();
          //  blankDateTime = day;//DateTime.ParseExact(yy + "/" + mm + "/" + absentDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
          //  //depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
          //  attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = blankDateTime, Day = blankDateTime.DayOfWeek.ToString(), Status = "Weekend" };
          //  LstTimeData.Add(attendance);
          //}


        }
        // print it or whatever



      }


      //-----------to avaid showing current month all data which is not happend yet
      foreach (var itm in LstTimeData.ToList())
      {
        if (itm.Date > DateTime.Now.Date)
        {
          LstTimeData.Remove(itm);
        }
      }

      ViewBag.TotalHours = TotalTime.TotalHours.ToString("N2");
      ViewBag.TotalWorkingHours = TotalWorkingHours.TotalHours.ToString("N2");
      con.Close();
      //LstTimeData.RemoveAll(x => x.Status == null);
      return Task.FromResult(LstTimeData);
    }
    public IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
    {
      for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
        yield return day;
    }
    private Task<List<TimeData>> ConnectToDBandReturnWorkingHours(string ReqMonthYear, List<int> UserIds)
    {
      List<string> dateAttr = ReqMonthYear.Split('-').ToList();
      string countryName = string.Empty;
      string previousCountryName = string.Empty;
      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      SqlCommand cmd;
      SqlDataReader dr;
      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan TotalTime = new TimeSpan();
      TimeSpan TotalWorkingHours = new TimeSpan();
      List<string> logg = new List<string>();
      DateTime reqDate = DateTime.ParseExact(dateAttr[1] + "/" + dateAttr[0] + "/01", "yyyy/MM/dd", CultureInfo.InvariantCulture);

      int ThisMonthTotalDays = DateTime.DaysInMonth(int.Parse(dateAttr[1]), int.Parse(dateAttr[0]));
      int toMonth = int.Parse(dateAttr[0]) + 1;
      int toYear = int.Parse(dateAttr[1]);
      if (toMonth == 13)
      {
        toMonth = 1;
        toYear = int.Parse(dateAttr[1]) + 1;
      }
      con.Open();
      foreach (int Id in UserIds)
      {
        int UserId = Id;//Assigns the current UserId for processing.
        cmd = new SqlCommand("select user_id,devdt,bsevtdt,DEVID,devnm from punchlog where USER_ID ='" + UserId + "' and devdt BETWEEN '" + dateAttr[1] + "-" + dateAttr[0] + "-01' AND '" + toYear + "-" + toMonth + "-" + "1" + "' order by devdt", con);//last good
        dr = cmd.ExecuteReader();//SqlCommand and SqlDataReader (cmd, dr) are initialized.

        DataTable dt = new DataTable();//A new DataTable dt is created for storing data related to the current user.

        string UserName = string.Empty;
        string timeZone = string.Empty;
        string userGuidId = string.Empty;

        AspNetUser aspNetUser = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId);

        //processing
        UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
        string depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
        string userLeavePolicyDescription = string.Empty;
        userGuidId = aspNetUser.Id;
        if (aspNetUser.CountryName == null)
        { logg.Add(aspNetUser.UserName); dr.Close(); continue; }
        if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
        if (aspNetUser.IsRelocated == true)
        {
          //in case relocate
          timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp).TimeZone;
          countryName = aspNetUser.CntryNameTemp;
        }
        else
        {
          timeZone = aspNetUser.CountryName.TimeZone;
          countryName = aspNetUser.CntryName;
        }
        //Processes user data to get UserName, depName, timeZone, countryName, and leave policy data.
        //Checks for relocation and adjusts timeZone and countryName accordingly.

        //Creating and Filling the DataTable:
        DataColumn dc = new DataColumn("USER_ID", typeof(String));
        dt.Columns.Add(dc);

        dc = new DataColumn("devdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("bsevtdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("DEVID", typeof(Int32));
        dt.Columns.Add(dc);

        dc = new DataColumn("DETAILS", typeof(string));
        dt.Columns.Add(dc);

        dc = new DataColumn("devnm", typeof(string));
        dt.Columns.Add(dc);
        //Columns are added to dt to represent user attendance data.
        //A loop reads data from dr and populates dt with rows representing the attendance logs.
        //↓

        while (dr.Read())//This loop iterates over each row returned by the SQL query executed above.
        {
          // for each row from the database, add the retrieved table name to the list
          DataRow dtrw = dt.NewRow(); //A new DataRow (dtrw) is created to store data for each attendance log.
          dtrw[0] = dr["USER_ID"];
          dtrw[1] = (DateTime)dr["devdt"];
          dtrw[2] = dr["bsevtdt"];
          dtrw[3] = dr["DEVID"];

          //The next lines check if the 'DEVID' is in the list LstCardReadersIn.
          //Depending on the result, "IN" or "OUT" is stored in the fifth column of dtrw.
          if (LstCardReadersIn.Contains(Convert.ToInt32(dr["DEVID"])))
          {
            dtrw[4] = "IN";
          }
          else
          {
            dtrw[4] = "OUT";
          }

          //dtrw[5] = dr["devnm"];: The 'devnm' (device name) column from dr is assigned to the sixth column of dtrw
          dtrw[5] = dr["devnm"];
          dt.Rows.Add(dtrw);
          //this will add the row at the end of the datatable
          //This step is repeated for each row returned by the SQL query, building up the attendance data for the user.
        }


        //After Reading All Data
        dr.Close();
        DataView view = dt.DefaultView;
        view.Sort = "devdt ASC";
        //The DataTable dt's default view is sorted by 'devdt' in ascending order: view.Sort = "devdt ASC";.


        //Processing Sorted Attendance Data
        dt = view.ToTable();

        int rowsCount = dt.Rows.Count;
        if (rowsCount <= 0) continue;

        /*

        Initialization of Variables for Attendance Calculation:
        Variables like firstDateTime, lastDateTime, firstDay, lastDay, and counters are initialized. 
        These will be used to track the dates, times, and other details for each punch.

        */
        DateTime firstDateTime = (DateTime)dt.Rows[0]["devdt"];//ConvertToCountryTimeZone(dt, 0, timeZone);//(DateTime)dt.Rows[0]["SRVDT"];
        DateTime lastDateTime = (DateTime)dt.Rows[rowsCount - 1]["devdt"];//ConvertToCountryTimeZone(dt, rowsCount - 1, timeZone);//(DateTime)dt.Rows[rowsCount - 1]["SRVDT"];
        int firstDay = firstDateTime.Day;
        int lastDay = lastDateTime.Day;
        List<int> LstEmptyDays = new List<int>();
        //---
        TimeData attendance;
        DateTime timeIn;
        DateTime timeOut;
        DateTime firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
        DateTime lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
        DateTime blankDateTime = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//DateTime.Today;
        bool IsCardIn = false;
        bool IsCardOut = false;
        bool lastsemiIn = false;
        TimeSpan ThidDayWorkingHours = new TimeSpan();
        int k = 0;//-1;

        for (int j = 0; j <= rowsCount - 1; j++)//this loop iterates over each row of the sorted DataTable to process attendance data
        {
          string shortCountryName = dt.Rows[j]["devnm"].ToString().Substring(0, 3);

          if (dt.Rows[j]["devnm"].ToString().Substring(0, 4) == "NG-L")
          {
            timeZone = "W. Central Africa Standard Time";
            countryName = "Nigeria Lagos";
          }
          else if (dt.Rows[j]["devnm"].ToString().Substring(0, 4) == "NG-P")
          {
            timeZone = "W. Central Africa Standard Time";
            countryName = "Nigeria Port Harcourt";
          }
          else
          {
            switch (shortCountryName)
            {
              case "PK ":
                timeZone = "Pakistan Standard Time";
                countryName = "Pakistan";
                break;
              case "PAK":
                timeZone = "Pakistan Standard Time";
                countryName = "Pakistan";
                break;
              case "UAE":
                timeZone = "Arab Standard Time";
                countryName = "United Arab Emirates";
                break;
              case "KSA":
                timeZone = "Arab Standard Time";
                countryName = "Saudi Arabia";
                break;
              case "GBR":
                timeZone = "GMT Standard Time";
                countryName = "United Kingdom";
                break;
              case "USA":
                timeZone = "Central Standard Time";
                countryName = "United States";
                break;
              case "NGA":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria";
                break;
              case "NG-":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria";
                break;
              case "EGY":
                timeZone = "Egypt Standard Time";
                countryName = "Egypt";
                break;
              case "IRQ":
                timeZone = "Arabic Standard Time";
                countryName = "Iraq";
                break;
              case "OMN":
                timeZone = "Arabian Standard Time";
                countryName = "Oman";
                break;
              case "QAT":
                timeZone = "Arab Standard Time";
                countryName = "Qatar";
                break;
              case "AGO":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Angola";
                break;
              case "KAZ":
                timeZone = "West Asia Standard Time";
                countryName = "Kazakhstan";
                break;
              // Add more cases for other countries
              default:
                timeZone = aspNetUser.CountryName.TimeZone; // Or handle the default case based on your requirements
                countryName = aspNetUser.CountryName.Name;
                break;
            }
          }

          // Convert the device date/time to the appropriate timezone
          firstDateTime = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);


          // Check for a change in date indicating a new day of attendance records
          if (firstDateTime.Day != firstDay)
          {//its mean new date started. so add all previois date calcuation here and add to list

            // If there are valid time-in and time-out records for the previous day, calculate total time
            if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
            {
              TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
              bool countryChanged = false;
              var leaveName = "";
              var leaveForDay = dbLeaveOn.Leaves.Where(x => x.UserId == aspNetUser.Id && DbFunctions.TruncateTime(x.StartDate) == DbFunctions.TruncateTime(firsTimeIn)).FirstOrDefault();
              if (leaveForDay != null)
              {
                var leaveType = dbLeaveOn.LeaveTypes.Find(leaveForDay.LeaveTypeId);
                leaveName = leaveType.Name;
              }
              else
              {
                leaveName = "";
              }
              if (countryName != previousCountryName && countryName != "" & previousCountryName != "")
              {
                countryChanged = true;
              }
              attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryChanged ? previousCountryName : countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn), Status = leaveName };

              LstTimeData.Add(attendance);
              TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);

            }
            //re-intiallize variables to next date calculations
            firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;

            lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
            ThidDayWorkingHours = new TimeSpan();
            IsCardIn = false; IsCardOut = false;
            firstDay = firstDateTime.Day;
          }

          k = j + 1;

          //get actual working hour of this date

          if ((k <= rowsCount - 1) && LstCardReadersIn.Contains((int)dt.Rows[j]["DEVID"]) && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]) &&
          Convert.ToDateTime(dt.Rows[j]["devdt"]).Day == firstDay && Convert.ToDateTime(dt.Rows[k]["devdt"]).Day == firstDay)
          {
            if (IsCardIn == false)
            {
              firsTimeIn = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);
              lastTimeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
              IsCardIn = true;
            }

            timeIn = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);
            timeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
            TimeSpan workingHour = (timeOut - timeIn);
            ThidDayWorkingHours = ThidDayWorkingHours.Add(workingHour);
            if (IsCardIn == true && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]))
            {
              IsCardOut = true;
              lastTimeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
            }
          }

          previousCountryName = countryName;
        }
        //datetime for end here

        //add last date to list here as loop ended
        if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
        {
          TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
          depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
          attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn) };

          LstTimeData.Add(attendance);
          TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);
        }

        ////Leaves Processing
        List<TimeData> offDays = new List<TimeData>();
        List<int> LstThisMonthsWeekEnds = new List<int>();
        if (aspNetUser.UserLeavePolicy == null || string.IsNullOrEmpty(aspNetUser.UserLeavePolicy.WeeklyOffDays))
        {
          LstThisMonthsWeekEnds = GetWeekEndList(int.Parse(dateAttr[1]), int.Parse(dateAttr[0]), "6,0");
        }
        else
        {
          LstThisMonthsWeekEnds = GetWeekEndList(int.Parse(dateAttr[1]), int.Parse(dateAttr[0]), aspNetUser.UserLeavePolicy.WeeklyOffDays);
        }

        int iEmpNum = aspNetUser.BioStarEmpNum.Value;
        foreach (int weekEndDay in LstThisMonthsWeekEnds)
        {
          TimeData thisWeekEnd = LstTimeData.FirstOrDefault(x => x.Date.Day == weekEndDay);
          if (thisWeekEnd != null)
          {
            thisWeekEnd.Status = "Weekend";
          }
          else
          {
            DateTime weekEndDate = DateTime.ParseExact(dateAttr[1] + "/" + dateAttr[0] + "/" + weekEndDay.ToString("00"), "yyyy/MM/dd", CultureInfo.InvariantCulture);
            TimeData weekEndOffDate = new TimeData
            {
              EmployeeName = UserName,
              EmployeeNumber = iEmpNum,
              TimeZone = countryName,
              Department = depName,
              Policy = userLeavePolicyDescription,
              Date = weekEndDate,
              Day = weekEndDate.ToString("dddd"),
              Status = "Weekend"
            };
            offDays.Add(weekEndOffDate);
          }
        }
        LstTimeData.AddRange(offDays);

        for (int day = 1; day <= ThisMonthTotalDays; day++)
        {
          DateTime currentDay = new DateTime(reqDate.Year, reqDate.Month, day);
          var timeDataForDay = LstTimeData.FirstOrDefault(x => x.Date.Day == day);
          var annualOffDay = dbLeaveOn.AnnualOffDays.FirstOrDefault(x => x.OffDay.HasValue && x.OffDay == currentDay && x.UserLeavePolicyId == aspNetUser.UserLeavePolicyId);
          // Check for any leave that spans the current day
          var leave = dbLeaveOn.Leaves.FirstOrDefault(x => x.StartDate <= currentDay && x.EndDate >= currentDay && x.IsAccepted1 != null && x.IsAccepted2 != null && x.UserId == userGuidId);
          if (timeDataForDay == null) // Employee was absent
          {
            string status = "Absent"; // Default to "Absent"
                                      // Check for holiday and leave
            if (annualOffDay != null)
            {
              status = annualOffDay.Description; // Holiday description
            }
            else if (leave != null)
            {
              // If a leave record exists, retrieve the corresponding leave type name
              var leaveTypeName = dbLeaveOn.LeaveTypes
                  .Where(l => l.Id == leave.LeaveTypeId)
                  .Select(l => l.Name)
                  .FirstOrDefault();
              status = leaveTypeName; // Leave type ID if on leave
            }
            //string status = annualOffDay != null ? annualOffDay.Description : "Absent"; // Use holiday description if it's a holiday, else mark as Absent
            depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId)?.DepartmentName ?? depName; // Safeguard against null

            attendance = new TimeData()
            {
              EmployeeName = UserName,
              EmployeeNumber = UserId,
              TimeZone = countryName,
              Policy = userLeavePolicyDescription,
              Department = depName,
              Date = annualOffDay?.OffDay ?? currentDay,
              Day = (annualOffDay?.OffDay ?? currentDay).DayOfWeek.ToString(),
              Status = status
            };
            LstTimeData.Add(attendance);
          }
          else if (annualOffDay != null) // Employee worked on an annual holiday
          {
            timeDataForDay.Status = annualOffDay.Description; // Append 'Worked' to the holiday description
          }
        }

      }
      //to avaid showing current month all data which is not happend yet
      foreach (var itm in LstTimeData.ToList())
      {
        if (itm.Date > DateTime.Now.Date)
        {
          LstTimeData.Remove(itm);
        }
      }

      ViewBag.TotalHours = TotalTime.TotalHours.ToString("N2");
      ViewBag.TotalWorkingHours = TotalWorkingHours.TotalHours.ToString("N2");
      con.Close();
      return Task.FromResult(LstTimeData);
    }


    private Task<List<TimeData>> ConnectToDBandReturnAttendanceReport(string formattedStartDate, string formattedEndDate, List<int> UserIds)
    {

      string countryName = string.Empty;
      string previousCountryName = string.Empty;
      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      SqlCommand cmd;
      SqlDataReader dr;
      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan TotalTime = new TimeSpan();
      TimeSpan TotalWorkingHours = new TimeSpan();
      List<string> logg = new List<string>();
      // Parse the formatted start and end dates
      DateTime startDate = DateTime.ParseExact(formattedStartDate, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
     // DateTime endDate = DateTime.ParseExact(formattedEndDate, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      DateTime endDate = DateTime.ParseExact(formattedEndDate, "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)
                              .AddDays(1).AddSeconds(-1);

      int totalDays = (endDate - startDate).Days + 1;
      con.Open();
      foreach (int Id in UserIds)
      {
        int UserId = Id;//Assigns the current UserId for processing.

        cmd = new SqlCommand("SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE USER_ID = @UserId AND devdt BETWEEN @StartDate AND @EndDate ORDER BY devdt", con);
        cmd.Parameters.AddWithValue("@UserId", UserId);
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);
     
        dr = cmd.ExecuteReader();//SqlCommand and SqlDataReader (cmd, dr) are initialized.

        DataTable dt = new DataTable();//A new DataTable dt is created for storing data related to the current user.

        string UserName = string.Empty;
        string timeZone = string.Empty;
        string userGuidId = string.Empty;

        AspNetUser aspNetUser = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId);

        //processing
        UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
        string depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
        string userLeavePolicyDescription = string.Empty;
        userGuidId = aspNetUser.Id;
        if (aspNetUser.CountryName == null)
        { logg.Add(aspNetUser.UserName); dr.Close(); continue; }
        if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
        if (aspNetUser.IsRelocated == true)
        {
          //in case relocate
          timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp).TimeZone;
          countryName = aspNetUser.CntryNameTemp;
        }
        else
        {
          timeZone = aspNetUser.CountryName.TimeZone;
          countryName = aspNetUser.CntryName;
        }
        //Processes user data to get UserName, depName, timeZone, countryName, and leave policy data.
        //Checks for relocation and adjusts timeZone and countryName accordingly.

        //Creating and Filling the DataTable:
        DataColumn dc = new DataColumn("USER_ID", typeof(String));
        dt.Columns.Add(dc);

        dc = new DataColumn("devdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("bsevtdt", typeof(DateTime));
        dt.Columns.Add(dc);

        dc = new DataColumn("DEVID", typeof(Int32));
        dt.Columns.Add(dc);

        dc = new DataColumn("DETAILS", typeof(string));
        dt.Columns.Add(dc);

        dc = new DataColumn("devnm", typeof(string));
        dt.Columns.Add(dc);
        //Columns are added to dt to represent user attendance data.
        //A loop reads data from dr and populates dt with rows representing the attendance logs.
        //↓

        while (dr.Read())//This loop iterates over each row returned by the SQL query executed above.
        {
          // for each row from the database, add the retrieved table name to the list
          DataRow dtrw = dt.NewRow(); //A new DataRow (dtrw) is created to store data for each attendance log.
          dtrw[0] = dr["USER_ID"];
          dtrw[1] = (DateTime)dr["devdt"];
          dtrw[2] = dr["bsevtdt"];
          dtrw[3] = dr["DEVID"];

          //The next lines check if the 'DEVID' is in the list LstCardReadersIn.
          //Depending on the result, "IN" or "OUT" is stored in the fifth column of dtrw.
          if (LstCardReadersIn.Contains(Convert.ToInt32(dr["DEVID"])))
          {
            dtrw[4] = "IN";
          }
          else
          {
            dtrw[4] = "OUT";
          }

          //dtrw[5] = dr["devnm"];: The 'devnm' (device name) column from dr is assigned to the sixth column of dtrw
          dtrw[5] = dr["devnm"];
          dt.Rows.Add(dtrw);
          //this will add the row at the end of the datatable
          //This step is repeated for each row returned by the SQL query, building up the attendance data for the user.
        }


        //After Reading All Data
        dr.Close();
        DataView view = dt.DefaultView;
        Console.WriteLine($"UserId: {dt.Rows}");
        Console.WriteLine($"dt.Rows.Count: {dt.Rows.Count}");
        view.Sort = "devdt ASC";
        //The DataTable dt's default view is sorted by 'devdt' in ascending order: view.Sort = "devdt ASC";.


        //Processing Sorted Attendance Data
        dt = view.ToTable();

        int rowsCount = dt.Rows.Count;
        
        if (rowsCount <= 0) continue;

        /*

        Initialization of Variables for Attendance Calculation:
        Variables like firstDateTime, lastDateTime, firstDay, lastDay, and counters are initialized. 
        These will be used to track the dates, times, and other details for each punch.

        */
        DateTime firstDateTime = (DateTime)dt.Rows[0]["devdt"];//ConvertToCountryTimeZone(dt, 0, timeZone);//(DateTime)dt.Rows[0]["SRVDT"];
        DateTime lastDateTime = (DateTime)dt.Rows[rowsCount - 1]["devdt"];//ConvertToCountryTimeZone(dt, rowsCount - 1, timeZone);//(DateTime)dt.Rows[rowsCount - 1]["SRVDT"];
        int firstDay = firstDateTime.Day;
        int lastDay = lastDateTime.Day;
        List<int> LstEmptyDays = new List<int>();
        //---
        TimeData attendance;
        DateTime timeIn;
        DateTime timeOut;
        DateTime firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;
        DateTime lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
        DateTime blankDateTime = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//DateTime.Today;
        bool IsCardIn = false;
        bool IsCardOut = false;
        bool lastsemiIn = false;
        TimeSpan ThidDayWorkingHours = new TimeSpan();
        int k = 0;//-1;

        for (int j = 0; j <= rowsCount - 1; j++)//this loop iterates over each row of the sorted DataTable to process attendance data
        {
          string shortCountryName = dt.Rows[j]["devnm"].ToString().Substring(0, 3);

          if (dt.Rows[j]["devnm"].ToString().Substring(0, 4) == "NG-L")
          {
            timeZone = "W. Central Africa Standard Time";
            countryName = "Nigeria Lagos";
          }
          else if (dt.Rows[j]["devnm"].ToString().Substring(0, 4) == "NG-P")
          {
            timeZone = "W. Central Africa Standard Time";
            countryName = "Nigeria Port Harcourt";
          }
          else
          {
            switch (shortCountryName)
            {
              case "PK ":
                timeZone = "Pakistan Standard Time";
                countryName = "Pakistan";
                break;
              case "PAK":
                timeZone = "Pakistan Standard Time";
                countryName = "Pakistan";
                break;
              case "UAE":
                timeZone = "Arab Standard Time";
                countryName = "United Arab Emirates";
                break;
              case "KSA":
                timeZone = "Arab Standard Time";
                countryName = "Saudi Arabia";
                break;
              case "GBR":
                timeZone = "GMT Standard Time";
                countryName = "United Kingdom";
                break;
              case "USA":
                timeZone = "Central Standard Time";
                countryName = "United States";
                break;
              case "NGA":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria";
                break;
              case "NG-":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Nigeria";
                break;
              case "EGY":
                timeZone = "Egypt Standard Time";
                countryName = "Egypt";
                break;
              case "IRQ":
                timeZone = "Arabic Standard Time";
                countryName = "Iraq";
                break;
              case "OMN":
                timeZone = "Arabian Standard Time";
                countryName = "Oman";
                break;
              case "QAT":
                timeZone = "Arab Standard Time";
                countryName = "Qatar";
                break;
              case "AGO":
                timeZone = "W. Central Africa Standard Time";
                countryName = "Angola";
                break;
              case "KAZ":
                timeZone = "West Asia Standard Time";
                countryName = "Kazakhstan";
                break;
              // Add more cases for other countries
              default:
                timeZone = aspNetUser.CountryName.TimeZone; // Or handle the default case based on your requirements
                countryName = aspNetUser.CountryName.Name;
                break;
            }
          }

          // Convert the device date/time to the appropriate timezone
          firstDateTime = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);


          // Check for a change in date indicating a new day of attendance records
          if (firstDateTime.Day != firstDay)
          {//its mean new date started. so add all previois date calcuation here and add to list

            // If there are valid time-in and time-out records for the previous day, calculate total time
            if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
            {
              TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
              bool countryChanged = false;
              var leaveName = "";
              var leaveForDay = dbLeaveOn.Leaves.Where(x => x.UserId == aspNetUser.Id && DbFunctions.TruncateTime(x.StartDate) == DbFunctions.TruncateTime(firsTimeIn)).FirstOrDefault();
              if (leaveForDay != null)
              {
                var leaveType = dbLeaveOn.LeaveTypes.Find(leaveForDay.LeaveTypeId);
                leaveName = leaveType.Name;
              }
              else
              {
                leaveName = "";
              }
              if (countryName != previousCountryName && countryName != "" & previousCountryName != "")
              {
                countryChanged = true;
              }
              attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryChanged ? previousCountryName : countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn), Status = leaveName };

              LstTimeData.Add(attendance);
              TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);

            }
            //re-intiallize variables to next date calculations
            firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//firstDateTime;//DateTime.Today;

            lastTimeOut = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);//lastDateTime;//DateTime.Today;
            ThidDayWorkingHours = new TimeSpan();
            IsCardIn = false; IsCardOut = false;
            firstDay = firstDateTime.Day;
          }

          k = j + 1;

          //get actual working hour of this date

          if ((k <= rowsCount - 1) && LstCardReadersIn.Contains((int)dt.Rows[j]["DEVID"]) && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]) &&
          Convert.ToDateTime(dt.Rows[j]["devdt"]).Day == firstDay && Convert.ToDateTime(dt.Rows[k]["devdt"]).Day == firstDay)
          {
            if (IsCardIn == false)
            {
              firsTimeIn = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);
              lastTimeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
              IsCardIn = true;
            }

            timeIn = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[j]["devdt"], timeZone);
            timeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
            TimeSpan workingHour = (timeOut - timeIn);
            ThidDayWorkingHours = ThidDayWorkingHours.Add(workingHour);
            if (IsCardIn == true && !LstCardReadersIn.Contains((int)dt.Rows[k]["DEVID"]))
            {
              IsCardOut = true;
              lastTimeOut = ConvertToCountryTimeZoneNew((DateTime)dt.Rows[k]["devdt"], timeZone);
            }
          }

          previousCountryName = countryName;
        }
        //datetime for end here

        //add last date to list here as loop ended
        if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)//(IsCardIn == true && IsCardOut == true)
        {
          TotalTime = TotalTime.Add(lastTimeOut - firsTimeIn);
          depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
          attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = UserId, TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = firsTimeIn.Date, Day = firsTimeIn.DayOfWeek.ToString(), TimeIn = firsTimeIn, TimeOut = lastTimeOut, WorkingHours = ThidDayWorkingHours, TotalTime = (lastTimeOut - firsTimeIn) };

          LstTimeData.Add(attendance);
          TotalWorkingHours = TotalWorkingHours.Add(ThidDayWorkingHours);
        }

        ////Leaves Processing
        List<TimeData> offDays = new List<TimeData>();
        List<string> LstThisMonthsWeekEnds;
        if (aspNetUser.UserLeavePolicy == null || string.IsNullOrEmpty(aspNetUser.UserLeavePolicy.WeeklyOffDays))
        {
          LstThisMonthsWeekEnds = GetUserDataWeekEndLists(startDate, endDate, "6,0");
        }
        else
        {
          LstThisMonthsWeekEnds = GetUserDataWeekEndLists(startDate, endDate, aspNetUser.UserLeavePolicy.WeeklyOffDays);
        }

        int iEmpNum = aspNetUser.BioStarEmpNum.Value;

        DateTime latestTimeData = LstTimeData
                .Where(x => x.Date.DayOfWeek != DayOfWeek.Saturday && x.Date.DayOfWeek != DayOfWeek.Sunday) // Exclude weekends
                .OrderByDescending(x => x.Date)
                .Select(x => x.Date.Date)
                .FirstOrDefault();
        foreach (string weekEndDay in LstThisMonthsWeekEnds)
        {
          // Create a DateTime object for the weekend day
      
          DateTime weekEndDate = DateTime.ParseExact(weekEndDay, "MM-dd-yyyy", CultureInfo.InvariantCulture);
          if (weekEndDate <= latestTimeData)
          {
            TimeData thisWeekEnd = LstTimeData.FirstOrDefault(x => x.Date.Date == weekEndDate.Date);

            if (thisWeekEnd != null)
            {
              thisWeekEnd.Status = "Weekend";
            }
            else
            {
              TimeData weekEndOffDate = new TimeData
              {
                EmployeeName = UserName,
                EmployeeNumber = iEmpNum,
                TimeZone = countryName,
                Department = depName,
                Policy = userLeavePolicyDescription,
                Date = weekEndDate,
                Day = weekEndDate.ToString("dddd"),
                Status = "Weekend"
              };
              offDays.Add(weekEndOffDate);
            }
          }
        }
        LstTimeData.AddRange(offDays);

         // Find the latest date for which timing data exists in the database (max date in LstTimeData)
         var lastExistingDate = LstTimeData
                .Where(x => x.Date.DayOfWeek != DayOfWeek.Saturday && x.Date.DayOfWeek != DayOfWeek.Sunday) // Exclude weekends
                .OrderByDescending(x => x.Date)
                .Select(x => x.Date.Date)
                .FirstOrDefault();

        // If no valid data is present, default to startDate
        if (lastExistingDate == default(DateTime))
        {
          lastExistingDate = startDate;
        }


        for (int day = 0; day < totalDays; day++)
        {
          DateTime currentDay = startDate.AddDays(day);

          if (currentDay > lastExistingDate)
          {
            // Stop processing dates after the last available data
            break;
          }
          var timeDataForDay = LstTimeData.FirstOrDefault(x => x.Date.Date == currentDay.Date);
          var annualOffDay = dbLeaveOn.AnnualOffDays
              .FirstOrDefault(x => DbFunctions.TruncateTime(x.OffDay) == currentDay.Date
                             && x.UserLeavePolicyId == aspNetUser.UserLeavePolicyId);
          // Check for any leave that spans the current day
          var leave = dbLeaveOn.Leaves
                .FirstOrDefault(x => DbFunctions.TruncateTime(x.StartDate) <= currentDay.Date 
                             && DbFunctions.TruncateTime(x.EndDate) >= currentDay.Date
                             && x.IsAccepted1 != null && x.IsAccepted2 != null 
                             && x.UserId == userGuidId);
          //if (timeDataForDay == null) // Employee was absent
               // Handle cases where no time data exists for the day
          if (timeDataForDay == null) 
          {
            string status = "Absent"; // Default to "Absent"
                                      // Check for holiday and leave
            if (annualOffDay != null)
            {
              status = annualOffDay.Description; // Holiday description
            }
            else if (leave != null)
            {
              // If a leave record exists, retrieve the corresponding leave type name
              var leaveTypeName = dbLeaveOn.LeaveTypes
                  .Where(l => l.Id == leave.LeaveTypeId)
                  .Select(l => l.Name)
                  .FirstOrDefault();
              status = leaveTypeName; // Leave type ID if on leave
            }
            //string status = annualOffDay != null ? annualOffDay.Description : "Absent"; // Use holiday description if it's a holiday, else mark as Absent
            depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId)?.DepartmentName ?? depName; // Safeguard against null
              // Add attendance data for the day
            attendance = new TimeData()
            {
              EmployeeName = UserName,
              EmployeeNumber = UserId,
              TimeZone = countryName,
              Policy = userLeavePolicyDescription,
              Department = depName,
              Date = annualOffDay?.OffDay ?? currentDay,
              Day = (annualOffDay?.OffDay ?? currentDay).DayOfWeek.ToString(),
              Status = status
            };
            LstTimeData.Add(attendance);
          }
          else if (annualOffDay != null) // Employee worked on an annual holiday
          {
            timeDataForDay.Status = annualOffDay.Description; // Append 'Worked' to the holiday description
          }
        }

      }


      //to avaid showing current month all data which is not happend yet
      foreach (var itm in LstTimeData.ToList())
      {
        if (itm.Date > DateTime.Now.Date)
        {
          LstTimeData.Remove(itm);
        }
      }

      ViewBag.TotalHours = TotalTime.TotalHours.ToString("N2");
      ViewBag.TotalWorkingHours = TotalWorkingHours.TotalHours.ToString("N2");
      con.Close();
      return Task.FromResult(LstTimeData);
    }

    protected List<string> GetUserDataWeekEndLists(DateTime startDate, DateTime endDate, string WeekEndDays)
    {
      List<int> LstWeekEndDays = WeekEndDays.Split(',').Select(int.Parse).ToList();
      List<string> LstThisMonthsWeekEnds = new List<string>();

      CultureInfo ci = new CultureInfo("en-US");

      // Loop through each day in the given date range
      for (DateTime date = startDate; date < endDate; date = date.AddDays(1))
      {
        // Check if the current day's DayOfWeek matches any of the provided weekend days
        if (LstWeekEndDays.Contains((int)date.DayOfWeek))
        {
          LstThisMonthsWeekEnds.Add(date.ToString("MM-dd-yyyy"));
        }
      }

      LstThisMonthsWeekEnds.Sort();
      return LstThisMonthsWeekEnds;
    }

    protected List<int> GetWeekEndLists(DateTime startDate, DateTime endDate, string WeekEndDays)
    {
      List<int> LstWeekEndDays = WeekEndDays.Split(',').Select(int.Parse).ToList();
      List<int> LstThisMonthsWeekEnds = new List<int>();

      CultureInfo ci = new CultureInfo("en-US");

      // Loop through each day in the given date range
      for (DateTime date = startDate; date < endDate; date = date.AddDays(1))
      {
        // Check if the current day's DayOfWeek matches any of the provided weekend days
        if (LstWeekEndDays.Contains((int)date.DayOfWeek))
        {
          LstThisMonthsWeekEnds.Add(date.Day);
        }
      }

      LstThisMonthsWeekEnds.Sort();
      return LstThisMonthsWeekEnds;
    }


    private Task<List<TimeData>> GetAttendanceSummary(string formattedStartDate, string formattedEndDate, List<int> UserIds)
    {
      DateTime startDate = DateTime.ParseExact(formattedStartDate.Trim(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      DateTime endDate = DateTime.ParseExact(formattedEndDate.Trim(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);


      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan totalWorkingHoursAllUsers = TimeSpan.Zero; 
      TimeSpan totalTimeAllUsers = TimeSpan.Zero;

      // Fetch attendance data for each user
      foreach (int UserId in UserIds)
      {
        // Fetch attendance records for this user within the provided date range
        var attendanceRecords = dbLeaveOn.AttendanceDatas
        .Where(a => a.BioStarEmpNum == UserId && a.CreatedDate >= startDate && a.CreatedDate <= endDate)
        .GroupBy(a => new { a.BioStarEmpNum, Date = DbFunctions.TruncateTime(a.CreatedDate) })
        .Select(g => g.FirstOrDefault()) // Take only the first record for each day per user
        .OrderBy(a => a.CreatedDate)
        .ToList();

        if (!attendanceRecords.Any()) continue; // If no records found, skip to the next user

        // Process each attendance record
        foreach (var record in attendanceRecords)
        {
          DateTime? timeInNullable = record.FirstPunchIn;
          DateTime? timeOutNullable = record.LastPunchOut;
          DateTime timeIn = timeInNullable ?? DateTime.MinValue;
          DateTime timeOut = timeOutNullable ?? (timeInNullable ?? DateTime.MinValue);
          TimeSpan totalTime = timeIn != DateTime.MinValue && timeOut != DateTime.MinValue ? timeOut - timeIn : TimeSpan.Zero;
          TimeSpan totalWorkingHours = record.TotalWorkHours.HasValue && record.TotalWorkHours > 0
             ? TimeSpan.FromSeconds((double)record.TotalWorkHours)
             : TimeSpan.Zero;
          string day = record.CreatedDate.HasValue ? record.CreatedDate.Value.ToString("dddd") : "N/A";

          totalWorkingHoursAllUsers += totalWorkingHours;
          totalTimeAllUsers += totalTime;

          //Chek weekend
          string status;
          if(record.CreatedDate.HasValue && (record.CreatedDate.Value.DayOfWeek == DayOfWeek.Saturday || record.CreatedDate.Value.DayOfWeek == DayOfWeek.Sunday))
          {
            status = "Weekend";
          } 
          else
          {
            status = record.IsAbsent == true ? "Absent" : null;
          }

          // Map the data directly from AttendanceData
          LstTimeData.Add(new TimeData()
          {
            EmployeeName = record.UserName, 
            EmployeeNumber = record.BioStarEmpNum ?? 0, 
            Department = record.DepartmentName,
            TimeZone = record.CountryName,
            Policy = record.UserLeavePolicyID, 
            Date = record.CreatedDate ?? DateTime.MinValue,
            Day = day,
            TimeIn = timeIn,
            TimeOut = timeOut,
            TotalTime = totalTime,
            WorkingHours = totalWorkingHours,
            Status = status,
   
          });
        }
      }

      ViewBag.TotalWorkingHours = totalWorkingHoursAllUsers.TotalHours.ToString("N2");
      ViewBag.TotalHours = totalTimeAllUsers.TotalHours.ToString("N2");

      return Task.FromResult(LstTimeData);
    }

    protected List<int> GetWeekEndList(int year, int month, string WeekEndDays)
    {
      List<int> LstWeekEndDays = WeekEndDays.Split(',').Select(int.Parse).ToList();
      List<int> LstThisMonthsWeekEnds = new List<int>();
      foreach (DayOfWeek weekEnd in LstWeekEndDays)
      {
        //DayOfWeek dayName= weekEnd;
        CultureInfo ci = new CultureInfo("en-US");
        for (int i = 1; i <= ci.Calendar.GetDaysInMonth(year, month); i++)
        {
          if (new DateTime(year, month, i).DayOfWeek == weekEnd)
            LstThisMonthsWeekEnds.Add(i);
        }
      }
      LstThisMonthsWeekEnds.Sort();
      return LstThisMonthsWeekEnds;
    }
    public DateTime ConvertToCountryTimeZoneNew(DataTable dt, int rowNo, string timeZone)
    {
      TimeZoneInfo customeTimeZone;
      DateTime ConvertedDateTime;

      //if (timeZone == "" || timeZone == "Pakistan Standard Time")
      //{
      //  //customeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
      //  //ConvertedDateTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)dt.Rows[rowNo]["devdt"], customeTimeZone);
      //  //return ConvertedDateTime;
      //  return (DateTime)dt.Rows[rowNo]["devdt"];
      //}
      //else
      //{
      customeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
      ConvertedDateTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)dt.Rows[rowNo]["devdt"], customeTimeZone);
      return ConvertedDateTime;
      //}

    }
    private DateTime ConvertToCountryTimeZoneNew(DateTime dateTime, string timeZone)
    {
      TimeZoneInfo customeTimeZone;
      DateTime ConvertedDateTime;

      //if (timeZone == "" || timeZone == "Pakistan Standard Time")
      //{
      //  //customeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
      //  //ConvertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, customeTimeZone);
      //  //return ConvertedDateTime;
      //  return dateTime;
      //}
      //else
      //{
      customeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
      ConvertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, customeTimeZone);
      return ConvertedDateTime;
      //}
    }
    public async Task<ActionResult> ConnectToDBandReturnOffHours(string reqDate, string UserId)
    {

      DateTime from_Date = DateTime.ParseExact(reqDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
      DateTime to_Date = from_Date.AddDays(1);
      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      SqlCommand cmd;
      SqlDataReader dr;
      con.Open();

      cmd = new SqlCommand("select * from punchlog" + " where USER_ID ='" + UserId + "' and devdt between @fromDate and @toDate order by devdt", con);
      cmd.Parameters.AddWithValue("@fromDate", from_Date);
      cmd.Parameters.AddWithValue("@toDate", to_Date);
      dr = cmd.ExecuteReader();
      DataTable dt = new DataTable();
      dt.Load(dr);
      int rowsCount = dt.Rows.Count;
      string UserName = string.Empty;
      string timeZone = string.Empty;
      string countryName = string.Empty;
      int intUserId = int.Parse(UserId);
      AspNetUser aspNetUser = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == intUserId);
      UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");
      string userLeavePolicyDescription = string.Empty;
      if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
      if (aspNetUser.IsRelocated == true)
      {
        //in case relocate
        timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp).TimeZone;
        countryName = aspNetUser.CntryNameTemp;
      }
      else
      {
        timeZone = aspNetUser.CountryName.TimeZone;
        countryName = aspNetUser.CntryName;
      }

      DateTime LastDate = ConvertToCountryTimeZoneNew(dt, rowsCount - 1, timeZone);
      DateTime thisDateTime = ConvertToCountryTimeZoneNew(dt, 0, timeZone);
      int thisDay = thisDateTime.Day;

      int LastDay = LastDate.Day;

      List<OffTimeDetial> LstOffTimeDetial = new List<OffTimeDetial>();

      OffTimeDetial offTimeDetail;
      DateTime timeIn;
      DateTime timeOut;
      DateTime FirsTimeIn = DateTime.Today;
      DateTime LastTimeOut = DateTime.Today;
      TimeSpan TotalTime = new TimeSpan();
      bool IsFirstDone = false;
      TimeSpan ThidDayTotalOffTime = new TimeSpan();
      TimeSpan TotalOffHours = new TimeSpan();
      for (int j = 0; j <= rowsCount - 1; j++)
      {
        if ((j + 1 <= rowsCount - 1) && !LstCardReadersIn.Contains(Convert.ToInt32(dt.Rows[j]["DEVID"])) && LstCardReadersIn.Contains(Convert.ToInt32(dt.Rows[j + 1]["DEVID"])))
        {
          timeOut = ConvertToCountryTimeZoneNew(dt, j, timeZone);
          timeIn = ConvertToCountryTimeZoneNew(dt, j + 1, timeZone);

          TimeSpan offHour = (timeIn - timeOut);

          ThidDayTotalOffTime = ThidDayTotalOffTime.Add(offHour);
          offTimeDetail = new OffTimeDetial() { TimeOut = timeOut, TimeIn = timeIn, OffHours = offHour };
          LstOffTimeDetial.Add(offTimeDetail);
        }

      }
      ViewBag.ThidDayTotalOffTime = ThidDayTotalOffTime;

      con.Close();
      return View("OffTimeDetail", await Task.FromResult(LstOffTimeDetial));
    }
    // GET: AccessBiostarAC

    public async Task<ActionResult> GetOffHours(string reqDate, string UserId)
    {
      try { 
      // Parse the date from request
      DateTime from_Date = DateTime.ParseExact(reqDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
      DateTime to_Date = from_Date.AddDays(1).Date;

      // Retrieve user data from BreakHours table using Entity Framework
      int intUserId = int.Parse(UserId);

        // Fetch BreakHours data for the specific UserId and Date range
        var breakHoursData = await dbLeaveOn.BreakHours
              .AsNoTracking()
              .Where(bh => bh.BioStarEmpNum == intUserId
                  && bh.PunchIn.Year == from_Date.Year && bh.PunchIn.Month == from_Date.Month && bh.PunchIn.Day == from_Date.Day
                  && bh.PunchOut.Year == from_Date.Year && bh.PunchOut.Month == from_Date.Month && bh.PunchOut.Day == from_Date.Day)
              .OrderBy(bh => bh.PunchIn)
              .ToListAsync();
    

        //var breakHoursData = await dbLeaveOn.BreakHours
        //                .AsNoTracking() // Disable tracking for read-only data
        //                .Where(bh => bh.BioStarEmpNum == intUserId
        //                  && bh.Date == from_Date) 
        //                .OrderBy(bh => bh.PunchIn)
        //                .ToListAsync();


        if (!breakHoursData.Any())
      {
        ViewBag.Message = "No break hours data found for the given date.";
        return View("OffTimeDetail", new List<OffTimeDetial>());
      }

      // Variables to calculate total off hours and manage the list of off-time details
      List<OffTimeDetial> LstOffTimeDetial = new List<OffTimeDetial>();
      TimeSpan ThidDayTotalOffTime = new TimeSpan();

      // Eliminate duplicates based on PunchIn and PunchOut values
      var distinctBreakHoursData = breakHoursData
          .GroupBy(bh => new { bh.PunchIn, bh.PunchOut }) // Group by PunchIn and PunchOut to avoid duplicates
          .Select(g => g.First()) // Take only the first entry in each group
          .ToList();

      foreach (var entry in distinctBreakHoursData)
      {
        // Create offTimeDetail /for each break
        DateTime punchIn = entry.PunchOut;
        DateTime punchOut = entry.PunchIn;
        TimeSpan offHours = punchIn - punchOut;

        OffTimeDetial offTimeDetail = new OffTimeDetial
        {
          TimeIn = punchIn,
          TimeOut = punchOut,
          OffHours = offHours
        };

        // Add to total off hours for the day
        ThidDayTotalOffTime = ThidDayTotalOffTime.Add(offHours);
        LstOffTimeDetial.Add(offTimeDetail);
      }

      // Store the total off time in the ViewBag
      ViewBag.ThidDayTotalOffTime = ThidDayTotalOffTime;

      // Return the view with the calculated off-time details
      return View("OffTimeDetail", LstOffTimeDetial);
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error: " + ex.ToString());
        // Check for inner exception
        if (ex.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
        }

        throw (ex);
      }
    }


    public async Task<ActionResult> UserDataX(string ReqMonthYear, string UserId)
    {
      try
      {
        ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime reqDate;
        int dEmpNum = 0;

        List<TimeData> LstAttendances = new List<TimeData>();

        if (!string.IsNullOrEmpty(ReqMonthYear))
        {
          dEmpNum = int.Parse(UserId);
          reqDate = DateTime.ParseExact(ReqMonthYear, "MM-yyyy", System.Globalization.CultureInfo.CurrentCulture);
        }
        else
        {
          // In case of empty parameters or first time
          reqDate = DateTime.Now;
          string userId = User.Identity.GetUserId();
          var user = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.Id == userId);

          if (user != null)
          {
            dEmpNum = (int)user.BioStarEmpNum;
            ViewBag.SelectedEmployees = new List<string> { dEmpNum.ToString() };

            // Set the login user's name
            //ViewBag.UserName = user.UserName.Split('@')[0].Replace(".", " ");
            ViewBag.UserName = user.UserName;
            ViewBag.userId = user.BioStarEmpNum;

            // Set the department name
            var department = dbLeaveOn.DepartmentNames.FirstOrDefault(d => d.Name == user.DepartmentName);
            ViewBag.DepartmentName = department != null ? department.Name : "N/A";
          }
        }

        if (!string.IsNullOrEmpty(ReqMonthYear))
        {
          List<AspNetUser> users = dbLeaveOn.AspNetUsers.Where(x => x.BioStarEmpNum == dEmpNum).ToList();
          string GuidUserId = users[0].Id;
          List<int> userIds = users.Select(x => x.BioStarEmpNum.Value).ToList<int>();

          string ReqMonthYearFormatted = reqDate.Month.ToString("00") + "-" + reqDate.Year;
          List<int> User_Ids = new List<int> { dEmpNum };
          LstAttendances = await ConnectToDBandReturnWorkingHours(ReqMonthYearFormatted, User_Ids);
        }

        if (string.IsNullOrEmpty(ReqMonthYear))
        {
          // In case of null param or first time
          return View(LstAttendances?.OrderBy(i => i.Date).ToList() ?? new List<TimeData>());
        }
        else
        {
          return PartialView("_UserData", LstAttendances.OrderBy(i => i.Date).ToList());
        }
      }
      catch (Exception ex)
      {
        throw ex; // It's better to log the error instead of rethrowing
      }
    }

    public async Task<ActionResult> MyReportData(string StartDate, string EndDate, List<string> UserIds)
    {
      try
      {
        ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime startDate, endDate;

        string userId = User.Identity.GetUserId();

        List<TimeData> LstAttendances = new List<TimeData>();

        // Set default date if ReqMonthYear is empty
        if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate))
        {

          startDate = DateTime.ParseExact(StartDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
          endDate = DateTime.ParseExact(EndDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
          // In case of empty parameters or first time
          startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
          endDate = DateTime.Now;

          ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers, "BioStarEmpNum", "UserName").OrderBy(i => i.Text);

        }

        //StartDate and EndDate for display
        ViewBag.StartDate = startDate.ToString("dd-MMM-yyyy");
        ViewBag.EndDate = endDate.ToString("dd-MMM-yyyy");

        // Role-based data population
        if (User.IsInRole("Admin"))
        {
          var departments = dbLeaveOn.AspNetUsers
                 .Where(u => !string.IsNullOrEmpty(u.DepartmentName))
                 .Select(u => u.DepartmentName)
                 .Distinct()
                 .Select(d => new SelectListItem { Value = d, Text = d })
                 .ToList();
          ViewBag.Departments = departments;
          ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers, "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = UserIds;
        }
        //else if (User.IsInRole("Manager") || User.IsInRole("User"))
        else if (User.IsInRole("Manager") || User.IsInRole("User"))
        {

          var managerDepartment = dbLeaveOn.AspNetUsers.FirstOrDefault(u => u.Id == userId).DepartmentName;
          ViewBag.Departments = new SelectList(new List<string> { managerDepartment });

          var employeesUnderManager = dbLeaveOn.AspNetUsers.Where(u => (u.ManagerID == userId || u.Manager2ID == userId)).ToList();
          ViewBag.Employees = new SelectList(employeesUnderManager, "BioStarEmpNum", "UserName");
          //ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers.Where(u => u.DepartmentName == managerDepartment), "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = UserIds;
        }

        if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate))
        {
          // Format the date range for querying
          string formattedStartDate = startDate.ToString("dd-MM-yyyy");
          string formattedEndDate = endDate.ToString("dd-MM-yyyy");
          var User_Ids = UserIds.Select(id => int.Parse(id)).ToList();
          LstAttendances = await ConnectToDBandReturnAttendanceReport(formattedStartDate, formattedEndDate, User_Ids);
          // LstAttendances = await GetAttendanceSummary(formattedStartDate, formattedEndDate, User_Ids);

        }
        //return View(await db.UD_TB_AccessTime_Data.ToListAsync());
        if (string.IsNullOrEmpty(StartDate) && string.IsNullOrEmpty(EndDate))
        {
          //in case of null param or first time
          if (!(LstAttendances is null))
          {
            return View(LstAttendances.OrderBy(i => i.Date).ToList());

          }
          else
          {
            return View();
          }
        }
        else
        {
          return PartialView("_UserData", LstAttendances.OrderBy(i => i.Date).ToList());
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error: " + ex.ToString());
        // Check for inner exception
        if (ex.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
        }

        throw (ex);
      }

    }


    public async Task<ActionResult> UserReportData(string ReqMonthYear, string UserId)
    {
      try
      {
        ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime reqDate;
        int dEmpNum = 0;

        List<TimeData> LstAttendances = new List<TimeData>();

        if (!string.IsNullOrEmpty(ReqMonthYear))
        {
          dEmpNum = int.Parse(UserId);
          reqDate = DateTime.ParseExact(ReqMonthYear, "MM-yyyy", System.Globalization.CultureInfo.CurrentCulture);
    
        }
        else
        {
          // In case of empty parameters or first time
          reqDate = DateTime.Now;
          string userId = User.Identity.GetUserId();
          var user = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.Id == userId);

          if (user != null)
          {
            dEmpNum = (int)user.BioStarEmpNum;
            ViewBag.SelectedEmployees = new List<string> { dEmpNum.ToString() };
            // Set ViewBag for start and end dates based on the requested month and year
            ViewBag.StartDate = new DateTime(reqDate.Year, reqDate.Month, 1).ToString("dd-MMM-yyyy");
            ViewBag.EndDate = reqDate.ToString("dd-MMM-yyyy");

            string username = user.UserName.Split('@')[0].Replace(".", " ");
            // Capitalize the first letter of each word
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            ViewBag.UserName = textInfo.ToTitleCase(username.ToLower());
            ViewBag.userId = user.BioStarEmpNum;

            // Set the department name
            var department = dbLeaveOn.DepartmentNames.FirstOrDefault(d => d.Name == user.DepartmentName);
            ViewBag.DepartmentName = department != null ? department.Name : "N/A";
          }
        }

        if (string.IsNullOrEmpty(ReqMonthYear))
        {
          // In case of null param or first time
          return View(LstAttendances?.OrderBy(i => i.Date).ToList() ?? new List<TimeData>());
        }
        else
        {
          return PartialView("_UserData", LstAttendances.OrderBy(i => i.Date).ToList());
        }
      }
      catch (Exception ex)
      {
        throw ex; // It's better to log the error instead of rethrowing
      }
    }

    public async Task<ActionResult> UserData(string StartDate, string EndDate, List<string> UserIds)
      {
      try
      {
        ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime startDate, endDate;

        string userId = User.Identity.GetUserId();

        List<TimeData> LstAttendances = new List<TimeData>();

        // Set default date if ReqMonthYear is empty
        if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate))
        {

          startDate = DateTime.ParseExact(StartDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
          endDate = DateTime.ParseExact(EndDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
          // In case of empty parameters or first time
          startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); 
          endDate = DateTime.Now; 

          ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers, "BioStarEmpNum", "UserName").OrderBy(i => i.Text);

        }

        //StartDate and EndDate for display
        ViewBag.StartDate = startDate.ToString("dd-MMM-yyyy");
        ViewBag.EndDate = endDate.ToString("dd-MMM-yyyy");

        // Role-based data population
        if (User.IsInRole("Admin"))
        {
          var departments = dbLeaveOn.AspNetUsers
                 .Where(u => !string.IsNullOrEmpty(u.DepartmentName))
                 .Select(u => u.DepartmentName)
                 .Distinct()
                 .Select(d => new SelectListItem { Value = d, Text = d })
                 .ToList();
          ViewBag.Departments = departments;
          ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers, "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = UserIds;
        }
        //else if (User.IsInRole("Manager") || User.IsInRole("User"))
        else if (User.IsInRole("Manager") || User.IsInRole("User"))
        {

          var managerDepartment = dbLeaveOn.AspNetUsers.FirstOrDefault(u => u.Id == userId).DepartmentName;
          ViewBag.Departments = new SelectList(new List<string> { managerDepartment });

          var employeesUnderManager = dbLeaveOn.AspNetUsers.Where(u => (u.ManagerID == userId || u.Manager2ID == userId)).ToList();
          ViewBag.Employees = new SelectList(employeesUnderManager, "BioStarEmpNum", "UserName");
          //ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers.Where(u => u.DepartmentName == managerDepartment), "BioStarEmpNum", "UserName");
          ViewBag.SelectedEmployees = UserIds;
        }
        //else if (User.IsInRole("User"))
        //{
        //  var currentUser = dbLeaveOn.AspNetUsers.FirstOrDefault(u => u.Id == userId);
        //  ViewBag.Departments = new SelectList(new List<string> { currentUser.DepartmentName });
        //  ViewBag.Employees = new SelectList(new List<AspNetUser> { currentUser }, "BioStarEmpNum", "UserName");
        //  ViewBag.SelectedEmployees = new List<string> { currentUser.BioStarEmpNum.ToString() }; // Populate selected employee for User
        //}

        if (!string.IsNullOrEmpty(StartDate) && !string.IsNullOrEmpty(EndDate))
        {
          // Format the date range for querying
          string formattedStartDate = startDate.ToString("dd-MM-yyyy");
          string formattedEndDate = endDate.ToString("dd-MM-yyyy");
          var User_Ids = UserIds.Select(id => int.Parse(id)).ToList();
       //   LstAttendances = await ConnectToDBandReturnAttendanceReport(formattedStartDate, formattedEndDate, User_Ids);
          LstAttendances = await GetAttendanceSummary(formattedStartDate, formattedEndDate, User_Ids);

        }
        //return View(await db.UD_TB_AccessTime_Data.ToListAsync());
        if (string.IsNullOrEmpty(StartDate) && string.IsNullOrEmpty(EndDate))
        {
          //in case of null param or first time
          if (!(LstAttendances is null))
          {
          //  return View(LstAttendances.OrderByDescending(i => i.Date).ToList());
            return View(LstAttendances.OrderByDescending(i => i.Date).ToList());
          }
          else
          {
            return View();
          }
        }
        else
        {
          return PartialView("_UserData", LstAttendances.OrderBy(i => i.Date).ToList());
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error: " + ex.ToString());
        // Check for inner exception
        if (ex.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
        }

        throw (ex);
      }

    }

    [HttpPost]
    public ActionResult GetUsersByDepartments(List<string> departmentNames)
    {
      if (departmentNames == null || !departmentNames.Any())
      {
        return Json(new List<SelectListItem>(), JsonRequestBehavior.AllowGet);
      }

      var users = dbLeaveOn.AspNetUsers
          .Where(u => departmentNames.Contains(u.DepartmentName))
          .AsEnumerable()
          .Select(u => new SelectListItem
          {
            Value = u.BioStarEmpNum.ToString(),
            /* Text = u.UserName*/
            Text = u.UserName.Split('@')[0].Replace('.', ' ')
          }).OrderBy(i => i.Text).ToList();
      //ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers.Where(u => departmentNames.Contains(u.DepartmentName)), "BioStarEmpNum", "UserName");
      return Json(users, JsonRequestBehavior.AllowGet);
    }
    private string CapitalizeName(string userName)
    {
      if (string.IsNullOrEmpty(userName))
        return string.Empty;

      // Split the userName to get the first name and last name
      var nameParts = userName.Split('@')[0].Split('.');
      var firstName = nameParts.Length > 0 ? nameParts[0] : "";
      var lastName = nameParts.Length > 1 ? nameParts[1] : "";

      // Capitalize the first letter of each name part
      if (!string.IsNullOrEmpty(firstName))
      {
        firstName = char.ToUpper(firstName[0]) + firstName.Substring(1).ToLower();
      }

      if (!string.IsNullOrEmpty(lastName))
      {
        lastName = char.ToUpper(lastName[0]) + lastName.Substring(1).ToLower();
      }

      // Return the combined formatted name
      return firstName + " " + lastName;
    }
    public List<SelectListItem> GetMonthSelectList()
    {
      int thisMonth = DateTime.Now.Month;
      //int monthCtr = thisMonth;
      int thisYear = DateTime.Now.Year;

      List<SelectListItem> monthSelectList = new List<SelectListItem>();
      for (int i = 1; i <= 13; i++)
      {
        if (thisMonth < 1)
        {
          thisMonth = 12;
          thisYear -= 1;
        }
        SelectListItem newItem = new SelectListItem();
        newItem.Value = thisMonth.ToString("00") + "-" + thisYear;
        newItem.Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(thisMonth) + " " + thisYear;
        thisMonth -= 1;
        monthSelectList.Add(newItem);
      }
      return monthSelectList;
    }
    [Authorize(Roles = "Admin,Manager,User")]
    public async Task<ActionResult> DepartmentData(string ReqMonthYear, string DepartmentName)
    {
      ViewBag.MonthSelectList = GetMonthSelectList();
      DateTime reqDate;
      DateTime endDate;

      if (!string.IsNullOrEmpty(ReqMonthYear))
      {
        //ReqMonthYear = "06-2023"; // hard coded value for testing
        reqDate = DateTime.ParseExact(ReqMonthYear, "MM-yyyy",
              System.Globalization.CultureInfo.CurrentCulture);
        // Get the last day of the month
        endDate = reqDate.AddMonths(1).AddDays(-1);
      }
      else
      {
        reqDate = DateTime.Now;
        endDate = new DateTime(reqDate.Year, reqDate.Month, DateTime.DaysInMonth(reqDate.Year, reqDate.Month)); // Set endDate as the last day of the current month

      }

      if (User.IsInRole("Admin"))
      {
        var departments = dbLeaveOn.AspNetUsers
               .Where(u => !string.IsNullOrEmpty(u.DepartmentName))
               .Select(u => u.DepartmentName)
               .Distinct()
               .Select(d => new SelectListItem { Value = d, Text = d })
               .ToList();
        ViewBag.Departments = new SelectList(departments, "Value", "Text");
      }
      else if (User.IsInRole("Manager") || User.IsInRole("User"))
      {
        string userId = User.Identity.GetUserId();
        //var departmentClaims = dbLeaveOn.AspNetUserClaims
        //    .Where(u => u.UserId == userId)
        //  .Select(u => new { Value = u.ClaimValue, Text = u.ClaimValue })
        //   .Distinct() 
        //    .ToList();\
        // Fetch the User own department
        var userDepartment = dbLeaveOn.AspNetUsers
            .Where(u => u.Id == userId)
            .Select(u => u.DepartmentName)
            .FirstOrDefault();


        // Get all employees under the manager
        var employeesUnderManager = dbLeaveOn.AspNetUsers
            .Where(u => u.ManagerID == userId || u.Manager2ID == userId)
            .ToList();

        // Retrieve unique departments of employees under the manager
        var employeeDepartments = employeesUnderManager
            .Where(e => !string.IsNullOrEmpty(e.DepartmentName)) // Ensure department is not null or empty
            .Select(e => e.DepartmentName)
            .Distinct() // Ensure distinct departments
            .ToList();
        //// Add the user own department to the list if it's not already included
        //if (!string.IsNullOrEmpty(userDepartment) && !employeeDepartments.Contains(userDepartment))
        //{
        //  employeeDepartments.Add(userDepartment);
        //}

        // Create the department list for the dropdown
        var departmentClaims = employeeDepartments
            .Select(d => new { Value = d, Text = d })
            .ToList();


          // Check if departments are available; if not, add a placeholder
          if (!departmentClaims.Any())
          {
            departmentClaims.Add(new { Value = "", Text = "Department Not Exists" });
          }

        

        //  // Set the departments in the ViewBag for use in the dropdown
         ViewBag.Departments = new SelectList(departmentClaims, "Value", "Text");
         ViewBag.SelectedDepartments = departmentClaims;
      }

      //string userId = User.Identity.GetUserId();
      // Get From Access Rights
      //  var departmentClaims = dbLeaveOn.AspNetUserClaims
      //      .Where(u => u.UserId == userId)
      //     .Select(u => new { Value = u.ClaimValue, Text = u.ClaimValue })
      //      .Distinct() 
      //      .ToList();

      //  // Check if departments are available; if not, add a placeholder
      //      if (!departmentClaims.Any())
      //   {
      //    departmentClaims.Add(new { Value = "", Text = "No department exists" });
      //   }

      //  // Set the departments in the ViewBag for use in the dropdown
      // ViewBag.Departments = new SelectList(departmentClaims, "Value", "Text");
      // ViewBag.SelectedDepartments = departmentClaims;
      //}
      List<TimeData> depData = null;
      if (!string.IsNullOrEmpty(ReqMonthYear))
      {
        //string userId = User.Identity.GetUserId();
        //int bioStarEmpNum = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.Id == userId).BioStarEmpNum.Value;

        //IQueryable<Attendance> allUsersData = null;
        List<AspNetUser> users = dbLeaveOn.AspNetUsers.Where(x => x.DepartmentName == DepartmentName).ToList<AspNetUser>();

        List<int> userIds = users.Select(x => x.BioStarEmpNum.Value).ToList<int>();
        string formattedStartDate = reqDate.ToString("dd-MM-yyyy");
        string formattedEndDate = endDate.ToString("dd-MM-yyyy");

        //string ReqMonthYearFormated = reqDate.Month.ToString("00") + "-" + reqDate.Year;
        //depData = await ConnectToDBandReturnWorkingHours(ReqMonthYearFormated, userIds);
        depData = await GetAttendanceSummary(formattedStartDate, formattedEndDate, userIds);

      }
      //return View(await db.Attendance.ToListAsync());
      if (string.IsNullOrEmpty(ReqMonthYear))
      {
        //in case of null param or first time
        if (!(depData is null))
        {
          return View(await Task.FromResult(depData.OrderBy(i => i.Date).ToList()));
        }
        else
        {
          return View();
        }
      }
      else
      {
        return PartialView("_UserData", await Task.FromResult(depData.OrderBy(i => i.Date).ToList()));
      }

    }



    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> CountriesData(string startDate, string endDate, string CountryName)
    {
      try
      {

        //DateTime myDate = DateTime.ParseExact("2009-05-08 14:40:52,531", "yyyy-MM-dd HH:mm:ss,fff",
        //                                 System.Globalization.CultureInfo.InvariantCulture);
        ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime reqDate;
        DateTime StartDate, EndDate;
        List<TimeData> depData = null;
        //int intDepartmentId;
        if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
        {

          //in case of empty parameters or First Time

          reqDate = DateTime.Now;
          // In case of empty parameters or first time
          StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
          EndDate = DateTime.Now;
          string userId = User.Identity.GetUserId();
          List<string> SelectedDeps = new List<string>();

          if (User.IsInRole("Admin")) { 
            string CountryId = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.Id == userId)?.CountryName?.Id.ToString();
            if (!string.IsNullOrEmpty(CountryId))
            {
              SelectedDeps.Add(CountryId);
            }
            SelectedDeps.Add(SelectedDeps.ToString());
            ViewBag.SelectedDepartments = SelectedDeps;
            ViewBag.Departments = new SelectList(dbLeaveOn.CountryNames, "Id", "Name");
            ViewBag.startDate = StartDate.ToString("dd-MMM-yyyy");
            ViewBag.endDate = EndDate.ToString("dd-MMM-yyyy");

          }
          else if (User.IsInRole("Manager"))
          {
            SelectedDeps = dbLeaveOn.AspNetUsers
                                   .Where(u => (u.ManagerID == userId || u.Manager2ID == userId)
                                               && u.CountryName != null)
                                   .Select(u => u.CountryName.Id.ToString())
                                   .Distinct()
                                   .ToList();
            SelectedDeps.Add(SelectedDeps.ToString());//CountryName);
            ViewBag.SelectedDepartments = SelectedDeps;
            //ViewBag.Departments = new SelectList(dbLeaveOn.Departments, "Id", "Name");
            ViewBag.Departments = new SelectList(
                dbLeaveOn.CountryNames
                         .Where(c => SelectedDeps.Contains(c.Id.ToString())),
                "Id",
                "Name"
            );
            ViewBag.startDate = StartDate.ToString("dd-MMM-yyyy");
            ViewBag.endDate = EndDate.ToString("dd-MMM-yyyy");
          }
     


        }
        else
        {
          //var identity = (ClaimsIdentity)User.Identity;
          //IEnumerable<Claim> claims = identity.Claims;
          //Claim claim = claims.Where(x => x.Value == CountryName).FirstOrDefault();
          //if (claim is null) return null;

          //string userId = User.Identity.GetUserId();

          //int bioStarEmpNum = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.Id == userId).BioStarEmpNum.Value;

          //IQueryable<Attendance> allUsersData = null;
          List<AspNetUser> users = dbLeaveOn.AspNetUsers.Where(x => x.CntryName == CountryName).ToList<AspNetUser>();

          List<int> userIds = users.Select(x => x.BioStarEmpNum.Value).ToList<int>();



          //string ReqMonthYearFormated = reqDate.Month.ToString("00") + "-" + reqDate.Year;
          //string ReqMonthYearFormated = startDate + "," + endDate; 
         // depData = await ConnectToDBandReturnCountriesData(startDate, endDate, userIds);
          string formattedStartDate = DateTime.ParseExact(startDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
          string formattedEndDate = DateTime.ParseExact(endDate, "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
          depData = await GetCountriesData(formattedStartDate, formattedEndDate, userIds);
        }


        //return View(await db.Attendance.ToListAsync());
        if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
        {
          //in case of null param or first time
          if (!(depData is null))
          {
            //return View(await depData.OrderBy(i => i.Date).ToList());

            return View(await Task.FromResult(depData.OrderBy(i => i.Date).ToList()));
          }
          else
          {
            return View();
          }

        }
        else
        {
          return PartialView("_CountriesData", await Task.FromResult(depData.OrderBy(i => i.Date).ToList()));
        }
      }
      catch (Exception ex)
      {
        throw ex;
      }
    }
    //[Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> AbsenteesData_UserWise(string startDate, string endDate, List<string> departmentName, List<string> bioStarEmpStr)
    {
      try { 
      ViewBag.MonthSelectList = GetMonthSelectList();
      DateTime reqDate;
      DateTime StartDate, EndDate;
        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
        {

          StartDate = DateTime.ParseExact(startDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
          EndDate = DateTime.ParseExact(endDate.Trim(), "dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
          // In case of empty parameters or first time
          StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // First day of the current month
          EndDate = DateTime.Now; // Current date

          ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers, "BioStarEmpNum", "UserName").OrderBy(i => i.Text);

        }

        List<TimeData> depData = null;
      if (!(string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate)))
      {
        var identity = (ClaimsIdentity)User.Identity;
          //List<AspNetUser> users = dbLeaveOn.AspNetUsers
          //    .Where(x => x.DepartmentName == departmentName && bioStarEmpStr.Contains(x.BioStarEmpNum.Value.ToString()))
          //   .ToList();
          List<AspNetUser> users = dbLeaveOn.AspNetUsers
         .Where(x => departmentName.Contains(x.DepartmentName) && bioStarEmpStr.Contains(x.BioStarEmpNum.Value.ToString()))
         .ToList();


          List<int> userIds = users.Select(x => x.BioStarEmpNum.Value).ToList<int>();
       // depData = await ConnectToDBandReturnAbsentees(startDate, endDate, userIds);
        depData = await GetAbsenteesData(startDate, endDate, userIds);
      }
      //return View(await db.Attendance.ToListAsync());
      if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
      {
        //in case of null param or first time
        if (!(depData is null))
        {
          //return View(await depData.OrderBy(i => i.Date).ToList());

          return View(await Task.FromResult(depData.OrderBy(i => i.Date).ToList()));
        }
        else
        {
          return View();
        }

      }
      else
      {
        return PartialView("_AbsenteesData_UserWise", await Task.FromResult(depData.OrderBy(i => i.Date).ToList()));
      }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Error: " + ex.ToString());
        // Check for inner exception
        if (ex.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
        }

        throw (ex);
      }

    }
    public JsonResult GetThisDepEmpsData(string DepartmentName)
    {
      List<SelectListItem> selDepEmps = new SelectList(dbLeaveOn.AspNetUsers.Where(x => x.DepartmentName == DepartmentName), "BioStarEmpNum", "UserName").OrderBy(i => i.Text).ToList();

      return Json(new SelectList(selDepEmps, "Value", "Text"));

    }
    [Authorize(Roles = "Admin,Manager,User")]
    public async Task<ActionResult> AbsenteesData(string ReqMonthYear, string DepartmentName, string UserId)
    {
      try
      {
        ViewBag.MonthSelectList = GetMonthSelectList();
        DateTime reqDate;
        int dEmpNum;
        string userId = User.Identity.GetUserId();

        //int intDepartmentId;
        if (!string.IsNullOrEmpty(ReqMonthYear))
        {
          reqDate = DateTime.ParseExact(ReqMonthYear, "MM-yyyy",
                System.Globalization.CultureInfo.CurrentCulture);
          //reqDate = reqDate.AddYears(-2);
          dEmpNum = int.Parse(UserId);
        }
        else
        {
          //in case of empty parameters or First Time

          reqDate = DateTime.Now;
          ViewBag.StartDate = new DateTime(reqDate.Year, reqDate.Month, 1).ToString("dd-MMM-yyyy");
          ViewBag.EndDate = reqDate.ToString("dd-MMM-yyyy");
        }
          if (User.IsInRole("Admin"))
          {
            var departments = dbLeaveOn.AspNetUsers
                   .Where(u => !string.IsNullOrEmpty(u.DepartmentName))
                   .Select(u => u.DepartmentName)
                   .Distinct()
                   .Select(d => new SelectListItem { Value = d, Text = d })
                   .ToList();
            ViewBag.Departments = departments;
            ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers, "BioStarEmpNum", "UserName");
            ViewBag.SelectedEmployees = UserId;
          }
          else if (User.IsInRole("Manager") || User.IsInRole("User"))
          {
            var managerDepartment = dbLeaveOn.AspNetUsers.FirstOrDefault(u => u.Id == userId).DepartmentName;
            ViewBag.Departments = new SelectList(new List<string> { managerDepartment });
            var employeesUnderManager = dbLeaveOn.AspNetUsers.Where(u => (u.ManagerID == userId || u.Manager2ID == userId)).ToList();
            ViewBag.Employees = new SelectList(employeesUnderManager, "BioStarEmpNum", "UserName");
            //ViewBag.Employees = new SelectList(dbLeaveOn.AspNetUsers.Where(u => u.DepartmentName == managerDepartment), "BioStarEmpNum", "UserName");
            ViewBag.SelectedEmployees = UserId;
          }
          List<TimeData> depData = null;
          if (!string.IsNullOrEmpty(ReqMonthYear))
        {
          var identity = (ClaimsIdentity)User.Identity;
          List<AspNetUser> users = dbLeaveOn.AspNetUsers.Where(x => x.DepartmentName == DepartmentName).ToList<AspNetUser>();


          List<int> userIds = users.Select(x => x.BioStarEmpNum.Value).ToList<int>();
          string ReqMonthYearFormated = reqDate.Month.ToString("00") + "-" + reqDate.Year;
          depData = await ConnectToDBandReturnWorkingHours(ReqMonthYearFormated, userIds);
        }
        //return View(  await db.Attendance.ToListAsync());
        if (string.IsNullOrEmpty(ReqMonthYear))
        {
          //in case of null param or first time
          if (!(depData is null))
          {
            //return View(await depData.OrderBy(i => i.Date).ToList());

            return View(await Task.FromResult(depData.OrderBy(i => i.Date).ToList()));
          }
          else
          {
            return View();
          }

        }
        else
        {
          return PartialView("_UserData", await Task.FromResult(depData.OrderBy(i => i.Date).ToList()));
        }
      }
      catch (Exception ex)
      {
        throw (ex);
      }

    }
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> MonthsWiseData(string ReqFromMonth, string ReqToMonth)
    {

      //DateTime myDate = DateTime.ParseExact("2009-05-08 14:40:52,531", "yyyy-MM-dd HH:mm:ss,fff",
      //                                 System.Globalization.CultureInfo.InvariantCulture);
      ViewBag.MonthSelectList = GetMonthSelectList();
      DateTime reqFromDate;
      DateTime reqToDate;
      //int intDepartmentId;
      if (!string.IsNullOrEmpty(ReqFromMonth))
      {

        //intDepartmentId = int.Parse(DepartmentId);
        //user.DepartmentId;//User.Identity.GetUserId();//
        reqFromDate = DateTime.ParseExact(ReqFromMonth, "MM-yyyy",
              System.Globalization.CultureInfo.CurrentCulture);
        reqToDate = DateTime.ParseExact(ReqToMonth, "MM-yyyy",
              System.Globalization.CultureInfo.CurrentCulture);
        //reqDate = reqDate.AddYears(-2);
      }
      else
      {
        //in case of empty parameters or First Time
        /*---------*/
        reqFromDate = DateTime.Now;
        reqToDate = DateTime.Now;
        //string userId = User.Identity.GetUserId();
        //DepartmentName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.Id == userId).DepartmentName;
        //List<string> SelectedDeps = new List<string>();
        //SelectedDeps.Add(DepartmentName);
        //ViewBag.SelectedDepartments = SelectedDeps;
        //ViewBag.Departments = new SelectList(dbLeaveOn.DepartmentNames, "Name", "Name");
        /*---------*/
      }
      List<TimeData> depData = null;
      List<TimeData> totalDepData = null;
      if (!string.IsNullOrEmpty(ReqFromMonth))
      {
        /*--------*/
        //var identity = (ClaimsIdentity)User.Identity;
        //IEnumerable<Claim> claims = identity.Claims;
        //Claim claim = claims.Where(x => x.Value == DepartmentName).FirstOrDefault();

        //if (claim is null) return null;
        /*--------*/
        List<AspNetUser> users = dbLeaveOn.AspNetUsers.ToList<AspNetUser>();

        List<int> userIds = users.Select(x => x.BioStarEmpNum.Value).ToList<int>();
        //foreach (AspNetUser user in users)
        //{

        string ReqMonthYearFormated = string.Empty;
        totalDepData = new List<TimeData>();
        for (var month = reqFromDate.Date; month.Date <= reqToDate.Date; month = month.AddMonths(1))
        {
          var startOfMonth = new DateTime(month.Year, month.Month, 1);
          var endOfMonth = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
          ReqMonthYearFormated = month.Month.ToString("00") + "-" + month.Year;
         // depData = await ConnectToDBandReturnWorkingHours(ReqMonthYearFormated, userIds);

          string formattedStartDate = startOfMonth.ToString("dd-MM-yyyy");
          string formattedEndDate = endOfMonth.ToString("dd-MM-yyyy");
          depData = await GetMonthWiseData(formattedStartDate, formattedEndDate, userIds);

          totalDepData.AddRange(depData);
        }
        //}
      }
      //return View(await db.Attendance.ToListAsync());
      if (string.IsNullOrEmpty(ReqFromMonth))
      {
        //in case of null param or first time
        if (!(totalDepData is null))
        {
          //return View(await totalDepData.OrderBy(i => i.Date).ToList());

          return View(await Task.FromResult(totalDepData.OrderBy(i => i.Date).ToList()));
        }
        else
        {
          return View();
        }

      }
      else
      {
        return PartialView("_MonthsWiseData", await Task.FromResult(totalDepData.OrderBy(i => i.Date).ToList()));
      }

    }
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> WhoIsIn(string reqDate)
    {
      //reqDate = "12-06-2019";
      DateTime from_Date = DateTime.Now.Date;//DateTime.ParseExact(reqDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
      DateTime to_Date = from_Date.AddDays(1);
      //ReqMonthYear = "06-2019";

      //reqDate = reqDate.Replace("/","-") + ".000";
      //List<string> dateAttr = ReqMonthYear.Split('-').ToList();
      //string connection = @"Data Source=10.1.10.28;Initial Catalog=BiostarAC;User Id=sa;Password=@Intech#123;";
      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      SqlCommand cmd;
      SqlDataReader dr;
      con.Open();

      //cmd = new SqlCommand("select * from T_LG202106 where USRID = '9919' and SRVDT>= '2021-06-01' AND SRVDT<= '2021-06-30' order by EVTLGUID", con);
      //cmd = new SqlCommand("select * from T_LG201901 where USRID = '2205' order by EVTLGUID", con);
      //cmd = new SqlCommand("select * from T_LG" + dateAttr[1] + dateAttr[0] + " where USRID ='" + UserId + "' and SRVDT>='#" +reqDate + "#' and  SRVDT<='#" + reqDate + "#' order by EVTLGUID", con);
      //cmd = new SqlCommand("select * from T_LG" + dateAttr[1] + dateAttr[0] + " where USRID ='" + UserId + "' and SRVDT between @fromDate and @toDate order by EVTLGUID", con);
      //cmd = new SqlCommand("select * from T_LG" + dateAttr[1] + dateAttr[0] + " where USRID ='" + UserId + "' and SRVDT>='" + from_Date.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "' and  SRVDT<='" + to_Date.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'  order by EVTLGUID", con);

      //cmd = new SqlCommand("SELECT USRID, MIN(DEVDT) DEVDT from punchlog" + from_Date.Year + from_Date.Month.ToString("00") + " where SRVDT between @fromDate and @toDate GROUP BY USRID order by DEVDT asc", con);
      cmd = new SqlCommand("SELECT user_id, MIN(devdt) devdt from punchlog" + " where devdt between '@fromDate' and '@toDate' GROUP BY USER_ID order by devdt asc", con);
      //cmd.Parameters.AddWithValue("@fromDate", from_Date.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
      //cmd.Parameters.AddWithValue("@toDate", to_Date.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
      cmd.Parameters.AddWithValue("@fromDate", from_Date);
      cmd.Parameters.AddWithValue("@toDate", to_Date);
      dr = cmd.ExecuteReader();
      DataTable dt = new DataTable();
      dt.Load(dr);
      con.Close();
      int rowsCount = dt.Rows.Count;


      List<TimeData> LstAttendances = new List<TimeData>();

      TimeData attendance;
      DateTime FirsTimeIn = DateTime.Today;
      DateTime LastTimeOut = DateTime.Today;

      long UserId;
      for (int j = 0; j <= rowsCount - 1; j++)
      {

        //------get actual off hour of this date--------
        //thisDateTime = (DateTime)dt.Rows[j]["SRVDT"];

        if (!string.IsNullOrEmpty(Convert.ToString(dt.Rows[j]["user_id"])))
        {
          try
          {
            UserId = Convert.ToInt64(dt.Rows[j]["user_id"]);
          }
          catch (Exception ex)
          {

            throw ex;
          }

        }
        else
        {
          continue;
        }

        //--------
        string UserName = string.Empty;
        string timeZone = string.Empty;
        string countryName = string.Empty;
        string userLeavePolicyDescription = string.Empty;
        string depName = string.Empty;
        AspNetUser aspNetUser = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId);
        if (aspNetUser != null)
        {
          UserName = aspNetUser.UserName.Substring(0, aspNetUser.UserName.IndexOf('@')).Replace(".", " ");

          if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
          if (aspNetUser.IsRelocated == true)
          {
            //in case relocate
            timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp).TimeZone;
            countryName = aspNetUser.CntryNameTemp;
          }
          else
          {
            timeZone = aspNetUser.CountryName.TimeZone;
            countryName = aspNetUser.CntryName;
          }
        }

        //----------


        //if (dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId) != null)
        //{
        //  timeZone = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId).CountryName.TimeZone;
        //  countryName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId).CountryName.Name;
        //}
        //else
        //{
        //  timeZone = string.Empty;
        //}


        FirsTimeIn = ConvertToCountryTimeZoneNew(dt, j, timeZone);//(DateTime)(dt.Rows[j]["devdt"]); //ConvertToCountryTimeZone(dt, j, timeZone);//(DateTime)(dt.Rows[j]["SRVDT"]);
        AspNetUser user = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId);
        if (aspNetUser != null)
        {
          if (aspNetUser.UserLeavePolicy != null) userLeavePolicyDescription = aspNetUser.UserLeavePolicy.Description;
          UserName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum.Value == UserId).UserName;
          UserName = UserName.Substring(0, UserName.IndexOf('@')).Replace(".", " ");
          depName = dbLeaveOn.AspNetUsers.FirstOrDefault(x => x.BioStarEmpNum == UserId).DepartmentName;
          attendance = new TimeData() { EmployeeName = UserName, EmployeeNumber = Convert.ToInt32(UserId), TimeZone = countryName, Policy = userLeavePolicyDescription, Department = depName, Date = FirsTimeIn.Date, Day = FirsTimeIn.DayOfWeek.ToString(), TimeIn = FirsTimeIn };

          LstAttendances.Add(attendance);

        }
      }
      return View("_UserDataToday", await Task.FromResult(LstAttendances));
    }


    private Task<List<TimeData>> GetCountriesData(string formattedStartDate, string formattedEndDate, List<int> UserIds)
    {
      DateTime startDate = DateTime.ParseExact(formattedStartDate.Trim(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      DateTime endDate = DateTime.ParseExact(formattedEndDate.Trim(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan totalWorkingHoursAllUsers = TimeSpan.Zero;
      TimeSpan totalTimeAllUsers = TimeSpan.Zero;

      // Fetch attendance data for each user
      foreach (int UserId in UserIds)
      {
        // Fetch attendance records for this user within the provided date range
        var attendanceRecords = dbLeaveOn.AttendanceDatas
        .Where(a => a.BioStarEmpNum == UserId && a.CreatedDate >= startDate && a.CreatedDate <= endDate)
        .GroupBy(a => new { a.BioStarEmpNum, Date = DbFunctions.TruncateTime(a.CreatedDate) })
        .Select(g => g.FirstOrDefault()) // Take only the first record for each day per user
        .OrderBy(a => a.CreatedDate)
        .ToList();

        if (!attendanceRecords.Any()) continue; // If no records found, skip to the next user

        // Process each attendance record
        foreach (var record in attendanceRecords)
        {
          DateTime? timeInNullable = record.FirstPunchIn;
          DateTime? timeOutNullable = record.LastPunchOut;
          DateTime timeIn = timeInNullable ?? DateTime.MinValue;
          DateTime timeOut = timeOutNullable ?? (timeInNullable ?? DateTime.MinValue);


          var aspNetUser = dbLeaveOn.AspNetUsers
                .FirstOrDefault(u => u.BioStarEmpNum == record.BioStarEmpNum);

          string countryName = record.CountryName?.Trim();

          if (timeIn != DateTime.MinValue && timeOut != DateTime.MinValue)
          {
            switch (countryName)
            {
              //    case "Pakistan":
              //      timeIn = timeIn.AddHours(5);
              //      timeOut = timeOut.AddHours(5);
              //      break;
              //case "United Arab Emirates":
              //  timeIn = timeIn.AddHours(1);
              //  timeOut = timeOut.AddHours(1);
              //  break;
              //    case "Saudi Arabia":
              //      timeIn = timeIn.AddHours(3);
              //      timeOut = timeOut.AddHours(3);
              //      break;
              //    //case "United Kingdom":
              //    //  timeIn = timeIn.AddHours(0);
              //    //  timeOut = timeOut.AddHours(0);
              //    //  break;
              //    case "United States":
              //      timeIn = timeIn.AddHours(5);
              //      timeOut = timeOut.AddHours(5);
              //      break;
              //    case "Nigeria":
              //      timeIn = timeIn.AddHours(1);
              //      timeOut = timeOut.AddHours(1);
              //      break;
              //    case "Egypt":
              //      timeIn = timeIn.AddHours(2);
              //      timeOut = timeOut.AddHours(2);
              //      break;
              //    case "Iraq":
              //      timeIn = timeIn.AddHours(3);
              //      timeOut = timeOut.AddHours(3);
              //      break;
              //case "Oman":
              //  timeIn = timeIn.AddHours(-1);
              //  timeOut = timeOut.AddHours(-1);
              //  break;
              //    case "Qatar":
              //      timeIn = timeIn.AddHours(3);
              //      timeOut = timeOut.AddHours(3);
              //      break;
              //case "Angola":
              //  timeIn = timeIn.AddHours(-4);
              //  timeOut = timeOut.AddHours(-4);
              //  break;
              //    case "Kazakhstan":
              //      timeIn = timeIn.AddHours(5);
              //      timeOut = timeOut.AddHours(5);
              //      break;
              //    case "Germany":
              //      timeIn = timeIn.AddHours(1); 
              //      timeOut = timeOut.AddHours(1);
              //      break;
              //    case "Singapore":
              //      timeIn = timeIn.AddHours(8); 
              //      timeOut = timeOut.AddHours(8);
              //      break;
              default:
                break;
            }
          }


          TimeSpan totalTime = timeIn != DateTime.MinValue && timeOut != DateTime.MinValue ? timeOut - timeIn : TimeSpan.Zero;
          TimeSpan totalWorkingHours = record.TotalWorkHours.HasValue && record.TotalWorkHours > 0
             ? TimeSpan.FromSeconds((double)record.TotalWorkHours)
             : TimeSpan.Zero;
          string day = record.CreatedDate.HasValue ? record.CreatedDate.Value.ToString("dddd") : "N/A";

          totalWorkingHoursAllUsers += totalWorkingHours;
          totalTimeAllUsers += totalTime;

      
          //Chek weekend
          string status;
          if (record.CreatedDate.HasValue && (record.CreatedDate.Value.DayOfWeek == DayOfWeek.Saturday || record.CreatedDate.Value.DayOfWeek == DayOfWeek.Sunday))
          {
            status = "Weekend";
          }
          else if (record.IsAbsent == true)
          {
            status = "Absent";
          } 
          //else  if (record.LeaveTypeID.HasValue && record.BreakHours == 0)
          //{
          //  var leaveName = dbLeaveOn.LeaveTypes
          //      .Where(a => a.Id == record.LeaveTypeID.Value)
          //      .Select(a => a.Name)
          //      .FirstOrDefault();
          //  status = leaveName;
          //}
          else
          {
            status = null;
          }

          // Map the data directly from AttendanceData
          LstTimeData.Add(new TimeData()
          {
            EmployeeName = record.UserName,
            EmployeeNumber = record.BioStarEmpNum ?? 0,
            Department = record.DepartmentName,
            TimeZone = record.CountryName,
            Policy = record.UserLeavePolicyID,
            Date = record.CreatedDate ?? DateTime.MinValue,
            Day = day,
            TimeIn = timeIn,
            TimeOut = timeOut,
            TotalTime = totalTime,
            WorkingHours = totalWorkingHours,
            Status = status,

          });
        }
      }

      ViewBag.TotalWorkingHours = totalWorkingHoursAllUsers.TotalHours.ToString("N2");
      ViewBag.TotalHours = totalTimeAllUsers.TotalHours.ToString("N2");

      return Task.FromResult(LstTimeData);
    }

    private Task<List<TimeData>> GetMonthWiseData(string formattedStartDate, string formattedEndDate, List<int> UserIds)
    {
      DateTime startDate = DateTime.ParseExact(formattedStartDate.Trim(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
      DateTime endDate = DateTime.ParseExact(formattedEndDate.Trim(), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);


      List<TimeData> LstTimeData = new List<TimeData>();
      TimeSpan totalWorkingHoursAllUsers = TimeSpan.Zero;
      TimeSpan totalTimeAllUsers = TimeSpan.Zero;

      // Fetch attendance data for each user
      foreach (int UserId in UserIds)
      {
        // Fetch attendance records for this user within the provided date range
        var attendanceRecords = dbLeaveOn.AttendanceDatas
        .Where(a => a.BioStarEmpNum == UserId && a.CreatedDate >= startDate && a.CreatedDate <= endDate)
        .GroupBy(a => new { a.BioStarEmpNum, Date = DbFunctions.TruncateTime(a.CreatedDate) })
        .Select(g => g.FirstOrDefault()) // Take only the first record for each day per user
        .OrderBy(a => a.CreatedDate)
        .ToList();

        if (!attendanceRecords.Any()) continue; // If no records found, skip to the next user

        // Process each attendance record
        foreach (var record in attendanceRecords)
        {
          DateTime? timeInNullable = record.FirstPunchIn;
          DateTime? timeOutNullable = record.LastPunchOut;
          DateTime timeIn = timeInNullable ?? DateTime.MinValue;
          DateTime timeOut = timeOutNullable ?? (timeInNullable ?? DateTime.MinValue);
          TimeSpan totalTime = timeIn != DateTime.MinValue && timeOut != DateTime.MinValue ? timeOut - timeIn : TimeSpan.Zero;
          TimeSpan totalWorkingHours = record.TotalWorkHours.HasValue && record.TotalWorkHours > 0
             ? TimeSpan.FromSeconds((double)record.TotalWorkHours)
             : TimeSpan.Zero;
          string day = record.CreatedDate.HasValue ? record.CreatedDate.Value.ToString("dddd") : "N/A";

          totalWorkingHoursAllUsers += totalWorkingHours;
          totalTimeAllUsers += totalTime;

          //Chek weekend
          string status;
          if (record.CreatedDate.HasValue && (record.CreatedDate.Value.DayOfWeek == DayOfWeek.Saturday || record.CreatedDate.Value.DayOfWeek == DayOfWeek.Sunday))
          {
            status = "Weekend";
          }
          else
          {
            status = record.IsAbsent == true ? "Absent" : null;
          }

          // Map the data directly from AttendanceData
          LstTimeData.Add(new TimeData()
          {
            EmployeeName = record.UserName,
            EmployeeNumber = record.BioStarEmpNum ?? 0,
            Department = record.DepartmentName,
            TimeZone = record.CountryName,
            Policy = record.UserLeavePolicyID,
            Date = record.CreatedDate ?? DateTime.MinValue,
            Day = day,
            TimeIn = timeIn,
            TimeOut = timeOut,
            TotalTime = totalTime,
            WorkingHours = totalWorkingHours,
            Status = status,

          });
        }
      }

      ViewBag.TotalWorkingHours = totalWorkingHoursAllUsers.TotalHours.ToString("N2");
      ViewBag.TotalHours = totalTimeAllUsers.TotalHours.ToString("N2");

      return Task.FromResult(LstTimeData);
    }


  }
}

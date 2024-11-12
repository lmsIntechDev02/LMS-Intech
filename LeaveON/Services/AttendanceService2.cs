using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LeaveON.UtilityClasses;
using Repository.Models;
using TimeManagement.Models;
using System.Diagnostics;

namespace LeaveON.Services
{
  public class AttendanceService2
  {
    private readonly List<int> CardReadersIn = new List<int> { 540099805, 543726490, /* other IDs */ };
    private readonly BioStarEntities dbBioStar = new BioStarEntities();
    private readonly LeaveONEntities dbLeaveOn = new LeaveONEntities();
    private readonly string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;

    public async Task ConnectToDBandFillAttendanceData(DateTime startDate, DateTime endDate)
    {
      var overallStopwatch = Stopwatch.StartNew();
      List<TimeData> attendanceDataList = new List<TimeData>();
      List<BreakHour> breakHours = new List<BreakHour>();

      try
      {
        var users = await dbLeaveOn.AspNetUsers.Where(u => u.BioStarEmpNum.HasValue).ToListAsync();
        using (SqlConnection con = new SqlConnection(connectionString))
        {
          try
          {
            await con.OpenAsync();
            foreach (var user in users)
            {
              await ProcessUserAttendance(con, user, startDate, endDate, attendanceDataList, breakHours);
            }

            //await ProcessHolidaysAndWeekends(attendanceDataList, startDate, endDate);
            await SaveAttendanceData(attendanceDataList, breakHours);
          }
          catch (SqlException ex)
          {
            Console.WriteLine($"Database connection error: {ex.Message}");
          }
          catch (Exception ex)
          {
            Console.WriteLine($"An error occurred in ConnectToDBandFillAttendanceData: {ex.Message}");
          }
          finally
          {
            if (con.State == System.Data.ConnectionState.Open)
              con.Close();
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"An error occurred while fetching users: {ex.Message}");
      }

      overallStopwatch.Stop();
      Console.WriteLine($"Total execution time for ConnectToDBandFillAttendanceData: {overallStopwatch.ElapsedMilliseconds} ms");
    }

    private async Task ProcessUserAttendance(SqlConnection con, AspNetUser user, DateTime startDate, DateTime endDate, List<TimeData> attendanceDataList, List<BreakHour> breakHours)
    {
      try
      {
        var cmd = new SqlCommand("SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE user_id = @UserId AND devdt BETWEEN @StartDate AND @EndDate ORDER BY devdt", con);
        cmd.Parameters.AddWithValue("@UserId", user.BioStarEmpNum);
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);

        using (var dr = await cmd.ExecuteReaderAsync())
        {
          DateTime? firstDateTime = null;
          DateTime lastDateTime = DateTime.MinValue;
          DateTime firsTimeIn = DateTime.ParseExact("2001-01-01 01:01:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
          DateTime lastTimeOut = firsTimeIn;
          TimeSpan dailyWorkingHours = TimeSpan.Zero;
          int currentDay = firsTimeIn.Day;

          while (await dr.ReadAsync())
          {
            try
            {
              DateTime punchTime = Convert.ToDateTime(dr["devdt"]);
              int deviceId = Convert.ToInt32(dr["DEVID"]);
              string punchType = CardReadersIn.Contains(deviceId) ? "IN" : "OUT";

              if (!firstDateTime.HasValue)
              {
                firstDateTime = punchTime;
                currentDay = punchTime.Day;
              }

              if (punchTime.Day != currentDay)
              {
                if (firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)
                {
                  string leaveName = "";
                  int leaveType = 0;
                  var leaveForDay = user.Leaves.Where(x => DbFunctions.TruncateTime(x.StartDate) == DbFunctions.TruncateTime(firsTimeIn)).FirstOrDefault();

                  if (leaveForDay != null)
                  {
                    var fetchLeaveType = dbLeaveOn.LeaveTypes.Find(leaveForDay.LeaveTypeId);
                    leaveName = fetchLeaveType?.Name ?? "";
                    leaveType = fetchLeaveType?.Id ?? 0;
                  }
                  attendanceDataList.Add(CreateTimeDataEntry(user, firsTimeIn, lastTimeOut, dailyWorkingHours, leaveName, leaveType));
                }

                firsTimeIn = punchTime;
                lastTimeOut = punchTime;
                dailyWorkingHours = TimeSpan.Zero;
                currentDay = punchTime.Day;
              }

              if (punchType == "IN" && firsTimeIn.Year == 2001)
              {
                firsTimeIn = punchTime;
              }

              if (punchType == "OUT")
              {
                lastTimeOut = punchTime;

                if (firsTimeIn.Year != 2001)
                {
                  dailyWorkingHours += lastTimeOut - firsTimeIn;
                }
              }

              if (punchType == "OUT" && !CardReadersIn.Contains(deviceId))
              {
                var timeOut = ConvertToCountryTimeZone(punchTime, user.CountryName?.TimeZone);
                var timeIn = ConvertToCountryTimeZone(punchTime, user.CountryName?.TimeZone);

                if (timeIn > timeOut && !breakHours.Any(b => b.UserId == user.Id && b.Date == timeOut.Date && b.PunchIn == timeOut && b.PunchOut == timeIn))
                {
                  breakHours.Add(new BreakHour
                  {
                    UserId = user.Id,
                    BioStarEmpNum = user.BioStarEmpNum.Value,
                    Date = timeOut.Date,
                    PunchIn = timeOut,
                    PunchOut = timeIn
                  });
                }
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine($"Error processing punch log for user {user.UserName}: {ex.Message}");
            }
          }

          if (firstDateTime.HasValue && firsTimeIn.Year != 2001 && lastTimeOut.Year != 2001)
          {
            attendanceDataList.Add(CreateTimeDataEntry(user, firsTimeIn, lastTimeOut, dailyWorkingHours, "Present"));
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error in ProcessUserAttendance for user {user.UserName}: {ex.Message}");
      }
    }
    private TimeData CreateTimeDataEntry(AspNetUser user, DateTime firsTimeIn, DateTime lastTimeOut, TimeSpan workingHours, string status, int leaveType=0)
    {
      return new TimeData
      {
        EmployeeName = user.UserName,
        EmployeeNumber = user.BioStarEmpNum.Value,
        TimeZone = user.CountryName?.TimeZone,
        CountryName = user.CountryName?.Name,
        Policy = user.UserLeavePolicy?.Description,
        Department = user.DepartmentName,
        Date = firsTimeIn.Date,
        Day = firsTimeIn.DayOfWeek.ToString(),
        TimeIn = firsTimeIn,
        TimeOut = lastTimeOut,
        WorkingHours = workingHours,
        TotalTime = lastTimeOut - firsTimeIn,
        Status = status,
        leaveType = status,
        leaveTypeID = leaveType,
      };
    }


    private async Task SaveAttendanceData(List<TimeData> attendanceDataList, List<BreakHour> breakHours)
    {
      using (var transaction = dbLeaveOn.Database.BeginTransaction())
      {
        try
        {
          if (breakHours.Any())
          {
            var newBreaks = breakHours
              .Where(b => !dbLeaveOn.BreakHours.Any(db => db.UserId == b.UserId && db.Date == b.Date && db.PunchIn == b.PunchIn && db.PunchOut == b.PunchOut))
              .ToList();

            dbLeaveOn.BreakHours.AddRange(newBreaks);
          }

          if (attendanceDataList.Any())
          {
            var newAttendance = new List<AttendanceData>();

            foreach (var a in attendanceDataList)
            {
              // Retrieve user info from dbLeaveOn.AspNetUsers for additional fields
              var userInfo = dbLeaveOn.AspNetUsers
                  .Where(u => u.BioStarEmpNum == a.EmployeeNumber)
                  .Select(u => new
                  {
                    u.Id,
                    u.ManagerEmail,
                    u.Manager2Email,
                    u.ManagerID,
                    u.Manager2ID
                  })
                  .FirstOrDefault();

              // Retrieve device ID and device name from BioStar database if available
              //var device = dbBioStar.PunchLogs
              //    .Where(p => p.UserId == a.EmployeeNumber && p.DevDateTime == a.TimeIn)
              //    .Select(p => new { p.DeviceId, p.DeviceName })
              //    .FirstOrDefault();

              if (userInfo != null)
              {
                newAttendance.Add(new AttendanceData
                {
                  BioStarEmpNum = a.EmployeeNumber,
                  UserName = a.EmployeeName,
                  DepartmentName = a.Department,
                  UserLeavePolicyID = a.Policy,
                  CreatedDate = a.Date,
                  FirstPunchIn = a.TimeIn,
                  LastPunchOut = a.TimeOut,
                  TotalWorkHours = (long)a.WorkingHours.TotalSeconds,
                  BreakHours = (long)(a.TotalTime.TotalSeconds - a.WorkingHours.TotalSeconds),
                  IsLateArrival = a.TimeIn.TimeOfDay > new TimeSpan(9, 30, 0),
                  IsEarlyDeparture = a.TimeOut.TimeOfDay < new TimeSpan(16, 45, 0),
                  IsAbsent = a.isAbsent,
                  IsLeave = a.leaveTypeID != 0,
                  LeaveTypeID = a.leaveTypeID,
                  LeaveType = a.leaveType,
                  CountryName = a.CountryName,
                  TimeZone = a.TimeZone,
                  ManagerEmail = userInfo.ManagerEmail,
                  Manager2Email = userInfo.Manager2Email,
                  ManagerId = userInfo.ManagerID,
                  Manager2Id = userInfo.Manager2ID,
                  UserID = userInfo.Id,
                  //Devnm = device.DeviceName,
                  //DEVID = device.DeviceId
                });
              }
            }

            dbLeaveOn.AttendanceDatas.AddRange(newAttendance);
          }

          await dbLeaveOn.SaveChangesAsync();
          transaction.Commit();
          Console.WriteLine("Data saved successfully.");
        }
        catch (Exception ex)
        {
          transaction.Rollback();
          Console.WriteLine("An error occurred while saving data. Transaction rolled back.");
          Console.WriteLine($"Error: {ex.Message}");
          if (ex.InnerException != null)
          {
            Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
          }
        }
      }
    }
    private DateTime ConvertToCountryTimeZone(DateTime dateTime, string timeZone)
    {
      try
      {
        if (string.IsNullOrEmpty(timeZone)) return dateTime;
        var customTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        return TimeZoneInfo.ConvertTimeFromUtc(dateTime, customTimeZone);
      }
      catch (TimeZoneNotFoundException ex)
      {
        Console.WriteLine($"Time zone not found: {ex.Message}");
        return dateTime;
      }
      catch (InvalidTimeZoneException ex)
      {
        Console.WriteLine($"Invalid time zone: {ex.Message}");
        return dateTime;
      }
    }
    private async Task ProcessHolidaysAndWeekends(List<TimeData> attendanceDataList, DateTime startDate, DateTime endDate)
    {
      foreach (var user in dbLeaveOn.AspNetUsers.Where(u => u.BioStarEmpNum.HasValue))
      {
        List<DateTime> weekends = GetWeekEndList(startDate, endDate, user.UserLeavePolicy?.WeeklyOffDays ?? "6,0");
        foreach (var date in weekends)
        {
          if (!attendanceDataList.Any(a => a.EmployeeNumber == user.BioStarEmpNum.Value && a.Date == date.Date))
          {
            //attendanceDataList.Add(CreateTimeDataEntry(user, date, date, TimeSpan.Zero, "Weekend"));
          }
        }

        var annualOffDays = dbLeaveOn.AnnualOffDays.Where(a => a.UserLeavePolicyId == user.UserLeavePolicyId && a.OffDay >= startDate && a.OffDay <= endDate).ToList();
        //foreach (var holiday in annualOffDays)
        //{
        //  if (!attendanceDataList.Any(a => a.EmployeeNumber == user.BioStarEmpNum.Value && a.Date == holiday.OffDay.Date))
        //  {
        //    attendanceDataList.Add(CreateTimeDataEntry(user, holiday.OffDay, holiday.OffDay, TimeSpan.Zero, holiday.Description));
        //  }
        //}

        var leaves = dbLeaveOn.Leaves.Where(l => l.UserId == user.Id && l.StartDate <= endDate && l.EndDate >= startDate && l.IsAccepted1 != null && l.IsAccepted2 != null).ToList();
        foreach (var leave in leaves)
        {
          DateTime leaveStart = leave.StartDate < startDate ? startDate : leave.StartDate;
          DateTime leaveEnd = leave.EndDate > endDate ? endDate : leave.EndDate;

          for (var date = leaveStart; date <= leaveEnd; date = date.AddDays(1))
          {
            if (!attendanceDataList.Any(a => a.EmployeeNumber == user.BioStarEmpNum.Value && a.Date == date.Date))
            {
              //attendanceDataList.Add(CreateTimeDataEntry(user, date, date, TimeSpan.Zero, "Leave"));
            }
          }
        }
      }
    }

    private List<DateTime> GetWeekEndList(DateTime startDate, DateTime endDate, string weekEndDays)
    {
      List<int> weekEndDayIndexes = weekEndDays.Split(',').Select(int.Parse).ToList();
      List<DateTime> weekEndDates = new List<DateTime>();

      for (var date = startDate; date <= endDate; date = date.AddDays(1))
      {
        if (weekEndDayIndexes.Contains((int)date.DayOfWeek))
        {
          weekEndDates.Add(date);
        }
      }

      return weekEndDates;
    }
  }
}

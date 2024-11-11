using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using TimeManagement.Models;
using LeaveON.UtilityClasses;
using Repository.Models;
using System.Threading.Tasks;

namespace LeaveON.Services
{
  public class AttendanceService3
  {
    private List<int> CardReadersIn = new List<int> { 540099805, 543726490, /* other IDs */ };
    private BioStarEntities dbBioStar = new BioStarEntities();
    private LeaveONEntities dbLeaveOn = new LeaveONEntities();

    public async Task ConnectToDBandFillAttendanceData(DateTime startDate, DateTime endDate)
    {
      Stopwatch overallStopwatch = Stopwatch.StartNew();
      Console.WriteLine("Connecting to database...");

      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      await con.OpenAsync();

      List<AspNetUser> users = await dbLeaveOn.AspNetUsers.Where(u => u.BioStarEmpNum.HasValue).ToListAsync();
      List<TimeData> attendanceDataList = new List<TimeData>();
      List<BreakHour> breakHoursList = new List<BreakHour>();

      foreach (var user in users)
      {
        var userStopwatch = Stopwatch.StartNew();
        await ProcessUserAttendance(con, user, startDate, endDate, attendanceDataList, breakHoursList);
        userStopwatch.Stop();
        Console.WriteLine($"Processed user {user.UserName} in {userStopwatch.ElapsedMilliseconds} ms");
      }

      //await con.CloseAsync();
      ProcessHolidaysAndWeekends(attendanceDataList, startDate, endDate, users);
      SaveAttendanceData(attendanceDataList, breakHoursList);

      overallStopwatch.Stop();
      Console.WriteLine($"Total execution time: {overallStopwatch.ElapsedMilliseconds} ms");
    }

    private async Task ProcessUserAttendance(SqlConnection con, AspNetUser user, DateTime startDate, DateTime endDate, List<TimeData> attendanceDataList, List<BreakHour> breakHoursList)
    {
      // Adjust time zone and country if relocated
      string timeZone = user.IsRelocated ? dbLeaveOn.CountryNames.FirstOrDefault(c => c.Name == user.CntryNameTemp)?.TimeZone : user.CountryName?.TimeZone;
      string countryName = user.IsRelocated ? user.CntryNameTemp : user.CountryName?.Name;

      string query = "SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE user_id = @UserId AND devdt BETWEEN @StartDate AND @EndDate ORDER BY devdt";
      using (SqlCommand cmd = new SqlCommand(query, con))
      {
        cmd.Parameters.AddWithValue("@UserId", user.BioStarEmpNum);
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);

        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        {
          List<DateTime> punches = new List<DateTime>();
          List<string> inOutFlags = new List<string>();
          string deviceName = string.Empty;

          while (await reader.ReadAsync())
          {
            punches.Add(ConvertToCountryTimeZone((DateTime)reader["devdt"], timeZone));
            inOutFlags.Add(CardReadersIn.Contains((int)reader["DEVID"]) ? "IN" : "OUT");
            deviceName = reader["devnm"].ToString();
          }

          CalculateAttendanceForUser(user, punches, inOutFlags, attendanceDataList, breakHoursList, timeZone, countryName, deviceName);
        }
      }
    }

    private void CalculateAttendanceForUser(AspNetUser user, List<DateTime> punches, List<string> inOutFlags, List<TimeData> attendanceDataList, List<BreakHour> breakHoursList, string timeZone, string countryName, string deviceName)
    {
      if (punches.Count == 0) return;

      DateTime firstIn = punches[0];
      DateTime lastOut = punches[0];
      TimeSpan totalWorkingHours = TimeSpan.Zero;
      TimeSpan breakTime = TimeSpan.Zero;

      for (int i = 0; i < punches.Count - 1; i++)
      {
        if (inOutFlags[i] == "IN" && inOutFlags[i + 1] == "OUT")
        {
          TimeSpan workingPeriod = punches[i + 1] - punches[i];
          totalWorkingHours += workingPeriod;
          lastOut = punches[i + 1];
        }
        else if (inOutFlags[i] == "OUT" && inOutFlags[i + 1] == "IN")
        {
          TimeSpan breakPeriod = punches[i + 1] - punches[i];
          breakTime += breakPeriod;
          breakHoursList.Add(new BreakHour
          {
            UserId = user.Id,
            BioStarEmpNum = user.BioStarEmpNum.Value,
            Date = punches[i].Date,
            PunchIn = punches[i],
            PunchOut = punches[i + 1]
          });
        }
      }

      bool isLate = firstIn.TimeOfDay > new TimeSpan(9, 30, 0);
      bool isEarly = lastOut.TimeOfDay < new TimeSpan(16, 45, 0);

      attendanceDataList.Add(new TimeData
      {
        EmployeeName = user.UserName,
        EmployeeNumber = user.BioStarEmpNum.Value,
        Date = firstIn.Date,
        TimeIn = firstIn,
        TimeOut = lastOut,
        WorkingHours = totalWorkingHours,
        TotalTime = totalWorkingHours + breakTime,
        Status = "Present",
        isLateArrival = isLate,
        isEarlyDeparture = isEarly,
        isAbsent = false,
        TimeZone = timeZone,
        CountryName = countryName,
        //Devnm = deviceName
      });
    }

    private void ProcessHolidaysAndWeekends(List<TimeData> attendanceDataList, DateTime startDate, DateTime endDate, List<AspNetUser> users)
    {
      foreach (var user in users)
      {
        List<DateTime> weekends = GetWeekendList(startDate, endDate, user.UserLeavePolicy?.WeeklyOffDays ?? "6,0");
        foreach (DateTime weekend in weekends)
        {
          if (!attendanceDataList.Any(a => a.EmployeeNumber == user.BioStarEmpNum && a.Date == weekend))
          {
            attendanceDataList.Add(new TimeData
            {
              EmployeeName = user.UserName,
              EmployeeNumber = user.BioStarEmpNum.Value,
              Date = weekend,
              Status = "Weekend",
              isAbsent = false
            });
          }
        }

        var holidays = dbLeaveOn.AnnualOffDays.Where(h => h.OffDay >= startDate && h.OffDay <= endDate && h.UserLeavePolicyId == user.UserLeavePolicyId).ToList();
        foreach (var holiday in holidays)
        {
          if (!attendanceDataList.Any(a => a.EmployeeNumber == user.BioStarEmpNum && a.Date == holiday.OffDay))
          {
            attendanceDataList.Add(new TimeData
            {
              EmployeeName = user.UserName,
              EmployeeNumber = user.BioStarEmpNum.Value,
              Date = (DateTime)holiday.OffDay,
              Status = holiday.Description,
              isAbsent = false
            });
          }
        }
      }
    }

    private async void SaveAttendanceData(List<TimeData> attendanceDataList, List<BreakHour> breakHoursList)
    {
      using (var transaction = dbLeaveOn.Database.BeginTransaction())
      {
        try
        {
          if (breakHoursList.Any())
          {
            dbLeaveOn.BreakHours.AddRange(breakHoursList);
          }

          if (attendanceDataList.Any())
          {
            foreach (var attendance in attendanceDataList)
            {
              if (!dbLeaveOn.AttendanceDatas.Any(a => a.BioStarEmpNum == attendance.EmployeeNumber && a.CreatedDate == attendance.Date))
              {
                dbLeaveOn.AttendanceDatas.Add(new AttendanceData
                {
                  BioStarEmpNum = attendance.EmployeeNumber,
                  UserName = attendance.EmployeeName,
                  CreatedDate = attendance.Date,
                  FirstPunchIn = attendance.TimeIn,
                  LastPunchOut = attendance.TimeOut,
                  TotalWorkHours = (long)attendance.WorkingHours.TotalSeconds,
                  BreakHours = (long)(attendance.TotalTime.TotalSeconds - attendance.WorkingHours.TotalSeconds),
                 // Status = attendance.Status,
                  IsLateArrival = attendance.isLateArrival,
                  IsEarlyDeparture = attendance.isEarlyDeparture,
                  IsAbsent = attendance.isAbsent,
                  CountryName = attendance.CountryName,
                  TimeZone = attendance.TimeZone,
                //  Devnm = attendance.Devnm
                });
              }
            }
          }

          await dbLeaveOn.SaveChangesAsync();
          transaction.Commit();
          Console.WriteLine("Data saved successfully.");
        }
        catch (Exception ex)
        {
          transaction.Rollback();
          Console.WriteLine($"An error occurred: {ex.Message}");
          if (ex.InnerException != null) Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }
      }
    }

    private DateTime ConvertToCountryTimeZone(DateTime dateTime, string timeZone)
    {
      return string.IsNullOrEmpty(timeZone) ? dateTime : TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
    }

    private List<DateTime> GetWeekendList(DateTime startDate, DateTime endDate, string weekendDays)
    {
      List<int> weekendIndexes = weekendDays.Split(',').Select(int.Parse).ToList();
      List<DateTime> weekends = new List<DateTime>();

      for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
      {
        if (weekendIndexes.Contains((int)date.DayOfWeek))
        {
          weekends.Add(date);
        }
      }
      return weekends;
    }
  }
}

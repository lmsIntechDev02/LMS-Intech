using System;
using System.Threading.Tasks;
using TimeManagement.Models;
using LeaveON.UtilityClasses;
using Repository.Models;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Diagnostics;

namespace LeaveON.Services
{
  public class BreakHoursService
  {
    List<int> LstCardReadersIn = new List<int> { 540099805, 543726490, 38677, 538595648, 35816, 540093375, 540093369, 540093374, 547241993, 540133115, 538848767, 540095692, 540130033, 540130042,
                                                 543734917, 538205733};
    private BioStarEntities dbBioStar = new BioStarEntities();
    LeaveONEntities dbLeaveOn = new LeaveONEntities();
    LeaveONEntitiesTarget dbLeaveOnTarget = new LeaveONEntitiesTarget();
    public async Task ConnectToDBandFillBreakHours(DateTime startDate, DateTime endDate)
    {
      var overallStopwatch = Stopwatch.StartNew();
      Console.WriteLine("Connecting to database...");
      string countryName = string.Empty;
      string previousCountryName = string.Empty;
      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      SqlConnection con = new SqlConnection(connection);
      SqlCommand cmd;
      SqlDataReader dr;
      List<string> logg = new List<string>();
      List<AspNetUser> users = dbLeaveOn.AspNetUsers.ToList();
      //  List<AspNetUser> users = dbLeaveOn.AspNetUsers.Where(u => u.Email == "kashif.ijaz@intechww.com").ToList();

      List<BreakHour> LstBreakHours = new List<BreakHour>();

      con.Open();
      foreach (var aspNetUser in users)
      {
        int UserId = aspNetUser.BioStarEmpNum.Value;

        // Query optimized to reduce repetitive queries
        cmd = new SqlCommand("SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE user_id = @UserId AND devdt BETWEEN @StartDate AND @EndDate ORDER BY devdt", con);
        cmd.Parameters.AddWithValue("@UserId", UserId);
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);
        dr = cmd.ExecuteReader();

        List<PunchLog> punchLogs = new List<PunchLog>();
        string timeZone = aspNetUser.CountryName?.TimeZone ?? string.Empty;
        string userGuidId = aspNetUser.Id;
        if (string.IsNullOrEmpty(userGuidId))
        {
          Console.WriteLine($"Skipping user with invalid Id: {aspNetUser.BioStarEmpNum}");
          continue;
        }

        // Processing user country and timezone data
        if (aspNetUser.IsRelocated)
        {
          timeZone = dbLeaveOn.CountryNames.FirstOrDefault(x => x.Name == aspNetUser.CntryNameTemp)?.TimeZone;
        }
       

        while (dr.Read())
        {
          PunchLog log = new PunchLog
          {
            UserId = Convert.ToInt32(dr["user_id"]),
            DeviceId = Convert.ToInt32(dr["DEVID"]),
            DeviceName = dr["devnm"].ToString(),
            DeviceDate = Convert.ToDateTime(dr["devdt"]),
            BreakStart = dr["bsevtdt"] != DBNull.Value ? (DateTime?)dr["bsevtdt"] : null
          };
          punchLogs.Add(log);
        }
        dr.Close();

        // Process the punch logs to determine break hours
        ProcessBreakHours(punchLogs, aspNetUser, timeZone, LstBreakHours);
      }

      // After processing all users, save break hours
      try
      {
        SaveBreakHours(LstBreakHours);
      } 
      catch (Exception ex)
      {
        Console.WriteLine("Exceptioin => " + ex.Message);
        if (ex.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
        }
      }
    }

    private void ProcessBreakHours(List<PunchLog> punchLogs, AspNetUser aspNetUser, string timeZone, List<BreakHour> LstBreakHours)
    {
      for (int i = 0; i < punchLogs.Count - 1; i++)
      {
        var currentLog = punchLogs[i];
        var nextLog = punchLogs[i + 1];

        if (!LstCardReadersIn.Contains(currentLog.DeviceId) && LstCardReadersIn.Contains(nextLog.DeviceId))
        {
          DateTime timeOutt = ConvertToCountryTimeZoneNew(currentLog.DeviceDate, timeZone);
          DateTime timeInn = ConvertToCountryTimeZoneNew(nextLog.DeviceDate, timeZone);

          if (timeInn > timeOutt)
          {
            TimeSpan breakDuration = timeInn - timeOutt;

            // Check for duplicates in LstBreakHours
            bool exists = LstBreakHours.Any(b =>
                b.UserId == aspNetUser.Id &&
                b.Date == timeOutt.Date &&
                b.PunchIn == timeOutt &&
                b.PunchOut == timeInn);

            if (!exists)
            {

              if (aspNetUser.BioStarEmpNum == null)
              {
                Console.WriteLine($"BioStarEmpNum is null for user with Id {aspNetUser.Id}. Skipping entry.");
                continue;
              }
              BreakHour breakEntry = new BreakHour
              {
                UserId = aspNetUser.Id,
                BioStarEmpNum = aspNetUser.BioStarEmpNum.Value,
                Date = timeOutt.Date,
                PunchIn = timeOutt,
                PunchOut = timeInn
              };
              Console.WriteLine($"Processing User: {breakEntry.UserId},{breakEntry.BioStarEmpNum}, Date: {breakEntry.Date}");
              LstBreakHours.Add(breakEntry);
            }
          }
        }
      }
    }
    private DateTime ConvertToCountryTimeZoneNew(DateTime dateTime, string timeZone)
    {
      try
      {
        var customTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        return TimeZoneInfo.ConvertTimeFromUtc(dateTime, customTimeZone);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error converting time: {ex.Message}");
        return dateTime;
      }
    }
    private void SaveBreakHours(List<BreakHour> LstBreakHours)
    {
      foreach (var breakEntry in LstBreakHours)
      {
        if (string.IsNullOrEmpty(breakEntry.UserId))
        {
          Console.WriteLine($"Skipping user with bio Id: {breakEntry.BioStarEmpNum}");
          continue;
        }
        bool dbExists = dbLeaveOn.BreakHours.Any(b =>
            b.UserId == breakEntry.UserId &&
            b.Date == breakEntry.Date &&
            b.PunchIn == breakEntry.PunchIn &&
            b.PunchOut == breakEntry.PunchOut);

        // Find existing record matching UserId & Date
        var existingRecord = dbLeaveOn.BreakHours
            .FirstOrDefault(b =>
                b.UserId == breakEntry.UserId &&
                DbFunctions.TruncateTime(b.Date) == breakEntry.Date.Date);

        //if (!dbExists)
        //{

        //  Console.WriteLine($"BreakHours for user: {breakEntry.UserId}, {breakEntry.BioStarEmpNum}, Date: {breakEntry.Date}, PunchIn: {breakEntry.PunchIn}, PunchOut: {breakEntry.PunchOut}");
        //  dbLeaveOn.BreakHours.Add(breakEntry);
        //}

        if (existingRecord != null)
        {
          // Update existing record with new punch times
          existingRecord.PunchIn = breakEntry.PunchIn;
          existingRecord.PunchOut = breakEntry.PunchOut;
          Console.WriteLine($"Updated BreakHours for user: {breakEntry.UserId}, Date: {breakEntry.Date}");
        }
        else
        {
          // Add new record
          Console.WriteLine($"BreakHours for user: {breakEntry.UserId}, {breakEntry.BioStarEmpNum}, Date: {breakEntry.Date}, PunchIn: {breakEntry.PunchIn}, PunchOut: {breakEntry.PunchOut}");
          dbLeaveOn.BreakHours.Add(breakEntry);
        }
      }
      try
      {
        dbLeaveOn.SaveChanges();
      }
      catch (SqlException ex)
      {
        Console.WriteLine($"SQL Error: {ex.Message}");
      }
    }

    public class PunchLog
    {
      public int UserId { get; set; }
      public int DeviceId { get; set; }
      public string DeviceName { get; set; }
      public DateTime DeviceDate { get; set; }
      public DateTime? BreakStart { get; set; }
    }
  }

}

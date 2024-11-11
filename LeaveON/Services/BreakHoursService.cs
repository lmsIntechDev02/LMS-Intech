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
namespace LeaveON.Services
{
  public class BreakHoursService
  {
    private readonly List<int> LstCardReadersIn = new List<int> { 540099805, 543726490, 38677, 538595648, 35816, 540093375, 540093369, 540093374, 547241993, 540133115, 538848767, 540095692, 540130033, 540130042 };
    private readonly BioStarEntities dbBioStar = new BioStarEntities();
    private readonly LeaveONEntities dbLeaveOn = new LeaveONEntities();

    public async Task ConnectToDBandFillBreakHours(DateTime startDate, DateTime endDate)
    {
      List<BreakHour> LstBreakHours = new List<BreakHour>();
      var users = dbLeaveOn.AspNetUsers.ToList();
      // var userIds = users.Select(x => x.BioStarEmpNum.Value).ToList();
      List<int> userIds = new List<int> { 1179 };

      string connection = System.Configuration.ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;
      using (SqlConnection con = new SqlConnection(connection))
      {
        await con.OpenAsync();
        try { 
        foreach (int userId in userIds)
        {
          List<DataRow> userPunchLogs = FetchUserPunchLogs(con, userId, startDate, endDate);
          DataTable sortedDataTable = SortDataTable(userPunchLogs);

          ProcessUserBreakHours(userId, sortedDataTable, LstBreakHours);
        }

        SaveBreakHours(LstBreakHours);
        }
        catch (Exception ex)
      {
        Console.WriteLine($"Error processing: {ex.Message}");
      }
      }
    }

    private List<DataRow> FetchUserPunchLogs(SqlConnection con, int userId, DateTime startDate, DateTime endDate)
    {
      List<DataRow> rows = new List<DataRow>();
      string query = "SELECT user_id, devdt, bsevtdt, DEVID, devnm FROM punchlog WHERE user_id = @UserId AND devdt BETWEEN @StartDate AND @EndDate ORDER BY devdt";

      using (SqlCommand cmd = new SqlCommand(query, con))
      {
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@StartDate", startDate);
        cmd.Parameters.AddWithValue("@EndDate", endDate);

        using (SqlDataReader dr = cmd.ExecuteReader())
        {
          DataTable dt = new DataTable();
          dt.Load(dr);
          rows = dt.Rows.Cast<DataRow>().ToList();
        }
      }
      return rows;
    }

    private DataTable SortDataTable(List<DataRow> rows)
    {
      DataTable dt = new DataTable();
      dt.Columns.Add("user_id", typeof(int));
      dt.Columns.Add("devdt", typeof(DateTime));
      dt.Columns.Add("bsevtdt", typeof(DateTime));
      dt.Columns.Add("DEVID", typeof(int));
      dt.Columns.Add("devnm", typeof(string));
      foreach (var row in rows)
      {
        dt.ImportRow(row);
      }
      dt.DefaultView.Sort = "devdt ASC";
      return dt.DefaultView.ToTable();
    }

    private void ProcessUserBreakHours(int userId, DataTable dt, List<BreakHour> LstBreakHours)
    {
      for (int i = 0; i < dt.Rows.Count - 1; i++)
      {
        try
        {
          int currentDeviceId = Convert.ToInt32(dt.Rows[i]["DEVID"]);
          int nextDeviceId = Convert.ToInt32(dt.Rows[i + 1]["DEVID"]);

          if (!LstCardReadersIn.Contains(currentDeviceId) && LstCardReadersIn.Contains(nextDeviceId))
          {
            DateTime punchOut = Convert.ToDateTime(dt.Rows[i]["devdt"]);
            DateTime punchIn = Convert.ToDateTime(dt.Rows[i + 1]["devdt"]);

            if (punchIn > punchOut)
            {
              var user = dbLeaveOn.AspNetUsers.FirstOrDefault(u => u.BioStarEmpNum == userId);
              if (user != null)
              {
                bool exists = LstBreakHours.Any(b =>
                    b.UserId == user.Id &&
                    b.Date == punchOut.Date &&
                    b.PunchIn == punchOut &&
                    b.PunchOut == punchIn);

                if (!exists)
                {
                  LstBreakHours.Add(new BreakHour
                  {
                    UserId = user.Id,
                    BioStarEmpNum = userId,
                    Date = punchOut.Date,
                    PunchIn = punchOut,
                    PunchOut = punchIn
                  });
                }
              }
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error processing: {ex.Message}");
        }
      }
    }

    private void SaveBreakHours(List<BreakHour> LstBreakHours)
    {
      foreach (var breakEntry in LstBreakHours)
      {
        try { 
        bool dbExists = dbLeaveOn.BreakHours.Any(b =>
            b.UserId == breakEntry.UserId &&
            b.Date == breakEntry.Date &&
            b.PunchIn == breakEntry.PunchIn &&
            b.PunchOut == breakEntry.PunchOut);

        if (!dbExists)
        {
          dbLeaveOn.BreakHours.Add(breakEntry);
        }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Error processing: {ex.Message}");
        }
      }
      Console.WriteLine("Saved breakhous");
      dbLeaveOn.SaveChanges();
    }
  }
}





//using System;
//using System.Threading.Tasks;
//using TimeManagement.Models;
//using LeaveON.UtilityClasses;
//using Repository.Models;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Data.Entity;
//using System.Configuration;
//using System.Threading;

//namespace LeaveON.Services
//{
//  public class BreakHoursService
//  {
//    private readonly List<int> LstCardReadersIn = new List<int> { 540099805, 543726490, 38677, 538595648, 35816, 540093375, 540093369, 540093374, 547241993, 540133115, 538848767, 540095692, 540130033, 540130042 };
//    private readonly BioStarEntities dbBioStar = new BioStarEntities();
//    private readonly LeaveONEntities dbLeaveOn = new LeaveONEntities();

//    public async Task ConnectToDBandFillBreakHours(DateTime startDate, DateTime endDate)
//    {
//      List<BreakHour> LstBreakHours = new List<BreakHour>();
//      List<int> userIds = new List<int> { 1179 }; // Define your user IDs as needed

//      // Fetch all necessary user data in a single query
//      var userIdDictionary = dbLeaveOn.AspNetUsers
//          .Where(u => userIds.Contains(u.BioStarEmpNum.Value))
//          .ToDictionary(u => u.BioStarEmpNum, u => u.Id);

//      string connectionString = ConfigurationManager.ConnectionStrings["BioStarEntities"].ConnectionString;

//      using (SqlConnection con = new SqlConnection(connectionString))
//      {
//        await con.OpenAsync();

//        // Parallelize the processing for each user
//        var tasks = userIds.Select(userId => Task.Run(() =>
//        {
//          var userPunchLogs = FetchUserPunchLogs(con, userId, startDate, endDate);
//          var sortedDataTable = SortDataTable(userPunchLogs);
//          ProcessUserBreakHours(userId, sortedDataTable, LstBreakHours, userIdDictionary);
//        })).ToArray();

//        await Task.WhenAll(tasks);
//      }

//      // Use BulkInsert for improved performance
//      await SaveBreakHoursAsync(LstBreakHours);
//    }

//    private List<DataRow> FetchUserPunchLogs(SqlConnection con, int userId, DateTime startDate, DateTime endDate)
//    {
   
//        List<DataRow> rows = new List<DataRow>();
//        string query = @"
//                SELECT user_id, devdt, bsevtdt, DEVID, devnm 
//                FROM punchlog 
//                WHERE user_id = @UserId 
//                AND devdt BETWEEN @StartDate AND @EndDate 
//                ORDER BY devdt ASC";

//        using (SqlCommand cmd = new SqlCommand(query, con))
//        {
//          cmd.Parameters.AddWithValue("@UserId", userId);
//          cmd.Parameters.AddWithValue("@StartDate", startDate);
//          cmd.Parameters.AddWithValue("@EndDate", endDate);

//          using (SqlDataReader dr = cmd.ExecuteReader())
//          {
//            DataTable dt = new DataTable();
//            dt.Load(dr);
//            rows = dt.Rows.Cast<DataRow>().ToList();
//          }
//        }
//        return rows;
//    }

//    private DataTable SortDataTable(List<DataRow> rows)
//    {
//      DataTable dt = new DataTable();
//      dt.Columns.Add("user_id", typeof(int));
//      dt.Columns.Add("devdt", typeof(DateTime));
//      dt.Columns.Add("bsevtdt", typeof(DateTime));
//      dt.Columns.Add("DEVID", typeof(int));
//      dt.Columns.Add("devnm", typeof(string));

//      foreach (var row in rows)
//      {
//        dt.ImportRow(row);
//      }

//      return dt.DefaultView.ToTable();

//    }

//    private void ProcessUserBreakHours(int userId, DataTable dt, List<BreakHour> LstBreakHours, Dictionary<int?, string> userIdDictionary)
//    {
//      var breakHoursSet = new HashSet<(int userId, DateTime date, DateTime punchIn, DateTime punchOut)>();

//      for (int i = 0; i < dt.Rows.Count - 1; i++)
//      {
//        int currentDeviceId = Convert.ToInt32(dt.Rows[i]["DEVID"]);
//        int nextDeviceId = Convert.ToInt32(dt.Rows[i + 1]["DEVID"]);

//        if (!LstCardReadersIn.Contains(currentDeviceId) && LstCardReadersIn.Contains(nextDeviceId))
//        {
//          try { 
//          DateTime punchOut = Convert.ToDateTime(dt.Rows[i]["devdt"]);
//          DateTime punchIn = Convert.ToDateTime(dt.Rows[i + 1]["devdt"]);

//          if (punchIn > punchOut)
//          {
//            // Retrieve the user ID from the dictionary
//            if (userIdDictionary.ContainsKey(userId))
//            {
//              string userIdString = userIdDictionary[userId]; // The ID is a string

//              // Check for duplicates using a HashSet
//              if (!breakHoursSet.Contains((userId, punchOut.Date, punchOut, punchIn)))
//              {
//                breakHoursSet.Add((userId, punchOut.Date, punchOut, punchIn));
//                LstBreakHours.Add(new BreakHour
//                {
//                  UserId = userIdString,  // Storing the user ID as a string
//                  BioStarEmpNum = userId,
//                  Date = punchOut.Date,
//                  PunchIn = punchOut,
//                  PunchOut = punchIn
//                });
//              }
//            }
//          }
//          }
//          catch (Exception ex)
//          {
//            Console.WriteLine($"Error processing: {ex.Message}");
//          }
//        }
//      }
//    }

//private async Task SaveBreakHoursAsync(List<BreakHour> LstBreakHours)
//{

//    // Fetch the existing break hours from the database
//    var existingBreakHours = await dbLeaveOn.BreakHours.AsNoTracking()
//        .Select(b => new { b.UserId, b.Date, b.PunchIn, b.PunchOut }) 
//        .ToListAsync();

//    // Create a HashSet for fast comparison in-memory
//    var existingBreakHoursSet = new HashSet<(string UserId, DateTime Date, DateTime PunchIn, DateTime PunchOut)>(
//        existingBreakHours.Select(b => (b.UserId, b.Date, b.PunchIn, b.PunchOut)) 
//    );

//    // Find new break hours that are not in the existing break hours set
//    var newBreakHours = LstBreakHours
//        .Where(b => !existingBreakHoursSet.Contains((b.UserId, b.Date, b.PunchIn, b.PunchOut))) 
//        .ToList();
//      try { 
//    // Bulk insert new break hours if there are any new ones
//    if (newBreakHours.Any())
//    {
//        dbLeaveOn.BreakHours.AddRange(newBreakHours);
//        await dbLeaveOn.SaveChangesAsync();
//    }

//    Console.WriteLine("Saved break hours.");
//      }
//      catch (Exception ex)
//      {
//        Console.WriteLine($"Error processing: {ex.Message}");
//      }
//    }

//  }
//}


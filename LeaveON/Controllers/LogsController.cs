using LeaveON.Models;
using Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeaveON.Controllers
{
  [Authorize(Roles = "Admin,Manager")]
  public class LogsController : Controller
  {
    private LeaveONEntities _context = new LeaveONEntities();
    // GET: Logs
    [HttpGet]
    public ActionResult Index(AuditLogFilterViewModel filter)
    {
      var query = _context.AuditLogs.AsQueryable();
      var totalCount = query.Count();

      if (!string.IsNullOrEmpty(filter.TableName))
        query = query.Where(a => a.TableName == filter.TableName);

      if (!string.IsNullOrEmpty(filter.RecordId))
        query = query.Where(a => a.RecordId == filter.RecordId);

      if (!string.IsNullOrEmpty(filter.UserId))
        query = query.Where(a => a.UserId.Contains(filter.UserId));

      if (!string.IsNullOrEmpty(filter.Action))
        query = query.Where(a => a.Action == filter.Action);

      if (filter.FromDate.HasValue)
        query = query.Where(a => a.AuditDate >= filter.FromDate.Value);

      if (filter.ToDate.HasValue)
        query = query.Where(a => a.AuditDate <= filter.ToDate.Value.AddDays(1));

      var rawLogs = query
                   .OrderByDescending(a => a.AuditDate)
                   .ToList();



      // Group rows that belong to the same "save" action together
      filter.Results = rawLogs
          .GroupBy(a => new { a.TableName, a.RecordId, a.AuditDate, a.UserId, a.Action, a.ScreenName })
          .Select(g => new AuditLogGroupViewModel
          {
            TableName = g.Key.TableName,
            RecordId = g.Key.RecordId,
            Action = g.Key.Action,
            ScreenName = g.Key.ScreenName,
            UserId = g.Key.UserId,
            AuditDate = g.Key.AuditDate,
            Fields = g.Select(x => new AuditLogFieldViewModel
            {
              ColumnName = x.ColumnName,
              OldValue = x.OldValue,
              NewValue = x.NewValue
            }).ToList()
          })
          .OrderByDescending(g => g.AuditDate)
          .ToList();

      ViewBag.TableNames = _context.AuditLogs.Select(a => a.TableName).Distinct().ToList();
      ViewBag.Actions = new[] { "Added", "Modified", "Deleted" };

      return View(filter);
    }

  }
}


 
  

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
 
[HttpGet]
public ActionResult Index()
    {
      ViewBag.TableNames = _context.AuditLogs
          .Select(a => a.TableName)
          .Distinct()
          .OrderBy(x => x)
          .ToList();

      ViewBag.Actions = new[] { "Added", "Modified", "Deleted" };

      return View();
    }


    [HttpPost]
    public JsonResult GetAuditLogs(AuditLogFilterViewModel filter)
    {
      try
      {
        var query = _context.AuditLogs.AsQueryable();

        // -----------------------------
        // Filters
        // -----------------------------

        if (!string.IsNullOrWhiteSpace(filter.TableName))
        {
          query = query.Where(a =>
              a.TableName == filter.TableName);
        }

        if (!string.IsNullOrWhiteSpace(filter.RecordId))
        {
          query = query.Where(a =>
              a.RecordId == filter.RecordId);
        }

        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
          query = query.Where(a =>
              a.UserId.Contains(filter.UserId));
        }

        if (!string.IsNullOrWhiteSpace(filter.DDLAction))
        {
          query = query.Where(a =>
              a.Action == filter.DDLAction);
        }

        if (filter.FromDate.HasValue)
        {
          query = query.Where(a =>
              a.AuditDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
          var toDate = filter.ToDate.Value.Date.AddDays(1);

          query = query.Where(a =>
              a.AuditDate < toDate);
        }


        // -----------------------------
        // Group records
        // -----------------------------

        var groupedQuery = query
            .GroupBy(a => new
            {
              a.TableName,
              a.RecordId,
              a.AuditDate,
              a.UserId,
              a.Action,
              a.ScreenName
            })
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
            });


        // -----------------------------
        // Total records after filtering
        // -----------------------------

        var totalRecords = groupedQuery.Count();


        // -----------------------------
        // Ordering
        // -----------------------------

        groupedQuery = groupedQuery
            .OrderByDescending(x => x.AuditDate);


        // -----------------------------
        // Paging
        // -----------------------------

        var page = filter.Start / filter.Length;

        var records = groupedQuery
            .Skip(filter.Start)
            .Take(filter.Length)
            .ToList();


        // -----------------------------
        // DataTables response
        // -----------------------------

        var data = records.Select(x => new
        {
          x.TableName,
          x.RecordId,
          x.Action,
          x.ScreenName,
          x.UserId,

          AuditDate = x.AuditDate.ToString("yyyy-MM-dd HH:mm:ss"),

          FieldsCount = x.Fields.Count,

          Fields = x.Fields.Select(f => new
          {
            f.ColumnName,
            f.OldValue,
            f.NewValue
          }).ToList()
        }).ToList();


        return Json(new
        {
          draw = filter.Draw,

          recordsTotal = totalRecords,
          recordsFiltered = totalRecords,

          data = data
        }, JsonRequestBehavior.AllowGet);
      }
      catch (Exception ex)
      {
        return Json(new
        {
          draw = filter.Draw,

          recordsTotal = 0,
          recordsFiltered = 0,

          data = new List<object>(),

          error = ex.Message
        }, JsonRequestBehavior.AllowGet);
      }
    }
 

  

  }
}


 
  

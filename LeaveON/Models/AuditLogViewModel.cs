using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeaveON.Models
{
  public class AuditLogViewModel
  {
  }
  public class AuditLogGroupViewModel
  {
    public string TableName { get; set; }
    public string RecordId { get; set; }
    public string Action { get; set; }        // Added / Modified / Deleted
    public string ScreenName { get; set; }
    public string UserId { get; set; }
    public DateTime AuditDate { get; set; }
    public List<AuditLogFieldViewModel> Fields { get; set; }
  }

  public class AuditLogFieldViewModel
  {
    public string ColumnName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
  }

  public class AuditLogFilterViewModel
  {
    public string TableName { get; set; }
    public string RecordId { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public List<AuditLogGroupViewModel> Results { get; set; }
  }
}

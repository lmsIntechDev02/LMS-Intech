using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeaveON.Models.DatatableVmModel
{
  public class LeaveListViewModel
  {
    public int Id { get; set; }

    public DateTime? DateCreated { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string LeaveTypeName { get; set; }

    public bool IsShortLeave { get; set; }
    public decimal? TotalDays { get; set; }
    public double? TotalHours { get; set; }

    public string Reason { get; set; }

    public int? IsAccepted1 { get; set; }
    public string Remarks1 { get; set; }

    public int? IsAccepted2 { get; set; }
    public string Remarks2 { get; set; }

    public string ApprovalStatus1 { get; set; }
    public string ApprovalStatus2 { get; set; }
  }
}

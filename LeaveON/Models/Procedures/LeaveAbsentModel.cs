using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeaveON.Models.Procedures
{
  public class LeaveAbsentModel
  {
    public string EmployeeName { get; set; }
    public int BioStarEmpNum { get; set; }
    public string DepartmentName { get; set; }
    public DateTime DateCreated { get; set; }
    public string DayName { get; set; }
    public string LeaveTypeName { get; set; }
    public DateTime LeaveStartDate { get; set; }
    public DateTime LeaveEndDate { get; set; }
    public string Status { get; set; }
    public decimal TotalDays { get; set; }
    public string Reason { get; set; }
    public string LineManager1 { get; set; }
    public string LineManager2 { get; set; }
  }
}

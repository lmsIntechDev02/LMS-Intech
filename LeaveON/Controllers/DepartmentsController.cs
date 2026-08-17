using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Repository.Models;
using Microsoft.AspNet.Identity;
using LeaveON.Services;

namespace LeaveON.Controllers
{
  
  [Authorize(Roles = "Admin")]
  public class DepartmentsController : Controller
  {
    private LeaveONEntities db = new LeaveONEntities();

    // GET: Departments
    public async Task<ActionResult> Index()
    {
      //var departments = db.Departments.Include(d => d.Country);
      return View(await db.DepartmentNames.ToListAsync());
    }

    public async Task<ActionResult> HODReport()
    {
      try
      {
     
        DateTime startDate, endDate;
        string userId = User.Identity.GetUserId();
        List<Leave> LstLeaves = new List<Leave>();

        //ViewBag.Employees = new SelectList(db.AspNetUsers, "BioStarEmpNum", "UserName").OrderBy(i => i.Text);
        //StartDate and EndDate for display
        DateTime startOfMonth = DateTime.Now;
        //DateTime startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.AddMonths(-1).Month, 1);
         DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        ViewBag.StartDate = startOfMonth.ToString("dd-MMM-yyyy");
         ViewBag.EndDate = endOfMonth.ToString("dd-MMM-yyyy");

         
        var departmentList = db.DepartmentNames.ToList();
        // Get all HOD IDs from departments
        var hodIds = departmentList
            .Where(d => d.HODID.HasValue)
            .Select(d => d.HODID.Value)
            .Distinct()
            .ToList();

        ViewBag.Departments = departmentList.Select(d => new SelectListItem { Value = d.Name, Text = d.Name }).ToList();
        ViewBag.hodList = db.AspNetUsers
               .Where(u => !string.IsNullOrEmpty(u.DepartmentName) && u.IsActive == true && u.BioStarEmpNum.HasValue && u.BioStarEmpNum.Value > 0 && u.IsDeleted != true
               && hodIds.Contains(u.BioStarEmpNum.Value)
               )
                .Distinct()
               .Select(d => new SelectListItem { Value = d.BioStarEmpNum.ToString(), Text = d.EmpolyeeName })
               .ToList();

       // ViewBag.hodList = departmentList.Where(x => x.HODID.HasValue).Select(d => new SelectListItem { Value = d.HODID.ToString(), Text = d.Name });
        // Role-based data population
         
      }
      catch (Exception ex)
      {
        throw (ex);
      }
      return View();
     
    }
    public async Task<ActionResult> HODReports(string startDate, List<string> hodIDs, List<string> departments)
    {
      AutomatedReportService serviceobject = new AutomatedReportService();
      var date = Convert.ToDateTime(startDate);
      serviceobject.GetHODDeparmentReportByDateRange(date.Month, date.Year, hodIDs, departments);
      return Json("Success");
    }



    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        db.Dispose();
      }
      base.Dispose(disposing);
    }
  }
}

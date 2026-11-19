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
using LeaveON.Models;

namespace LeaveON.Controllers
{
  
  [Authorize(Roles = "Admin")]
  public class LeaveTypesController : Controller
  {
    private LeaveONEntities db = new LeaveONEntities();

    // GET: LeaveTypes
    public async Task<ActionResult> Index()
    {
      var leaveTypes = db.LeaveTypes;//.Include(l => l.Country);
      return View(await leaveTypes.ToListAsync());
    }

    // GET: LeaveTypes/Details/5
    public async Task<ActionResult> Details(int? id)
    {
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      LeaveType leaveType = await db.LeaveTypes.FindAsync(id);
      if (leaveType == null)
      {
        return HttpNotFound();
      }
      return View(leaveType);
    }

    // GET: LeaveTypes/Create
    public ActionResult Create(int? id)
    {
      LeaveTypeModel model = new LeaveTypeModel();
      if (id.HasValue)
      {
        // Get previously selected countries
        LeaveType leaveType =   db.LeaveTypes.FirstOrDefault(k => k.Id == id.Value);
        if (leaveType != null)
        {
          model.CountryIDs = leaveType.tblLeaveTypeCountries.Where(k => k.LeaveTypeID == id).Select(k => k.CountryID.HasValue ? k.CountryID.Value : 0).ToList();
          model.Name = leaveType.Name;
          model.Id = leaveType.Id;
          model.HrBPID = leaveType.HrBPID;
          model.IsCalenderDays = Convert.ToBoolean(leaveType.IsCalenderDays);
        }
      }
      List<CountryName> countryList = new List<CountryName>();
      List<tblHRBP> hrbpList = new List<tblHRBP>();
      countryList = db.CountryNames.OrderBy(k=>k.Name).ToList();
      hrbpList = db.tblHRBPs.OrderBy(k=>k.HRBPName).ToList();
      ViewBag.CountryList =   new SelectList(countryList, "Id", "Name");
      ViewBag.HrBPList = new SelectList(hrbpList, "ID", "HRBPName", null);
      return View(model);
    }

    // POST: LeaveTypes/Create
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(LeaveTypeModel model)
    {
       
        string LoggedInUserId = User.Identity.GetUserId();

        
          try
          {
            LeaveType leaveType;

            if (model.Id > 0)
            {
              // EDIT
              leaveType = await db.LeaveTypes
                  .FirstOrDefaultAsync(k => k.Id == model.Id);

              if (leaveType == null)
              {
                return HttpNotFound();
              }

              leaveType.Name = model.Name;
              leaveType.HrBPID = model.HrBPID;
              leaveType.IsCalenderDays = model.IsCalenderDays;

              // Remove old country relationships
              var oldCountries = await db.tblLeaveTypeCountries
                  .Where(k => k.LeaveTypeID == model.Id)
                  .ToListAsync();

              db.tblLeaveTypeCountries.RemoveRange(oldCountries);
            }
            else
            {
              // CREATE
              leaveType = new LeaveType
              {
                Name = model.Name,
                HrBPID = model.HrBPID ,IsCalenderDays = model.IsCalenderDays
        };

              db.LeaveTypes.Add(leaveType);

              // Save first to generate LeaveType ID
              await db.SaveChangesAsync();
            }

            // Insert selected countries
            if (model.CountryIDs != null && model.CountryIDs.Any())
            {
              var countryList = model.CountryIDs
                  .Distinct()
                  .Select(countryId => new tblLeaveTypeCountry
                  {
                    LeaveTypeID = leaveType.Id,
                    CountryID = countryId
                  })
                  .ToList();

              db.tblLeaveTypeCountries.AddRange(countryList);
            }

            await db.SaveChangesAsync();

            return RedirectToAction("Index");
          }
          catch (Exception ex)
          {
            ModelState.AddModelError("", ex.Message);
          }
        

        

      
      

      return RedirectToAction("Create");
    }

    // GET: LeaveTypes/Edit/5
    public async Task<ActionResult> Edit(int? id)
    {
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      LeaveType leaveType = await db.LeaveTypes.FindAsync(id);
      if (leaveType == null)
      {
        return HttpNotFound();
      }
      
      return View(leaveType);
    }

    // POST: LeaveTypes/Edit/5
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
    // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit([Bind(Include = "Id,Name,CountryId")] LeaveType leaveType)
    {
      if (ModelState.IsValid)
      {
        db.Entry(leaveType).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return RedirectToAction("Index");
      }
      
      return View(leaveType);
    }

    // GET: LeaveTypes/Delete/5
    public async Task<ActionResult> Delete(int? id)
    {
      if (id == null)
      {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
      }
      LeaveType leaveType = await db.LeaveTypes.FindAsync(id);
      if (leaveType == null)
      {
        return HttpNotFound();
      }
      return View(leaveType);
    }

    // POST: LeaveTypes/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteConfirmed(int id)
    {
      LeaveType leaveType = await db.LeaveTypes.FindAsync(id);
      db.LeaveTypes.Remove(leaveType);
      await db.SaveChangesAsync();
      return RedirectToAction("Index");
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

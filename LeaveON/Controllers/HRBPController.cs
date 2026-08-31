using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LeaveON.Models;
using Repository.Models;
namespace LeaveON.Controllers
{
     

 

public class HRBPController : Controller
  {


    private Repository.Models.LeaveONEntities db = new Repository.Models.LeaveONEntities();


    // GET: HRBP
    public ActionResult Index()
    {
      var data = db.tblHRBPs
                   .OrderBy(x => x.HRBPName)
                   .ToList().Select(k=> new HRBPModel
                   {
                     Email=k.HRBPEmail,
                     Name=k.HRBPName,
                     UserID=k.UserID,
                     IsActive=k.IsActive,
                     HRBPID=k.ID,
                   });

      return View(data);
    }

    // GET: HRBP/Details/5HBRL
    public ActionResult Details(int? id)
    {
      if (id == null)
        return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

      HRBPModel hrbp = db.tblHRBPs
     .Where(k => k.ID == id)
     .Select(k => new HRBPModel
     {
       Email = k.HRBPEmail,
       Name = k.HRBPName,
       UserID = k.UserID,
       HRBPID = k.ID,
       IsActive = Convert.ToBoolean(k.IsActive),
     })
     .FirstOrDefault();

      if (hrbp == null)
        return HttpNotFound();

      return View(hrbp);
    }

    // GET: HRBP/Create
    public ActionResult AddEdit(int? id)
    {
      HRBPModel hrbp = new HRBPModel();
      var userlist = db.AspNetUsers
     .Where(u => u.Email != null && u.IsActive == true).AsEnumerable();

      // Fetch available HRBP emails from AspNetUsers
      var hrbpEmails = userlist
          .Select(u => new SelectListItem
          {
            Value = u.Id,
            Text = !String.IsNullOrEmpty(u.EmpolyeeName) ? u.EmpolyeeName : System.Globalization.CultureInfo.CurrentCulture.TextInfo
                                .ToTitleCase(u.Email.Split('@')[0].Replace(".", " "))
          })
          .ToList();

      if (id.HasValue && id.Value > 0)
      {


            hrbp = db.tblHRBPs
       .Where(k => k.ID == id)
       .Select(k => new HRBPModel
       {
         Email = k.HRBPEmail,
         Name = k.HRBPName,
         UserID = k.UserID,
         HRBPID = k.ID,
       })
       .FirstOrDefault();
      }
      ViewBag.HRBPEmailList = new SelectList(hrbpEmails, "Value", "Text", hrbp.UserID);
      return View(hrbp);
    }

    // POST: HRBP/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult AddEdit(HRBPModel hrbp)
    {
      if (ModelState.IsValid)
      {
        tblHRBP tbmodel = new tblHRBP();

       
       
       
        if (hrbp.HRBPID > 0)
        {
         var user= db.AspNetUsers.FirstOrDefault(k => k.Id == hrbp.UserID);
          var odlitem  =db.tblHRBPs.FirstOrDefault(k => k.ID == hrbp.HRBPID);
          if(odlitem != null)
          {
            odlitem.HRBPEmail = user.Email;
            odlitem.HRBPName = !String.IsNullOrEmpty(user.EmpolyeeName) ? user.EmpolyeeName : (System.Globalization.CultureInfo.CurrentCulture.TextInfo
                                .ToTitleCase(user.Email.Split('@')[0].Replace(".", " ")));
            odlitem.UserID = hrbp.UserID;
            odlitem.IsActive = hrbp.IsActive;
            odlitem.ID = hrbp.HRBPID;
          }
        
        }
        else
        {
          var user = db.AspNetUsers.FirstOrDefault(k => k.Id == hrbp.UserID);
         
          tbmodel.HRBPEmail = user.Email;
          tbmodel.HRBPName = !String.IsNullOrEmpty(user.EmpolyeeName) ? user.EmpolyeeName : (System.Globalization.CultureInfo.CurrentCulture.TextInfo
                                         .ToTitleCase(user.Email.Split('@')[0].Replace(".", " ")));
                   tbmodel.UserID = hrbp.UserID;
          tbmodel.IsActive = hrbp.IsActive;
          db.tblHRBPs.Add(tbmodel);
        }

       
        db.SaveChanges();

        return RedirectToAction("Index");
      }
      else
      {
        var userlist = db.AspNetUsers
.Where(u => u.Email != null && u.IsActive == true).AsEnumerable();
        var hrbpEmails = userlist
        .Select(u => new SelectListItem
        {
          Value = u.Id,
          Text = !String.IsNullOrEmpty(u.EmpolyeeName) ? u.EmpolyeeName : System.Globalization.CultureInfo.CurrentCulture.TextInfo
                              .ToTitleCase(u.Email.Split('@')[0].Replace(".", " "))
        })
        .ToList();
        ViewBag.HRBPEmailList = new SelectList(hrbpEmails, "Value", "Text", hrbp.UserID);
      }

      return View(hrbp);
    }

     
 

    // GET: HRBP/Delete/5
    public ActionResult Delete(int? id)
    {
      if (id == null)
      return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
      tblHRBP  hrbp = db.tblHRBPs.Find(id);
      if (hrbp == null)
        return HttpNotFound();

      db.tblHRBPs.Remove(hrbp);
      db.SaveChanges();

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

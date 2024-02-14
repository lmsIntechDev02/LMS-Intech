using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeaveON.Controllers
{
  public class SharedController : Controller
  {
    public ActionResult Error()
    {
      return View("Error"); // Make sure you have an Error.cshtml view in the correct folder
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LeaveON.Models;
namespace LeaveON.Controllers
{
  public class ErrorController : Controller
  {
    // GET: Error
    // General error action
    public ActionResult General()
    {
      var viewModel = new ErrorViewModel
      {
        Message = "An error occurred.",
        CustomMessage = TempData["ErrorMessage"] as string // Retrieve the custom message
      };

      return View(viewModel);
    }

  }
}

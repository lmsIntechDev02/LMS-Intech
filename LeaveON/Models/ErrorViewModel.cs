using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeaveON.Models
{
  public class ErrorViewModel
  {
    public bool IsHandled { get; set; } = false;
    public int? StatusCode { get; set; }
    public string Message { get; set; }
    public string CustomMessage { get; set; } 
  }
}

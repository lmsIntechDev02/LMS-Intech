using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace LeaveON.Models
{
  public class HRBPModel
  {

    [Key]
    public int HRBPID { get; set; }

  
    //[StringLength(200)]
    public string Name { get; set; }

    //[Required]
    //[StringLength(200)]
    //[EmailAddress]
    public string Email { get; set; }
    [Required]

    public bool? IsActive { get; set; }
    [Required]
    public string UserID { get; set; }

  }
}

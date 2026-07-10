using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Models
{
    public interface IAspNetUser_MetadataType
    {
        [DisplayName("Date Created")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        Nullable<System.DateTime> DateCreated { get; set; }
        [DisplayName("Date Modified")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        Nullable<System.DateTime> DateModified { get; set; }



    }

    [MetadataType(typeof(IAspNetUser_MetadataType))]
    public partial class AspNetUser : IAspNetUser_MetadataType
    {
        /* Id property has already existed in the mapped class */
        public DateTime? DateCreated { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime? DateModified { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }


}

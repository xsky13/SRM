using System;
using System.Collections.Generic;
using System.Text;


namespace SRM.Api.Models
{
    public interface ISoftDeleteable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedOnUTC { get; set; }

    }
}

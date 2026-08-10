using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Models
{
    public class AppUser : ISoftDeleteable 
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string  Surname { get; set; }

        public string Email  { get; set; }

        public string? PwdHash { get; set; }

        public string  Telefono { get; set; }

        public UserType  Usertype { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }
    }
}

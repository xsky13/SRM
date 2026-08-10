using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Models
{
    internal class Reservation
    {
        public  int Id { get; set; }

        public DateTime CheckinDate { get; set; }

        public DateTime CheckoutDate  { get; set; }

        public  string  State { get; set; }

        public int UserId { get; set; }
        public  int  ApartamentId { get; set; }
        public  DateTime CreatedAt { get; set; }
        public  DateTim UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }
    }
}

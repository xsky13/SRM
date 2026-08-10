using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Models
{
    internal class Payment
    {
        public int  Id { get; set; }
        public  float Amount { get; set; }
        public  int ReservationId { get; set; }
        public bool Manual { get; set; }
        public  bool sign { get; set; }
        public Datetime PaymentDate { get; set; }

    }
}

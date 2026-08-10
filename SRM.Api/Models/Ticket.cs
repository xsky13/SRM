using System;
using System.Collections.Generic;
using System.Text;

namespace SRM.Api.Models
{
    internal class Ticket
    {
        public int Id { get; set; }
        public  int  ReservationId { get; set; }
        public string Description { get; set; }
        public DateTime DateSent { get; set; }

    }
}

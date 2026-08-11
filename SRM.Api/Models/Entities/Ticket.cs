namespace SRM.Api.Models.Entities
{
    public class Ticket
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
        public DateTime DateSent { get; set; }
        public int PaymentId { get; set; }
        public Payment Payment { get; set; }
    }
}

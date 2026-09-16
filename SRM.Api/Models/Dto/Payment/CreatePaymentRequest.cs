namespace SRM.Api.Models.Dto.Payment
{

    public class Identification
    {
        public string Type { get; set; }
        public string Number { get; set; }
    }
    public class Payer
    {
        public string Email { get; set; }
        public Identification Identification { get; set; }
    }


    public record CreatePaymentRequest
    {
        public decimal TransactionAmount { get; set; }
        public string Token { get; set; }
        public int Installments { get; set; }
        public string Payment_Method_Id { get; set; }
        public Payer Payer { get; set; }

        public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
    }
}

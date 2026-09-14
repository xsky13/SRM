namespace SRM.Api.Models.Dto.Payment
{

    public class Identification
    {
        public string Type { get; set; }
        public string Number { get; set; }
    }
    public class Payer
    {
        public string CardholderEmail { get; set; }
        public Identification Identification { get; set; }
    }

    public class FormData
    {
        public decimal TransactionAmount { get; set; }
        public string Token { get; set; }
        public int Installments { get; set; }
        public string PaymentMethodId { get; set; }
        public Payer Payer { get; set; }
    }

    public record CreatePaymentRequest(
        FormData FormData,

        DateTime CheckInDate,
        DateTime CheckOutDate
    );
}

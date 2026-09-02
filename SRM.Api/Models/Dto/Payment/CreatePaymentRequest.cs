namespace SRM.Api.Models.Dto.Payment
{
    public record CreatePaymentRequest(
        decimal TransactionAmount,
        string Token,
        string Description,
        int Installments,
        string PaymentMethodId,
        string CardholderEmail,
        string IdentificationType,
        string IdentificationNumber,
        string CardholderName,


        DateTime CheckInDate,
        DateTime CheckOutDate
    );
}

namespace SRM.Api.Models.Enums
{
    public enum ReservationState
    {
        NotConfirmed = 1,
        ConfirmedPaymentIncomplete = 2,
        ConfirmedPaymentComplete = 3,
        PaymentPending = 4,
        Cancelled = 5
    }
}

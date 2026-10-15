using SRM.Api.Models.Enums;

namespace SRM.Api.Utils
{
    public static class PaymentStatusMapper
    {
        public static PaymentStatus MapMercadoPagoStatus(string? mercadoPagoStatus)
        {
            return mercadoPagoStatus switch
            {
                "approved" => PaymentStatus.Approved,
                "pending" => PaymentStatus.Pending,
                "in_process" => PaymentStatus.Pending,
                "authorized" => PaymentStatus.Pending,
                "rejected" => PaymentStatus.Rejected,
                "cancelled" => PaymentStatus.Cancelled,
                "refunded" => PaymentStatus.Refunded,
                "charged_back" => PaymentStatus.ChargedBack,
                "in_mediation" => PaymentStatus.InMediation,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(mercadoPagoStatus),
                    mercadoPagoStatus,
                    $"Estado de Mercado Pago no reconocido: '{mercadoPagoStatus}'")
            };
        }
    }
}

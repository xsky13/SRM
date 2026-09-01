using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using SRM.Api.Services.Interfaces;

namespace SRM.Api.Services
{
    public class PaymentService : IPaymentService
    {
        public async Task<string> CreatePreference(string title, int unitPrice)
        {
            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = title,
                        Quantity = 1,
                        UnitPrice = unitPrice,
                        CurrencyId = "ARS"
                    }
                }
            };

            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            return preference.Id;
        }
    }
}

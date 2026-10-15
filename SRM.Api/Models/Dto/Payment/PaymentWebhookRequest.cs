namespace SRM.Api.Models.Dto.Payment
{
    public class Data
    {
        public string Id { get; set; }
    }
    public class PaymentWebhookRequest
    {
        public int Id { get; set; }
        public bool Live_mode { get; set; }
        public string Type { get; set; }
        public DateTime Date_created { get; set; }
        public int User_id { get; set; }
        public string Api_version{ get; set; }
        public string Action { get; set; }
        public Data Data { get; set; }
    }
}

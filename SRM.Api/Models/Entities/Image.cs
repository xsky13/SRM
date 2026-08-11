namespace SRM.Api.Models.Entities
{
    public class Image
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public Guid ApartamentId { get; set; }
        public Apartment Apartment { get; set; } = null!;
    }
}

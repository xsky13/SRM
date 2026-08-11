namespace SRM.Api.Models.Entities
{
    internal class Image
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public int ApartamentId { get; set; }
        public Apartment Apartment { get; set; } = null!;
    }
}

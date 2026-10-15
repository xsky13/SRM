namespace SRM.Api.Models.Entities
{
    public class Image : ISoftDeletable
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;

        public Guid ApartmentId { get; set; }
        public Apartment Apartment { get; set; } = null!;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }
    }
}

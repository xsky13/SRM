namespace SRM.Api.Models.Entities
{
    public class Apartment : ISoftDeletable
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CoverImgUrl { get; set; } = "/images/shrek_harvey.webp";
        public string Description { get; set; } = string.Empty;
        public float Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }
        public List<Image> Images { get; set; } = [];
        public List<Reservation> Reservations { get; set; } = [];
    }
}

using SRM.Api.Models.Dto.Apartment;

namespace SRM.Api.Models.Entities
{
    public class Apartment : ISoftDeletable
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CoverImgUrl { get; set; } = (Environment.GetEnvironmentVariable("SITE_URL") ?? throw new InvalidOperationException("SITE_URL not set")) + "/images/shrek_harvey.webp";
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }
        public List<Image> Images { get; set; } = [];
        public List<Reservation> Reservations { get; set; } = [];

        public ApartmentListingDto ToListingDto()
        {
            return new ApartmentListingDto(
                Id,
                Name,
                CoverImgUrl,
                Location,
                Price
            );
        }

        public ApartmentDetailsDto ToDetailsDto()
        {
            return new ApartmentDetailsDto(
                Id,
                Name,
                Description,
                Price,
                Location,
                CoverImgUrl,
                Latitude,
                Longitude,
                Images.Select(image => image.ToDto()).ToList()
            );
        }
    }
}

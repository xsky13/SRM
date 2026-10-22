namespace SRM.Api.Models.Dto.Apartment
{
    public class UpdateApartmentDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Location { get; set; }
        public string? CoverImgUrl { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
    }
}

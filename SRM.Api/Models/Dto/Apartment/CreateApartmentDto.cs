using System.ComponentModel.DataAnnotations;

namespace SRM.Api.Models.Dto.Apartment
{
    public class CreateApartmentDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }
        [Required(AllowEmptyStrings = false)]
        public string CoverImgUrl { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

    }
}

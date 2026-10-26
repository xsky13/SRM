using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SRM.Api.Models.Dto.Apartment
{
    public class AddImagesDto
    {
        // Agregamos "= new List<IFormFile>();" para evitar el warning
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }
}
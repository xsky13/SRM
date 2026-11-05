using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SRM.Api.Models.Dto.Apartment
{
    public class AddImagesDto
    {
        // Debe ser IFormFile para recibir archivos físicos
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }
}
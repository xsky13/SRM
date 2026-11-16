using System.ComponentModel.DataAnnotations;

namespace SRM.Api.Models.Dto.User
{
    public class CreateUserRequestDto
    {
        [MinLength(5, ErrorMessage = "Nombre muy corto gay")]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Pwd { get; set; }
    }
}

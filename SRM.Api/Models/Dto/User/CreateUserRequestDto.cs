namespace SRM.Api.Models.Dto.User
{
    public class CreateUserRequestDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Pwd { get; set; }
    }
}

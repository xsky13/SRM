namespace SRM.Api.Models.Entities
{
    public class Apartment : ISoftDeleteable
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public float Price { get; set; }
        public string Location { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedOnUTC { get; set; }
    }
}

namespace SRM.Api.Models
{
    public interface ISoftDeleteable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedOnUTC { get; set; }
    }
}

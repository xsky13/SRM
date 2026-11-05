namespace SRM.Api.Models
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedOnUTC { get; set; }
    }
}

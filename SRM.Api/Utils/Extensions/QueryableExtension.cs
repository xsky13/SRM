using Microsoft.EntityFrameworkCore;
using SRM.Api.Models.Pagination;

namespace SRM.Api.Utils.Extensions
{ 
    public static class QueryableExtensions
    {
        public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize)
        {
            // 1. Get total count before skipping records
            var totalRecords = await query.CountAsync();

            // 2. Fetch the paginated chunk from the database
            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResponse<T>(data, pageNumber, pageSize, totalRecords);
        }
    }

}

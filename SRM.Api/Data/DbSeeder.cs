using Microsoft.EntityFrameworkCore;
using SRM.Api.Models.Entities;

namespace SRM.Api.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Apartments.AnyAsync())
                return; // ya hay datos, no hacemos nada

            var apartments = new List<Apartment>
            {
                new() { 
                    Name = "Depto Centro",
                    Description = "Lorem ipsum dolor sit amet.",
                    Price = 40000F,
                    Location = "Libertador San Martin",
                    Latitude = 0,
                    Longitude = 0,
                    IsDeleted = false,
                },
                new() {
                    Name = "Depto Superior",
                    Description = "Lorem ipsum dolor sit amet.",
                    Price = 40000F,
                    Latitude = 0,
                    Longitude = 0,
                    Location = "Libertador San Martin",
                    IsDeleted = false,
                },
            };

            context.Apartments.AddRange(apartments);
            await context.SaveChangesAsync();
        }
    }
}

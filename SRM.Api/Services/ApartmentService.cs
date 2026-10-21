using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Entities;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using SRM.Api.Models.Enums;

namespace SRM.Api.Services
{
    public class ApartmentService(AppDbContext _db) : IApartmentService
    {

        public async Task<Result<List<ApartmentListingDto>>> GetAll(int limit = 10)
        {
            var apartments = await _db.Apartments
                .AsNoTracking()
                .Select(apartment => new ApartmentListingDto(
                    apartment.Id,
                    apartment.Name,
                    apartment.CoverImgUrl,
                    apartment.Location,
                    apartment.Price
                ))
                .Take(limit)
                .ToListAsync();
            return Result<List<ApartmentListingDto>>.Ok(apartments);
        }

        public async Task<Result<ApartmentDetailsDto>> GetById(Guid id)
        {
            var apartment = await _db.Apartments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new ApartmentDetailsDto(
                    a.Id,
                    a.Name,
                    a.Description,
                    a.Price,
                    a.Location,
                    a.CoverImgUrl,
                    a.Latitude,
                    a.Longitude,
                    a.Images.Select(i => new Models.Dto.Images.ImageDto(
                        i.Id,
                        i.Url,
                        i.ApartmentId
                    )).ToList()
                ))
                .FirstOrDefaultAsync();

            if (apartment == null)
                return Result<ApartmentDetailsDto>.Fail("No se encontro el departamento", 404);

            return Result<ApartmentDetailsDto>.Ok(apartment);
        }

        public async Task<Result<Apartment>> CreateApartmentAsync(CreateApartmentDto dto)
        {
            /*
            // Obtener el usuario de la base de datos no es necesario por que la autenticación y autorización se manejan en el controlador
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Result<Apartment>.Fail("Usuario no encontrado.", 401);

            // Verificar que el usuario sea administrador
            if (user.Usertype != UserType.Admin)
                return Result<Apartment>.Fail("No tienes permiso para crear departamentos. Solo administradores pueden hacerlo.", 403);
            */

            // Validar campos del DTO
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length < 3 || dto.Name.Length > 100)
                return Result<Apartment>.Fail("El nombre del departamento debe tener entre 3 y 100 caracteres.", 400);

            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length < 10 || dto.Description.Length > 1000)
                return Result<Apartment>.Fail("La descripción debe tener entre 10 y 1000 caracteres.", 400);

            if (dto.Price <= 0)
                return Result<Apartment>.Fail("El precio debe ser mayor a 0.", 400);

            if (string.IsNullOrWhiteSpace(dto.Location) || dto.Location.Length < 5 || dto.Location.Length > 100)
                return Result<Apartment>.Fail("La ubicación debe tener entre 5 y 100 caracteres.", 400);

            if (string.IsNullOrWhiteSpace(dto.CoverImgUrl))
                return Result<Apartment>.Fail("La imagen de portada es requerida.", 400);

            if (dto.CoverImgUrl == null)
                return Result<Apartment>.Fail("Debe proporcionar una imagen.", 400);

            // Crear la entidad Apartment
            var apartment = new Apartment
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Location = dto.Location,
                CoverImgUrl = dto.CoverImgUrl,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
            };

            _db.Apartments.Add(apartment);
            await _db.SaveChangesAsync();

            return Result<Apartment>.Ok(apartment);
        }

        public async Task<Result<Apartment>> DeleteApartmentAsync(Guid id)
        {
            // Buscar el departamento
            var apartment = await _db.Apartments.FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return Result<Apartment>.Fail("No se encontró el departamento", 404);

            if (apartment.IsDeleted)
                return Result<Apartment>.Fail("El departamento ya ha sido eliminado", 410);

            // Realizar soft delete
            apartment.IsDeleted = true;
            apartment.DeletedOnUTC = DateTime.UtcNow;

            _db.Apartments.Update(apartment);
            await _db.SaveChangesAsync();

            return Result<Apartment>.Ok(apartment);
        }
    }
}
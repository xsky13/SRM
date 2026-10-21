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

        public async Task<Result<Apartment>> UpdateApartmentAsync(Guid id, UpdateApartmentDto dto)
        {
            // Buscar el departamento
            var apartment = await _db.Apartments.FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return Result<Apartment>.Fail("No se encontró el departamento", 404);

            // Validar que no esté eliminado
            if (apartment.IsDeleted)
                return Result<Apartment>.Fail("No se encontró el departamento", 404);

            // Actualizar campos si no son nulos
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                if (dto.Name.Length < 3 || dto.Name.Length > 100)
                    return Result<Apartment>.Fail("El nombre del departamento debe tener entre 3 y 100 caracteres.", 400);
                apartment.Name = dto.Name;
            }

            if (!string.IsNullOrWhiteSpace(dto.Description))
            {
                if (dto.Description.Length < 10 || dto.Description.Length > 1000)
                    return Result<Apartment>.Fail("La descripción debe tener entre 10 y 1000 caracteres.", 400);
                apartment.Description = dto.Description;
            }

            if (dto.Price.HasValue)
            {
                if (dto.Price <= 0)
                    return Result<Apartment>.Fail("El precio debe ser mayor a 0.", 400);
                apartment.Price = dto.Price.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Location))
            {
                if (dto.Location.Length < 5 || dto.Location.Length > 100)
                    return Result<Apartment>.Fail("La ubicación debe tener entre 5 y 100 caracteres.", 400);
                apartment.Location = dto.Location;
            }

            if (!string.IsNullOrWhiteSpace(dto.CoverImgUrl))
            {
                apartment.CoverImgUrl = dto.CoverImgUrl;
            }

            _db.Apartments.Update(apartment);
            await _db.SaveChangesAsync();

            return Result<Apartment>.Ok(apartment);
        }

        public async Task<Result<List<Models.Dto.Images.ImageDto>>> AddImagesAsync(Guid id, AddImagesDto dto)
        {
            // Buscar el departamento
            var apartment = await _db.Apartments.FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return Result<List<Models.Dto.Images.ImageDto>>.Fail("No se encontró el departamento", 404);

            // Validar que no esté eliminado
            if (apartment.IsDeleted)
                return Result<List<Models.Dto.Images.ImageDto>>.Fail("No se encontró el departamento", 404);

            // Validar que al menos hay una URL
            if (dto.ImageUrls == null || dto.ImageUrls.Count == 0)
                return Result<List<Models.Dto.Images.ImageDto>>.Fail("Debe proporcionar al menos una URL de imagen.", 400);

            // Crear nuevas imágenes
            var newImages = new List<Image>();
            foreach (var url in dto.ImageUrls)
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var image = new Image
                    {
                        Id = Guid.NewGuid(),
                        Url = url,
                        ApartmentId = id
                    };
                    newImages.Add(image);
                    _db.Images.Add(image);
                }
            }

            if (newImages.Count == 0)
                return Result<List<Models.Dto.Images.ImageDto>>.Fail("Debe proporcionar al menos una URL de imagen válida.", 400);

            await _db.SaveChangesAsync();

            // Retornar las imágenes creadas como DTOs
            var imageDtos = newImages.Select(i => new Models.Dto.Images.ImageDto(i.Id, i.Url, i.ApartmentId)).ToList();
            return Result<List<Models.Dto.Images.ImageDto>>.Ok(imageDtos);
        }

        public async Task<Result<bool>> RemoveImagesAsync(Guid id, RemoveImagesDto dto)
        {
            // Buscar el departamento
            var apartment = await _db.Apartments.FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return Result<bool>.Fail("No se encontró el departamento", 404);

            // Validar que not esté eliminado
            if (apartment.IsDeleted)
                return Result<bool>.Fail("No se encontró el departamento", 404);

            // Validar que hay IDs para eliminar
            if (dto.ImageIds == null || dto.ImageIds.Count == 0)
                return Result<bool>.Fail("Debe proporcionar al menos un ID de imagen.", 400);

            // Obtener las imágenes a eliminar
            var imagesToDelete = await _db.Images
                .Where(i => dto.ImageIds.Contains(i.Id) && i.ApartmentId == id)
                .ToListAsync();

            // Validar que todas las imágenes solicitadas pertenecen al departamento
            if (imagesToDelete.Count != dto.ImageIds.Count)
            {
                var notFound = dto.ImageIds
                    .Where(id => !imagesToDelete.Any(i => i.Id == id))
                    .ToList();
                return Result<bool>.Fail($"Las siguientes imágenes no pertenecen al departamento o no existen: {string.Join(", ", notFound)}", 400);
            }

            // Realizar soft delete de las imágenes
            foreach (var image in imagesToDelete)
            {
                image.IsDeleted = true;
                image.DeletedOnUTC = DateTime.UtcNow;
                _db.Images.Update(image);
            }

            await _db.SaveChangesAsync();

            return Result<bool>.Ok(true);
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
using Microsoft.EntityFrameworkCore;
using SRM.Api.Data;
using SRM.Api.Models.Dto.Apartment;
using SRM.Api.Models.Entities;
using SRM.Api.Services.Interfaces;
using SRM.Api.Utils;
using SRM.Api.Models.Enums;
using Microsoft.Extensions.Hosting; // Nueva directiva usando agregada

namespace SRM.Api.Services
{
    public class ApartmentService : IApartmentService
    {
        private readonly AppDbContext _db;
        private readonly IHostEnvironment _env; // O IWebHostEnvironment

        public ApartmentService(AppDbContext db, IHostEnvironment env) // O IWebHostEnvironment
        {
            _db = db;
            _env = env;
        }

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
            var apartment = await _db.Apartments.FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return Result<List<Models.Dto.Images.ImageDto>>.Fail("No se encontró el departamento", 404);

            if (apartment.IsDeleted)
                return Result<List<Models.Dto.Images.ImageDto>>.Fail("No se encontró el departamento", 404);

            if (dto.Images == null || dto.Images.Count == 0)
                return Result<List<Models.Dto.Images.ImageDto>>.Fail("Debe proporcionar al menos una imagen.", 400);

            // 1. Definir la ruta física donde se guardarán las imágenes en el contenedor
            var imagesPath = Path.Combine(_env.ContentRootPath, "Storage", "Images");

            // Crear nuevas imágenes
            var newImages = new List<Image>();
            foreach (var imageFile in dto.Images)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    // 2. Generar un nombre único (GUID) y mantener la extensión (.jpg, .png, etc.)
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(imagesPath, fileName);

                    // 3. Guardar el archivo físicamente en el volumen del contenedor
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // 4. Crear la entidad con la URL virtual (string) para la Base de Datos
                    var image = new Image
                    {
                        Id = Guid.NewGuid(),
                        Url = $"/images/{fileName}", // Aquí generamos el string que guardará en la BD
                        ApartmentId = id
                    };

                    newImages.Add(image);
                    _db.Images.Add(image);
                }
            }

            if (newImages.Count == 0)
                return Result<List<Models.Dto.Images.ImageDto>>.Fail("Las imágenes proporcionadas no son válidas o están vacías.", 400);

            await _db.SaveChangesAsync();

            // Retornar las imágenes creadas como DTOs
            var imageDtos = newImages.Select(i => new Models.Dto.Images.ImageDto(i.Id, i.Url, i.ApartmentId)).ToList();
            return Result<List<Models.Dto.Images.ImageDto>>.Ok(imageDtos);
        }

        public async Task<Result<bool>> RemoveImagesAsync(Guid id, RemoveImagesDto dto)
        {
            var apartment = await _db.Apartments.FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null)
                return Result<bool>.Fail("No se encontró el departamento", 404);

            if (apartment.IsDeleted)
                return Result<bool>.Fail("No se encontró el departamento", 404);

            // Validar que hay IDs para eliminar
            if (dto.ImageIds == null || dto.ImageIds.Count == 0)
                return Result<bool>.Fail("Debe proporcionar al menos un ID de imagen.", 400);

            var imagesToDelete = await _db.Images
                .Where(i => dto.ImageIds.Contains(i.Id) && i.ApartmentId == id)
                .ToListAsync();

            // Validar que todas las imágenes solicitadas pertenecen al departamento
            if (imagesToDelete.Count != dto.ImageIds.Count)
            {
                var notFound = dto.ImageIds
                    .Where(imageId => !imagesToDelete.Any(i => i.Id == imageId))
                    .ToList();
                return Result<bool>.Fail($"Las siguientes imágenes no pertenecen al departamento o no existen: {string.Join(", ", notFound)}", 400);
            }

            // Obtener la ruta de la carpeta de imágenes en Docker
            var imagesPath = Path.Combine(_env.ContentRootPath, "Storage", "Images");

            // soft delete de las imágenes y borrado físico
            foreach (var image in imagesToDelete)
            {
                // 1. Borrar el archivo físico del disco
                var fileName = Path.GetFileName(image.Url); // Extrae "foto.jpg" de "/images/foto.jpg"
                var filePath = Path.Combine(imagesPath, fileName);

                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (IOException)
                    {
                        // Si el archivo está siendo usado por otro proceso, ignoramos el error
                        // para que no falle la eliminación en la base de datos.
                    }
                }

                // 2. Hacer el soft delete en la Base de Datos
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
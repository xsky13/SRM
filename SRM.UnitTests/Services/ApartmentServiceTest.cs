using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SRM.API.Controllers;
using SRM.API.Models;
using SRM.API.Services;
using Xunit;

namespace SRM.UnitTests.Services
{
    public class ApartmentServiceTest
    {
        private readonly Mock<IApartmentService> _apServiceMock;
        private readonly ApartmentController _controller;

        public ApartmentServiceTest()
        {
            _apServiceMock = new Mock<IApartmentService>();
            _controller = new ApartmentController(_apServiceMock.Object);
        }

        // create a list of test apartments
        private List<Apartment> CreateApartments(int quantity)
        {
            var apartments = new List<Apartment>();
            for (int i = 0; i < quantity; i++)
            {
                apartments.Add(new Apartment
                {
                    Id = Guid.NewGuid(),
                    Name = $"AP {i}",
                    Description = $"{i} Description",
                    Price = 100 + i * 10,
                    Location = $"Location {i}",
                    Latitude = 40.0 + i,
                    Longitude = -3.0 - i,
                    IsDeleted = false,
                    DeletedOnUTC = null,
                    Images = new List<Image>(),
                    Reservations = new List<Reservation>()
                });
            }
            return apartments;
        }

        // ==================== GET ALL TESTS ====================

        [Fact]
        public async Task GetAll_WhenApartmentsExist_ReturnsOkWithList()
        {
            // Arrange
            var expectedApartments = CreateApartments(10);
            _apServiceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync(expectedApartments);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var data = okResult.Value.Should().BeAssignableTo<IEnumerable<Apartment>>().Subject;

            data.Should().HaveCount(10);
            data.Should().BeEquivalentTo(expectedApartments);
        }

        [Fact]
        public async Task GetAll_WhenNoApartments_ReturnsOkWithEmptyList()
        {
            // Arrange
            var emptyList = new List<Apartment>();
            _apServiceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var data = okResult.Value.Should().BeAssignableTo<IEnumerable<Apartment>>().Subject;
            data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAll_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _apServiceMock
                .Setup(s => s.GetAllAsync())
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act
            var result = await _controller.GetAll();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        // ==================== GET BY ID TESTS ====================

        [Fact]
        public async Task GetById_WhenApartmentExists_ReturnsOkWithApartment()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            var expectedApartment = new Apartment
            {
                Id = apartmentId,
                Name = "Test Apartment",
                Description = "Test Description",
                Price = 200,
                Location = "Test Location",
                Latitude = 45.0,
                Longitude = -4.0,
                IsDeleted = false,
                DeletedOnUTC = null,
                Images = new List<Image>(),
                Reservations = new List<Reservation>()
            };

            _apServiceMock
                .Setup(s => s.GetByIdAsync(apartmentId))
                .ReturnsAsync(expectedApartment);

            // Act
            var result = await _controller.GetById(apartmentId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var data = okResult.Value.Should().BeOfType<Apartment>().Subject;

            data.Should().BeEquivalentTo(expectedApartment);
            data.Id.Should().Be(apartmentId);
        }

        [Fact]
        public async Task GetById_WhenApartmentDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _apServiceMock
                .Setup(s => s.GetByIdAsync(nonExistentId))
                .ReturnsAsync((Apartment)null);

            // Act
            var result = await _controller.GetById(nonExistentId);

            // Assert
            result.Should().BeOfType<NotFoundResult>(); // 404
        }

        [Fact]
        public async Task GetById_WhenIdIsEmpty_ReturnsBadRequest()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act
            var result = await _controller.GetById(emptyId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);

            // Verify that the service was never called
            _apServiceMock.Verify(s => s.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetById_WhenServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var apartmentId = Guid.NewGuid();
            _apServiceMock
                .Setup(s => s.GetByIdAsync(apartmentId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act
            var result = await _controller.GetById(apartmentId);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
}
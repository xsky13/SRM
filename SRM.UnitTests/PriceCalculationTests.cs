using System.Reflection;
using FluentAssertions;
using SRM.Api.Services;

namespace SRM.UnitTests;

public class PriceCalculationTests
{
    [Theory]
    [InlineData("2026-10-17", "2026-10-17", 1)]
    [InlineData("2026-10-17", "2026-10-18", 2)]
    [InlineData("2026-10-17", "2026-10-20", 4)]
    public void Nights_ShouldMatchBusinessRule(string checkInIso, string checkOutIso, int expectedNights)
    {
        var method = typeof(ReservationService).GetMethod("Nights", BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull("debe existir la regla de negocio real de noches en ReservationService");

        var result = method!.Invoke(null, [DateTime.Parse(checkInIso), DateTime.Parse(checkOutIso)]);
        result.Should().Be(expectedNights);
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRM.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Reservations""
                ADD CONSTRAINT ""no_overlapping_reservations""
                EXCLUDE USING gist (
                    ""ApartmentId"" WITH =,
                    (daterange(
                        (""CheckInDate"" AT TIME ZONE 'UTC')::date,
                        GREATEST((""CheckOutDate"" AT TIME ZONE 'UTC')::date,
                                 (""CheckInDate"" AT TIME ZONE 'UTC')::date + 1),
                        '[)')) WITH &&
                )
                WHERE (""IsDeleted"" = false AND ""State"" IN (/* valores de ConfirmedPaymentComplete, ConfirmedPaymentIncomplete, PaymentPending */));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Reservations"" DROP CONSTRAINT ""no_overlapping_reservations"";");
        }
    }
}

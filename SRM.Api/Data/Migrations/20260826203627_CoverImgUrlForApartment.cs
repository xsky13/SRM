using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRM.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CoverImgUrlForApartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImgUrl",
                table: "Apartments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImgUrl",
                table: "Apartments");
        }
    }
}

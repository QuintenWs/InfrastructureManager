using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDenormalizedLocationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Locations_LocationId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Networks_Locations_LocationId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Networks_LocationId",
                table: "Networks");

            migrationBuilder.DropIndex(
                name: "IX_Devices_LocationId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Networks");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Devices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Networks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Devices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Networks_LocationId",
                table: "Networks",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_LocationId",
                table: "Devices",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Locations_LocationId",
                table: "Devices",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Networks_Locations_LocationId",
                table: "Networks",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");
        }
    }
}

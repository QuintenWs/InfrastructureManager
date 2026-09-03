using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDienstenDocumentenEnDashboardUitbreiding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecentActivityCount",
                table: "UserDashboardSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecentDevicesCount",
                table: "UserDashboardSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOverdueVisits",
                table: "UserDashboardSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DepartmentDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentDocuments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceDocuments_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLocationAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLocationAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLocationAccess_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentDocuments_DepartmentId",
                table: "DepartmentDocuments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceDocuments_DeviceId",
                table: "DeviceDocuments",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLocationAccess_LocationId",
                table: "UserLocationAccess",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLocationAccess_UserId_LocationId",
                table: "UserLocationAccess",
                columns: new[] { "UserId", "LocationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentDocuments");

            migrationBuilder.DropTable(
                name: "DeviceDocuments");

            migrationBuilder.DropTable(
                name: "UserLocationAccess");

            migrationBuilder.DropColumn(
                name: "RecentActivityCount",
                table: "UserDashboardSettings");

            migrationBuilder.DropColumn(
                name: "RecentDevicesCount",
                table: "UserDashboardSettings");

            migrationBuilder.DropColumn(
                name: "ShowOverdueVisits",
                table: "UserDashboardSettings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessControlSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLocationAccess");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewHistory",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AccessGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CanViewHistory = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessGrants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccessGrants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccessGrants_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccessGrants_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccessGroupGrants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessGroupId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroupGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessGroupGrants_AccessGroups_AccessGroupId",
                        column: x => x.AccessGroupId,
                        principalTable: "AccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessGroupGrants_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessGroupGrants_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserAccessGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AccessGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccessGroups_AccessGroups_AccessGroupId",
                        column: x => x.AccessGroupId,
                        principalTable: "AccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccessGroups_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_DepartmentId",
                table: "AuditLogs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupGrants_AccessGroupId_DepartmentId",
                table: "AccessGroupGrants",
                columns: new[] { "AccessGroupId", "DepartmentId" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupGrants_AccessGroupId_LocationId",
                table: "AccessGroupGrants",
                columns: new[] { "AccessGroupId", "LocationId" },
                unique: true,
                filter: "[LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupGrants_DepartmentId",
                table: "AccessGroupGrants",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupGrants_LocationId",
                table: "AccessGroupGrants",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroups_Name",
                table: "AccessGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGrants_DepartmentId",
                table: "UserAccessGrants",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGrants_LocationId",
                table: "UserAccessGrants",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGrants_UserId_DepartmentId",
                table: "UserAccessGrants",
                columns: new[] { "UserId", "DepartmentId" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGrants_UserId_LocationId",
                table: "UserAccessGrants",
                columns: new[] { "UserId", "LocationId" },
                unique: true,
                filter: "[LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGroups_AccessGroupId",
                table: "UserAccessGroups",
                column: "AccessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGroups_UserId_AccessGroupId",
                table: "UserAccessGroups",
                columns: new[] { "UserId", "AccessGroupId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessGroupGrants");

            migrationBuilder.DropTable(
                name: "UserAccessGrants");

            migrationBuilder.DropTable(
                name: "UserAccessGroups");

            migrationBuilder.DropTable(
                name: "AccessGroups");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_DepartmentId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CanViewHistory",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "UserLocationAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
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
                name: "IX_UserLocationAccess_LocationId",
                table: "UserLocationAccess",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLocationAccess_UserId_LocationId",
                table: "UserLocationAccess",
                columns: new[] { "UserId", "LocationId" },
                unique: true);
        }
    }
}

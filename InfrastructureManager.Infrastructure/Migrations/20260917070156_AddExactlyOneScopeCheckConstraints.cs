using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExactlyOneScopeCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_UserAccessGrant_ExactlyOneScope",
                table: "UserAccessGrants",
                sql: "([DepartmentId] IS NOT NULL AND [LocationId] IS NULL) OR ([DepartmentId] IS NULL AND [LocationId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessGroupGrant_ExactlyOneScope",
                table: "AccessGroupGrants",
                sql: "([DepartmentId] IS NOT NULL AND [LocationId] IS NULL) OR ([DepartmentId] IS NULL AND [LocationId] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserAccessGrant_ExactlyOneScope",
                table: "UserAccessGrants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessGroupGrant_ExactlyOneScope",
                table: "AccessGroupGrants");
        }
    }
}

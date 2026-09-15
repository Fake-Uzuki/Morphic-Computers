using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanNameToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanName",
                table: "Companies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Micro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanName",
                table: "Companies");
        }
    }
}

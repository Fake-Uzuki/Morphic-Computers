using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP.infrastructure.Migrations.TenantErpDb
{
    /// <inheritdoc />
    public partial class AddPayrollStatutoryDeductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OtherDeductions",
                table: "PayrollRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PagIbigDeduction",
                table: "PayrollRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PhilHealthDeduction",
                table: "PayrollRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SssDeduction",
                table: "PayrollRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingTax",
                table: "PayrollRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtherDeductions",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "PagIbigDeduction",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "PhilHealthDeduction",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "SssDeduction",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "WithholdingTax",
                table: "PayrollRecords");
        }
    }
}

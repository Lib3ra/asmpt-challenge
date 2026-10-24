using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Components",
                columns: new[] { "Id", "Description", "Name", "Quantity" },
                values: new object[,]
                {
                    { 1, "0603 thick-film chip resistor, 1 %, 0.1 W", "Resistor 10 kOhm", 24 },
                    { 2, "0402 MLCC, X7R, 16 V, decoupling", "Ceramic capacitor 100 nF", 32 },
                    { 3, "0805 SMD LED, 2.1 V forward voltage", "LED green", 4 },
                    { 4, "LQFP-48, ARM Cortex-M3, 72 MHz", "Microcontroller STM32F103", 1 },
                    { 5, "SOT-223 LDO, 3.3 V, 1 A", "Voltage regulator AMS1117-3.3", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Components",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Components",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Components",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Components",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Components",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}

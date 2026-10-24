using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrderManagement.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                });

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
            migrationBuilder.DropTable(
                name: "Components");
        }
    }
}

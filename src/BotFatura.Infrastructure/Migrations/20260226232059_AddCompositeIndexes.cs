using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotFatura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faturas_ClienteId",
                table: "Faturas");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_ClienteId_Status",
                table: "Faturas",
                columns: new[] { "ClienteId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_Status_DataVencimento",
                table: "Faturas",
                columns: new[] { "Status", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_WhatsApp",
                table: "Clientes",
                column: "WhatsApp",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Faturas_ClienteId_Status",
                table: "Faturas");

            migrationBuilder.DropIndex(
                name: "IX_Faturas_Status_DataVencimento",
                table: "Faturas");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_WhatsApp",
                table: "Clientes");

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_ClienteId",
                table: "Faturas",
                column: "ClienteId");
        }
    }
}

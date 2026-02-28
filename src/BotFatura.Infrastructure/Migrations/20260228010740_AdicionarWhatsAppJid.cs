using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotFatura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarWhatsAppJid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsAppJid",
                table: "Clientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsAppJid",
                table: "Clientes");
        }
    }
}

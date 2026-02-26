using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotFatura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoNotificacaoToMensagemTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoNotificacao",
                table: "MensagensTemplate",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateIndex(
                name: "IX_MensagensTemplate_TipoNotificacao",
                table: "MensagensTemplate",
                column: "TipoNotificacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MensagensTemplate_TipoNotificacao",
                table: "MensagensTemplate");

            migrationBuilder.DropColumn(
                name: "TipoNotificacao",
                table: "MensagensTemplate");
        }
    }
}

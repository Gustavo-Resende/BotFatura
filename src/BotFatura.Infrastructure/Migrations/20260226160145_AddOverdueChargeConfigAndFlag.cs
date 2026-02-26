using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotFatura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOverdueChargeConfigAndFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CobrancaAposVencimentoEnviada",
                table: "Faturas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiasAntecedenciaLembrete",
                table: "Configuracoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiasAposVencimentoCobranca",
                table: "Configuracoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CobrancaAposVencimentoEnviada",
                table: "Faturas");

            migrationBuilder.DropColumn(
                name: "DiasAntecedenciaLembrete",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "DiasAposVencimentoCobranca",
                table: "Configuracoes");
        }
    }
}

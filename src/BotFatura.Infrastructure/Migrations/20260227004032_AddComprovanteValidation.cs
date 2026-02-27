using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotFatura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComprovanteValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NomeTitularPix",
                table: "Configuracoes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ChavePix",
                table: "Configuracoes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "GrupoSociosWhatsAppId",
                table: "Configuracoes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LogsComprovante",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    FaturaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValorExtraido = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ValorEsperado = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    Erro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TipoArquivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TamanhoArquivo = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsComprovante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogsComprovante_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LogsComprovante_Faturas_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "Faturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogsComprovante_ClienteId",
                table: "LogsComprovante",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsComprovante_CreatedAt",
                table: "LogsComprovante",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LogsComprovante_FaturaId",
                table: "LogsComprovante",
                column: "FaturaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogsComprovante");

            migrationBuilder.DropColumn(
                name: "GrupoSociosWhatsAppId",
                table: "Configuracoes");

            migrationBuilder.AlterColumn<string>(
                name: "NomeTitularPix",
                table: "Configuracoes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ChavePix",
                table: "Configuracoes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}

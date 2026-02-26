using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotFatura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarContratos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContratoId",
                table: "Faturas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contratos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor_mensal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fim = table.Column<DateOnly>(type: "date", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contratos", x => x.id);
                    table.ForeignKey(
                        name: "FK_contratos_Clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Faturas_ContratoId",
                table: "Faturas",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_contratos_cliente_id",
                table: "contratos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_contratos_dia_vencimento_ativo",
                table: "contratos",
                columns: new[] { "dia_vencimento", "ativo" });

            migrationBuilder.AddForeignKey(
                name: "FK_Faturas_contratos_ContratoId",
                table: "Faturas",
                column: "ContratoId",
                principalTable: "contratos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Faturas_contratos_ContratoId",
                table: "Faturas");

            migrationBuilder.DropTable(
                name: "contratos");

            migrationBuilder.DropIndex(
                name: "IX_Faturas_ContratoId",
                table: "Faturas");

            migrationBuilder.DropColumn(
                name: "ContratoId",
                table: "Faturas");
        }
    }
}

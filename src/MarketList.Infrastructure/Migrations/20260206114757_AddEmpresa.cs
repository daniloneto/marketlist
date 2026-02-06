using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketList.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "empresa_id",
                table: "listas_de_compras",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "empresa_id",
                table: "historico_precos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listas_de_compras_empresa_id",
                table: "listas_de_compras",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_historico_precos_empresa_id",
                table: "historico_precos",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_empresas_cnpj",
                table: "empresas",
                column: "cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_empresas_nome",
                table: "empresas",
                column: "nome");

            migrationBuilder.AddForeignKey(
                name: "FK_historico_precos_empresas_empresa_id",
                table: "historico_precos",
                column: "empresa_id",
                principalTable: "empresas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_listas_de_compras_empresas_empresa_id",
                table: "listas_de_compras",
                column: "empresa_id",
                principalTable: "empresas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_historico_precos_empresas_empresa_id",
                table: "historico_precos");

            migrationBuilder.DropForeignKey(
                name: "FK_listas_de_compras_empresas_empresa_id",
                table: "listas_de_compras");

            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropIndex(
                name: "IX_listas_de_compras_empresa_id",
                table: "listas_de_compras");

            migrationBuilder.DropIndex(
                name: "IX_historico_precos_empresa_id",
                table: "historico_precos");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                table: "listas_de_compras");

            migrationBuilder.DropColumn(
                name: "empresa_id",
                table: "historico_precos");
        }
    }
}

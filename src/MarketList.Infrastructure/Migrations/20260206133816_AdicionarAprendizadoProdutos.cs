using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketList.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAprendizadoProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "categoria_precisa_revisao",
                table: "produtos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "nome_normalizado",
                table: "produtos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "precisa_revisao",
                table: "produtos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "regras_classificacao_categoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    termo_normalizado = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    prioridade = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    contagem_usos = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regras_classificacao_categoria", x => x.id);
                    table.ForeignKey(
                        name: "FK_regras_classificacao_categoria_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sinonimos_produto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    texto_original = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    texto_normalizado = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    fonte_origem = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sinonimos_produto", x => x.id);
                    table.ForeignKey(
                        name: "FK_sinonimos_produto_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_produtos_nome_normalizado",
                table: "produtos",
                column: "nome_normalizado");

            migrationBuilder.CreateIndex(
                name: "IX_regras_classificacao_categoria_categoria_id",
                table: "regras_classificacao_categoria",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_regras_classificacao_categoria_termo_normalizado",
                table: "regras_classificacao_categoria",
                column: "termo_normalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sinonimos_produto_produto_id",
                table: "sinonimos_produto",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "IX_sinonimos_produto_texto_normalizado",
                table: "sinonimos_produto",
                column: "texto_normalizado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "regras_classificacao_categoria");

            migrationBuilder.DropTable(
                name: "sinonimos_produto");

            migrationBuilder.DropIndex(
                name: "IX_produtos_nome_normalizado",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "categoria_precisa_revisao",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "nome_normalizado",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "precisa_revisao",
                table: "produtos");
        }
    }
}

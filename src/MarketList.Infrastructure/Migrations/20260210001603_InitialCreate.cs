using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketList.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "empresas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    cnpj = table.Column<string>(type: "TEXT", maxLength: 18, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    nome_normalizado = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    unidade = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    codigo_loja = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    precisa_revisao = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    categoria_precisa_revisao = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    categoria_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produtos", x => x.id);
                    table.ForeignKey(
                        name: "FK_produtos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "regras_classificacao_categoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    termo_normalizado = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    prioridade = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    contagem_usos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    categoria_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
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
                name: "listas_de_compras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    texto_original = table.Column<string>(type: "TEXT", nullable: true),
                    tipo_entrada = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    processado_em = table.Column<DateTime>(type: "TEXT", nullable: true),
                    erro_processamento = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    empresa_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listas_de_compras", x => x.id);
                    table.ForeignKey(
                        name: "FK_listas_de_compras_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "historico_precos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    produto_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    preco_unitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    data_consulta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fonte_preco = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    empresa_id = table.Column<Guid>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_precos", x => x.id);
                    table.ForeignKey(
                        name: "FK_historico_precos_empresas_empresa_id",
                        column: x => x.empresa_id,
                        principalTable: "empresas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_historico_precos_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sinonimos_produto",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    texto_original = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    texto_normalizado = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    fonte_origem = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    produto_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "itens_lista_de_compras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "TEXT", nullable: false),
                    lista_de_compras_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    produto_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    quantidade = table.Column<decimal>(type: "TEXT", precision: 18, scale: 3, nullable: false),
                    unidade_de_medida = table.Column<int>(type: "INTEGER", nullable: true),
                    preco_unitario = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    preco_total = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    texto_original = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    comprado = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_lista_de_compras", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_lista_de_compras_listas_de_compras_lista_de_compras_id",
                        column: x => x.lista_de_compras_id,
                        principalTable: "listas_de_compras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_itens_lista_de_compras_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_nome",
                table: "categorias",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_empresas_cnpj",
                table: "empresas",
                column: "cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_empresas_nome",
                table: "empresas",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "IX_historico_precos_empresa_id",
                table: "historico_precos",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_historico_precos_produto_id_data_consulta",
                table: "historico_precos",
                columns: new[] { "produto_id", "data_consulta" });

            migrationBuilder.CreateIndex(
                name: "IX_itens_lista_de_compras_lista_de_compras_id",
                table: "itens_lista_de_compras",
                column: "lista_de_compras_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_lista_de_compras_produto_id",
                table: "itens_lista_de_compras",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "IX_listas_de_compras_created_at",
                table: "listas_de_compras",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_listas_de_compras_empresa_id",
                table: "listas_de_compras",
                column: "empresa_id");

            migrationBuilder.CreateIndex(
                name: "IX_listas_de_compras_status",
                table: "listas_de_compras",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_categoria_id",
                table: "produtos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_codigo_loja",
                table: "produtos",
                column: "codigo_loja");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_nome",
                table: "produtos",
                column: "nome");

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
                name: "historico_precos");

            migrationBuilder.DropTable(
                name: "itens_lista_de_compras");

            migrationBuilder.DropTable(
                name: "regras_classificacao_categoria");

            migrationBuilder.DropTable(
                name: "sinonimos_produto");

            migrationBuilder.DropTable(
                name: "listas_de_compras");

            migrationBuilder.DropTable(
                name: "produtos");

            migrationBuilder.DropTable(
                name: "empresas");

            migrationBuilder.DropTable(
                name: "categorias");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketList.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCodigoLojaETipoEntrada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona campo CodigoLoja na tabela produtos
            migrationBuilder.AddColumn<string>(
                name: "codigo_loja",
                table: "produtos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Cria índice para código da loja
            migrationBuilder.CreateIndex(
                name: "IX_produtos_codigo_loja",
                table: "produtos",
                column: "codigo_loja");

            // Adiciona campo TipoEntrada na tabela listas_de_compras
            migrationBuilder.AddColumn<int>(
                name: "tipo_entrada",
                table: "listas_de_compras",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Adiciona campo UnidadeDeMedida na tabela itens_lista_de_compras
            migrationBuilder.AddColumn<int>(
                name: "unidade_de_medida",
                table: "itens_lista_de_compras",
                type: "integer",
                nullable: true);

            // Adiciona campo PrecoTotal na tabela itens_lista_de_compras
            migrationBuilder.AddColumn<decimal>(
                name: "preco_total",
                table: "itens_lista_de_compras",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_produtos_codigo_loja",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "codigo_loja",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "tipo_entrada",
                table: "listas_de_compras");

            migrationBuilder.DropColumn(
                name: "unidade_de_medida",
                table: "itens_lista_de_compras");

            migrationBuilder.DropColumn(
                name: "preco_total",
                table: "itens_lista_de_compras");
        }
    }
}

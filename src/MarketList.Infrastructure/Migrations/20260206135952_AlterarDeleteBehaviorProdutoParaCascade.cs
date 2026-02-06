using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketList.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterarDeleteBehaviorProdutoParaCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_itens_lista_de_compras_produtos_produto_id",
                table: "itens_lista_de_compras");

            migrationBuilder.AddForeignKey(
                name: "FK_itens_lista_de_compras_produtos_produto_id",
                table: "itens_lista_de_compras",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_itens_lista_de_compras_produtos_produto_id",
                table: "itens_lista_de_compras");

            migrationBuilder.AddForeignKey(
                name: "FK_itens_lista_de_compras_produtos_produto_id",
                table: "itens_lista_de_compras",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

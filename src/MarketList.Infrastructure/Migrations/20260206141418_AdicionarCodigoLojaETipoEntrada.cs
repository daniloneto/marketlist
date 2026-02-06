using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketList.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCodigoLojaETipoEntrada : Migration
    {        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adiciona campo CodigoLoja na tabela produtos (se não existir)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'produtos' AND column_name = 'codigo_loja') THEN
                        ALTER TABLE produtos ADD COLUMN codigo_loja character varying(50);
                    END IF;
                END $$;
            ");

            // Cria índice para código da loja (se não existir)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_produtos_codigo_loja"" ON produtos (codigo_loja);
            ");

            // Adiciona campo TipoEntrada na tabela listas_de_compras (se não existir)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'listas_de_compras' AND column_name = 'tipo_entrada') THEN
                        ALTER TABLE listas_de_compras ADD COLUMN tipo_entrada integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            ");

            // Adiciona campo UnidadeDeMedida na tabela itens_lista_de_compras (se não existir)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'itens_lista_de_compras' AND column_name = 'unidade_de_medida') THEN
                        ALTER TABLE itens_lista_de_compras ADD COLUMN unidade_de_medida integer;
                    END IF;
                END $$;
            ");

            // Adiciona campo PrecoTotal na tabela itens_lista_de_compras (se não existir)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'itens_lista_de_compras' AND column_name = 'preco_total') THEN
                        ALTER TABLE itens_lista_de_compras ADD COLUMN preco_total numeric(18,2);
                    END IF;
                END $$;
            ");
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

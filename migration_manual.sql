-- Migration: AdicionarAprendizadoProdutos
-- Add new columns to produtos table
ALTER TABLE produtos ADD COLUMN categoria_precisa_revisao boolean NOT NULL DEFAULT false;
ALTER TABLE produtos ADD COLUMN nome_normalizado character varying(200) NULL;
ALTER TABLE produtos ADD COLUMN precisa_revisao boolean NOT NULL DEFAULT false;

-- Create regras_classificacao_categoria table
CREATE TABLE regras_classificacao_categoria (
    id uuid NOT NULL,
    termo_normalizado character varying(200) NOT NULL,
    prioridade integer NOT NULL DEFAULT 0,
    contagem_usos integer NOT NULL DEFAULT 0,
    categoria_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL,
    CONSTRAINT "PK_regras_classificacao_categoria" PRIMARY KEY (id),
    CONSTRAINT "FK_regras_classificacao_categoria_categorias_categoria_id" 
        FOREIGN KEY (categoria_id) REFERENCES categorias (id) ON DELETE CASCADE
);

-- Create sinonimos_produto table
CREATE TABLE sinonimos_produto (
    id uuid NOT NULL,
    texto_original character varying(300) NOT NULL,
    texto_normalizado character varying(300) NOT NULL,
    fonte_origem character varying(50) NULL,
    produto_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL,
    CONSTRAINT "PK_sinonimos_produto" PRIMARY KEY (id),
    CONSTRAINT "FK_sinonimos_produto_produtos_produto_id" 
        FOREIGN KEY (produto_id) REFERENCES produtos (id) ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX "IX_produtos_nome_normalizado" ON produtos (nome_normalizado);
CREATE INDEX "IX_regras_classificacao_categoria_categoria_id" ON regras_classificacao_categoria (categoria_id);
CREATE UNIQUE INDEX "IX_regras_classificacao_categoria_termo_normalizado" ON regras_classificacao_categoria (termo_normalizado);
CREATE INDEX "IX_sinonimos_produto_produto_id" ON sinonimos_produto (produto_id);
CREATE INDEX "IX_sinonimos_produto_texto_normalizado" ON sinonimos_produto (texto_normalizado);

-- Update __EFMigrationsHistory
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260206133816_AdicionarAprendizadoProdutos', '9.0.1');

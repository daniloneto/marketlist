CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE TABLE categorias (
        id uuid NOT NULL,
        nome character varying(100) NOT NULL,
        descricao character varying(500),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_categorias" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE TABLE listas_de_compras (
        id uuid NOT NULL,
        nome character varying(200) NOT NULL,
        texto_original text,
        status integer NOT NULL,
        processado_em timestamp with time zone,
        erro_processamento character varying(2000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_listas_de_compras" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE TABLE produtos (
        id uuid NOT NULL,
        nome character varying(200) NOT NULL,
        descricao character varying(500),
        unidade character varying(20),
        categoria_id uuid NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_produtos" PRIMARY KEY (id),
        CONSTRAINT "FK_produtos_categorias_categoria_id" FOREIGN KEY (categoria_id) REFERENCES categorias (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE TABLE historico_precos (
        id uuid NOT NULL,
        produto_id uuid NOT NULL,
        preco_unitario numeric(18,2) NOT NULL,
        data_consulta timestamp with time zone NOT NULL,
        fonte_preco character varying(100),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_historico_precos" PRIMARY KEY (id),
        CONSTRAINT "FK_historico_precos_produtos_produto_id" FOREIGN KEY (produto_id) REFERENCES produtos (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE TABLE itens_lista_de_compras (
        id uuid NOT NULL,
        lista_de_compras_id uuid NOT NULL,
        produto_id uuid NOT NULL,
        quantidade numeric(18,3) NOT NULL,
        preco_unitario numeric(18,2),
        texto_original character varying(500),
        comprado boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT "PK_itens_lista_de_compras" PRIMARY KEY (id),
        CONSTRAINT "FK_itens_lista_de_compras_listas_de_compras_lista_de_compras_id" FOREIGN KEY (lista_de_compras_id) REFERENCES listas_de_compras (id) ON DELETE CASCADE,
        CONSTRAINT "FK_itens_lista_de_compras_produtos_produto_id" FOREIGN KEY (produto_id) REFERENCES produtos (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_categorias_nome" ON categorias (nome);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE INDEX "IX_historico_precos_produto_id_data_consulta" ON historico_precos (produto_id, data_consulta);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE INDEX "IX_itens_lista_de_compras_lista_de_compras_id" ON itens_lista_de_compras (lista_de_compras_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE INDEX "IX_itens_lista_de_compras_produto_id" ON itens_lista_de_compras (produto_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE INDEX "IX_listas_de_compras_created_at" ON listas_de_compras (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE INDEX "IX_listas_de_compras_status" ON listas_de_compras (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE INDEX "IX_produtos_categoria_id" ON produtos (categoria_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    CREATE INDEX "IX_produtos_nome" ON produtos (nome);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260203192008_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260203192008_InitialCreate', '9.0.4');
    END IF;
END $EF$;
COMMIT;


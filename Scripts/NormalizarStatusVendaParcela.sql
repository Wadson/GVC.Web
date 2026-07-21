SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE dbo.Venda
SET StatusVenda = CASE StatusVenda
    WHEN N'Finalizada' THEN N'Concluída'
    WHEN N'Concluida' THEN N'Concluída'
    WHEN N'Aguardando Pagamento' THEN N'AguardandoPagamento'
    WHEN N'Cancelado' THEN N'Cancelada'
    ELSE StatusVenda
END;

UPDATE dbo.Parcela
SET Status = CASE Status
    WHEN N'Pago Parcial' THEN N'ParcialmentePago'
    WHEN N'Cancelado' THEN N'Cancelada'
    WHEN N'Atrasado' THEN N'Pendente'
    WHEN N'Atrasada' THEN N'Pendente'
    WHEN N'Vencido' THEN N'Pendente'
    WHEN N'Vencida' THEN N'Pendente'
    ELSE Status
END;

IF EXISTS (
    SELECT 1 FROM dbo.Venda
    WHERE StatusVenda NOT IN (N'Aberta', N'Concluída', N'AguardandoPagamento', N'Cancelada')
)
    THROW 51000, 'Existem status de Venda desconhecidos. A normalização foi cancelada.', 1;

IF EXISTS (
    SELECT 1 FROM dbo.Parcela
    WHERE Status NOT IN (N'Pendente', N'Pago', N'ParcialmentePago', N'Cancelada')
)
    THROW 51001, 'Existem status de Parcela desconhecidos. A normalização foi cancelada.', 1;

DECLARE @DefaultVenda sysname;
DECLARE @Sql nvarchar(max);
SELECT @DefaultVenda = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Venda') AND c.name = N'StatusVenda';

IF @DefaultVenda IS NOT NULL
BEGIN
    SET @Sql = N'ALTER TABLE dbo.Venda DROP CONSTRAINT ' + QUOTENAME(@DefaultVenda) + N';';
    EXEC sys.sp_executesql @Sql;
END;

DECLARE @DefaultParcela sysname;
SELECT @DefaultParcela = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Parcela') AND c.name = N'Status';

IF @DefaultParcela IS NOT NULL
BEGIN
    SET @Sql = N'ALTER TABLE dbo.Parcela DROP CONSTRAINT ' + QUOTENAME(@DefaultParcela) + N';';
    EXEC sys.sp_executesql @Sql;
END;

ALTER TABLE dbo.Venda
    ADD CONSTRAINT DF_Venda_StatusVenda DEFAULT N'Aberta' FOR StatusVenda;

ALTER TABLE dbo.Parcela
    ADD CONSTRAINT DF_Parcela_Status DEFAULT N'Pendente' FOR Status;

COMMIT TRANSACTION;

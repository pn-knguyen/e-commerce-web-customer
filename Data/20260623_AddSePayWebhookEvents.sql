IF OBJECT_ID(N'dbo.sepay_webhook_events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.sepay_webhook_events
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        SePayTransactionId BIGINT NOT NULL,
        Gateway NVARCHAR(120) NOT NULL,
        TransactionDate NVARCHAR(60) NOT NULL,
        AccountNumber NVARCHAR(80) NOT NULL,
        SubAccount NVARCHAR(80) NULL,
        Code NVARCHAR(120) NULL,
        Content NVARCHAR(1000) NOT NULL,
        TransferType NVARCHAR(20) NOT NULL,
        Description NVARCHAR(1000) NULL,
        TransferAmount DECIMAL(18,2) NOT NULL,
        Accumulated DECIMAL(18,2) NOT NULL,
        ReferenceCode NVARCHAR(120) NULL,
        RawPayload NVARCHAR(MAX) NOT NULL,
        IsTestMode BIT NOT NULL,
        ProcessingStatus NVARCHAR(40) NOT NULL,
        ProcessingMessage NVARCHAR(1000) NULL,
        ReceivedAt DATETIME2 NOT NULL CONSTRAINT DF_sepay_webhook_events_ReceivedAt DEFAULT SYSUTCDATETIME(),
        ProcessedAt DATETIME2 NULL,
        MatchedOrderId BIGINT NULL,

        CONSTRAINT PK_sepay_webhook_events PRIMARY KEY (Id),
        CONSTRAINT FK_sepay_webhook_events_orders_MatchedOrderId
            FOREIGN KEY (MatchedOrderId)
            REFERENCES dbo.orders (Id)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_sepay_webhook_events_SePayTransactionId'
      AND object_id = OBJECT_ID(N'dbo.sepay_webhook_events')
)
BEGIN
    CREATE UNIQUE INDEX IX_sepay_webhook_events_SePayTransactionId
        ON dbo.sepay_webhook_events (SePayTransactionId);
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_sepay_webhook_events_MatchedOrderId'
      AND object_id = OBJECT_ID(N'dbo.sepay_webhook_events')
)
BEGIN
    CREATE INDEX IX_sepay_webhook_events_MatchedOrderId
        ON dbo.sepay_webhook_events (MatchedOrderId);
END;
GO

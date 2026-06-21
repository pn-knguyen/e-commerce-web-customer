IF OBJECT_ID(N'dbo.sepay_webhook_events', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.sepay_webhook_events
    (
        Id BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_sepay_webhook_events PRIMARY KEY,
        SePayTransactionId BIGINT NOT NULL,
        Gateway NVARCHAR(100) NOT NULL,
        TransactionDate NVARCHAR(19) NOT NULL,
        AccountNumber NVARCHAR(100) NOT NULL,
        SubAccount NVARCHAR(100) NULL,
        Code NVARCHAR(100) NULL,
        Content NVARCHAR(1000) NOT NULL,
        TransferType NVARCHAR(10) NOT NULL,
        Description NVARCHAR(2000) NULL,
        TransferAmount DECIMAL(18,2) NOT NULL,
        Accumulated DECIMAL(18,2) NOT NULL,
        ReferenceCode NVARCHAR(200) NULL,
        RawPayload NVARCHAR(MAX) NOT NULL,
        IsTestMode BIT NOT NULL,
        MatchedOrderId BIGINT NULL,
        ProcessingStatus NVARCHAR(50) NOT NULL
            CONSTRAINT DF_sepay_webhook_events_ProcessingStatus DEFAULT N'Received',
        ProcessingMessage NVARCHAR(500) NULL,
        ProcessedAt DATETIME2 NULL,
        ReceivedAt DATETIME2 NOT NULL,
        CONSTRAINT UQ_sepay_webhook_events_SePayTransactionId
            UNIQUE (SePayTransactionId)
    );

    CREATE INDEX IX_sepay_webhook_events_MatchedOrderId
        ON dbo.sepay_webhook_events (MatchedOrderId);
END;

IF COL_LENGTH(N'dbo.sepay_webhook_events', N'MatchedOrderId') IS NULL
BEGIN
    ALTER TABLE dbo.sepay_webhook_events ADD
        MatchedOrderId BIGINT NULL,
        ProcessingStatus NVARCHAR(50) NOT NULL
            CONSTRAINT DF_sepay_webhook_events_ProcessingStatus DEFAULT N'Received',
        ProcessingMessage NVARCHAR(500) NULL,
        ProcessedAt DATETIME2 NULL;

    CREATE INDEX IX_sepay_webhook_events_MatchedOrderId
        ON dbo.sepay_webhook_events (MatchedOrderId);
END;

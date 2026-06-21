IF EXISTS (SELECT 1 FROM dbo.payment_methods WHERE Id = 7)
BEGIN
    UPDATE dbo.payment_methods
    SET Name = N'SePay Banking',
        Description = N'Quét QR chuyển khoản ngân hàng qua SePay',
        IsActive = 1
    WHERE Id = 7;
END
ELSE
BEGIN
    SET IDENTITY_INSERT dbo.payment_methods ON;
    INSERT INTO dbo.payment_methods (Id, Name, Description, IsActive)
    VALUES (7, N'SePay Banking', N'Quét QR chuyển khoản ngân hàng qua SePay', 1);
    SET IDENTITY_INSERT dbo.payment_methods OFF;
END;

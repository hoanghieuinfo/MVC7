-- Script tạo CSDL & bảng tblUsers (tùy chọn: ứng dụng cũng tự tạo bằng EnsureCreated khi chạy)
IF DB_ID(N'MVC7_SecurityDb') IS NULL
    CREATE DATABASE MVC7_SecurityDb;
GO
USE MVC7_SecurityDb;
GO
IF OBJECT_ID(N'dbo.tblUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblUsers
    (
        UserId            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Username          NVARCHAR(50)  NOT NULL,
        PasswordHashed    NVARCHAR(256) NOT NULL,
        FullName          NVARCHAR(100) NOT NULL,
        Email             NVARCHAR(100) NULL,
        AccessFailedCount INT           NOT NULL CONSTRAINT DF_tblUsers_AccessFailedCount DEFAULT (0),
        LockoutEnd        DATETIME2     NULL,
        IsLocked          BIT           NOT NULL CONSTRAINT DF_tblUsers_IsLocked DEFAULT (0),
        CONSTRAINT UQ_tblUsers_Username UNIQUE (Username)
    );
END
GO

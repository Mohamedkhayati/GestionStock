/*
 GESTION STOCK PRO v2 - installation + migration depuis la v1
 A executer dans SQL Server Management Studio (SSMS), compte autorise.
 Compatible avec SQL Server Express 2022/2025.
 IMPORTANT : sauvegarder GestionStockDb AVANT une migration d'une base existante.
 Le logiciel execute automatiquement la partie apres USE [GestionStockDb].
*/
IF DB_ID(N'GestionStockDb') IS NULL
    EXEC (N'CREATE DATABASE [GestionStockDb]');
GO
USE [GestionStockDb];
GO

IF OBJECT_ID(N'dbo.Fournisseurs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Fournisseurs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Fournisseurs PRIMARY KEY,
        Nom NVARCHAR(200) NOT NULL,
        Telephone NVARCHAR(35) NULL,
        Email NVARCHAR(254) NULL,
        Adresse NVARCHAR(300) NULL,
        MatriculeFiscal NVARCHAR(80) NULL,
        Notes NVARCHAR(500) NULL,
        DateCreation DATETIME2(0) NOT NULL CONSTRAINT DF_Fournisseurs_DateCreation DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Fournisseurs_Nom CHECK (LEN(LTRIM(RTRIM(Nom))) > 0)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Fournisseurs') AND name = N'UX_Fournisseurs_Nom')
    CREATE UNIQUE INDEX UX_Fournisseurs_Nom ON dbo.Fournisseurs(Nom);
GO

IF OBJECT_ID(N'dbo.Articles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Articles
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Articles PRIMARY KEY,
        Reference NVARCHAR(80) NOT NULL CONSTRAINT UQ_Articles_Reference UNIQUE,
        Nom NVARCHAR(250) NOT NULL,
        Quantite INT NOT NULL CONSTRAINT CK_Articles_Qte CHECK (Quantite >= 0),
        PrixAchatHT DECIMAL(18,3) NOT NULL CONSTRAINT CK_Articles_Achat CHECK (PrixAchatHT >= 0),
        PrixVenteHT DECIMAL(18,3) NOT NULL CONSTRAINT CK_Articles_Vente CHECK (PrixVenteHT >= 0),
        Tva INT NOT NULL CONSTRAINT CK_Articles_Tva CHECK (Tva IN (7,19)),
        PrixVenteTTC AS CONVERT(DECIMAL(18,3), ROUND(PrixVenteHT * (1 + CONVERT(DECIMAL(5,2), Tva) / 100), 3)) PERSISTED,
        FournisseurId INT NULL,
        DateCreation DATETIME2 NOT NULL CONSTRAINT DF_Articles_DateCreation DEFAULT SYSUTCDATETIME()
    );
END;
GO
-- Migration sans perte des articles de la version 1.
IF COL_LENGTH(N'dbo.Articles', N'FournisseurId') IS NULL
    ALTER TABLE dbo.Articles ADD FournisseurId INT NULL;
GO
IF NOT EXISTS
(
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.Articles')
      AND name = N'FK_Articles_Fournisseurs'
)
    ALTER TABLE dbo.Articles WITH CHECK ADD CONSTRAINT FK_Articles_Fournisseurs
    FOREIGN KEY (FournisseurId) REFERENCES dbo.Fournisseurs(Id)
    ON DELETE NO ACTION;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Articles') AND name = N'IX_Articles_FournisseurId')
    CREATE INDEX IX_Articles_FournisseurId ON dbo.Articles(FournisseurId);
GO
-- Verifications facultatives :
SELECT COUNT(*) AS NombreFournisseurs FROM dbo.Fournisseurs;
SELECT COUNT(*) AS NombreArticles FROM dbo.Articles;

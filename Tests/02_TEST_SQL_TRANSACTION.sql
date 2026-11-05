/* Test SQL a executer sur une BASE DE TEST ou apres sauvegarde.
   Toutes les insertions et mises a jour sont annulees en fin de test.
   Exige l'installation du schema et les droits d'ecriture.
*/
USE [GestionStockDb];
GO
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @ref NVARCHAR(80) = N'TEST-' + CONVERT(NVARCHAR(36), NEWID());
    DECLARE @nom NVARCHAR(200) = N'TEST-FOURNISSEUR-' + CONVERT(NVARCHAR(36), NEWID());
    DECLARE @fid INT;
    INSERT INTO dbo.Fournisseurs (Nom, Telephone) VALUES (@nom, N'00000000');
    SET @fid = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.Articles (Reference, Nom, Quantite, PrixAchatHT, PrixVenteHT, Tva, FournisseurId)
    VALUES (@ref, N'Test SSD', 3, 80.000, 100.000, 7, @fid);

    IF NOT EXISTS (SELECT 1 FROM dbo.Articles WHERE Reference=@ref AND FournisseurId=@fid AND PrixVenteTTC=107.000)
        THROW 50100, N'Test TVA 7% ou cle fournisseur : ECHEC', 1;

    UPDATE dbo.Articles SET Tva=19 WHERE Reference=@ref;
    IF NOT EXISTS (SELECT 1 FROM dbo.Articles WHERE Reference=@ref AND PrixVenteTTC=119.000)
        THROW 50101, N'Test TVA 19% : ECHEC', 1;

    BEGIN TRY
        DELETE FROM dbo.Fournisseurs WHERE Id=@fid;
        THROW 50102, N'Suppression autorisee alors que les articles sont lies : ECHEC', 1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() <> 547 THROW;
    END CATCH;

    PRINT N'TESTS SQL OK : relation, TVA 7/19, interdiction de supprimer fournisseur lie.';
    ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

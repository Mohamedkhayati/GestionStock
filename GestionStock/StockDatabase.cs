using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace GestionStock;

internal static class StockDatabase
{
    private const string DatabaseName = "GestionStockDb";
    private static string serverName = @".\SQLEXPRESS";

    private sealed class Settings
    {
        public string? Server { get; set; }
    }

    private static string ConnectionString(string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = serverName,
            InitialCatalog = database,
            IntegratedSecurity = true,
            Encrypt = true,
            TrustServerCertificate = true, // Seulement pour SQL Server local de confiance.
            ConnectTimeout = 8,
            ApplicationName = "GestionStock Pro v2"
        };
        return builder.ConnectionString;
    }

    private static SqlConnection Open()
    {
        var connection = new SqlConnection(ConnectionString(DatabaseName));
        connection.Open();
        return connection;
    }

    public static void Initialize()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "connection.settings.json");
        if (File.Exists(configPath))
        {
            Settings? settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(configPath));
            if (string.IsNullOrWhiteSpace(settings?.Server))
                throw new InvalidOperationException("Server absent du fichier connection.settings.json.");
            serverName = settings.Server.Trim();
        }

        using (var connection = new SqlConnection(ConnectionString("master")))
        {
            connection.Open();
            using var createDatabase = new SqlCommand(
                "IF DB_ID(N'GestionStockDb') IS NULL EXEC(N'CREATE DATABASE [GestionStockDb]');", connection);
            createDatabase.ExecuteNonQuery();
        }

        using var db = Open();
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GestionStock.Schema.sql");
        if (stream is null) throw new InvalidOperationException("Le script de base de données manque dans l'application.");
        using var reader = new StreamReader(stream);
        string script = reader.ReadToEnd();
        // Le script manuel peut contenir un prefixe CREATE DATABASE/USE, mais
        // on ne rejoue que les batches de schema apres le deuxieme GO.
        string[] parts = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        for (int i = 2; i < parts.Length; i++)
        {
            string batch = parts[i].Trim();
            if (batch.Length == 0) continue;
            using var command = new SqlCommand(batch, db) { CommandTimeout = 30 };
            command.ExecuteNonQuery();
        }
    }

    public static DataTable GetFournisseurs(string recherche = "")
    {
        const string sql = @"
SELECT f.Id, f.Nom, f.Telephone, f.Email, f.Adresse, f.MatriculeFiscal, f.Notes,
       (SELECT COUNT(*) FROM dbo.Articles a WHERE a.FournisseurId = f.Id) AS NombreArticles
FROM dbo.Fournisseurs f
WHERE @Recherche = N'' OR f.Nom LIKE N'%' + @Recherche + N'%'
                       OR f.Telephone LIKE N'%' + @Recherche + N'%'
                       OR f.Email LIKE N'%' + @Recherche + N'%'
ORDER BY f.Nom;";
        using var db = Open();
        using var command = new SqlCommand(sql, db);
        command.Parameters.Add("@Recherche", SqlDbType.NVarChar, 254).Value = recherche.Trim();
        return ReadTable(command);
    }

    public static DataTable GetFournisseursDropdown()
    {
        using var db = Open();
        using var command = new SqlCommand("SELECT Id, Nom FROM dbo.Fournisseurs ORDER BY Nom;", db);
        return ReadTable(command);
    }

    public static DataTable GetArticles(string recherche)
    {
        const string sql = @"
SELECT a.Id, a.Reference, a.Nom, f.Nom AS Fournisseur, a.FournisseurId,
       a.Quantite, a.PrixAchatHT, a.PrixVenteHT, a.Tva, a.PrixVenteTTC
FROM dbo.Articles a
LEFT JOIN dbo.Fournisseurs f ON a.FournisseurId = f.Id
WHERE @Recherche = N'' OR a.Reference LIKE N'%' + @Recherche + N'%'
 OR a.Nom LIKE N'%' + @Recherche + N'%'
 OR f.Nom LIKE N'%' + @Recherche + N'%'
ORDER BY a.Nom, a.Reference;";
        using var db = Open();
        using var command = new SqlCommand(sql, db);
        command.Parameters.Add("@Recherche", SqlDbType.NVarChar, 250).Value = recherche.Trim();
        return ReadTable(command);
    }

    public static (int Articles, long Quantite, int Fournisseurs, decimal ValeurAchatHT) GetStatistics()
    {
        const string sql = @"
SELECT (SELECT COUNT(*) FROM dbo.Articles),
       (SELECT COALESCE(SUM(CONVERT(BIGINT, Quantite)), 0) FROM dbo.Articles),
       (SELECT COUNT(*) FROM dbo.Fournisseurs),
       (SELECT COALESCE(SUM(CONVERT(DECIMAL(28,3), Quantite) * PrixAchatHT), 0) FROM dbo.Articles);";
        using var db = Open();
        using var command = new SqlCommand(sql, db);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return (0, 0, 0, 0m);
        return (reader.GetInt32(0), reader.GetInt64(1), reader.GetInt32(2), reader.GetDecimal(3));
    }

    public static void AddArticle(Article a)
    {
        const string sql = @"INSERT INTO dbo.Articles
(Reference, Nom, Quantite, PrixAchatHT, PrixVenteHT, Tva, FournisseurId)
VALUES (@Reference, @Nom, @Quantite, @PrixAchatHT, @PrixVenteHT, @Tva, @FournisseurId);";
        ExecuteArticleCommand(sql, a, null);
    }

    public static void UpdateArticle(int id, Article a)
    {
        const string sql = @"UPDATE dbo.Articles
SET Reference=@Reference, Nom=@Nom, Quantite=@Quantite,
PrixAchatHT=@PrixAchatHT, PrixVenteHT=@PrixVenteHT, Tva=@Tva,
FournisseurId=@FournisseurId WHERE Id=@Id;";
        ExecuteArticleCommand(sql, a, id);
    }

    private static void ExecuteArticleCommand(string sql, Article a, int? id)
    {
        using var db = Open();
        using var command = new SqlCommand(sql, db);
        command.Parameters.Add("@Reference", SqlDbType.NVarChar, 80).Value = a.Reference;
        command.Parameters.Add("@Nom", SqlDbType.NVarChar, 250).Value = a.Nom;
        command.Parameters.Add("@Quantite", SqlDbType.Int).Value = a.Quantite;
        AddPrice(command, "@PrixAchatHT", a.PrixAchatHT);
        AddPrice(command, "@PrixVenteHT", a.PrixVenteHT);
        command.Parameters.Add("@Tva", SqlDbType.Int).Value = a.Tva;
        command.Parameters.Add("@FournisseurId", SqlDbType.Int).Value = a.FournisseurId;
        if (id.HasValue) command.Parameters.Add("@Id", SqlDbType.Int).Value = id.Value;
        command.ExecuteNonQuery();
    }

    public static void DeleteArticle(int id)
    {
        using var db = Open();
        using var command = new SqlCommand("DELETE FROM dbo.Articles WHERE Id=@Id", db);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.ExecuteNonQuery();
    }

    public static void AddFournisseur(Fournisseur f)
    {
        const string sql = @"INSERT INTO dbo.Fournisseurs
(Nom, Telephone, Email, Adresse, MatriculeFiscal, Notes)
VALUES (@Nom, @Telephone, @Email, @Adresse, @MatriculeFiscal, @Notes);";
        ExecuteFournisseurCommand(sql, f, null);
    }

    public static void UpdateFournisseur(int id, Fournisseur f)
    {
        const string sql = @"UPDATE dbo.Fournisseurs SET
Nom=@Nom, Telephone=@Telephone, Email=@Email, Adresse=@Adresse,
MatriculeFiscal=@MatriculeFiscal, Notes=@Notes WHERE Id=@Id;";
        ExecuteFournisseurCommand(sql, f, id);
    }

    private static void ExecuteFournisseurCommand(string sql, Fournisseur f, int? id)
    {
        using var db = Open();
        using var command = new SqlCommand(sql, db);
        command.Parameters.Add("@Nom", SqlDbType.NVarChar, 200).Value = f.Nom;
        OptionalString(command, "@Telephone", 35, f.Telephone);
        OptionalString(command, "@Email", 254, f.Email);
        OptionalString(command, "@Adresse", 300, f.Adresse);
        OptionalString(command, "@MatriculeFiscal", 80, f.MatriculeFiscal);
        OptionalString(command, "@Notes", 500, f.Notes);
        if (id.HasValue) command.Parameters.Add("@Id", SqlDbType.Int).Value = id.Value;
        command.ExecuteNonQuery();
    }

    private static void OptionalString(SqlCommand command, string parameter, int maxLength, string value)
    {
        command.Parameters.Add(parameter, SqlDbType.NVarChar, maxLength).Value =
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }

    public static void DeleteFournisseur(int id)
    {
        using var db = Open();
        using var command = new SqlCommand("DELETE FROM dbo.Fournisseurs WHERE Id=@Id", db);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.ExecuteNonQuery();
    }

    private static DataTable ReadTable(SqlCommand command)
    {
        using var reader = command.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private static void AddPrice(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 3;
        parameter.Value = value;
    }
}

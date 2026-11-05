using System;
using System.Windows.Forms;

namespace GestionStock;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            // SQL Server doit être installé et démarré sur le PC.
            StockDatabase.Initialize();
            Application.Run(new StockForm());
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Impossible de démarrer l'application.\n\n" +
                "Vérifiez que SQL Server Express est installé, " +
                "que le service SQL Server (SQLEXPRESS) fonctionne et " +
                "que le nom du serveur est correct dans StockDatabase.cs.\n\n" +
                "Détails : " + exception.Message,
                "Erreur de connexion SQL Server",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

using System.Data;
using System.Drawing;
using System.Net.Mail;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace GestionStock;

internal sealed class FournisseursForm : Form
{
    private readonly TextBox txtNom = new();
    private readonly TextBox txtTelephone = new();
    private readonly TextBox txtEmail = new();
    private readonly TextBox txtAdresse = new();
    private readonly TextBox txtMatricule = new();
    private readonly TextBox txtNotes = new();
    private readonly TextBox txtRecherche = new();
    private readonly DataGridView grille = new();
    private readonly Label lblStatut = new();
    private int? selectedId;
    private bool refreshing;

    public FournisseursForm()
    {
        Text = "Fournisseurs | Gestion de Stock PRO";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1100, 680);
        Size = new Size(1380, 840);
        BackColor = Color.FromArgb(245, 247, 251);
        Font = new Font("Segoe UI", 10f);

        var header = new Panel { Dock = DockStyle.Top, Height = 85, BackColor = Color.FromArgb(26, 43, 75) };
        header.Controls.Add(new Label
        {
            Text = "GESTION DES FOURNISSEURS",
            Location = new Point(24, 10),
            Size = new Size(660, 40),
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            ForeColor = Color.White
        });
        header.Controls.Add(new Label
        {
            Text = "Créer les fournisseurs d'abord, puis les choisir dans la liste de la fiche article.",
            Location = new Point(25, 52),
            Size = new Size(850, 24),
            ForeColor = Color.FromArgb(202, 217, 235)
        });
        var btnRetour = new Button
        {
            Text = "RETOUR AUX ARTICLES",
            Dock = DockStyle.Right,
            Width = 235,
            BackColor = Color.FromArgb(42, 113, 168),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        btnRetour.FlatAppearance.BorderSize = 0;
        btnRetour.Click += (_, _) => Close();
        header.Controls.Add(btnRetour);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Size = new Size(1280, 700),
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 8,
            BackColor = Color.FromArgb(225, 231, 238)
        };
        split.Panel1MinSize = 355;
        split.SplitterDistance = 400;

        var formArea = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(17),
            BackColor = Color.White
        };
        formArea.Controls.Add(new Label
        {
            Text = "Fiche fournisseur",
            Width = 330,
            Height = 43,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 43, 75)
        });
        txtNom.MaxLength = 200;
        txtTelephone.MaxLength = 35;
        txtEmail.MaxLength = 254;
        txtAdresse.MaxLength = 300;
        txtMatricule.MaxLength = 80;
        txtNotes.MaxLength = 500;
        txtNotes.Multiline = true;
        txtNotes.ScrollBars = ScrollBars.Vertical;
        formArea.Controls.Add(Field("Nom fournisseur *", txtNom));
        formArea.Controls.Add(Field("Téléphone", txtTelephone));
        formArea.Controls.Add(Field("Adresse e-mail", txtEmail));
        formArea.Controls.Add(Field("Adresse", txtAdresse));
        formArea.Controls.Add(Field("Matricule fiscal", txtMatricule));
        formArea.Controls.Add(Field("Notes (facultatif)", txtNotes, 95));

        var actions = new TableLayoutPanel { Width = 338, Height = 116, ColumnCount = 2, RowCount = 2 };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var btnAjouter = MakeButton("+ Ajouter", Color.FromArgb(26, 119, 81));
        var btnModifier = MakeButton("Modifier", Color.FromArgb(33, 100, 169));
        var btnSupprimer = MakeButton("Supprimer", Color.FromArgb(173, 49, 62));
        var btnVider = MakeButton("Nouvelle fiche", Color.FromArgb(105, 115, 133));
        actions.Controls.Add(btnAjouter, 0, 0);
        actions.Controls.Add(btnModifier, 1, 0);
        actions.Controls.Add(btnSupprimer, 0, 1);
        actions.Controls.Add(btnVider, 1, 1);
        formArea.Controls.Add(actions);
        formArea.Controls.Add(new Label
        {
            Width = 320,
            Height = 58,
            ForeColor = Color.FromArgb(93, 105, 124),
            Text = "Un fournisseur lié à un ou plusieurs articles ne peut pas être supprimé."
        });
        split.Panel1.Controls.Add(formArea);

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = Color.FromArgb(245, 247, 251) };
        var searchBar = new Panel { Dock = DockStyle.Top, Height = 76 };
        searchBar.Controls.Add(new Label
        {
            Text = "Liste des fournisseurs",
            Location = new Point(0, 1),
            Size = new Size(350, 32),
            Font = new Font("Segoe UI", 15, FontStyle.Bold)
        });
        txtRecherche.PlaceholderText = "Rechercher par nom, téléphone ou e-mail...";
        txtRecherche.Location = new Point(0, 39);
        txtRecherche.Size = new Size(405, 32);
        txtRecherche.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        var btnActualiser = MakeButton("Actualiser", Color.FromArgb(57, 72, 100));
        btnActualiser.Location = new Point(422, 37);
        btnActualiser.Size = new Size(120, 35);
        btnActualiser.Dock = DockStyle.None;
        searchBar.Controls.Add(txtRecherche);
        searchBar.Controls.Add(btnActualiser);

        grille.Dock = DockStyle.Fill;
        grille.ReadOnly = true;
        grille.AllowUserToAddRows = false;
        grille.AllowUserToDeleteRows = false;
        grille.MultiSelect = false;
        grille.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grille.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grille.BackgroundColor = Color.White;
        grille.BorderStyle = BorderStyle.None;
        grille.RowHeadersVisible = false;
        grille.RowTemplate.Height = 35;
        grille.EnableHeadersVisualStyles = false;
        grille.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(26, 43, 75);
        grille.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grille.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        grille.ColumnHeadersHeight = 40;
        grille.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(242, 247, 253);
        lblStatut.Dock = DockStyle.Bottom;
        lblStatut.Height = 40;
        lblStatut.TextAlign = ContentAlignment.MiddleLeft;
        lblStatut.ForeColor = Color.FromArgb(59, 75, 102);
        content.Controls.Add(grille);
        content.Controls.Add(lblStatut);
        content.Controls.Add(searchBar);
        split.Panel2.Controls.Add(content);
        Controls.Add(split);
        Controls.Add(header);

        txtRecherche.TextChanged += (_, _) => RefreshFournisseurs();
        grille.SelectionChanged += (_, _) => FillFromSelection();
        btnActualiser.Click += (_, _) => RefreshFournisseurs();
        btnAjouter.Click += (_, _) => Insert();
        btnModifier.Click += (_, _) => Update();
        btnSupprimer.Click += (_, _) => Delete();
        btnVider.Click += (_, _) => ClearForm();
        RefreshFournisseurs();
    }

    private static Panel Field(string label, Control input, int height = 73)
    {
        var panel = new Panel { Width = 338, Height = height, Margin = new Padding(4, 1, 4, 3) };
        panel.Controls.Add(new Label
        {
            Text = label,
            Location = new Point(0, 0),
            Size = new Size(330, 25),
            ForeColor = Color.FromArgb(45, 58, 82)
        });
        input.Location = new Point(0, 27);
        input.Width = 325;
        input.Height = height == 73 ? 33 : height - 30;
        input.Font = new Font("Segoe UI", 11);
        panel.Controls.Add(input);
        return panel;
    }

    private static Button MakeButton(string text, Color background) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        BackColor = background,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Cursor = Cursors.Hand,
        Margin = new Padding(4)
    };

    private bool ValidateForm(out Fournisseur fournisseur)
    {
        fournisseur = new Fournisseur("", "", "", "", "", "");
        string nom = txtNom.Text.Trim();
        string email = txtEmail.Text.Trim();
        if (nom.Length == 0)
        {
            MessageBox.Show(this, "Le nom du fournisseur est obligatoire.", "Vérification");
            txtNom.Focus();
            return false;
        }
        if (email.Length > 0 && (!MailAddress.TryCreate(email, out var address) || address.Address != email))
        {
            MessageBox.Show(this, "L'adresse e-mail n'est pas valide.", "Vérification");
            txtEmail.Focus();
            return false;
        }
        fournisseur = new Fournisseur(nom, txtTelephone.Text.Trim(), email,
            txtAdresse.Text.Trim(), txtMatricule.Text.Trim(), txtNotes.Text.Trim());
        return true;
    }

    private void RefreshFournisseurs()
    {
        try
        {
            refreshing = true;
            DataTable table = StockDatabase.GetFournisseurs(txtRecherche.Text);
            grille.DataSource = table;
            if (grille.Columns.Contains("Id")) grille.Columns["Id"]!.Visible = false;
            if (grille.Columns.Contains("Notes")) grille.Columns["Notes"]!.Visible = false;
            SetHeader("Nom", "Fournisseur");
            SetHeader("Telephone", "Téléphone");
            SetHeader("Email", "E-mail");
            SetHeader("Adresse", "Adresse");
            SetHeader("MatriculeFiscal", "Matricule fiscal");
            SetHeader("NombreArticles", "Articles liés");
            grille.ClearSelection();
            selectedId = null;
            lblStatut.Text = $"{table.Rows.Count} fournisseur(s) | Sélectionnez une ligne pour modifier la fiche.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossible de charger les fournisseurs : " + ex.Message,
                "Erreur SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { refreshing = false; }
    }

    private void SetHeader(string column, string label)
    {
        if (grille.Columns.Contains(column)) grille.Columns[column]!.HeaderText = label;
    }

    private void FillFromSelection()
    {
        if (refreshing || grille.SelectedRows.Count == 0) return;
        var row = grille.SelectedRows[0];
        if (row.IsNewRow || row.Cells["Id"].Value is null) return;
        selectedId = Convert.ToInt32(row.Cells["Id"].Value);
        txtNom.Text = StringCell(row, "Nom");
        txtTelephone.Text = StringCell(row, "Telephone");
        txtEmail.Text = StringCell(row, "Email");
        txtAdresse.Text = StringCell(row, "Adresse");
        txtMatricule.Text = StringCell(row, "MatriculeFiscal");
        txtNotes.Text = StringCell(row, "Notes");
    }

    private static string StringCell(DataGridViewRow row, string col)
    {
        object? value = row.Cells[col].Value;
        return value is null or DBNull ? "" : Convert.ToString(value) ?? "";
    }

    private void Insert()
    {
        if (!ValidateForm(out Fournisseur f)) return;
        try
        {
            StockDatabase.AddFournisseur(f);
            RefreshFournisseurs();
            ClearForm();
            MessageBox.Show(this, "Fournisseur ajouté. Vous pouvez maintenant le choisir dans la fiche article.",
                "Ajout réussi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            MessageBox.Show(this, "Ce nom de fournisseur existe déjà.", "Doublon");
        }
        catch (Exception ex) { MessageBox.Show(this, "Erreur d'ajout : " + ex.Message, "Erreur"); }
    }

    private void Update()
    {
        if (selectedId is null)
        {
            MessageBox.Show(this, "Sélectionnez d'abord un fournisseur dans le tableau.", "Modification");
            return;
        }
        if (!ValidateForm(out Fournisseur f)) return;
        try
        {
            StockDatabase.UpdateFournisseur(selectedId.Value, f);
            RefreshFournisseurs();
            ClearForm();
            MessageBox.Show(this, "Fournisseur modifié. Son nom sera mis à jour dans les articles liés.",
                "Modification réussie", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            MessageBox.Show(this, "Ce nom de fournisseur est déjà utilisé.", "Doublon");
        }
        catch (Exception ex) { MessageBox.Show(this, "Erreur de modification : " + ex.Message, "Erreur"); }
    }

    private void Delete()
    {
        if (selectedId is null)
        {
            MessageBox.Show(this, "Sélectionnez d'abord un fournisseur à supprimer.", "Suppression");
            return;
        }
        if (MessageBox.Show(this, "Supprimer le fournisseur « " + txtNom.Text + " » ?",
            "Confirmer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            StockDatabase.DeleteFournisseur(selectedId.Value);
            RefreshFournisseurs();
            ClearForm();
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            MessageBox.Show(this, "Suppression bloquée : ce fournisseur est lié à des articles.\n" +
                "Modifiez ou supprimez d'abord les articles concernés.",
                "Liaison SQL protégée", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, "Erreur de suppression : " + ex.Message, "Erreur"); }
    }

    private void ClearForm()
    {
        selectedId = null;
        grille.ClearSelection();
        txtNom.Clear();
        txtTelephone.Clear();
        txtEmail.Clear();
        txtAdresse.Clear();
        txtMatricule.Clear();
        txtNotes.Clear();
        txtNom.Focus();
    }
}

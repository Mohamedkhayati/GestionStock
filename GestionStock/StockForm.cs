using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace GestionStock;

internal sealed class StockForm : Form
{
    private readonly TextBox txtReference = new();
    private readonly TextBox txtNom = new();
    private readonly TextBox txtQuantite = new();
    private readonly TextBox txtAchat = new();
    private readonly TextBox txtVente = new();
    private readonly ComboBox cboTva = new();
    private readonly ComboBox cboFournisseur = new();
    private readonly TextBox txtRecherche = new();
    private readonly Label lblTtc = new();
    private readonly Label lblStatut = new();
    private readonly DataGridView grille = new();
    private int? selectedId;
    private bool refreshing;

    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public StockForm()
    {
        Text = "Gestion de stock PRO | Articles & Fournisseurs";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 680);
        Size = new Size(1440, 870);
        BackColor = Color.FromArgb(245, 247, 251);
        Font = new Font("Segoe UI", 10f);

        // En-tête
        var header = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Color.FromArgb(26, 43, 75) };
        var heading = new Label
        {
            Text = "GESTION DE STOCK",
            Location = new Point(24, 14),
            Size = new Size(600, 36),
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White
        };
        var subtitle = new Label
        {
            Text = "Articles • Fournisseurs liés • TVA 7 % / 19 % • Calcul TTC automatique",
            Location = new Point(26, 52),
            Size = new Size(740, 24),
            ForeColor = Color.FromArgb(204, 216, 235)
        };
        var btnFournisseurs = new Button
        {
            Text = "GÉRER LES FOURNISSEURS",
            Dock = DockStyle.Right,
            Width = 255,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(42, 113, 168),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        btnFournisseurs.FlatAppearance.BorderSize = 0;
        btnFournisseurs.Click += (_, _) => OpenFournisseurs();
        header.Controls.Add(btnFournisseurs);
        header.Controls.Add(heading);
        header.Controls.Add(subtitle);

        var split = new SplitContainer
        {
            Size = new Size(1250, 650),
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 8,
            BackColor = Color.FromArgb(225, 231, 238)
        };
        // Régler les dimensions avant la distance évite une exception
        // quand SplitContainer possède encore sa petite taille par défaut.
        split.Panel1MinSize = 355;
        split.SplitterDistance = 405;

        // Formulaire à gauche
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
            Text = "Fiche article",
            Width = 325,
            Height = 44,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 43, 75)
        });

        txtReference.MaxLength = 80;
        txtNom.MaxLength = 250;
        txtQuantite.Text = "0";
        txtAchat.Text = "0,000";
        txtVente.Text = "0,000";
        cboTva.DropDownStyle = ComboBoxStyle.DropDownList;
        cboTva.Items.AddRange(new object[] { "7", "19" });
        cboTva.SelectedItem = "19";

        formArea.Controls.Add(Field("Référence article", txtReference));
        formArea.Controls.Add(Field("Nom de l'article", txtNom));
        cboFournisseur.DropDownStyle = ComboBoxStyle.DropDownList;
        cboFournisseur.DropDownWidth = 420;
        formArea.Controls.Add(Field("Fournisseur (obligatoire)", cboFournisseur));
        var btnAddSupplier = new Button
        {
            Text = "+ Ajouter / gérer un fournisseur",
            Width = 329,
            Height = 36,
            BackColor = Color.FromArgb(236, 245, 255),
            ForeColor = Color.FromArgb(26, 80, 147),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 4, 9)
        };
        btnAddSupplier.FlatAppearance.BorderColor = Color.FromArgb(186, 213, 245);
        btnAddSupplier.Click += (_, _) => OpenFournisseurs();
        formArea.Controls.Add(btnAddSupplier);
        formArea.Controls.Add(Field("Quantité en stock", txtQuantite));
        formArea.Controls.Add(Field("Prix d'achat HT (DT)", txtAchat));
        formArea.Controls.Add(Field("Prix de vente HT (DT)", txtVente));
        formArea.Controls.Add(Field("TVA (%)", cboTva));

        var ttcPanel = new Panel { Width = 335, Height = 77, BackColor = Color.FromArgb(236, 245, 255), Margin = new Padding(4, 5, 4, 10) };
        ttcPanel.Controls.Add(new Label
        {
            Text = "Prix de vente TTC (automatique)",
            Location = new Point(12, 8),
            AutoSize = true,
            ForeColor = Color.FromArgb(57, 72, 100)
        });
        lblTtc.Location = new Point(12, 31);
        lblTtc.Size = new Size(290, 37);
        lblTtc.Font = new Font("Segoe UI", 17, FontStyle.Bold);
        lblTtc.ForeColor = Color.FromArgb(24, 92, 169);
        ttcPanel.Controls.Add(lblTtc);
        formArea.Controls.Add(ttcPanel);

        var actions = new TableLayoutPanel { Width = 338, Height = 116, ColumnCount = 2, RowCount = 2 };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        var btnAjouter = MakeButton("+ Ajouter", Color.FromArgb(26, 119, 81));
        var btnModifier = MakeButton("Modifier", Color.FromArgb(33, 100, 169));
        var btnSupprimer = MakeButton("Supprimer", Color.FromArgb(173, 49, 62));
        var btnVider = MakeButton("Vider", Color.FromArgb(105, 115, 133));
        actions.Controls.Add(btnAjouter, 0, 0);
        actions.Controls.Add(btnModifier, 1, 0);
        actions.Controls.Add(btnSupprimer, 0, 1);
        actions.Controls.Add(btnVider, 1, 1);
        formArea.Controls.Add(actions);
        split.Panel1.Controls.Add(formArea);

        // Liste des articles à droite
        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = Color.FromArgb(245, 247, 251) };
        var searchBar = new Panel { Dock = DockStyle.Top, Height = 76 };
        var searchLabel = new Label
        {
            Text = "Liste des articles",
            Location = new Point(0, 1),
            Size = new Size(290, 32),
            Font = new Font("Segoe UI", 15, FontStyle.Bold)
        };
        txtRecherche.PlaceholderText = "Référence, article ou fournisseur...";
        txtRecherche.Location = new Point(0, 39);
        txtRecherche.Size = new Size(285, 32);
        txtRecherche.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        var btnActualiser = MakeButton("Actualiser", Color.FromArgb(57, 72, 100));
        btnActualiser.Location = new Point(300, 36);
        btnActualiser.Size = new Size(106, 36);
        btnActualiser.Dock = DockStyle.None;
        var btnExporter = MakeButton("Export CSV", Color.FromArgb(26, 119, 81));
        btnExporter.Location = new Point(416, 36);
        btnExporter.Size = new Size(120, 36);
        btnExporter.Dock = DockStyle.None;
        btnExporter.Click += (_, _) => ExporterCsv();
        searchBar.Controls.Add(searchLabel);
        searchBar.Controls.Add(txtRecherche);
        searchBar.Controls.Add(btnActualiser);
        searchBar.Controls.Add(btnExporter);

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
        grille.RowTemplate.Height = 33;
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
        lblStatut.Padding = new Padding(6, 0, 0, 0);

        content.Controls.Add(grille);
        content.Controls.Add(lblStatut);
        content.Controls.Add(searchBar);
        split.Panel2.Controls.Add(content);
        Controls.Add(split);
        Controls.Add(header);

        txtVente.TextChanged += (_, _) => UpdateTtc();
        cboTva.SelectedIndexChanged += (_, _) => UpdateTtc();
        txtRecherche.TextChanged += (_, _) => RefreshArticles();
        grille.SelectionChanged += (_, _) => FillFromSelection();
        btnActualiser.Click += (_, _) => RefreshArticles();
        btnAjouter.Click += (_, _) => Insert();
        btnModifier.Click += (_, _) => Update();
        btnSupprimer.Click += (_, _) => Delete();
        btnVider.Click += (_, _) => ClearForm();

        UpdateTtc();
        LoadFournisseurs();
        RefreshArticles();
    }

    private static Panel Field(string label, Control input)
    {
        var panel = new Panel { Width = 338, Height = 73, Margin = new Padding(4, 1, 4, 3) };
        panel.Controls.Add(new Label
        {
            Text = label,
            Location = new Point(0, 0),
            Size = new Size(330, 25),
            ForeColor = Color.FromArgb(45, 58, 82)
        });
        input.Location = new Point(0, 27);
        input.Width = 325;
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

    private static bool TryMoney(string text, out decimal amount)
    {
        var normalized = text.Trim().Replace(" ", "").Replace(',', '.');
        return decimal.TryParse(
                   normalized,
                   NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out amount)
               && amount >= 0
               && decimal.Round(amount, 3) == amount
               && amount < 1_000_000_000_000_000m;
    }

    private void UpdateTtc()
    {
        if (TryMoney(txtVente.Text, out decimal ht) && int.TryParse(cboTva.Text, out int tva))
        {
            var ttc = Math.Round(ht * (1 + tva / 100m), 3, MidpointRounding.AwayFromZero);
            lblTtc.Text = ttc.ToString("N3", French) + " DT";
        }
        else
        {
            lblTtc.Text = "—";
        }
    }

    private bool ValidateArticle(out Article article)
    {
        article = new Article("", "", 0, 0, 0, 19, 0);
        string reference = txtReference.Text.Trim();
        string nom = txtNom.Text.Trim();
        if (reference.Length == 0 || nom.Length == 0)
        {
            MessageBox.Show(this, "Remplissez la référence et le nom de l'article.", "Vérification");
            return false;
        }
        if (!int.TryParse(txtQuantite.Text.Trim(), out int quantite) || quantite < 0)
        {
            MessageBox.Show(this, "Quantité invalide. Saisissez un entier positif (ou zéro).", "Vérification");
            return false;
        }
        if (!TryMoney(txtAchat.Text, out decimal achat) || !TryMoney(txtVente.Text, out decimal vente))
        {
            MessageBox.Show(this, "Prix invalide. Utilisez par exemple 150,000 (3 décimales maximum).", "Vérification");
            return false;
        }
        if (!int.TryParse(cboTva.Text, out int tva) || (tva != 7 && tva != 19))
        {
            MessageBox.Show(this, "Choisissez une TVA de 7 % ou 19 %.", "Vérification");
            return false;
        }
        if (cboFournisseur.SelectedValue is not int fournisseurId || fournisseurId <= 0)
        {
            MessageBox.Show(this, "Choisissez d'abord un fournisseur dans la liste.\n" +
                "Pour en ajouter un, cliquez sur '+ Ajouter / gérer un fournisseur'.", "Fournisseur obligatoire");
            return false;
        }
        // Prix TTC doit aussi tenir dans DECIMAL(18,3).
        decimal ttc = Math.Round(vente * (1 + tva / 100m), 3, MidpointRounding.AwayFromZero);
        if (ttc >= 1_000_000_000_000_000m)
        {
            MessageBox.Show(this, "Prix de vente trop grand.", "Vérification");
            return false;
        }
        article = new Article(reference, nom, quantite, achat, vente, tva, fournisseurId);
        return true;
    }

    private void RefreshArticles()
    {
        try
        {
            refreshing = true;
            DataTable table = StockDatabase.GetArticles(txtRecherche.Text);
            grille.DataSource = table;
            if (grille.Columns.Contains("Id")) grille.Columns["Id"]!.Visible = false;
            if (grille.Columns.Contains("FournisseurId")) grille.Columns["FournisseurId"]!.Visible = false;
            SetHeader("Fournisseur", "Fournisseur");
            SetHeader("Reference", "Référence");
            SetHeader("Nom", "Nom article");
            SetHeader("Quantite", "Qté");
            SetHeader("PrixAchatHT", "Achat HT (DT)");
            SetHeader("PrixVenteHT", "Vente HT (DT)");
            SetHeader("Tva", "TVA %");
            SetHeader("PrixVenteTTC", "Vente TTC (DT)");
            foreach (var name in new[] { "PrixAchatHT", "PrixVenteHT", "PrixVenteTTC" })
            {
                if (grille.Columns.Contains(name))
                {
                    grille.Columns[name]!.DefaultCellStyle.Format = "N3";
                    grille.Columns[name]!.DefaultCellStyle.FormatProvider = French;
                    grille.Columns[name]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            grille.ClearSelection();
            selectedId = null;
            var stats = StockDatabase.GetStatistics();
            lblStatut.Text = $"{table.Rows.Count} affiché(s)  •  Total {stats.Articles} articles  •  " +
                $"{stats.Quantite} unités  •  {stats.Fournisseurs} fournisseurs  •  Stock achat {stats.ValeurAchatHT:N3} DT HT";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossible de charger les articles : " + ex.Message, "Erreur SQL",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            refreshing = false;
        }
    }

    private void SetHeader(string column, string label)
    {
        if (grille.Columns.Contains(column)) grille.Columns[column]!.HeaderText = label;
    }

    private void FillFromSelection()
    {
        if (refreshing || grille.SelectedRows.Count == 0) return;
        DataGridViewRow row = grille.SelectedRows[0];
        if (row.IsNewRow || row.Cells["Id"].Value is null) return;
        selectedId = Convert.ToInt32(row.Cells["Id"].Value);
        txtReference.Text = Convert.ToString(row.Cells["Reference"].Value) ?? "";
        txtNom.Text = Convert.ToString(row.Cells["Nom"].Value) ?? "";
        txtQuantite.Text = Convert.ToString(row.Cells["Quantite"].Value) ?? "0";
        txtAchat.Text = Convert.ToDecimal(row.Cells["PrixAchatHT"].Value).ToString("F3", French);
        txtVente.Text = Convert.ToDecimal(row.Cells["PrixVenteHT"].Value).ToString("F3", French);
        cboTva.SelectedItem = Convert.ToString(row.Cells["Tva"].Value);
        if (row.Cells["FournisseurId"].Value is not DBNull and not null)
            cboFournisseur.SelectedValue = Convert.ToInt32(row.Cells["FournisseurId"].Value);
        else
            cboFournisseur.SelectedIndex = -1; // Ancien article v1 sans fournisseur.
        UpdateTtc();
    }

    private void Insert()
    {
        if (!ValidateArticle(out Article article)) return;
        try
        {
            StockDatabase.AddArticle(article);
            RefreshArticles();
            ClearForm();
            MessageBox.Show(this, "Article ajouté avec succès.", "Ajout", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            MessageBox.Show(this, "Cette référence existe déjà. Choisissez une autre référence.", "Référence en double");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Erreur d'ajout : " + ex.Message, "Erreur");
        }
    }

    private void Update()
    {
        if (selectedId is null)
        {
            MessageBox.Show(this, "Sélectionnez d'abord une ligne dans le tableau.", "Modification");
            return;
        }
        if (!ValidateArticle(out Article article)) return;
        try
        {
            StockDatabase.UpdateArticle(selectedId.Value, article);
            RefreshArticles();
            ClearForm();
            MessageBox.Show(this, "Article modifié avec succès.", "Modification", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            MessageBox.Show(this, "Cette référence est déjà utilisée par un autre article.", "Référence en double");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Erreur de modification : " + ex.Message, "Erreur");
        }
    }

    private void Delete()
    {
        if (selectedId is null)
        {
            MessageBox.Show(this, "Sélectionnez d'abord un article à supprimer.", "Suppression");
            return;
        }
        var confirmation = MessageBox.Show(this,
            "Supprimer définitivement l'article « " + txtNom.Text + " » ?",
            "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirmation != DialogResult.Yes) return;
        try
        {
            StockDatabase.DeleteArticle(selectedId.Value);
            RefreshArticles();
            ClearForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Erreur de suppression : " + ex.Message, "Erreur");
        }
    }

    private void ClearForm()
    {
        selectedId = null;
        grille.ClearSelection();
        txtReference.Clear();
        txtNom.Clear();
        txtQuantite.Text = "0";
        txtAchat.Text = "0,000";
        txtVente.Text = "0,000";
        cboTva.SelectedItem = "19";
        cboFournisseur.SelectedIndex = -1;
        txtReference.Focus();
        UpdateTtc();
    }

    private void LoadFournisseurs()
    {
        // Rafraîchissement du dropdown après l'ajout/modification d'un fournisseur.
        // Un article existant conserve son identifiant durant le rafraîchissement.
        int? previous = cboFournisseur.SelectedValue is int id ? id : null;
        DataTable items = StockDatabase.GetFournisseursDropdown();
        cboFournisseur.DataSource = null;
        cboFournisseur.DisplayMember = "Nom";
        cboFournisseur.ValueMember = "Id";
        cboFournisseur.DataSource = items;
        cboFournisseur.SelectedIndex = -1;
        if (previous.HasValue)
        {
            foreach (DataRow item in items.Rows)
            {
                if (Convert.ToInt32(item["Id"]) == previous.Value)
                {
                    cboFournisseur.SelectedValue = previous.Value;
                    break;
                }
            }
        }
    }

    private void OpenFournisseurs()
    {
        try
        {
            using var form = new FournisseursForm();
            form.ShowDialog(this);
            LoadFournisseurs();
            RefreshArticles(); // L'affichage des noms reflète les modifications SQL.
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossible d'ouvrir les fournisseurs : " + ex.Message,
                "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void ExporterCsv()
    {
        try
        {
            DataTable articles = StockDatabase.GetArticles(txtRecherche.Text);
            using var dialog = new SaveFileDialog
            {
                Title = "Exporter les articles vers Excel (CSV)",
                Filter = "Fichier CSV (*.csv)|*.csv",
                FileName = "Stock_Articles_" + DateTime.Now.ToString("yyyyMMdd") + ".csv",
                AddExtension = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            var csv = new StringBuilder();
            csv.AppendLine("Reference;Article;Fournisseur;Quantite;PrixAchatHT;PrixVenteHT;TVA;PrixVenteTTC");
            foreach (DataRow row in articles.Rows)
            {
                string[] cells =
                {
                    CsvCell(Convert.ToString(row["Reference"]) ?? ""),
                    CsvCell(Convert.ToString(row["Nom"]) ?? ""),
                    CsvCell(row["Fournisseur"] is DBNull ? "" : Convert.ToString(row["Fournisseur"]) ?? ""),
                    Convert.ToString(row["Quantite"]) ?? "0",
                    Convert.ToDecimal(row["PrixAchatHT"]).ToString("F3", French),
                    Convert.ToDecimal(row["PrixVenteHT"]).ToString("F3", French),
                    Convert.ToString(row["Tva"]) ?? "",
                    Convert.ToDecimal(row["PrixVenteTTC"]).ToString("F3", French)
                };
                csv.AppendLine(string.Join(";", cells));
            }
            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
            MessageBox.Show(this, "Export réussi : " + articles.Rows.Count + " article(s).",
                "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Export impossible : " + ex.Message, "Erreur CSV");
        }
    }

    private static string CsvCell(string value)
    {
        // Protection contre l'exécution de formules dans un tableur.
        string cleaned = value.TrimStart();
        if (cleaned.Length > 0 && "=+-@".Contains(cleaned[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

}

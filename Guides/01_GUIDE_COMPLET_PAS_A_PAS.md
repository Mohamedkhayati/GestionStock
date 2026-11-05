# GESTION DE STOCK PRO v2 — Guide de A à Z (FR + Darja)

## 0. Ce que vous avez téléchargé

Il s'agit du **projet source complet C# Windows Forms** (interfaces construites en C# directement), pas d'un logiciel Windows déjà compilé. L'application exige SQL Server Express. Vous devrez la compiler **sur Windows** une première fois avec Visual Studio 2026 ; le ZIP **ne contient pas** déjà le fichier .exe / Setup et ne contient pas SQL Server.

Fonctions :
- Articles : ajout, recherche, modification, suppression (avec confirmation) ; référence unique ; nom, quantité, prix achat HT, prix vente HT, TVA 7 % ou 19 % ; calcul du TTC dans la fenêtre + colonne calculée dans SQL.
- Fournisseurs : une **fenêtre indépendante** pour ajout / modification / suppression ; nom, téléphone, email, adresse, matricule fiscal, notes.
- Chaque article a un fournisseur à choisir dans un **ComboBox / DropDownList**, qui ne permet pas de saisir librement un fournisseur inconnu.
- Une relation SQL (clé étrangère) protège les données : un fournisseur associé à des articles ne peut pas être supprimé.
- Recherche aussi par **nom de fournisseur** ; export des articles en CSV (Excel) ; indicateurs totaux sur la barre inférieure.
- Paramètres de serveur dans `GestionStock/connection.settings.json`.
- Base locale gratuite SQL Server Express avec authentification Windows.
- Migration automatique **de la v1 si les anciennes tables sont déjà dans la base GestionStockDb**. Les anciens articles qui n'ont pas de fournisseur restent présents, sans association ; sélectionnez un fournisseur avant de modifier ces lignes.

## 1. Installer les 3 logiciels (une seule fois sur le PC de développement)

### A. Visual Studio 2026 Community
1. Ouvrir https://visualstudio.microsoft.com/fr/downloads/ .
2. Installer **Visual Studio 2026 Community**.
3. Dans Visual Studio Installer, cocher **Développement .NET Desktop** (en anglais: `.NET desktop development`).
4. Vérifier que **SDK .NET 10** / **.NET 10** est sélectionné.
5. Redémarrer si demandé.

### B. SQL Server Express 2022 ou 2025
1. Page officielle : https://www.microsoft.com/fr-fr/sql-server/sql-server-downloads .
2. Télécharger la version **Express** (gratuite).
3. Choisir une instance **SQLEXPRESS**. Exemple : nom serveur `MONPC\SQLEXPRESS` (sur le PC courant : `.\SQLEXPRESS`).
4. **Authentification Windows** : ajouter le compte Windows utilisé dans Visual Studio en tant qu'administrateur SQL lors de l'installation (ou lui donner les droits nécessaires via un administrateur).
5. Dans Services Windows (touche `Win+R` → `services.msc`), vérifier que le service `SQL Server (SQLEXPRESS)` est **En cours d'exécution** ; démarrage **Automatique** conseillé.

### C. SQL Server Management Studio (SSMS)
1. Télécharger SSMS : https://learn.microsoft.com/fr-fr/ssms/install/install .
2. Ouvrir SSMS.
3. Entrer le nom du serveur `.\SQLEXPRESS`, choisir **Authentification Windows**, cliquer **Se connecter**.
4. S'il n'y a pas de connexion, résoudre ce problème avant de lancer le logiciel.

## 2. Ouvrir et démarrer le logiciel dans Visual Studio

1. Décompresser `GestionStock_PRO_V2_Complet.zip` dans un dossier, ex. `C:\MesProjets\GestionStock_PRO_V2`.
2. Ouvrir **Visual Studio 2026**.
3. Choisir **Ouvrir un projet ou une solution**.
4. Sélectionner `GestionStock\GestionStock.csproj` dans le dossier décompressé.
5. La première fois, garder Internet pour **Restaurer les packages NuGet** (Microsoft.Data.SqlClient). Laisser Visual Studio terminer la restauration.
6. Dans `GestionStock\connection.settings.json`, garder par défaut :
   ```json
   { "Server": ".\\SQLEXPRESS" }
   ```
7. Cliquer sur la flèche verte **Démarrer** (ou `F5`).
8. **La base de données `GestionStockDb` est créée automatiquement** si votre compte possède les droits SQL. Les tables `Fournisseurs` et `Articles` sont créées et liées.
9. Si une ancienne version existe, une colonne `FournisseurId` est ajoutée **sans supprimer les anciens articles**. Faire une sauvegarde avant toute migration en situation réelle.

Si SQL renvoie une erreur de droits : ouvrir `SQL\01_INSTALLATION_ET_MIGRATION.sql` dans SSMS connecté avec un compte administrateur, **Exécuter**, puis relancer le logiciel avec un compte disposant des droits de lecture/écriture sur la base. Les mises à niveau nécessitent également des droits DDL.

## 3. Utilisation : fournisseurs PUIS articles (bech tfhem l'ordre)

### Etape A : ajouter un fournisseur
1. Dans la fenêtre **Articles**, cliquer **GÉRER LES FOURNISSEURS** (en haut à droite) ou **+ Ajouter / gérer un fournisseur**.
2. Dans la fenêtre **Gestion des Fournisseurs** :
   - Nom : `SPACENET`.
   - Téléphone : `71 000 000` (exemple fictif).
   - Email / adresse / matricule fiscal : facultatifs.
3. Cliquer **+ Ajouter**. La fiche apparaît dans le tableau.
4. Sélectionner la ligne pour **Modifier** ou **Supprimer**. Pour nouveau fournisseur, cliquer **Nouvelle fiche**.
5. Fermer la fenêtre avec **RETOUR AUX ARTICLES**.

### Etape B : associer le fournisseur à un article
1. Renseigner :
   - Référence : `SSD-500-001`
   - Nom : `SSD PNY 500 Go`
   - Fournisseur : choisir **SPACENET** dans la liste déroulante
   - Quantité : `20`
   - Prix achat HT : `100,000`
   - Prix vente HT : `150,000`
   - TVA : **19** ou **7**
2. Le **Prix vente TTC** apparaît automatiquement :
   - TVA 19 % → `178,500 DT`.
   - TVA 7 % → `160,500 DT`.
3. Cliquer **+ Ajouter**.
4. En haut de la liste, rechercher un article par **référence**, **nom** ou **fournisseur**.
5. Cliquer sur une ligne pour remplir le formulaire. Modifier et cliquer **Modifier**, ou cliquer **Supprimer** après confirmation.
6. Cliquer **Export CSV** pour obtenir un fichier ouvrable avec Excel (les articles actuellement filtrés).

### Etape C : liaison automatique dans SQL

**Important :** l'application n'enregistre pas `SPACENET` comme texte directement dans `Articles`. Elle y enregistre l'identifiant numérique SQL `FournisseurId`. Le libellé est obtenu par jointure `LEFT JOIN dbo.Fournisseurs`. Résultat : si vous renommez SPACENET en « SPACENET TUNISIE », les anciens articles afficheront le nouveau nom sans modification article par article.

Pour vérifier le lien dans SSMS :
```sql
USE GestionStockDb;
SELECT a.Reference, a.Nom AS Article, f.Nom AS Fournisseur,
       a.Quantite, a.PrixVenteHT, a.Tva, a.PrixVenteTTC
FROM dbo.Articles a
LEFT JOIN dbo.Fournisseurs f ON f.Id = a.FournisseurId
ORDER BY a.Nom;
```

## 4. Compiler en EXE pour installer sur un autre PC

1. Fermer le logiciel si ouvert.
2. Double-cliquer `INSTALLER_ET_PUBLIER_WIN64.bat` (dans le dossier racine du ZIP décompressé).
3. Attendre la fin de la commande `dotnet publish`.
4. Le résultat se trouve dans le dossier `Publication\` : `GestionStock.exe` et ses éventuels fichiers associés. **Copier tout le dossier** `Publication`, y compris `connection.settings.json`.
5. Sur le PC cible 64 bits, copier `Publication` et ouvrir `GestionStock.exe`.
6. L'EXE est publié avec le **runtime .NET intégré**, donc pas besoin d'installer Visual Studio ni le runtime .NET sur le PC cible, **mais SQL Server Express (ou un SQL Server distant) demeure obligatoire**.

## 5. Générer le SETUP avec un vrai assistant d'installation

Si vous souhaitez `Setup_GestionStock_PRO_V2.exe` :
1. Installer **Inno Setup** : https://jrsoftware.org/isinfo.php .
2. Publier d'abord avec `INSTALLER_ET_PUBLIER_WIN64.bat`.
3. Ouvrir `Installateur\GestionStock.iss` dans Inno Setup.
4. Menu **Build → Compile**.
5. Le fichier installateur sera dans `Installateur\Sortie\Setup_GestionStock_PRO_V2.exe`.
6. Ce SETUP crée le raccourci et copie l'EXE ; **il n'installe pas SQL Server Express**. Installer/configurer SQL Express séparément avant le premier lancement sur PC cible.

## 6. Copier vers un PC différent — deux scénarios

### Scenario A : stock indépendant sur chaque PC

- Sur le **PC B**, installer SQL Server Express, instance SQLEXPRESS.
- Copier `Publication` ou installer le SETUP.
- Au premier lancement, si la base ne se trouve pas sur le PC B, elle sera créée (vide).
- Pour emmener aussi les données : sur PC A, dans **SSMS** sauvegarder la base `GestionStockDb` dans un fichier `.bak` (clic droit base → Tâches → Sauvegarder). Sur PC B, restaurer ce fichier dans l'instance locale avant le premier démarrage du logiciel.
- **La copie de l'EXE seule ne transporte jamais les données.**

### Scenario B : même stock partagé entre PC A et PC B sur réseau local

- Installer SQL Server Express **uniquement sur PC A**, désigné serveur.
- Sur PC A, autoriser le protocole TCP/IP pour l'instance avec SQL Server Configuration Manager, configurer le port TCP selon le réseau, redémarrer le service et autoriser uniquement les clients du **réseau local** dans le pare-feu Windows. Sécuriser les droits SQL.
- Sur PC B, le fichier `connection.settings.json` peut indiquer `"Server": "MON-PC-A\\SQLEXPRESS"` si le nom d'instance est résolu.
- L'application est configurée avec **authentification Windows**. L'utilisateur du PC B doit donc disposer d'une identité Windows reconnue et de droits SQL sur PC A (en environnement domaine par exemple). Sur deux PC en comptes locaux isolés, il faudra une configuration d'authentification spécifique côté serveur et une adaptation du client si nécessaire.
- Ne **jamais** ouvrir SQL Server directement sur Internet ; utiliser un LAN de confiance ou un VPN correctement administré.

## 7. Si ça ne marche pas — dépannage

| Erreur | Explication / solution |
|---|---|
| Impossible de contacter SQL Server / Error 26 | `services.msc`, démarrer `SQL Server (SQLEXPRESS)` ; vérifier `Server` dans `connection.settings.json`. |
| Login failed / permission denied | Votre utilisateur Windows n'a pas de compte/droits SQL ; attribuer les droits via SSMS. |
| Erreur création base | Exécuter `SQL\01_INSTALLATION_ET_MIGRATION.sql` une seule fois avec un administrateur SQL. |
| Les packages NuGet ne se restaurent pas | Connecter Internet, vérifier le proxy et restaurer les packages dans Visual Studio. |
| Fournisseur absent de la liste | Créer d'abord dans Gestion des Fournisseurs, revenir sur Articles. |
| Impossible de supprimer fournisseur | Normal si des articles sont liés : modifier/supprimer les articles d'abord. |
| EXE ne se connecte pas sur PC B | SQL Express absent ou `Server`/droits incorrects. Copier `connection.settings.json`. |
| Quelques articles anciens sans fournisseur | Migration v1 : les anciens articles restent sans fournisseur. Les sélectionner puis leur choisir un fournisseur avant de sauvegarder. |

## 8. Sécurité et limites honnêtes

- Cette v2 est une application de **gestion des articles et fournisseurs**, pas un ERP complet : pas de factures, de sorties/entrées historisées, de comptes utilisateurs ni de transactions de caisse.
- Les prix sont stockés en **DECIMAL(18,3)** adaptés aux DT ; TVA limitée à 7 et 19 % comme demandé.
- Le mode Windows Authentication évite d'enregistrer un mot de passe SQL dans le projet.
- `TrustServerCertificate=true` facilite le test avec SQL Server Express **local seulement**. Pour un vrai réseau d'entreprise, installer un certificat SQL approuvé et réviser ce paramètre.
- Faire une **sauvegarde quotidienne** de la base de données avant de mettre en production. Sauvegarder aussi avant toute migration.
- Compiler et tester sur Windows avant de l'utiliser avec de vraies données ; le projet a été préparé dans un environnement sans .NET SDK ni SQL Server Windows et n'a donc pas été exécuté de bout en bout ici.

## 9. Fichiers du projet

- `GestionStock/StockForm.cs` : **interface Articles**.
- `GestionStock/FournisseursForm.cs` : **interface Fournisseurs**.
- `GestionStock/StockDatabase.cs` : toutes les requêtes SQL paramétrées, connexion, initialisation, migration.
- `GestionStock/Article.cs` et `GestionStock/Fournisseur.cs` : modèles métiers.
- `GestionStock/Program.cs` : point de démarrage.
- `GestionStock/connection.settings.json` : nom serveur SQL.
- `SQL/01_INSTALLATION_ET_MIGRATION.sql` : script SQL complet, compatible v1.
- `Tests/02_TEST_SQL_TRANSACTION.sql` : tests SQL transactionnels TVA + clé étrangère.
- `INSTALLER_ET_PUBLIER_WIN64.bat` : publication.
- `Installateur/GestionStock.iss` : configuration pour générer un setup via Inno Setup.

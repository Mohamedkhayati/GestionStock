GESTION DE STOCK PRO V2 - LIRE EN PREMIER
========================================

Projet C# Windows Forms .NET 10 + SQL Server Express.

1) Installer Visual Studio 2026 Community + workload Developpement .NET Desktop.
2) Installer SQL Server Express avec une instance SQLEXPRESS et droits Windows.
3) Decompresser le ZIP.
4) Ouvrir GestionStock\GestionStock.csproj dans Visual Studio.
5) Verifier GestionStock\connection.settings.json (par defaut .\SQLEXPRESS).
6) Cliquer F5 (restauration NuGet avec Internet lors du premier build).
7) Gérer les Fournisseurs en premier puis les Articles. Fournisseur obligatoire pour tout nouvel article.
8) Pour un EXE, executer INSTALLER_ET_PUBLIER_WIN64.bat.
9) Pour le Setup, installer Inno Setup et compiler Installateur\GestionStock.iss.

ATTENTION : Le ZIP contient des fichiers source, PAS un .exe deja compile,
PAS le moteur SQL Server ni un serveur deja installe.

Guide DETAILLE : Guides\01_GUIDE_COMPLET_PAS_A_PAS.md
Tests : Tests\CHECKLIST_MANUELLE.txt

Migration depuis v1 : sauvegarder GestionStockDb avant premier demarrage.
Le logiciel cree la table Fournisseurs et ajoute une colonne FournisseurId
sans effacer les articles existants. Ceux-ci restent sans fournisseur jusqu'a leur mise a jour.

Ce projet a ete prepare sans machine Windows / SQL Server pour compiler et tester.

@echo off
setlocal
cd /d "%~dp0"
echo ================================================
echo GESTION STOCK PRO V2 - PUBLICATION WINDOWS X64
echo ================================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERREUR: .NET 10 SDK n'est pas installe.
    echo Installez Visual Studio 2026 avec Developpement .NET desktop.
    pause
    exit /b 1
)
dotnet publish "GestionStock\GestionStock.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "Publication"
if errorlevel 1 (
    echo.
    echo ECHEC de publication. Lisez les erreurs affichees.
    pause
    exit /b 1
)
echo.
echo Les fichiers pour le PC cible sont dans: %CD%\Publication
echo Pour creer Setup_GestionStock.exe, ouvrir Installateur\GestionStock.iss
pause

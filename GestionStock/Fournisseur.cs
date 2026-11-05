namespace GestionStock;

internal sealed record Fournisseur(
    string Nom,
    string Telephone,
    string Email,
    string Adresse,
    string MatriculeFiscal,
    string Notes);

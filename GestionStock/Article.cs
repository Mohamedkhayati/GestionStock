namespace GestionStock;

internal sealed record Article(
    string Reference,
    string Nom,
    int Quantite,
    decimal PrixAchatHT,
    decimal PrixVenteHT,
    int Tva,
    int FournisseurId);

// Assets/Scripts/Entities/ColorState.cs
//
// Structure de données pure : représente l'état d'une entité (une LED virtuelle).
// Aucune logique ici, aucune référence réseau, aucune référence à Unity même
// (pas de MonoBehaviour) — c'est volontaire, ça reste testable et réutilisable
// partout (y compris plus tard côté export réseau).

public class ColorState
{
    public int Id;
    public byte R;
    public byte G;
    public byte B;

    public ColorState(int id, byte r = 0, byte g = 0, byte b = 0)
    {
        Id = id;
        R = r;
        G = g;
        B = b;
    }
}
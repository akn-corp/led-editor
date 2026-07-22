// Assets/Scripts/Timeline/PixelFont.cs
//
// Police matricielle 3x5 utilisee par l'effet "texte defilant".
// Chaque caractere est decrit par 5 lignes de 3 caracteres : '#' = LED allumee.
//
// BuildBand() pre-calcule une "bande" de bits (5 lignes x N colonnes) a partir
// d'un message. On fait ensuite defiler cette bande horizontalement. Ce
// pre-calcul evite de relire la police a chaque frame.

using System.Collections.Generic;

public static class PixelFont
{
    public const int GlyphHeight = 5;
    public const int GlyphWidth = 3;

    private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        { 'A', new[] { ".#.", "#.#", "###", "#.#", "#.#" } },
        { 'B', new[] { "##.", "#.#", "##.", "#.#", "##." } },
        { 'C', new[] { "###", "#..", "#..", "#..", "###" } },
        { 'D', new[] { "##.", "#.#", "#.#", "#.#", "##." } },
        { 'E', new[] { "###", "#..", "###", "#..", "###" } },
        { 'F', new[] { "###", "#..", "###", "#..", "#.." } },
        { 'G', new[] { "###", "#..", "#.#", "#.#", "###" } },
        { 'H', new[] { "#.#", "#.#", "###", "#.#", "#.#" } },
        { 'I', new[] { "###", ".#.", ".#.", ".#.", "###" } },
        { 'J', new[] { "..#", "..#", "..#", "#.#", "###" } },
        { 'K', new[] { "#.#", "#.#", "##.", "#.#", "#.#" } },
        { 'L', new[] { "#..", "#..", "#..", "#..", "###" } },
        { 'M', new[] { "#.#", "###", "###", "#.#", "#.#" } },
        { 'N', new[] { "#.#", "###", "###", "###", "#.#" } },
        { 'O', new[] { "###", "#.#", "#.#", "#.#", "###" } },
        { 'P', new[] { "###", "#.#", "###", "#..", "#.." } },
        { 'Q', new[] { "###", "#.#", "#.#", "###", "..#" } },
        { 'R', new[] { "##.", "#.#", "##.", "#.#", "#.#" } },
        { 'S', new[] { "###", "#..", "###", "..#", "###" } },
        { 'T', new[] { "###", ".#.", ".#.", ".#.", ".#." } },
        { 'U', new[] { "#.#", "#.#", "#.#", "#.#", "###" } },
        { 'V', new[] { "#.#", "#.#", "#.#", "#.#", ".#." } },
        { 'W', new[] { "#.#", "#.#", "###", "###", "#.#" } },
        { 'X', new[] { "#.#", "#.#", ".#.", "#.#", "#.#" } },
        { 'Y', new[] { "#.#", "#.#", ".#.", ".#.", ".#." } },
        { 'Z', new[] { "###", "..#", ".#.", "#..", "###" } },
        { '0', new[] { "###", "#.#", "#.#", "#.#", "###" } },
        { '1', new[] { ".#.", "##.", ".#.", ".#.", "###" } },
        { '2', new[] { "###", "..#", "###", "#..", "###" } },
        { '3', new[] { "###", "..#", "###", "..#", "###" } },
        { '4', new[] { "#.#", "#.#", "###", "..#", "..#" } },
        { '5', new[] { "###", "#..", "###", "..#", "###" } },
        { '6', new[] { "###", "#..", "###", "#.#", "###" } },
        { '7', new[] { "###", "..#", "..#", "..#", "..#" } },
        { '8', new[] { "###", "#.#", "###", "#.#", "###" } },
        { '9', new[] { "###", "#.#", "###", "..#", "###" } },
        { '!', new[] { ".#.", ".#.", ".#.", "...", ".#." } },
        { '-', new[] { "...", "...", "###", "...", "..." } },
        { ' ', new[] { "...", "...", "...", "...", "..." } },
    };

    /// <summary>
    /// Bande de bits d'un message. Indexation : band[ligne][colonne].
    /// Largeur = 4 px par caractere (3 de glyphe + 1 d'espacement).
    /// </summary>
    public static bool[][] BuildBand(string message)
    {
        if (string.IsNullOrEmpty(message)) message = " ";
        message = message.ToUpperInvariant();

        var rows = new List<bool>[GlyphHeight];
        for (int r = 0; r < GlyphHeight; r++) rows[r] = new List<bool>();

        foreach (char c in message)
        {
            if (!Glyphs.TryGetValue(c, out string[] glyph))
                glyph = Glyphs[' '];

            for (int r = 0; r < GlyphHeight; r++)
            {
                foreach (char pixel in glyph[r])
                    rows[r].Add(pixel == '#');

                rows[r].Add(false); // colonne d'espacement entre les lettres
            }
        }

        var band = new bool[GlyphHeight][];
        for (int r = 0; r < GlyphHeight; r++) band[r] = rows[r].ToArray();
        return band;
    }
}

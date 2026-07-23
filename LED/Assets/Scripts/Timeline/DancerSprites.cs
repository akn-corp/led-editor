// Assets/Scripts/Timeline/DancerSprites.cs
//
// Silhouettes de danseurs en pixel-art (11 x 19), et construction du parcours
// de dalles qui dessine un mot.
//
// Les poses alternent sur le tempo pour donner l'illusion du mouvement. Elles
// sont volontairement simples : a cette resolution, une silhouette lisible vaut
// mieux qu'un dessin detaille qui devient une bouillie de pixels.

using System.Collections.Generic;

public static class DancerSprites
{
    public const int Width = 11;
    public const int Height = 19;

    // Pose 0 — bras leves en V, jambes ecartees.
    private static readonly string[] PoseArmsUp =
    {
        "#.........#",
        "#...###...#",
        ".#..###..#.",
        ".#..###..#.",
        "..#.###.#..",
        "...#####...",
        "...#####...",
        "...#####...",
        "...#####...",
        "....###....",
        "....###....",
        "...##.##...",
        "...#...#...",
        "...#...#...",
        "..#.....#..",
        "..#.....#..",
        "..#.....#..",
        ".##.....##.",
        ".##.....##.",
    };

    // Pose 1 — bras a l'horizontale, fente laterale.
    private static readonly string[] PoseArmsWide =
    {
        "...........",
        "....###....",
        "....###....",
        "....###....",
        "...........",
        "###########",
        "...#####...",
        "...#####...",
        "...#####...",
        "....###....",
        "....###....",
        "...##.##...",
        "..#....##..",
        "..#.....#..",
        ".#......#..",
        ".#.......#.",
        "#........#.",
        "##.......##",
        "##.......##",
    };

    // Pose 2 — bras le long du corps, position resserree.
    private static readonly string[] PoseNeutral =
    {
        "...........",
        "...........",
        "....###....",
        "....###....",
        "....###....",
        "..#######..",
        "..#######..",
        "..#######..",
        "...#####...",
        "...#####...",
        "....###....",
        "...##.##...",
        "...#...#...",
        "...#...#...",
        "..##...##..",
        "..#.....#..",
        "..#.....#..",
        ".##.....##.",
        ".##.....##.",
    };

    private static readonly string[][] Poses = { PoseArmsUp, PoseArmsWide, PoseNeutral };

    public static int PoseCount => Poses.Length;

    /// <summary>
    /// Le pixel (x, y) de la pose est-il allume ? y = 0 correspond aux pieds,
    /// pour coller au repere du mur ou la ligne 0 est en bas.
    /// </summary>
    public static bool IsFilled(int poseIndex, int x, int yFromFeet)
    {
        if (x < 0 || x >= Width || yFromFeet < 0 || yFromFeet >= Height) return false;

        string[] pose = Poses[((poseIndex % Poses.Length) + Poses.Length) % Poses.Length];
        return pose[Height - 1 - yFromFeet][x] == '#';
    }

    /// <summary>
    /// Transforme la bande de texte d'un mot en parcours de dalles.
    ///
    /// L'ordre de remplissage part du bas et remonte, en balayage alterne
    /// (gauche-droite, puis droite-gauche) : les danseurs font des allers-
    /// retours et "impriment" le mot ligne par ligne.
    /// </summary>
    public static void BuildTrail(
        bool[][] band,
        out int[] tileX,
        out int[] tileY,
        out int[][] order,
        out int cols,
        out int rows)
    {
        rows = band.Length;
        cols = band[0].Length;

        order = new int[rows][];
        for (int y = 0; y < rows; y++)
        {
            order[y] = new int[cols];
            for (int x = 0; x < cols; x++) order[y][x] = -1;
        }

        var xs = new List<int>();
        var ys = new List<int>();

        for (int yFromBottom = 0; yFromBottom < rows; yFromBottom++)
        {
            // La bande est stockee du haut vers le bas : on inverse.
            int sourceRow = rows - 1 - yFromBottom;
            bool leftToRight = (yFromBottom % 2 == 0);

            for (int step = 0; step < cols; step++)
            {
                int x = leftToRight ? step : cols - 1 - step;
                if (!band[sourceRow][x]) continue;

                order[yFromBottom][x] = xs.Count;
                xs.Add(x);
                ys.Add(yFromBottom);
            }
        }

        tileX = xs.ToArray();
        tileY = ys.ToArray();
    }
}

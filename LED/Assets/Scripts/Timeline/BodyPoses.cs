// Assets/Scripts/Timeline/BodyPoses.cs
//
// Silhouettes humaines construites a partir d'un squelette articule plutot
// qu'a partir de pixel-art. Chaque os est un segment epais : on teste la
// distance du pixel au segment, ce qui donne des membres lisses, sans escalier,
// et une silhouette qui reste propre a n'importe quelle taille.
//
// Les cinq poses sont acrobatiques et dessinent chacune une lettre du mot :
//   0 = H   bras leves, jambes ecartees   (deux montants + le torse en barre)
//   1 = E   colonne a gauche, trois branches vers la droite
//   2 = T   bras a l'horizontale, jambes serrees
//   3 = I   corps ramasse a la verticale
//   4 = C   corps cambre en arc, ouverture a droite
//
// Repere de travail : x vers la droite (environ -0.42 a +0.42), y de 0 aux
// pieds a 1 au sommet du crane. Le repere est isotrope : une distance
// horizontale et une distance verticale ont la meme echelle.

using UnityEngine;

public static class BodyPoses
{
    public const int PoseCount = 5;

    private const float TorsoThickness = 0.075f;
    private const float UpperLimbThickness = 0.042f;
    private const float LowerLimbThickness = 0.034f;

    private struct Bone
    {
        public Vector2 A;
        public Vector2 B;
        public float Thickness;

        public Bone(Vector2 a, Vector2 b, float thickness)
        {
            A = a; B = b; Thickness = thickness;
        }
    }

    private struct Figure
    {
        public Bone[] Bones;
        public Vector2 Head;
        public float HeadRadius;
    }

    private static readonly Figure[] Figures = BuildFigures();

    // Silhouette interpolee du moment. Recalculee une seule fois par frame,
    // pas a chaque pixel : les parametres de melange sont identiques pour
    // toute la trame.
    private static Figure _blended;
    private static int _cachedA = -1, _cachedB = -1;
    private static float _cachedT = -1f;

    /// <summary>
    /// Silhouette a mi-chemin entre deux poses. C'est ce qui donne le
    /// mouvement : le corps glisse d'une lettre a l'autre au lieu de sauter.
    /// Appeler PrepareBlend() une fois par frame, puis IsInsideBlend() par pixel.
    /// </summary>
    public static void PrepareBlend(int poseA, int poseB, float t)
    {
        int a = ((poseA % PoseCount) + PoseCount) % PoseCount;
        int b = ((poseB % PoseCount) + PoseCount) % PoseCount;
        t = Mathf.Clamp01(t);

        if (a == _cachedA && b == _cachedB && Mathf.Abs(t - _cachedT) < 0.002f) return;

        _cachedA = a; _cachedB = b; _cachedT = t;

        Figure fa = Figures[a];
        Figure fb = Figures[b];

        if (_blended.Bones == null || _blended.Bones.Length != fa.Bones.Length)
            _blended.Bones = new Bone[fa.Bones.Length];

        _blended.Head = Vector2.Lerp(fa.Head, fb.Head, t);
        _blended.HeadRadius = Mathf.Lerp(fa.HeadRadius, fb.HeadRadius, t);

        for (int i = 0; i < fa.Bones.Length; i++)
        {
            _blended.Bones[i] = new Bone(
                Vector2.Lerp(fa.Bones[i].A, fb.Bones[i].A, t),
                Vector2.Lerp(fa.Bones[i].B, fb.Bones[i].B, t),
                fa.Bones[i].Thickness);
        }
    }

    /// <summary>Test d'appartenance sur la silhouette preparee par PrepareBlend().</summary>
    public static bool IsInsideBlend(float x, float y)
    {
        return IsInsideFigure(_blended, x, y);
    }

    /// <summary>
    /// Couverture douce de la silhouette preparee : 1 bien a l'interieur, 0
    /// dehors, avec une transition lisse sur une bande de largeur "edge".
    /// C'est ce qui supprime l'escalier sur les bords — l'anticrénelage.
    /// </summary>
    public static float CoverageBlend(float x, float y, float edge)
    {
        float d = SignedDistance(_blended, x, y);   // <0 dedans, >0 dehors
        if (edge <= 1e-5f) return d <= 0f ? 1f : 0f;
        return Mathf.Clamp01(0.5f - d / (2f * edge));
    }

    /// <summary>
    /// Distance signee au bord de la silhouette : negative a l'interieur,
    /// positive a l'exterieur. Sert a lisser les contours.
    /// </summary>
    private static float SignedDistance(Figure figure, float x, float y)
    {
        if (figure.Bones == null) return 1f;

        var p = new Vector2(x, y);
        float best = float.MaxValue;

        float dHead = (p - figure.Head).magnitude - figure.HeadRadius;
        if (dHead < best) best = dHead;

        for (int i = 0; i < figure.Bones.Length; i++)
        {
            Bone bone = figure.Bones[i];
            float d = DistanceToSegment(p, bone.A, bone.B) - bone.Thickness;
            if (d < best) best = d;
        }

        return best;
    }

    /// <summary>
    /// Le point (x, y) est-il a l'interieur de la silhouette ?
    /// x est suppose exprime dans le meme repere que y (isotrope).
    /// </summary>
    public static bool IsInside(int poseIndex, float x, float y)
    {
        int index = ((poseIndex % PoseCount) + PoseCount) % PoseCount;
        return IsInsideFigure(Figures[index], x, y);
    }

    private static bool IsInsideFigure(Figure figure, float x, float y)
    {
        if (figure.Bones == null) return false;

        // Tete : simple disque.
        float dxHead = x - figure.Head.x;
        float dyHead = y - figure.Head.y;
        if (dxHead * dxHead + dyHead * dyHead <= figure.HeadRadius * figure.HeadRadius)
            return true;

        var point = new Vector2(x, y);
        for (int i = 0; i < figure.Bones.Length; i++)
        {
            Bone bone = figure.Bones[i];
            if (DistanceToSegment(point, bone.A, bone.B) <= bone.Thickness)
                return true;
        }

        return false;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;
        if (lengthSquared < 1e-6f) return (p - a).magnitude;

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared);
        return (p - (a + ab * t)).magnitude;
    }

    private static Figure Build(
        Vector2 head, float headRadius,
        Vector2 neck, Vector2 hip,
        Vector2 shoulderL, Vector2 elbowL, Vector2 handL,
        Vector2 shoulderR, Vector2 elbowR, Vector2 handR,
        Vector2 kneeL, Vector2 footL,
        Vector2 kneeR, Vector2 footR)
    {
        return new Figure
        {
            Head = head,
            HeadRadius = headRadius,
            Bones = new[]
            {
                new Bone(neck, hip, TorsoThickness),
                new Bone(shoulderL, shoulderR, UpperLimbThickness),
                new Bone(shoulderL, elbowL, UpperLimbThickness),
                new Bone(elbowL, handL, LowerLimbThickness),
                new Bone(shoulderR, elbowR, UpperLimbThickness),
                new Bone(elbowR, handR, LowerLimbThickness),
                new Bone(hip, kneeL, UpperLimbThickness),
                new Bone(kneeL, footL, LowerLimbThickness),
                new Bone(hip, kneeR, UpperLimbThickness),
                new Bone(kneeR, footR, LowerLimbThickness),
            }
        };
    }

    private static Figure[] BuildFigures()
    {
        var figures = new Figure[PoseCount];

        // ---- H : bras tendus vers le haut, jambes ecartees ----
        figures[0] = Build(
            head: new Vector2(0f, 0.83f), headRadius: 0.085f,
            neck: new Vector2(0f, 0.72f), hip: new Vector2(0f, 0.42f),
            shoulderL: new Vector2(-0.08f, 0.70f),
            elbowL: new Vector2(-0.19f, 0.79f),
            handL: new Vector2(-0.20f, 1.00f),
            shoulderR: new Vector2(0.08f, 0.70f),
            elbowR: new Vector2(0.19f, 0.79f),
            handR: new Vector2(0.20f, 1.00f),
            kneeL: new Vector2(-0.18f, 0.24f), footL: new Vector2(-0.20f, 0.02f),
            kneeR: new Vector2(0.18f, 0.24f), footR: new Vector2(0.20f, 0.02f));

        // ---- E : colonne a gauche, trois branches vers la droite ----
        figures[1] = Build(
            head: new Vector2(-0.20f, 0.85f), headRadius: 0.080f,
            neck: new Vector2(-0.20f, 0.74f), hip: new Vector2(-0.20f, 0.40f),
            shoulderL: new Vector2(-0.26f, 0.72f),
            elbowL: new Vector2(-0.06f, 0.73f),
            handL: new Vector2(0.15f, 0.73f),
            shoulderR: new Vector2(-0.14f, 0.72f),
            elbowR: new Vector2(-0.02f, 0.58f),
            handR: new Vector2(0.13f, 0.55f),
            kneeL: new Vector2(-0.20f, 0.21f), footL: new Vector2(-0.20f, 0.02f),
            kneeR: new Vector2(-0.05f, 0.14f), footR: new Vector2(0.15f, 0.08f));

        // ---- T : bras a l'horizontale, jambes serrees ----
        figures[2] = Build(
            head: new Vector2(0f, 0.84f), headRadius: 0.085f,
            neck: new Vector2(0f, 0.73f), hip: new Vector2(0f, 0.42f),
            shoulderL: new Vector2(-0.07f, 0.71f),
            elbowL: new Vector2(-0.19f, 0.71f),
            handL: new Vector2(-0.37f, 0.71f),
            shoulderR: new Vector2(0.07f, 0.71f),
            elbowR: new Vector2(0.19f, 0.71f),
            handR: new Vector2(0.37f, 0.71f),
            kneeL: new Vector2(-0.04f, 0.22f), footL: new Vector2(-0.04f, 0.02f),
            kneeR: new Vector2(0.04f, 0.22f), footR: new Vector2(0.04f, 0.02f));

        // ---- I : corps ramasse, tout vertical ----
        figures[3] = Build(
            head: new Vector2(0f, 0.84f), headRadius: 0.085f,
            neck: new Vector2(0f, 0.73f), hip: new Vector2(0f, 0.42f),
            shoulderL: new Vector2(-0.06f, 0.71f),
            elbowL: new Vector2(-0.09f, 0.56f),
            handL: new Vector2(-0.08f, 0.38f),
            shoulderR: new Vector2(0.06f, 0.71f),
            elbowR: new Vector2(0.09f, 0.56f),
            handR: new Vector2(0.08f, 0.38f),
            kneeL: new Vector2(-0.035f, 0.22f), footL: new Vector2(-0.035f, 0.02f),
            kneeR: new Vector2(0.035f, 0.22f), footR: new Vector2(0.035f, 0.02f));

        // ---- C : corps en croissant, bras et jambes superposes ----
        // Les deux bras sont presque confondus, les deux jambes aussi : les
        // extremites forment des traits nets plutot que des fourches, ce qui
        // est indispensable pour que l'arc se lise comme une lettre.
        figures[4] = Build(
            head: new Vector2(0.04f, 0.81f), headRadius: 0.075f,
            neck: new Vector2(-0.03f, 0.68f), hip: new Vector2(-0.03f, 0.34f),
            shoulderL: new Vector2(-0.07f, 0.66f),
            elbowL: new Vector2(0.05f, 0.80f),
            handL: new Vector2(0.22f, 0.85f),
            shoulderR: new Vector2(0.01f, 0.66f),
            elbowR: new Vector2(0.07f, 0.78f),
            handR: new Vector2(0.24f, 0.83f),
            kneeL: new Vector2(-0.02f, 0.18f), footL: new Vector2(0.20f, 0.09f),
            kneeR: new Vector2(0.01f, 0.16f), footR: new Vector2(0.22f, 0.07f));

        return figures;
    }
}

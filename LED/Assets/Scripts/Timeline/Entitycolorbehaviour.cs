// Assets/Scripts/Timeline/EntityColorBehaviour.cs
//
// Contient les données d'un clip EntityColor pendant qu'il est joué : quelle
// plage d'entités il cible, et quelle couleur il applique.

using UnityEngine;
using UnityEngine.Playables;

public class EntityColorBehaviour : PlayableBehaviour
{
    public int firstEntityId;
    public int entityCount = 1;
    public Color32 color = new Color32(255, 255, 255, 255);
}
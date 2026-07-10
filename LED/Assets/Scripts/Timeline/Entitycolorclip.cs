// Assets/Scripts/Timeline/EntityColorClip.cs
//
// Représente un clip posé sur la timeline. Les champs ci-dessous sont
// visibles et modifiables directement dans l'Inspector Unity quand tu
// cliques sur un clip dans la fenêtre Timeline.

using UnityEngine;
using UnityEngine.Playables;

public class EntityColorClip : PlayableAsset
{
    public int firstEntityId;
    [Min(1)] public int entityCount = 1;
    public Color color = Color.red;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<EntityColorBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.firstEntityId = firstEntityId;
        behaviour.entityCount = entityCount;
        behaviour.color = color;
        return playable;
    }
}
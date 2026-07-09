// Assets/Scripts/Timeline/EntityColorMixerBehaviour.cs
//
// Le "chef d'orchestre" de la piste : à chaque frame, Unity l'appelle avec
// tous les clips actuellement actifs (leurs "inputs"). Pour chaque clip
// actif (poids > 0), on applique sa couleur aux entités qu'il cible.
//
// Règle de fusion actuelle si 2 clips se superposent : le dernier input
// traité "gagne" (écrase la couleur du précédent sur les mêmes entités).
// À affiner plus tard si vous voulez un vrai blending (mélange pondéré).

using UnityEngine;
using UnityEngine.Playables;

public class EntityColorMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var entityManager = playerData as EntityManager;
        if (entityManager == null) return;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue; // ce clip n'est pas actif à cet instant

            var inputPlayable = (ScriptPlayable<EntityColorBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();

            for (int id = behaviour.firstEntityId; id < behaviour.firstEntityId + behaviour.entityCount; id++)
            {
                entityManager.SetColor(id, behaviour.color.r, behaviour.color.g, behaviour.color.b);
            }
        }
    }
}
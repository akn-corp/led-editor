// Détient toutes les entités (LED virtuelles) et leur couleur courante.
// C'est LE point central que Dev 2 (timeline) va appeler pour modifier les
// couleurs, et que l'export réseau va lire pour construire l'état à envoyer.
//
// Règle absolue rappelée : cette classe ne connaît RIEN du réseau, ni d'IP,
// ni d'univers DMX. Elle ne manipule que des identifiants d'entités abstraits.

using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    // Toutes les entités connues, indexées par identifiant.
    private readonly Dictionary<int, ColorState> _entities = new Dictionary<int, ColorState>();

    // Déclenché à chaque fois qu'une couleur change, avec l'id concerné.
    // Le rendu 3D (Dev 1) et l'export réseau (Dev 2) s'abonnent à cet événement
    // au lieu de vérifier en boucle si quelque chose a changé.
    public event Action<int> OnColorChanged;

    /// <summary>
    /// Enregistre une nouvelle entité (à appeler une fois au chargement de
    /// l'installation, avant toute animation).
    /// </summary>
    public void RegisterEntity(int id)
    {
        if (!_entities.ContainsKey(id))
        {
            _entities[id] = new ColorState(id);
        }
    }

    /// <summary>
    /// Modifie la couleur d'une entité existante. Ne fait rien silencieusement
    /// si l'entité n'a jamais été enregistrée (évite de planter sur un id
    /// invalide venant d'un clip mal configuré).
    /// </summary>
    public void SetColor(int id, byte r, byte g, byte b)
    {
        if (!_entities.TryGetValue(id, out var state))
        {
            Debug.LogWarning($"[EntityManager] Entité {id} inconnue — ignorée.");
            return;
        }

        state.R = r;
        state.G = g;
        state.B = b;

        OnColorChanged?.Invoke(id);
    }

    public ColorState GetColor(int id)
    {
        _entities.TryGetValue(id, out var state);
        return state;
    }

    public IEnumerable<int> AllEntityIds => _entities.Keys;
}
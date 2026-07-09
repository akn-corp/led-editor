// Assets/Scripts/Network/StateExporter.cs
//
// S'abonne aux changements de couleur de EntityManager, accumule les
// entités modifiées, et envoie un seul paquet UDP toutes les ~25ms (40Hz)
// contenant uniquement ce qui a changé depuis le dernier envoi.
//
// Pour l'instant, cible un script d'écoute local de test (127.0.0.1), en
// attendant que le vrai logiciel de routage existe.

using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class StateExporter : MonoBehaviour
{
    [SerializeField] private EntityManager entityManager;

    [Header("Cible réseau (test local pour l'instant)")]
    [SerializeField] private string targetIp = "127.0.0.1";
    [SerializeField] private int targetPort = 9999; // volontairement différent du port ArtNet (6454)

    [SerializeField] private float sendIntervalSeconds = 1f / 40f; // ~40Hz

    private readonly HashSet<int> _changedEntityIds = new HashSet<int>();
    private UdpClient _client;
    private IPEndPoint _endpoint;
    private float _timer;
    private int _seq;

    void OnEnable()
    {
        if (entityManager == null)
        {
            Debug.LogError("[StateExporter] EntityManager manquant dans l'Inspector.");
            enabled = false;
            return;
        }

        entityManager.OnColorChanged += OnEntityColorChanged;
        _client = new UdpClient();
        _endpoint = new IPEndPoint(IPAddress.Parse(targetIp), targetPort);
    }

    void OnDisable()
    {
        if (entityManager != null)
            entityManager.OnColorChanged -= OnEntityColorChanged;

        _client?.Close();
    }

    private void OnEntityColorChanged(int id)
    {
        _changedEntityIds.Add(id);
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < sendIntervalSeconds) return;
        _timer = 0f;

        if (_changedEntityIds.Count == 0) return; // rien à envoyer ce tour-ci

        var message = new StateUpdateMessage
        {
            seq = _seq++,
            entities = BuildEntityList()
        };

        string json = JsonUtility.ToJson(message);
        byte[] data = Encoding.UTF8.GetBytes(json);
        _client.Send(data, data.Length, _endpoint);

        Debug.Log($"[StateExporter] Envoyé seq={message.seq}, {message.entities.Length} entité(s) : {json}");

        _changedEntityIds.Clear();
    }

    private StateEntityData[] BuildEntityList()
    {
        var list = new StateEntityData[_changedEntityIds.Count];
        int i = 0;
        foreach (var id in _changedEntityIds)
        {
            var state = entityManager.GetColor(id);
            if (state == null) continue;

            list[i++] = new StateEntityData { id = id, r = state.R, g = state.G, b = state.B };
        }
        return list;
    }
}
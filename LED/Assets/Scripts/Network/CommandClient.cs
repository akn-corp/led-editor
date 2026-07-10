// Client TCP pour le canal Commandes (START_SHOW, STOP_SHOW, PAUSE, RESUME...).
// Contrairement au canal État (UDP, tolérant à la perte), ce canal doit être
// fiable : on gère la reconnexion automatique si le serveur (futur routage)
// n'est pas encore lancé.
//
// Framing des messages : chaque message JSON est suivi d'un '\n' (TCP est un
// flux continu, il faut un séparateur pour savoir où un message se termine).

using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class CommandClient : MonoBehaviour
{
    [Header("Cible réseau (serveur de test local pour l'instant)")]
    [SerializeField] private string serverIp = "127.0.0.1";
    [SerializeField] private int serverPort = 9998; // distinct du port UDP (9999) et d'ArtNet (6454)
    [SerializeField] private float reconnectDelaySeconds = 3f;

    [Header("Test manuel (touches clavier, à retirer plus tard)")]
    [SerializeField] private bool enableKeyboardTestShortcuts = true;

    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private Thread _readThread;
    private volatile bool _connected;
    private volatile bool _shouldStop;
    private int _messageCounter;

    void Start()
    {
        StartCoroutine(ConnectLoop());
    }

    void Update()
    {
        if (!enableKeyboardTestShortcuts) return;

        // Raccourcis manuels pour tester sans attendre que Dev 2 branche
        // les vrais boutons UI de start/stop. Utilise le nouveau Input
        // System (Keyboard.current), pas l'ancienne API UnityEngine.Input.
        var keyboard = Keyboard.current;
        if (keyboard == null) return; // pas de clavier détecté (rare, sécurité)

        if (keyboard.sKey.wasPressedThisFrame) SendCommand("START_SHOW");
        if (keyboard.xKey.wasPressedThisFrame) SendCommand("STOP_SHOW");
        if (keyboard.pKey.wasPressedThisFrame) SendCommand("PAUSE");
        if (keyboard.rKey.wasPressedThisFrame) SendCommand("RESUME");
    }

    private IEnumerator ConnectLoop()
    {
        while (!_shouldStop)
        {
            if (!_connected)
            {
                TryConnect();
            }
            yield return new WaitForSeconds(reconnectDelaySeconds);
        }
    }

    private void TryConnect()
    {
        try
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(serverIp, serverPort);
            _stream = _tcpClient.GetStream();
            _connected = true;

            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();

            Debug.Log($"[CommandClient] Connecté à {serverIp}:{serverPort}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CommandClient] Connexion échouée ({e.Message}), nouvelle tentative dans {reconnectDelaySeconds}s.");
            _connected = false;
        }
    }

    /// <summary>
    /// Envoie une commande de haut niveau. Ne fait rien (avec un log) si pas
    /// connecté — ne plante jamais l'appelant.
    /// </summary>
    public void SendCommand(string type, string payloadJson = "{}")
    {
        if (!_connected || _stream == null)
        {
            Debug.LogWarning($"[CommandClient] Impossible d'envoyer {type} — non connecté.");
            return;
        }

        var message = new CommandMessage
        {
            type = type,
            id = $"cmd-{_messageCounter++:0000}",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            payload = payloadJson
        };

        string json = JsonUtility.ToJson(message);
        byte[] data = Encoding.UTF8.GetBytes(json + "\n");

        try
        {
            _stream.Write(data, 0, data.Length);
            Debug.Log($"[CommandClient] Envoyé : {json}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CommandClient] Échec d'envoi ({e.Message}) — connexion perdue.");
            _connected = false;
        }
    }

    private void ReadLoop()
    {
        var buffer = new byte[4096];
        var accumulated = new StringBuilder();

        try
        {
            while (!_shouldStop && _connected)
            {
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // connexion fermée par le serveur

                accumulated.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                // Découpe sur les '\n' — plusieurs messages peuvent arriver groupés.
                string content = accumulated.ToString();
                int newlineIndex;
                while ((newlineIndex = content.IndexOf('\n')) >= 0)
                {
                    string line = content.Substring(0, newlineIndex);
                    content = content.Substring(newlineIndex + 1);
                    if (!string.IsNullOrWhiteSpace(line))
                        HandleIncomingMessage(line);
                }
                accumulated.Clear();
                accumulated.Append(content);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CommandClient] Lecture interrompue ({e.Message}).");
        }

        _connected = false;
    }

    private void HandleIncomingMessage(string json)
    {
        // Note : ce log vient d'un thread séparé — c'est acceptable pour un
        // simple Debug.Log, mais évitez de toucher directement des objets
        // Unity (Transform, etc.) depuis ce thread sans passer par la
        // boucle principale.
        Debug.Log($"[CommandClient] Reçu : {json}");
    }

    void OnDestroy()
    {
        _shouldStop = true;
        try { _stream?.Close(); } catch { }
        try { _tcpClient?.Close(); } catch { }
        _readThread?.Join(500);
    }
}
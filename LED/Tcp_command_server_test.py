"""
Serveur TCP de test — diagnostic uniquement.

Objectif : recevoir les commandes envoyées par CommandClient (Unity) et
répondre ACK puis DONE, pour valider le canal Commandes AVANT que le vrai
logiciel de routage existe.

Usage :
    python tcp_command_server_test.py
"""

import socket
import json

LISTEN_IP = "127.0.0.1"
LISTEN_PORT = 9998  # doit correspondre à serverPort dans CommandClient.cs


def handle_client(conn, addr):
    print(f"Client connecté : {addr}")
    buffer = ""

    with conn:
        while True:
            data = conn.recv(4096)
            if not data:
                print(f"Client déconnecté : {addr}")
                break

            buffer += data.decode("utf-8")

            while "\n" in buffer:
                line, buffer = buffer.split("\n", 1)
                if not line.strip():
                    continue

                try:
                    message = json.loads(line)
                except json.JSONDecodeError:
                    print(f"Message non-JSON ignoré : {line}")
                    continue

                print(f"Commande reçue : {message}")

                # Répond ACK puis DONE, comme le ferait le vrai routage.
                for response_type in ("ACK", "DONE"):
                    response = {
                        "type": response_type,
                        "refId": message.get("id", "unknown"),
                    }
                    payload = (json.dumps(response) + "\n").encode("utf-8")
                    conn.sendall(payload)
                    print(f"  -> Réponse envoyée : {response}")


def main():
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((LISTEN_IP, LISTEN_PORT))
    server.listen(1)

    print(f"Serveur de test en écoute sur {LISTEN_IP}:{LISTEN_PORT} — Ctrl+C pour arrêter.\n")

    try:
        while True:
            conn, addr = server.accept()
            handle_client(conn, addr)
    except KeyboardInterrupt:
        print("\nArrêt du serveur.")
    finally:
        server.close()


if __name__ == "__main__":
    main()
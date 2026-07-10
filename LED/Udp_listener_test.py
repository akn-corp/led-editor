"""
Écouteur UDP local — diagnostic uniquement.

Objectif : recevoir et afficher les paquets envoyés par StateExporter (Unity),
pour valider le format et la fréquence AVANT que le vrai logiciel de routage
existe. Ne fait aucune traduction ArtNet, juste un affichage brut.

Usage :
    python udp_listener_test.py
"""

import socket
import time

LISTEN_IP = "127.0.0.1"
LISTEN_PORT = 9999  # doit correspondre à targetPort dans StateExporter.cs

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.bind((LISTEN_IP, LISTEN_PORT))

print(f"En écoute sur {LISTEN_IP}:{LISTEN_PORT} — Ctrl+C pour arrêter.\n")

count = 0
last_time = time.time()

try:
    while True:
        data, addr = sock.recvfrom(65536)
        count += 1
        now = time.time()
        elapsed = now - last_time
        last_time = now

        print(f"[{count}] depuis {addr}, {len(data)} octets, "
              f"{elapsed*1000:.1f} ms depuis le précédent")
        print(f"    {data.decode('utf-8')}\n")

except KeyboardInterrupt:
    print("\nArrêt de l'écoute.")
finally:
    sock.close()
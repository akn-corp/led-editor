# 👤 DEV 2 — Timeline, clips & export réseau (Animation)

**Rôle :** composer l'animation dans le temps (clips, illuminateurs, sync audio) et envoyer l'état résultant au logiciel de routage.

---

## 1. Ton rôle en une phrase
Tu es responsable de la logique temporelle du spectacle : décider quoi afficher, à quel
moment, et transmettre le résultat au logiciel de routage.

## 2. Pourquoi c'est crucial
Le logiciel d'animation et le logiciel de routage sont deux projets séparés (deux
repos), il ne faut donc pas d'appel direct implicite. Il faut un contrat clair, mais
attention : **il y a deux canaux différents, pas un seul protocole générique.**

## 3. Le problème à résoudre
Le système doit pouvoir :
- envoyer en continu l'état des entités modifiées (canal État) ;
- envoyer ponctuellement des commandes de haut niveau (canal Commandes) ;
- recevoir les confirmations/erreurs du routage ;
- gérer les pertes de paquets sans casser le spectacle (le canal État tolère la perte,
  contrairement à ce que suggérait la version originale pour tous les messages).

## 4. L'architecture recommandée

### Canal 1 — Commandes (TCP)
Pour les événements rares et importants : `START_SHOW`, `STOP_SHOW`, `PAUSE`, `RESUME`,
`GET_STATUS`. Doit être fiable, une perte est inacceptable.

### Canal 2 — État (UDP)
Pour le flux continu de couleurs, ~40x/seconde. Doit être rapide et léger ; une perte
ponctuelle n'est pas grave, le paquet suivant corrige.

## 5. Format recommandé des messages

### Commande (canal 1)
```json
{
  "type": "START_SHOW",
  "id": "cmd-0001",
  "timestamp": 1720350000000,
  "version": 1,
  "payload": { "showId": "..." }
}
```

### État (canal 2)
```json
{
  "type": "STATE_UPDATE",
  "seq": 812345,
  "entities": [
    { "id": 4237, "r": 255, "g": 0, "b": 0 }
  ]
}
```
`seq` permet au routage d'ignorer un paquet arrivé en retard ou dans le désordre
(UDP ne garantit pas l'ordre).

## 6. Commandes recommandées (canal 1)
- START_SHOW
- STOP_SHOW
- PAUSE
- RESUME
- GET_STATUS
- RELOAD_CONFIG

## 7. Réponses attendues du routage
- ACK : commande reçue ;
- DONE : exécution terminée ;
- ERROR : problème (version incompatible, commande inconnue...) ;
- STATUS : état courant (contrôleurs connectés, fréquence d'update réelle...) ;
- DEVICE_FAULT : un contrôleur BC216 ne répond plus.

## 8. Gestion des erreurs
Il faut prévoir :
- perte de paquets d'état (tolérable, ne pas essayer de retransmettre à 40Hz) ;
- perte de commande (canal TCP, doit être fiable — prévoir un timeout + nouvelle tentative) ;
- version de protocole incompatible ;
- commande inconnue.

## 9. Structure recommandée
- `TimelineController` : avance dans le temps, active/désactive les clips ;
- `ClipManager` : applique les couleurs des clips sur `EntityManager` (Dev 1) ;
- `AudioSync` : lecture audio + affichage waveform ;
- `StateExporter` : sérialise l'état et l'envoie en UDP (canal État) ;
- `CommandClient` : envoie/reçoit les commandes TCP (canal Commandes).

## 10. Recommandation concrète pour votre cas
- Ne renvoyer que les entités dont la couleur a changé depuis le dernier envoi, pas
  toutes les 16 384 à chaque frame.
- Le calcul de l'état à un instant T doit être déterministe (rejouer la même timeline
  au même moment doit toujours donner le même résultat).
- Garder `StateExporter` sur un timer stable, indépendant des à-coups de l'éditeur/UI.

## 11. Critère de réussite
La communication sera réussie si :
- une commande START_SHOW envoyée est bien reçue et acquittée par le routage ;
- le flux État est envoyé à fréquence stable (~40Hz), même en manipulant l'éditeur en
  parallèle ;
- une timeline avec plusieurs clips superposés produit un état cohérent ;
- la communication reste stable même si un paquet d'état est perdu.
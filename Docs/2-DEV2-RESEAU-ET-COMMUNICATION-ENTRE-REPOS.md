# 👤 DEV 2 — Réseau et communication entre les deux repositories

**Rôle :** définir le canal de communication entre le repo de contrôle et le repo de rendu / affichage.

---

## 1. Ton rôle en une phrase
Tu es le pont entre les deux repos. Ton travail est de rendre la communication fiable, standardisée et propre.

## 2. Pourquoi c’est crucial
Comme vous avez deux repositories différents, il ne faut pas compter sur des appels directs implicites ou un couplage fort. Il faut un contrat clair entre les deux parties.

## 3. Le problème à résoudre
Le système doit pouvoir :
- envoyer des commandes depuis le repo de contrôle ;
- faire exécuter ces commandes dans le repo de rendu ;
- renvoyer un état ou une confirmation ;
- gérer les pertes, les déconnexions et les versions incompatibles.

## 4. L’architecture recommandée
### Option A — communication réseau directe
- un service écoute sur un port ;
- l’autre repo envoie des messages structurés ;
- les messages sont de type JSON, MessagePack ou binaire simple.

### Option B — protocole partagé via fichiers ou contrat
- un fichier de contrat commun ;
- un schema défini avec les actions possibles ;
- chaque repo l’implémente localement.

## 5. Format recommandé des messages
Un message doit contenir au minimum :
- type : nom de la commande ;
- id : identifiant unique ;
- timestamp : horodatage ;
- payload : données utiles ;
- version : version du protocole.

Exemple :

```json
{
  "type": "START_SCENE",
  "id": "scene-001",
  "timestamp": 1712345678,
  "version": 1,
  "payload": {
    "sceneName": "intro",
    "intensity": 0.8
  }
}
```

## 6. Commandes recommandées
- START_SCENE
- STOP_SCENE
- SET_PARAMETER
- TRIGGER_EVENT
- PAUSE
- RESUME
- SYNC_CLOCK
- GET_STATUS

## 7. Réponses attendues
Le repo rendu peut répondre avec :
- ACK : commande reçue ;
- DONE : exécution terminée ;
- ERROR : problème pendant l’exécution ;
- STATUS : état courant.

## 8. Gestion des erreurs
Il faut prévoir :
- perte de message ;
- latence ;
- reconnexion ;
- version incompatible ;
- commande inconnue.

## 9. Bonne pratique de conception
- définir un protocole stable ;
- versionner chaque message ;
- éviter les messages trop ambigus ;
- documenter chaque commande ;
- garder un log de communication.

## 10. Recommandation concrète pour votre cas
Le plus propre est de rendre le protocole partagé entre les deux logiciels. Par exemple :
- un fichier common/protocol.json ou un contrat commun ;
- un module C# partagé si les deux projets sont en .NET ;
- ou un mini service de message standard basé sur JSON sur TCP/UDP/OSC.

Dans votre cas précis, cette communication doit relier :
- le logiciel d’animation (partie view / conception artistique) ;
- le logiciel de routage (partie logique de distribution et de pilotage).

Ainsi, l’animation peut envoyer des commandes ou des états, et le routage peut les transformer en instructions de circulation et de pilotage vers les dispositifs.

## 11. Critère de réussite
La communication sera réussie si :
- une commande lancée depuis le repo de contrôle est reçue par l’autre repo ;
- le rendu exécute la commande ;
- un retour d’état est renvoyé ;
- la communication reste stable même si une commande est perdue ou retardée.

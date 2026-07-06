# 👤 DEV 1 — Orchestration et contrôle

**Rôle :** gérer les scènes, les événements, le timing et les commandes de départ.

---

## 1. Ton rôle en une phrase
Tu es responsable de la logique centrale du projet : décider quoi faire, à quel moment, et dans quel ordre.

## 2. Pourquoi c’est important
Sans cette couche, le système n’a pas de cerveau. Elle sert de source de vérité pour :
- l’enchaînement des scènes ;
- le timing ;
- les changements d’état ;
- la synchronisation générale.

## 3. Ce que tu dois gérer
### 3.1 Les scènes
Tu dois définir :
- les scènes disponibles ;
- les transitions ;
- les conditions de lancement ;
- les paramètres associés.

### 3.2 Les événements
Tu dois organiser :
- début de scène ;
- fin de scène ;
- changement de paramètre ;
- tempo ou trigger externe ;
- événement réseau.

### 3.3 Le timing
Tu dois gérer :
- le temps global ;
- les durées de scène ;
- les offsets ;
- la synchronisation avec d’autres composants.

## 4. Structure recommandée
Une bonne structure est :
- SceneManager : coordination des scènes ;
- TimelineController : gestion du timing ;
- EventBus : circulation des événements ;
- CommandSender : envoi des commandes au second repo.

## 5. Le flux de travail typique
```text
[Contrôle] -> [Commande] -> [Repo rendu]
            ^
            |
      [Retour d'état]
```

## 6. Ce qu’il faut envoyer
Tu peux envoyer des messages de type :
- START_SCENE
- STOP_SCENE
- SET_PARAMETER
- TRIGGER_EVENT
- SET_BPM
- PAUSE
- RESUME

## 7. Ce qu’il faut recevoir
Tu dois aussi pouvoir recevoir :
- état courant ;
- confirmation d’exécution ;
- erreur de rendu ;
- état de connexion.

## 8. Bonnes pratiques
- garder la logique de contrôle indépendante du rendu ;
- éviter de faire du rendu dans cette couche ;
- définir un protocole clair ;
- versionner les commandes.

## 9. Critère de réussite
Le contrôle doit être capable de :
- lancer une scène ;
- la stopper proprement ;
- transmettre les paramètres ;
- recevoir un retour d’état.

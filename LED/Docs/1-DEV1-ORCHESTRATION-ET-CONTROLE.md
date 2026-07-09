# 👤 DEV 1 — Scène 3D & modèle d'entités (Animation)

**Rôle :** construire la représentation 3D de l'installation physique et le modèle de données des entités (LED virtuelles).

---

## 1. Ton rôle en une phrase
Tu es responsable du socle sur lequel toute l'animation repose : la géométrie 3D de
l'installation et le modèle des entités (LED) que Dev 2 va animer dans le temps.


## 2. Pourquoi c'est important
Sans cette couche, il n'y a rien à animer. Elle sert de source de vérité pour :
- la position réelle de chaque LED dans l'espace 3D ;
- l'identifiant et l'état de couleur de chaque entité ;
- la structure que Dev 2 va manipuler via la timeline.

## 3. Ce que tu dois gérer

### 3.1 La géométrie de l'installation
Tu dois définir :
- le modèle 3D représentant la forme physique réelle (mur, araignée, arbre...) ;
- le chargement d'un fichier de configuration décrivant le nombre d'entités et leur
  position (pas de valeurs codées en dur) ;
- l'affichage visuel de chaque entité dans la scène.

### 3.2 Le modèle d'entité
Tu dois organiser :
- un identifiant unique par entité (`int`, pas forcément séquentiel) ;
- une position 3D ;
- une couleur courante (R, G, B).


### 3.3 La sauvegarde/chargement
Tu dois gérer :
- le chargement d'un fichier de config d'installation sans recompiler ;
- la possibilité de passer d'une petite maquette de test au vrai mur de 16 384 LED
  juste en changeant de fichier.

## 4. Structure recommandée
Une bonne structure est :
- `EntityManager` : détient la liste des entités et leur état de couleur courant ;
- `InstallationLoader` : charge la géométrie/config depuis un fichier ;
- `SceneBuilder` : instancie la représentation 3D à partir des entités chargées ;
- `ColorState` : structure simple `{id, r, g, b}`, réutilisée par Dev 2.

## 5. Le flux de travail typique
```text
[Fichier de config] -> [InstallationLoader] -> [EntityManager] -> [SceneBuilder (3D)]
```

## 6. Ce que tu dois exposer à Dev 2
Au lieu d'envoyer des commandes vers un "second repo" (ancienne version), tu exposes
une API locale à l'intérieur du même logiciel d'animation :
- `EntityManager.SetColor(id, r, g, b)` ;
- un événement/callback déclenché quand une couleur change, pour rafraîchir la scène 3D
  en direct.

## 7. Ce qu'il faut recevoir
- Rien depuis le réseau : Dev 1 ne communique pas directement avec le logiciel de
  routage. Toute communication réseau passe par Dev 2 (StateExporter/CommandClient).

## 8. Bonnes pratiques
- ne jamais coder en dur le nombre d'entités ou leur position ;
- garder `EntityManager` indépendant du rendu 3D (testable sans lancer Unity) ;
- garder la logique de géométrie/données séparée de la logique de timeline (rôle de Dev 2).

## 9. Critère de réussite
Ton module doit être capable de :
- charger un fichier de config décrivant N entités et leurs positions ;
- les afficher à la bonne place en 3D ;
- changer la couleur d'une entité par code et la voir changer visuellement en temps réel.
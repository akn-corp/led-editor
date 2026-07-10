# 🎯 Comment marche le projet LAPS Light Show (vue d'ensemble)

> À lire en premier par toute l’équipe. C’est la vue d’ensemble du système.

---

## 1. L’idée en une phrase
Le projet est un système de contrôle et d’affichage pour une expérience de son et lumière. Une partie du système orchestre les scènes, une autre partie rend les contenus et une autre encore pilote les dispositifs physiques.

## 2. Pourquoi ce modèle est adapté
Le projet doit être :
- synchronisé ;
- modulaire ;
- testable ;
- facile à faire évoluer.

Pour cela, il faut séparer clairement :
- le contrôle ;
- le rendu ;
- la communication ;
- l’infrastructure.

## 3. Les acteurs du système

```text
[PARTIE ANIMATION]
    -> conçoit et gère les contenus visuels et artistiques
    -> pilote l’aspect “view” du logiciel d’animation
    -> est assurée par le Dev 1 et le Dev 2

[PARTIE ROUTAGE]
    -> gère la logique de distribution, de connexion et de routage
    -> pilote le logiciel dédié au routage
    -> est assurée par le Dev 3 et le Dev 4

[DISPOSITIFS / APPAREILS]
    -> reçoivent les ordres réseau ou protocolaires
    -> exécutent les actions demandées
```

## 4. Le déroulé d’une scène

```text
1. L’orchestration charge la scène
2. Elle envoie les paramètres de début de scène
3. Le moteur de rendu applique les contenus
4. Les dispositifs réagissent selon les commandes reçues
5. La scène évolue jusqu’à la fin ou au prochain événement
```

## 5. Les données importantes à transmettre
Le système doit échanger au minimum :
- l’état de la scène ;
- le timing ;
- les commandes d’action ;
- les paramètres de rendu ;
- les événements de retour d’état.

## 6. Le point crucial : deux repositories différents
Comme vous avez créé deux repos distincts, il faut imaginer le système comme deux briques communicantes :
- un repo pour la logique de contrôle ;
- un repo pour le rendu / prévisualisation / affichage.

Le contrat entre eux doit être défini clairement.

## 7. Ce que doit garantir la communication
La communication doit permettre :
- de lancer une scène ;
- de modifier un paramètre en temps réel ;
- de synchroniser le timing ;
- de transmettre l’état courant ;
- de remonter les erreurs ou les déconnexions.

## 8. Ce qu’il faut éviter
- mélanger contrôle et rendu dans un seul module ;
- envoyer des données trop lourdes sans structure ;
- dépendre d’un protocole implicite non documenté ;
- oublier la version du protocole.

# Projet LAPS Light Show — Documentation de référence

> Ce document sert d’entrée principale au projet. Il reprend la logique d’une documentation de type “comment ça marche” et pointe vers les documents détaillés de développement, d’architecture et de communication.

---

## 1. L’idée du projet en une phrase
Le projet LAPS Light Show vise à créer une expérience de son et lumière pilotée de manière cohérente, synchronisée et évolutive, en associant contenu artistique, orchestration logicielle, dispositifs physiques et infrastructure réseau.

## 2. Pourquoi ce projet est important
Ce projet n’est pas seulement un projet “lumière”. C’est un système intégré où plusieurs briques doivent fonctionner ensemble :
- la scénographie artistique ;
- la logique de contrôle ;
- les dispositifs de sortie ;
- la synchronisation temporelle ;
- la gestion réseau et la stabilité d’exploitation.

C’est pour cette raison que la documentation doit être claire, structurée et assez précise pour permettre à toute l’équipe de travailler sur la même base.

## 3. Les grands objectifs
Le système doit permettre de :
1. créer une expérience visuelle et lumineuse harmonieuse ;
2. piloter plusieurs appareils de manière synchronisée ;
3. fournir une base robuste pour la production et l’exploitation ;
4. faciliter la maintenance et l’évolution future ;
5. réduire les erreurs liées à la communication entre les composants.

## 4. Les acteurs du projet
Plusieurs profils sont impliqués :
- direction artistique : vision, narration et scénographie ;
- ingénierie lumière : choix des appareils, intensité, répartition ;
- ingénierie système : orchestration, logique, protocole ;
- installation physique : câblage, fixation, alimentation ;
- exploitation : mise en route, réglages, tests, dépannage.

## 4.1 Répartition du travail par binôme
Comme le projet repose sur deux logiciels distincts, l’équipe est organisée en deux binômes :
- Développeur 1 et Développeur 2 : partie animation / logiciel de gestion de l’animation ;
- Développeur 3 et Développeur 4 : partie routage / logiciel de gestion du routage.

Dans cette organisation :
- le Dev 1 et le Dev 2 travaillent sur la partie animation, la logique de view, les contenus et l’interface liée à l’affichage artistique ;
- le Dev 3 et le Dev 4 travaillent sur la partie routage, la logique de distribution, de connexion et de pilotage des dispositifs.

Cette répartition est cohérente avec la logique du projet :
- la partie animation couvre la création, l’ordonnancement et l’affichage des contenus visuels et artistiques ;
- la partie routage couvre la logique de circulation, de distribution, de connexion et de pilotage des dispositifs.

## 5. L’architecture fonctionnelle générale
Le projet peut être découpé en 4 couches principales :

### 5.1 Couche contenu
Elle contient les éléments créatifs :
- musique et audio ;
- vidéos, visuels et animations ;
- séquences lumineuses ;
- éléments de narration ou de scène.

### 5.2 Couche orchestration
C’est le cœur du système. Elle déclenche les événements, gère le timing et synchronise les actions.

### 5.3 Couche dispositifs
Elle pilote les équipements de sortie :
- écrans LED ;
- projecteurs ;
- fixtures ;
- modules d’éclairage ;
- autres appareils connectés.

### 5.4 Couche infrastructure
Elle comprend :
- le câblage ;
- le réseau ;
- l’alimentation ;
- la supervision ;
- la gestion des erreurs et de la redondance.

## 6. Le fonctionnement de base
Un scénario typique suit cette logique :
1. les contenus sont préparés et rangés par scène ;
2. l’orchestration déclenche les événements dans l’ordre ;
3. les commandes sont envoyées aux appareils via les protocoles adaptés ;
4. les appareils réagissent en temps réel ;
5. l’équipe contrôle la cohérence visuelle et technique.

## 7. Les points techniques essentiels
### 7.1 Synchronisation
La synchronisation doit être pensée très tôt. Il faut définir :
- la source de temps principale ;
- la tolérance d’écart ;
- la stratégie en cas de décalage.

### 7.2 Fiabilité
Le système doit rester stable pendant la présentation. Il faut prévoir :
- des tests réguliers ;
- une logique de secours ;
- des procédures de dépannage ;
- une surveillance simple de l’état du système.

### 7.3 Évolutivité
Le système doit être conçu pour évoluer :
- ajouter un appareil ;
- changer une scène ;
- modifier les contenus ;
- intégrer de nouveaux protocoles.

## 8. Recommandation de départ
Pour avancer proprement, il est conseillé de :
- définir un périmètre fonctionnel clair ;
- séparer les responsabilités entre contrôle, rendu et dispositifs ;
- choisir un protocole de communication stable ;
- tester rapidement un prototype avant l’intégration complète.

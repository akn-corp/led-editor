# Stack recommandée pour LAPS Light Show

> Ce document donne la pile technique recommandée pour le projet, avec une logique très proche des documentations de type “développement” de Pong : on explique le rôle de chaque couche, pourquoi ce choix est pertinent, et comment l’implémenter de manière propre.

---

## 1. Recommandation générale
Pour un projet de show lumineuse, la meilleure solution n’est pas un seul outil unique, mais une stack modulaire et fiable composée de :
- un moteur d’orchestration et de contrôle ;
- un moteur de rendu ou de prévisualisation ;
- un système de communication réseau adapté ;
- des outils de test et de déploiement simples à reproduire.

La solution la plus solide pour votre contexte est :
- C# / .NET pour la logique centrale ;
- Unity pour la partie visuelle, la prévisualisation et la logique temps réel ;
- OSC, Art-Net et DMX pour la communication avec les dispositifs ;
- Python pour les outils d’intégration et d’automatisation ;
- Git, Docker et GitHub Actions pour la gestion du projet et du déploiement.

## 2. Stack recommandée par couche

### 2.1 Couche orchestration et logique
- Langage : C#
- Framework : .NET 8
- Pourquoi : très adapté aux applications temps réel, à la gestion réseau, aux services de contrôle et à l’intégration avec divers protocoles.

### 2.2 Couche rendu / prévisualisation
- Outil principal : Unity
- Pourquoi : très pratique pour créer des scènes, simuler des visuels, tester des comportements et piloter des séquences de manière claire.

### 2.3 Communication avec les dispositifs
- Protocoles recommandés : OSC, Art-Net, DMX
- Pourquoi : ce sont des standards très utilisés dans les environnements lumière, vidéo et événementiel.

### 2.4 Outils annexes
- Langage : Python
- Pourquoi : utile pour automatiser les tests, créer des scripts de configuration, gérer des conversions ou piloter certains outils externes.

### 2.5 Interface de pilotage
- Option simple : WPF ou Blazor
- Option web : React/Vue avec un backend .NET
- Pourquoi : une interface bien pensée facilite les tests, la gestion des scènes et le contrôle en production.

### 2.6 Données et configuration
- Base locale : SQLite
- Base plus robuste : PostgreSQL
- Pourquoi : utile pour stocker les scènes, les presets, les réglages et les historiques d’exploitation.

### 2.7 Déploiement et maintenance
- Outils : Docker, Git, GitHub Actions
- Pourquoi : permet de reproduire les environnements, de partager le projet, d’automatiser les tests et de gagner en fiabilité.

## 3. Pourquoi cette stack est la meilleure ici
Cette combinaison est intéressante parce qu’elle offre :
- une stabilité suffisante pour une production réelle ;
- une bonne évolutivité ;
- une intégration simple avec les protocoles de lumière et de média ;
- une grosse communauté et de nombreuses ressources ;
- un bon équilibre entre rapidité de développement et fiabilité.

## 4. Les contraintes à prévoir
### 4.1 Synchronisation temporelle
Il faut définir très tôt :
- quelle machine sert de référence ;
- comment les dispositifs sont synchronisés ;
- comment gérer les retards éventuels.

### 4.2 Réseau
Le réseau doit rester propre, stable et bien organisé :
- éviter le trafic inutile ;
- prévoir la latence ;
- isoler les flux critiques si nécessaire.

### 4.3 Redondance et sécurité
Pour une production critique, il faut prévoir :
- une logique de secours ;
- un backup des configurations ;
- des procédures de reprise rapide.

## 5. Ce qu’il faut éviter
- tout centraliser dans une seule app sans séparation claire ;
- choisir une stack trop fragile pour la production ;
- ignorer la latence et le timing ;
- mélanger trop de responsabilités dans un seul module.

## 6. Recommandation finale
Si vous voulez une solution solide, pragmatique et durable, la meilleure base est :
- C# / .NET pour l’orchestration ;
- Unity pour le rendu et la prévisualisation ;
- OSC + Art-Net + DMX pour la communication ;
- Python pour les outils annexes ;
- Docker + Git pour la collaboration et le déploiement.

## 7. Point crucial pour votre cas : communication entre deux repos différents
Puisque vous avez créé deux repositories distincts, il faut absolument définir une interface de communication claire entre eux. Le plus simple et le plus propre est :
- un repo pour la logique de contrôle / orchestration ;
- un repo pour le rendu / affichage / prévisualisation ;
- un protocole commun partagé entre les deux.

Ce protocole doit être versionné et documenté. Les deux parties doivent savoir :
- quelles commandes sont envoyées ;
- quels états sont renvoyés ;
- quelles erreurs peuvent survenir ;
- quelles versions de protocole sont compatibles.

La documentation détaillée de cette partie est dans [Docs/2-DEV2-RESEAU-ET-COMMUNICATION-ENTRE-REPOS.md](Docs/2-DEV2-RESEAU-ET-COMMUNICATION-ENTRE-REPOS.md).

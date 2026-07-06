# 👤 DEV 4 — Intégration, tests et déploiement

**Rôle :** rendre le système jouable, testable et stable en production.

---

## 1. Ton rôle en une phrase
Tu es le garant de la qualité finale : tu testes l’intégration, tu valides le fonctionnement global et tu prépares le déploiement.

## 2. Pourquoi c’est important
Même si les couches individuelles fonctionnent, le système ne marche vraiment que si elles sont connectées correctement.

## 3. Ce que tu dois vérifier
- la communication entre les deux repos ;
- la bonne réception des commandes ;
- le bon déclenchement des scènes ;
- la stabilité du timing ;
- le bon comportement en cas d’erreur.

## 4. Plan de tests
### 4.1 Test unitaire
Vérifier chaque composant isolément :
- contrôle ;
- communication ;
- rendu ;
- service réseau.

### 4.2 Test d’intégration
Vérifier la chaîne complète :
- orchestration -> réseau -> rendu -> dispositif.

### 4.3 Test en environnement réel
Tester sur la machine cible ou le matériel cible si possible.

## 5. Procédure de validation recommandée
1. lancer la partie contrôle ;
2. lancer la partie rendu ;
3. envoyer une scène de test ;
4. vérifier la réception ;
5. vérifier l’affichage ;
6. vérifier la réponse de retour ;
7. répéter sur plusieurs scénarios.

## 6. Problèmes fréquents
- protocole mal défini ;
- message perdu ;
- mauvaise version du contrat ;
- latence non gérée ;
- rendu qui ne suit pas le timing.

## 7. Déploiement
Avant de livrer, il faut prévoir :
- versioning clair ;
- configuration portable ;
- log de fonctionnement ;
- procédure de redémarrage ;
- sauvegarde des paramètres.

## 8. Critère de réussite
Le système est prêt quand :
- une scène peut être lancée depuis le repo de contrôle ;
- le rendu l’exécute correctement ;
- l’intégration est stable ;
- les tests de base sont passés.

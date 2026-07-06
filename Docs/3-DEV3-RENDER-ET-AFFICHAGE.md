# 👤 DEV 3 — Rendu et affichage

**Rôle :** recevoir les commandes, appliquer les contenus et préparer la sortie visuelle / lumineuse.

---

## 1. Ton rôle en une phrase
Tu es responsable de la partie affichage et d’exécution des scènes à l’écran, sur les dispositifs ou dans la prévisualisation.

## 2. Pourquoi c’est important
Même si le contrôle décide de la scène, c’est toi qui la rends concrètement. Si l’affichage est mal géré, l’expérience globale échoue.

## 3. Ce que tu dois faire
- recevoir une commande de scène ;
- charger les contenus associés ;
- appliquer les paramètres ;
- produire la sortie attendue ;
- transmettre un retour d’état.

## 4. Les objets à gérer
Tu peux organiser ton code avec :
- RendererController : orchestration du rendu ;
- SceneRenderer : rendu d’une scène spécifique ;
- EffectPlayer : lecture des effets ;
- ParameterApplier : application des réglages.

## 5. Flux recommandés
```text
[Commande reçue] -> [Chargement scène] -> [Application param.] -> [Sortie affichage]
```

## 6. Ce qu’il faut rendre
Selon le projet, cela peut être :
- une animation visuelle ;
- une séquence lumineuse ;
- un effet dynamique ;
- une prévisualisation ;
- une sortie vers un dispositif externe.

## 7. Gestion de la synchronisation
Tu dois respecter :
- le timing de la scène ;
- les paramètres reçus ;
- les changements en temps réel ;
- les retours d’état si nécessaire.

## 8. Bonnes pratiques
- séparer le rendu de la logique de contrôle ;
- prévoir des états de fallback ;
- garder la logique de sortie simple et robuste ;
- envoyer des confirmations d’exécution.

## 9. Critère de réussite
Le rendu doit être capable de :
- recevoir une commande ;
- afficher la scène attendue ;
- répondre correctement à l’orchestration.

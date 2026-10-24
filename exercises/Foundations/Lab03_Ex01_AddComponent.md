# Exercice (Lab03) — Ajouter un composant et le rendre visible au modèle

## Objectif
Ajouter un nouveau composant **Search** (owner: Team Platform) au fichier `assets/components.json`, puis vérifier que l’agent propose ce composant quand la demande parle de recherche.

## Étapes
1) Modifier `assets/components.json`
2) Exécuter `Lab03_ToolsCatalog` et demander :
   - « La recherche sémantique ne remonte plus aucun résultat depuis la dernière release. »
3) Vérifier que le composant proposé est **Search**.

## Critère de réussite
Le modèle appelle `list_components` et propose **Search**.
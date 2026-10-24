# Exercice (Lab04) — Ajouter un tool de changement de statut

## Objectif
Ajouter un tool `set_backlog_status(id, status)` qui met à jour le champ `Status` (New/Triaged/Planned).

## Contraintes
- validation humaine obligatoire
- requête SQL paramétrée
- après update, afficher `list`

## Critère de réussite
Après création d'un item, l'agent peut proposer de le passer en `Triaged` et l'action est visible dans `list`.
# Exercice Lab06 — Déclarations, provenance et correction

Prérequis : Lot-005 et guide Lot-006. Utiliser la variante manuelle rénovée, avec la connexion commune.

1. Envoyer `module=Reporting; priority=stability`, puis `mem`.
2. Envoyer `module=Billing`, puis `mem`. L'ancienne valeur Reporting doit disparaître du store.
3. Envoyer une question sans déclaration, puis `mem`. La question et la réponse de l'assistant ne doivent pas devenir des faits.
4. Ajouter successivement de nouvelles clés autorisées pour dépasser cinq faits cumulés. Vérifier que le store reste borné et que les faits les plus anciens sont évincés.
5. Lire la citation, le tour et l'horodatage de chaque entrée. Une assertion utilisateur n'est pas une vérité vérifiée.

Critère : admission de la syntaxe key=value contrôlée côté hôte, correction par clé, cinq faits au maximum, aucune conversion automatique du texte libre en fait. Un diagnostic d'extraction invalide n'est pas un succès ; le store précédent reste en place.


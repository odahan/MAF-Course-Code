# Exercice Lab06 — Partage et oubli dans deux agents

1. Dans chaque variante (manuelle puis AIContextProvider), envoyer `module=Reporting` et inspecter `mem`.
2. Poser une question commençant par `How` pour router vers KnowledgeAgent. La première déclaration était traitée par BacklogAgent.
3. Corriger avec `module=Billing`, puis vérifier le store partagé.
4. Exécuter `forget module` : les autres faits restent mais les deux historiques métier sont remplacés.
5. Exécuter `forget`, poser une question ne nommant aucun module, puis `mem`. Aucun ancien module ne doit réapparaître.
6. Fermer le flux d'entrée et constater la sortie du processus. Relancer : la mémoire de faits est vide, car ce lab est en processus.

Comparer avec le lab de session qui sauvegarde son historique, et avec le lab SQLite qui sauvegarde seulement un contexte explicite. Une compaction de requête seule ne garantit pas l'oubli de l'historique source.

Critère : oubli coordonné de tous les historiques actifs et du store, writer sans session, extraction limitée aux messages utilisateur externes, aucune réponse d'assistant mémorisée.


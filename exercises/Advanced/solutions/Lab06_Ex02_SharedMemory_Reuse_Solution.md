# Corrigé Lab06 — Partage et oubli

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Les deux agents utilisent la même WorkingMemory. La variante manuelle injecte ToPromptBlock avant l'appel puis extrait le message utilisateur original. La variante provider injecte le snapshot dans ProvideAIContextAsync et extrait seulement les messages User de source External dans StoreAIContextAsync, après succès.

MemoryDemo remplace les deux AgentSession après tout forget (même ciblé). Le writer n'a pas de session. WorkingMemory incrémente la génération afin qu'un résultat d'extraction en attente ne puisse pas restaurer les faits oubliés.

Le test ProviderDoesNotExtractAssistantAnswersAndForgetClearsBothHistories contrôle les requêtes envoyées au writer et aux deux agents après oubli. La recette tools/Verify-Lot006.ps1 exécute les deux vraies consoles avec Ollama, vérifie la correction à la clé module et les snapshots après oubli. Le texte généré seul n'est pas une preuve.

La mémoire partagée ne persiste pas entre processus. SessionFile persiste un historique MAF dans l'autre lab ; ContextStore persiste un contexte SQLite. Les journaux de recette et éventuelles sauvegardes externes ne font pas partie de la commande forget.


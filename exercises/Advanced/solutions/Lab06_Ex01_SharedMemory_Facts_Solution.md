# Corrigé Lab06 — Déclarations et correction

WorkingMemory remplace le dictionnaire à la même clé, valide tout le batch avant mutation et évince le fait au plus ancien tour au-delà de cinq. MemoryExtraction compare les candidats structurés aux déclarations explicites parsées par ExplicitAssertions ; elle refuse une mauvaise clé, une omission ou une valeur inventée. WorkingMemory vérifie en plus les citations exactes et les longueurs.

Après `module=Reporting`, puis `module=Billing`, le snapshot contient une seule entrée module, de valeur Billing et de provenance au second tour. Après une question sans déclaration, aucune nouvelle entrée n'est admissible. Le writer reçoit seulement le texte utilisateur actuel et n'a pas de session.

Pour démontrer un rejet fiable sans dépendre du modèle, lire les tests ExplicitAdmissionRejectsWrongSemanticKeyAndQuestionsAsFacts, ExplicitAdmissionRetainsOnlyFinalCorrectionAndRejectsMissingFacts et InvalidBatchIsAtomicAndDoesNotPolluteFacts. Ils observent le store après le rejet. Ne pas remplacer les erreurs par une extraction heuristique silencieuse.


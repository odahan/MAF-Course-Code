# Corpus, embeddings et recherche SQLite

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

## Décision de stockage pour toute la rénovation

Tous les exemples vectoriels rénovés utilisent **Microsoft.Data.Sqlite**, avec ou sans extension. Le Lot-007 utilise SQLite 10.0.13 sans extension : vecteurs float32 little-endian dans des BLOB et cosinus exact en C#. Aucun serveur vectoriel, conteneur ou port supplémentaire. Cette décision remplace les préconisations Qdrant des documents historiques, qui restent des preuves datées. Elle figure aussi dans le plan général et dans `Cours/AGENTS.md` pour les lots suivants.

La recherche est réellement vectorielle : les embeddings des documents et de la question viennent du module `Maf.DemoConnection` ; le cosinus dépend de leurs coordonnées. Une branche lexicale compte les termes exacts ; la fusion RRF utilise les rangs de ces deux branches. Aucun moteur dense simulé, bonus canonique ou ancien score métier n'est conservé.

Ce choix vise les petits corpus pédagogiques : toutes les lignes admissibles sont scorées, coût O(N × dimension). Il ne démontre pas une recherche ANN à grande échelle. Une extension SQLite pourra être retenue ultérieurement si nécessaire, sans introduire Docker.

## Points d'entrée

| Projet actuel | Fonction | Ancien chemin au Lot-001 |
|---|---|---|
| `demos/Demo180_VectorIngestion` | Reconstruction et inspection de l'index | `Lab02_QdrantIngest` |
| `demos/Demo190_RetrievedEvidence` | Récupération hôte obligatoire, appel d'outil MAF observé, réponse structurée contrôlée | Inchangé |
| `demos/Demo200_RetrievalProvider` | Même politique, extraits injectés par `TextSearchProvider` | `Lab02_QdrantIngest_Search` |
| `demos/Demo210_CorpusQuality` | Audit, classement vectoriel brut comparé au contexte gouverné, réponse sur remboursement | Inchangé |
| `demos/Demo220_BusinessRetrieval` | Recherche hybride réelle, référence produit et procédure structurée dans SQLite | Inchangé |
| `shared/Maf.Rag` | Corpus, persistance, embeddings, recherche, contrats et vérifications partagés | Nouveau |
| `tests/Maf.Rag.Tests` | Tests déterministes sur de vrais fichiers SQLite | Nouveau |

Les originaux restent dans les arbres historiques et les références du Lot-001. Le manifeste de provenance n'est pas réécrit pour faire passer les copies rénovées pour les fichiers initiaux.

## Exécution

Depuis `BaseTechnique`, compiler et tester avec `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`. Les modèles locaux nécessaires sont `granite4:3b` et `embeddinggemma:300m`. Le module de connexion conserve ses paramètres indépendants pour chat et embeddings et le mode chat API par clé explicite. Le Lot-007 qualifie uniquement le profil Ollama ; aucune nouvelle recette API n'est revendiquée.

```powershell
dotnet run --project demos/Demo180_VectorIngestion --configuration Release --no-build -- --Rag:Action rebuild
dotnet run --project demos/Demo200_RetrievalProvider --configuration Release --no-build -- --Rag:AsOf 2026-10-23
dotnet run --project demos/Demo210_CorpusQuality --configuration Release --no-build -- --Rag:Action compare --Rag:AsOf 2026-10-23
# Archived acceptance recipe: tools/Verify-Lot007.ps1
```

Les interfaces console et textes applicatifs sont en anglais. Commandes interactives : `rebuild`, `audit`, `compare`, `probe <question>`, `quit`. Les autres entrées posent une question. Entrée vide et fin de flux quittent ; Ctrl+C propage l'annulation.

Les actions non interactives sont `--Rag:Action rebuild|audit|compare|probe|answer|verify`. `probe` et `answer` demandent `--Rag:Question "..."`. Exemples : « Where can I export CSV in Reporting? », « What should I check for Auth error 401 on a mobile device? », « Can I get train ticket reimbursement without a receipt? », « Which quality control procedure applies to GX-900 gears after heat treatment? ».

`ai-settings.json` et `corpus.json` sont copiés dans la sortie et chargés depuis le répertoire de l'exécutable. Le fonctionnement depuis un autre répertoire est vérifié. `--Rag:Corpus` sélectionne un corpus externe, `--Rag:Database` un chemin SQLite **absolu** et `--Rag:AsOf` une date ISO explicite. Sans date fournie, l'application utilise la date locale. La recette fournit toujours une date. `MAF_STATE_ROOT` définit une racine absolue ; sinon l'état va dans `%LOCALAPPDATA%/MafWorkshop/rag/vectors.db`.

## Corpus et admissibilité

Le corpus livré contient 15 documents, regroupés en trois périmètres : support AcmeShop, remboursements de train et procédures de qualité. Il contient aussi les contre-exemples : brouillon, miroir non canonique, règle future, texte expiré, procédure archivée et autre produit. Les deux faits produit sont indépendants des extraits et sont enregistrés dans `RagProducts`.

Une source doit avoir `status=active`, `canonical=true`, `validFrom <= asOf` et `validUntil` absent ou supérieur ou égal à `asOf`. Les bornes sont inclusives. La date de début est vérifiée, ce qui corrige l'omission d'EP007. Le périmètre, la référence produit ou `GENERAL` sont filtrés en SQL **avant** le calcul et le top-K. Le lien entre produit actif et procédure est également contrôlé avant le top-K hybride. Un produit inconnu ou retiré ne donne pas de contexte métier exploitable.

L'API diagnostique peut calculer un classement brut non gouverné pour montrer le problème du corpus. La console `compare` affiche ce classement et le contexte contrôlé séparément ; le classement brut n'est pas envoyé au service de réponse. Le contrôle de citation revérifie aussi statut, canonicité et validité des sources citées.

## Reconstruction et invariants

Les identifiants sont `sourceId#part`, issus d'un ordre ordinal des sources. Le découpage normalise CRLF puis utilise 600 caractères avec recouvrement de 80, version `chars-600-overlap-80-v1`. Les documents du corpus livré tiennent chacun dans un extrait. L'entrée est bornée à 500 sources, 10 000 caractères par source et 4 096 caractères par question. Les longs documents sont découpés avant embeddings ; Ollama reçoit `truncate=false` via la connexion commune.

Les embeddings sont générés en lots de 16 avant d'ouvrir la transaction de reconstruction. Nombre, dimension, finitude et norme non nulle sont contrôlés. Une erreur réseau ou un vecteur invalide laisse l'ancien index intact. Une transaction remplace uniquement les tables `RagChunks`, `RagProducts` et `RagManifest`, sans supprimer le fichier SQLite ni les tables d'un autre module. Une annulation pendant la transaction la fait annuler. Les lecteurs utilisent un instantané transactionnel.

Le manifeste conserve fournisseur, endpoint, modèle, dimension, distance, version du découpage et SHA-256 du corpus normalisé incluant les faits produit. L'ordre des fichiers n'affecte pas l'empreinte. À la lecture, une différence exige `rebuild` ; l'application contrôle aussi le nombre d'extraits, les identifiants, les textes récupérés et la longueur des vecteurs. Ces contrôles détectent une incompatibilité ou une corruption courante ; ils ne constituent pas une signature contre un attaquant contrôlant la base et le corpus.

La reconstruction remplace toutes les lignes du module. Les sources retirées et les anciennes parties d'un document raccourci disparaissent. Les tests vérifient ces deux situations ainsi que la réouverture du fichier après reconstruction.

## Pertinence et limites des scores

La politique commune exige un cosinus **>= 0,60** et un recouvrement lexical significatif. Les mots outils anglais et français sont ignorés. La fusion RRF utilise une constante 60 ; son score est un rang combiné, pas une probabilité ni un cosinus. Trois extraits au maximum sont transmis. Tous les candidats admissibles sont considérés avant cette sélection.

Mesures réelles avec `embeddinggemma:300m`, dimension 768, corpus livré et questions de réception :

| Cas | Meilleur cosinus avant seuil | Contexte contrôlé |
|---|---:|---|
| Export CSV | 0,7676 | `ACME-EXPORT#0` |
| Auth 401 sur mobile | 0,8000 | `ACME-AUTH#0` |
| Remboursement de train sans reçu | 0,6197 | `TRAIN-CURRENT#0` |
| Procédure qualité GX-900 | 0,7722 | `GX-PROCEDURE#0` |
| Recette de tarte | 0,2168 | Aucun |
| Retour GX-900 déjà installé | 0,4234 | Aucun |
| Produit GX-999 inconnu | Aucun après filtre | Aucun |

Le seuil a été ajusté sur cette petite matrice de réception ; il ne s'agit pas d'une évaluation indépendante ni d'une garantie de rappel pour toute paraphrase ou langue. Un autre corpus, modèle, digest, découpage ou prétraitement doit être recalibré. Les anciens seuils 0,35/0,85 et scores simulés ne sont plus utilisés. Le manifeste identifie l'alias configuré ; le digest réellement présent au moment de la recette figure dans `smoke-result.json`, sans certifier qu'un alias distant ne changera jamais.

## Réponse contrôlée et intégrations MAF

La récupération s'exécute côté hôte à chaque question. Zéro extrait admissible produit un refus avant tout appel au chat. Chaque requête utilise un agent sans historique conversationnel partagé, afin que les citations du tour précédent ne puissent pas autoriser le suivant.

La variante provider injecte les seuls extraits déjà récupérés via `TextSearchProvider` et vérifie l'exécution du provider. La variante outil utilise deux étapes : un agent MAF appelle `retrieve_knowledge` sans contrainte JSON simultanée, puis un second agent produit le contrat structuré avec les données réellement lues. Le nombre d'appels hôte est observé ; zéro appel refuse la réponse. Le texte libre de l'étape outil n'est pas utilisé comme preuve. Cette séparation a été nécessaire sur le profil local testé.

Le modèle propose `CanAnswer` et jusqu'à trois couples `SourceId`/`Quote`. L'application exige une source réellement récupérée au tour courant, encore admissible à la date fournie, et une citation littérale non vide d'au moins 20 caractères. Elle rejette les identifiants inventés et les citations altérées. La sortie affichée est **extractive** : seuls les passages vérifiés et leurs références sont rendus, sans prose libre du modèle.

Cette règle garantit la provenance littérale de la sortie acceptée. Elle ne prouve pas à elle seule que chaque citation répond complètement à toute question possible : des extraits authentiques peuvent être incomplets ou hors propos. Les tests de pertinence et le refus du modèle complètent ce contrôle sur les cas livrés. Le corpus et les passages restent des données non fiables ; le scénario d'injection d'une instruction dans un extrait admissible est testé et ne doit produire ni source inventée ni marqueur d'attaque. Ce test ponctuel ne revendique pas une immunité générale aux injections.

## Exercice et preuves

L'exercice rénové se trouve dans `exercises/Advanced/Lab03_Ex01_Rag_SourcesFormat.md`, avec sa solution. La réception, les tests et les limites effectives sont consignés dans Reception.md (unpublished local archive). `Verify-Lot007.ps1` exécute embeddings réels, réponses MAF, comparaisons, réindexation, réouverture dans un autre processus et fin de flux sur les cinq points d'entrée.

# Contrats métier et outils contrôlés — Lot-005

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

Le parcours rénové va de la décision sans agent à une action SQLite réelle. Les consoles et les prompts sont en anglais. Les anciens films et les supports historiques ne constituent pas le guide de cette version.

## Parcours exécutable

Depuis `BaseTechnique`, compiler avec `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`, puis lancer un projet avec `dotnet run --project <chemin.csproj> --configuration Release --no-build`. Les chemins sont donnés ci-dessous.

| Projet sous `src` | Ce qui est effectivement enseigné | Vérification pertinente |
|---|---|---|
| `jeudis/AgentOuSimpleCode.Episode02` | Calcul déterministe, orchestration C# explicite, branche agent facultative `--agent` | Exécution sans modèle ; compilation ; ce n'est pas un graphe MAF |
| `course/J1/src/Lab01_IntakeText` | Premier agent sans outil ni session persistante | Pilote texte du Lot-004 ; essai local du lot |
| `course/J1/src/Lab02_StructuredDraft` | Schéma natif `BacklogDraft`, désérialisation puis validation hôte | 11 contre-exemples métier dans `Maf.Backlog.Tests` |
| `course/J1/src/Lab03_ToolsCatalog` | Outil de lecture, allowlist de confiance, invocation observée | Catalogue exposé sans outil d'écriture ; essai local |
| `course/J1/src/Lab04_SqliteBacklog` | Contrat validé puis outil avec confirmation console dans son corps | Tests `WriteGate` et SQLite ; comparaison introductive |
| `course/J1/src/Lab04_SqliteBacklog_Extended` | Même contrat, approbation native MAF et reprise dans la même session | Tests natifs positifs, refus, absence, autre session, altération et rejeu |
| `course/J1/src/Lab05_PipelineComplete` | Catalogue hôte → analyse sans outil sensible → contrat validé → approbation → écriture | Tests de garde et boucle MAF ; deux essais réels acceptation/refus |

Le pipeline est un enchaînement C# explicite. L'agent analyste produit un contrat ; l'agent écrivain choisit d'appeler son unique outil. Il ne peut plus régénérer les champs métier à l'étape d'écriture. Le Lot-008 introduira le moteur de graphe MAF.

Les six composants historiques et `Other` constituent l'allowlist commune `Catalog.Names`. Le fichier historique `assets/components.json` reste une référence ; l'ajout d'un composant dans un document externe ne donne aucun droit. Pour modifier le catalogue, modifier cette politique hôte et ses tests.

## Contrat et effets

`shared/Maf.Backlog` contient le contrat, sa validation, les résultats et la persistance ; les agents et leurs instructions restent dans les programmes. La connexion provient exclusivement du module du Lot-003 via `DemoSettings.Load` : fichier distribué commun `shared/Configuration/ai-settings.json`, surcharges d'environnement `AI__...`, puis arguments `--AI:...`. Ollama `granite4:3b` reste le défaut, sans bascule automatique. Le modèle API et sa clé doivent être configurés explicitement ; voir [Connection.md](../lot-003/Connection.md).

Les champs obligatoires, longueurs, domaines et questions sont vérifiés côté hôte. `ClarifyingQuestions: null`, une priorité `urgent`, un composant inconnu et une demande encore ambiguë sont refusés. Aucune priorité n'est remplacée arbitrairement. Les questions sont affichées avec le draft ; une demande clarifiée fait l'objet d'une nouvelle analyse. Aucune boucle de clarification persistante n'est revendiquée.

`WriteGate` copie le draft et son tableau avant revue. L'outil ne reçoit que `proposalId`, identifiant aléatoire propre à cette proposition. Le texte source, le contenu de facture et les champs générés ne deviennent pas des paramètres d'autorisation. L'écriture revalide le contrat, exige le droit hôte et une approbation, puis consomme une capacité en mémoire. La ligne insérée reprend exactement le draft revu, sans nouvel appel de reformulation.

| Résultat hôte | Sens |
|---|---|
| `Created` et `ItemId` | Invocation observée et identifiant renvoyé par SQLite |
| `Invalid` sans identifiant | Contrat métier non actionnable |
| `Refused` sans identifiant | Droit, approbation ou proposition correspondante inutilisable |
| `Error` sans identifiant | Échec de stockage observé ; pas de réessai automatique |
| Aucune invocation | Aucun succès d'outil ne peut être affirmé ; refus MAF, demande encore pendante ou modèle sans appel |

Les consoles affichent les observations du host puis les lignes relues en base, séparément de la déclaration de l'assistant. Le test `GeneratedCreationClaimWithoutToolIsNotEvidence` illustre ce piège. Dans l'essai réel de refus, le modèle a effectivement annoncé à tort une création ; la base est vide et le host n'a exécuté aucune écriture. Ce résultat reste visible dans `ollama-refuse.log`.

La base locale par défaut est `%LOCALAPPDATA%/MafBaseTechnique/lot005-backlog.db`. `MAF_DEMO_DB_PATH` permet une base de recette isolée. `list` lit les vingt dernières lignes ; aucun reset n'est exécuté au démarrage. SQLite écrit réellement. Les fixtures de factures des Jeudis sont, elles, explicitement simulées.

`MAF_DEMO_READ_ONLY=1` retire le droit d'écriture hôte. Ce commutateur est une politique de démonstration, pas une authentification de production ni un rôle accepté depuis le prompt. L'authentification réelle relève de l'hôte qui intégrera l'exemple.

## Approbation MAF : fiche API et exercice

La boucle suit [la documentation officielle Microsoft](https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval), vérifiée contre MAF 1.26.0 : `ApprovalRequiredAIFunction`, `ToolApprovalRequestContent.ToolCall`, `request.CreateResponse(bool)`, nouveau `ChatMessage(ChatRole.User, ...)`, même `AgentSession`. Une session neuve est créée pour chaque proposition.

`ApprovalReview` n'accepte que les demandes observées dans cette session, mémorise le nom, l'identifiant d'appel et les arguments, puis consomme chaque demande. Un objet étranger, un autre appel, un autre identifiant de proposition, une mutation des arguments ou une seconde réponse ne peut pas élargir l'autorisation. Plusieurs demandes sont examinées séparément ; une seule écriture de la proposition reste possible. Une entrée vide ou autre que `y` refuse. Sans réponse, aucune reprise n'est envoyée. Les quatre tours maximaux bornent la boucle de démonstration ; une demande encore pendante n'est pas traitée comme une action réussie.

Exercice : exécuter Extended avec un bug clair, accepter puis inspecter `list`. Refaire avec une base neuve et refuser. Retirer ensuite le droit hôte et constater que `y` ne suffit pas. Dans les tests natifs, modifier les arguments après `Register`, réutiliser une demande consommée ou présenter une autre session : le host doit rejeter avant reprise. Les corrigés exécutables sont les tests `NativeMafApprovalControlsActualSqliteWrite`, `ChangedArgumentsAndRepeatedRegistrationAreRejected`, `MissingResponseAndForeignSessionDoNotAuthorize` et `MultipleRequestsAreReviewedSeparatelyAndCannotDoubleWrite`.

## Apports Jeudis réunis dans ce parcours

| Source rénovée | Apport et limite explicite |
|---|---|
| EP001 `PromptOnlyVsControlledAgent` | Prompt libre comparé au schéma et au contrôle hôte ; contre-exemple métier explicite ; aucun remboursement ni action simulée annoncée comme réelle |
| EP002 | Décider si du code suffit avant d'appeler un agent ; orchestration C# distincte de MAF Workflows |
| EP003 `Episode03PromptInjection` | Contre-exemple de spotlighting Base64 ; aucun outil sensible exposé ; aucune promesse de sécurité par encodage |
| EP003 `Episode03PromptInjectionDeuxCouches` | Analyse structurée du document sans outil sensible puis politique hôte ; même un contrat valide ne donne pas de droit |
| EP004 `AgentCiCdDemo` | Connexion commune injectée et possédée par le caller, classification anglaise et cinq tests déterministes conservés ; décision sans création de ticket |
| EP009 `AgentToolHallucinationDemo` | Fixture de facture absente ; réponse factuelle construite depuis un résultat observé `Found`/`NotFound`/`Refused`/`Error` |
| EP012 `MauvaisToolReliabilityDemo` | Catalogue volontairement vague comparé à un nom et une description précis ; les descriptions ne remplacent pas la politique hôte |

Les anciens scénarios de remboursement et les auxiliaires de tournage EP001 sont consolidés dans la comparaison backlog et ses contre-exemples. Les copies historiques sont récupérables dans la baseline Lot-001 ; aucun original n'a été modifié. Les anciens README `.txt`, prompts de tournage et exercices non rénovés doivent être lus comme provenance, pas comme procédure courante. Les nouveaux README Markdown et cet exercice sont les entrées de cette version.

## Limites

La capacité consommée ne survit pas à l'arrêt du processus. Elle n'apporte ni transaction métier distribuée ni idempotence durable entre propositions ou redémarrages : Lot-009. Aucun prétendu classificateur de prompt injection n'est une frontière de sécurité. Une sortie typée peut encore contenir de mauvaises conclusions métier ; les tests déterministes ne qualifient pas la qualité d'un modèle. La nouvelle chaîne complète a été essayée sur Ollama ; aucune nouvelle recette API du Lot-005 n'est revendiquée. Le Lot-004 reste la preuve de capacités API. Les supports historiques PowerPoint ne sont pas déclarés alignés ; la fiche et l'exercice ci-dessus font foi pour le code rénové. C12 et les plans JSON de J2 restent au Lot-008.

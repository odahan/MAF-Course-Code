# Sessions, persistance, oubli et mémoire — Lot-006

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

La progression P05 commence après les outils contrôlés du Lot-005. Elle distingue trois états : un contexte explicite SQLite, un historique conversationnel MAF et une mémoire partagée de déclarations utilisateur. Les copies rénovées utilisent la connexion commune et les instructions `memory-1.1`. Aucun catalogue métier n'a été ajouté au module de connexion.

## Exemples et garanties

| Exemple | État conservé | Redémarrage et maintenance |
|---|---|---|
| J2 Lab01_StatePersistence | Contexte SQLite ; historique MAF uniquement en processus | Initialisation sans DELETE ; `setctx TEXT` remplace le contexte et la session ; `reset` / `cleardb` vide les lignes et remplace la session |
| J2 Lab01_StatePersistence_Session | Historique MAF complet dans une enveloppe JSON | Restauration automatique ; sauvegarde après chaque réponse ; `save`, `load` ; `reset`, `deletesession`, `newsession`, `forget` suppriment le snapshot et remplacent la session |
| J2 Lab06_SharedMemory | Déclarations partagées ; injection manuelle | Mémoire en processus ; `forget` / `reset` vide les faits et remplace les deux historiques |
| J2 Lab06_SharedMemory_AiContext | Même store, injecté par AIContextProvider | Extraction après un appel réussi, uniquement depuis les messages User externes ; même contrat d'oubli |
| EP013 ContextDriftMemoryDemo | Scénario chronologique fixe ; aucune persistance des faits | Historique complet, fenêtre de trois tours, résumé généré, faits bornés puis compaction native |
| EP014 PromptProfilesDemo | Profils d'instructions versionnés dans appsettings.json | Sélection par `MAF_INSTRUCTION_PROFILE` ; connexion sélectionnée séparément |
| EP015 TokenDiet | Corpus historique copié dans la sortie ; sélection explicite de lignes | Compare corpus brut et filtre propre à cette fixture, puis les stratégies EP013 |

Les commandes sont en minuscules. Ligne vide, `quit` ou fin de flux terminent les boucles. Ctrl+C transmet l'annulation aux appels IA ; les exemples peuvent terminer avec une exception d'annulation visible. Les programmes sont séquentiels, destinés à un utilisateur local.

## Lancer et vérifier

Depuis `E:\CoursMAF\Cours\BaseTechnique` :

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
$env:MAF_STATE_ROOT = 'E:\CoursMAF\Cours\BaseTechnique\local-state'
dotnet run --project demos/Demo130_SessionPersistence -c Release --no-build
dotnet run --project demos/Demo140_NativeSession -c Release --no-build
dotnet run --project demos/Demo150_SharedFacts -c Release --no-build
dotnet run --project demos/Demo160_ContextMemory -c Release --no-build
# Archived acceptance recipe: tools/Verify-Lot006.ps1 -IncludeComparisons
```

`MAF_STATE_ROOT` doit être absolu. Par défaut, les états vont sous LocalApplicationData/MafBaseTechnique, dans deux sous-dossiers distincts `context` et `session`. Les paramètres IA, profils d'instructions et corpus sont lus depuis AppContext.BaseDirectory ; la recette lance effectivement les DLL depuis le dossier temporaire. Pour lancer depuis un autre répertoire avec `dotnet run`, donner un chemin absolu à `--project`.

Le guide de connexion du Lot-003 décrit les paramètres AI et la sélection API explicite par clé. Aucun essai API supplémentaire n'est revendiqué ici. Ollama reste le défaut ; aucune bascule implicite n'existe.

Une enveloppe de session corrompue ou d'une autre version échoue explicitement, sans création silencieuse d'une nouvelle conversation. Pour la supprimer volontairement au démarrage :

```powershell
dotnet run --project demos/Demo140_NativeSession -c Release --no-build -- --reset-state=true
```

Le fichier temporaire est écrit à côté du snapshot puis renommé. Une sauvegarde n'est pas un checkpoint de workflow, ni une transaction distribuée. Les anciens états historiques MafDay2 ne sont pas importés automatiquement.

## Admission des faits, provenance et correction

La syntaxe volontairement étroite est `key=value`, avec séparateurs point-virgule ou retour à la ligne. Exemple :

```text
module=Reporting; database=PostgreSQL
module=Billing; database=SQL Server 2022
mem
forget module
mem
forget
How can I identify the affected module?
mem
quit
```

Les clés autorisées sont product, module, priority, database, constraint, customer, version, environment, symptom et log. Une déclaration est une assertion utilisateur, pas une vérité indépendante. Le texte libre, les questions, les inférences et les réponses générées restent des données conversationnelles ; ils ne sont pas admis automatiquement comme faits.

Le writer MAF travaille sans session et reçoit uniquement le message utilisateur actuel, jamais la réponse de l'assistant. Il produit des candidats structurés. L'hôte vérifie leur égalité avec les déclarations explicites, la présence de chaque clé, l'absence de doublons, la citation exacte de provenance et la présence de la valeur dans cette citation. Il rejette la totalité du lot de candidats si une condition échoue. La même clé est remplacée ; en cas de répétition dans un message, seule sa dernière valeur est admissible.

Bornes hôte : cinq faits au total et par extraction, valeur de 256 caractères, preuve de 512 caractères et message de 4096 caractères. Quand une nouvelle clé dépasse le total, le fait le plus ancien est évincé ; les égalités sont départagées par clé ordinale. Le snapshot porte clé, valeur, citation, numéro de tour et date UTC. Les deux historiques métier sont réduits par MAF à trois tours après ajout ; ce sont des bornes en tours et en faits, pas un budget exact de tokens ou d'octets des réponses générées.

`forget key` conserve les autres faits mais remplace aussi les deux sessions pour éviter la réinjection des anciennes assertions ou résumés. `forget` supprime tous les faits. Le writer n'a pas d'historique à vider. Un numéro de génération invalide les extractions en attente ; une extraction ancienne ne peut pas écraser une correction plus récente.

Le premier essai réel a montré que la seule citation exacte ne suffisait pas : le modèle classait parfois module sous product, ou prenait une question pour un fait. Les journaux `initial-*.log` conservent cette observation. Le contrat final vérifie les déclarations explicites côté hôte. Ce choix limite volontairement l'extraction sémantique générale ; une application de production pourra ajouter une revue humaine ou des sources structurées identifiées.

Un échec de format, de fournisseur ou d'admission est affiché avec son type ; le store précédent est conservé et aucun fallback n'est appliqué. L'annulation est propagée. Si une correction est rejetée, l'ancien fait demeure et le diagnostic le dit : il faut contrôler `mem` ou oublier explicitement la clé.

## Compaction et persistance

La baseline MAF 1.26.0 expose les API expérimentales de compaction, vérifiées dans les XML NuGet locaux. La [documentation Microsoft sur la compaction](https://learn.microsoft.com/agent-framework/concepts/agents/conversations/compaction) et l'[exemple officiel](https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/02-agents/Agents/Agent_Step18_CompactionPipeline/README.md) éclairent leur place dans le cycle d'appel. Le code limite la suppression de l'avertissement MAAI001 aux deux méthodes qui les utilisent.

Deux intégrations ont des effets distincts :

- `CompactionProvider` sur `ChatClientBuilder.UseAIContextProviders` réduit la vue envoyée au modèle ; la session source conserve les tours anciens. Le test vérifie simultanément les messages sortants et la sérialisation.
- `SlidingWindowCompactionStrategy.AsChatReducer()` sur `InMemoryChatHistoryProvider`, déclenché après ajout, réduit l'historique conservé. La session sérialisée et restaurée ne contient plus les anciens tours ; un autre test le vérifie.

Les deux utilisent une fenêtre de trois tours. La compaction native ne constitue pas un oubli : l'original peut rester dans le snapshot dans la première intégration. Un résumé narratif est généré séparément, limité à 700 caractères et étiqueté non vérifié ; son échec ou son dépassement arrête la comparaison, sans résumé de secours codé en dur. Le résumé peut perdre une interdiction ou déformer un fait. Une fenêtre peut perdre un élément ancien indispensable. Les faits contrôlés peuvent être plus volumineux qu'une très courte conversation à cause de leur provenance ; aucune réduction universelle n'est promise.

## Mesure, exercice et limites

EP015 mesure la taille de contexte brut et sélectionné. La métrique caractères/4 est explicitement une estimation de ce seul contexte, sans les instructions et la question. Les compteurs input/output viennent du fournisseur pour les appels réels, y compris les extractions et le résumé. Une valeur absente est `unknown`, jamais zéro. Les coûts et la qualité ne sont pas déduits de la seule réduction de caractères ; les appels supplémentaires du writer et du résumé comptent aussi.

Les exercices rénovés sont Lab06_Ex01_SharedMemory_Facts et Lab06_Ex02_SharedMemory_Reuse, avec leurs corrigés. Ils vérifient les contrôles hôte et les instantanés. La suite Maf.Memory.Tests remplace uniquement le client de modèle par un client scripté observable ; SQLite, MAF, providers, sérialisation et compaction restent réels.

L'oubli concerne les stores et sessions actifs de ces exemples. Il n'efface pas les journaux de recette, les sauvegardes externes, les pages SQLite déjà écrites sur disque ni la rétention éventuelle d'un fournisseur API. Aucun effacement sécurisé des supports n'est revendiqué. La mémoire de faits reste en processus ; la persistance explicite est enseignée séparément. La concurrence multiprocessus, le chiffrement, les quotas de disque et la reprise de workflows appartiennent à d'autres garanties.


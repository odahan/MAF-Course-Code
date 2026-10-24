# Pilote des capacités agents

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Le pilote `src/pilot/AgentCapabilities` utilise MAF 1.26.0 et la connexion commune du Lot-003. Les instructions, schémas, outils et sessions restent visibles dans le projet pilote. Il ne migre pas encore les démonstrations historiques.

## Profils de référence

| Profil | Service et modèle | Protocole | Réglage |
|---|---|---|---|
| Local par défaut | Ollama `http://localhost:11434/`, `granite4:3b` | `/api/chat`, JSON ou NDJSON | Paramètres de raisonnement du fournisseur |
| API choisie par l'utilisateur | OpenAI `https://api.openai.com/v1/`, `gpt-6-luna` | `/v1/chat/completions`, JSON ou SSE | `reasoning_effort=none` explicite |

Le [catalogue officiel de Luna](https://developers.openai.com/api/docs/models/gpt-6-luna), consulté le 23 octobre 2026, annonce streaming, outils et sorties structurées. Il impose `reasoning_effort=none` pour les outils via Chat Completions. Ces annonces ne remplacent pas les essais réels du compte utilisé. Aucun modèle de substitution n'est sélectionné en cas d'échec.

Le profil API reste une sélection explicite : le défaut global Ollama et les embeddings indépendants sont conservés. Aucun flux d'authentification par compte ChatGPT n'est utilisé.

## Exécution

Depuis `BaseTechnique`, compiler avec `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`, puis :

```powershell
# Archived acceptance recipe: tools/Test-AgentCapabilities.ps1
# Archived acceptance recipe: tools/Test-OpenAILuna.ps1
```

La seconde commande demande la clé dans une saisie locale masquée. Elle la transmet au processus enfant par une variable temporaire `MAF_LOT004_API_KEY`, retirée dans `finally`. Elle n'enregistre la clé dans aucun fichier. Le compte doit permettre l'accès à Luna et disposer d'un financement API. Ne pas coller la clé dans une commande ou dans le chat.

Pour une variable déjà provisionnée dans le processus :

```powershell
# Archived acceptance recipe: tools/Test-AgentCapabilities.ps1 -Provider OpenAICompatible `
    -Endpoint 'https://api.openai.com/v1/' -Model 'gpt-6-luna' `
    -ApiKeyEnvironmentVariable 'MAF_API_KEY' -ReasoningEffort none
```

Le script exige endpoint et modèle explicites en mode API. Il ne lance qu'un profil. Le rapport JSON est enregistré dans `docs/lot-004/ollama.json` ou `openaiCompatible.json` (casse indifférente sur Windows), avec possibilité de fournir `-Report`. Les cinq capacités doivent réussir pour obtenir le code de sortie zéro. Chaque sonde dispose de deux minutes, l'exécution de dix minutes, chaque réponse de 1024 tokens de sortie maximum. Une boucle d'outils peut provoquer plusieurs requêtes ; ces bornes ne constituent pas un plafond financier. Ctrl+C annule le pilote lancé directement dans sa console.

## Ce que vérifient les sondes

1. **Texte** : réponse non vide obtenue via `AIAgent.RunAsync`.
2. **Structure** : `ChatResponseFormat.ForJsonSchema<TicketDecision>()` transmis nativement ; désérialisation insensible à la casse, puis validation hôte du domaine d'action, du score 0–100 et de la raison obligatoire, limitée à 300 caractères. Aucune conversion d'un score invalide ni remplacement du schéma par un prompt.
3. **Outil** : invocation par MAF d'une fonction hôte en lecture seule, sans argument, retournant un reçu aléatoire absent du prompt. Le pilote exige l'exécution de la fonction et la présence exacte de son résultat dans la réponse finale.
4. **Streaming** : parcours complet de `RunStreamingAsync`, texte non vide, nombre d'événements observé, assemblage avec `ToAgentResponse()`.
5. **Annulation** : annulation du token après le premier fragment de texte d'un flux long, poursuite de l'énumération jusqu'à sa terminaison. Aucun nouveau texte n'est accepté après annulation. Une exception d'annulation ou une terminaison propre en moins de cinq secondes est acceptée. Un timeout avant le premier texte est un échec. Cela prouve l'arrêt du flux côté client, pas l'arrêt de tout calcul ou de toute facturation côté serveur.

Chaque sonde conserve son propre résultat. Un échec n'empêche pas les autres sondes, tandis qu'une annulation globale se propage. Les erreurs publiques indiquent le type d'exception sans recopier les diagnostics potentiellement sensibles du service.

## Usage et limites

`UsageSnapshot` conserve séparément input, output et total. Un compteur absent reste `null`, une valeur mesurée de zéro reste zéro ; `Usage: null` signifie indisponible. Les chiffres sont ceux exposés par MAF et le fournisseur, sans estimation monétaire. Le test déterministe vérifie l'agrégation de deux appels de la boucle d'outils (2+2 entrées, 3+3 sorties, total 10). Un fournisseur omettant l'usage d'un appel peut produire un agrégat incomplet ; le pilote n'en fait pas une mesure exhaustive du service. L'usage de la sonde annulée reste indisponible.

Les tests HTTP de connexion vérifient pour les deux protocoles la présence d'un vrai objet schéma et de la définition d'outil dans la requête. Les tests du pilote simulent le modèle derrière un vrai agent MAF et vérifient le retour du résultat d'outil, les erreurs, l'annulation et les contrats. Ils ne qualifient pas un fournisseur réel.

Les cas de contrat reprennent les situations déterministes d'EP004 : action valide, revue humaine, action interdite, risque invalide et raison vide. Ce pilote valide une décision ; l'escalade imposée par une politique métier, les outils d'écriture et leurs autorisations restent au Lot-005. Une réponse JSON valide ne constitue jamais une autorisation métier.

Référence MAF : [sorties structurées avec les agents](https://learn.microsoft.com/agent-framework/agents/structured-output). Le pilote utilise la voie native `ResponseFormat` et ne met pas en œuvre le décorateur de conversion textuelle décrit comme solution de repli dans cette documentation.

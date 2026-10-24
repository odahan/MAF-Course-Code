# Connexion commune

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

Current setup entry point: [00-ConfigurateurDemo](../../demos/00-ConfigurateurDemo/README.md). Newly started demos read binary defaults, then the shared central file, then environment variables and command-line overrides. Editing the central file no longer requires rebuilding all demos; restart running processes. `MAF_DEMO_CONFIG` selects an existing absolute alternative file. Standalone binaries retain their local-copy fallback. The interactive utility can store API keys separately in the Windows user environment; existing process values take precedence.

Le module `shared/Maf.DemoConnection` fournit `IChatClient` et `IEmbeddingGenerator<string, Embedding<float>>`. Il ne dépend ni de WPF ni de MAF : les agents, outils, instructions et sessions restent dans les démonstrations. Les deux exemples sous `src/connection` chargent le même `ai-settings.json`, puis les variables d'environnement et enfin les arguments de commande. Les variables utilisent `__` pour les sections imbriquées. Sans configuration, Ollama est sélectionné.

## Utilisation directe

```csharp
using Maf.DemoConnection;
using Microsoft.Extensions.AI;

using IChatClient chat = DemoConnection.CreateChatClient();
var response = await chat.GetResponseAsync("Hello", cancellationToken: cancellationToken);

using var embeddings = DemoConnection.CreateEmbeddingGenerator();
var vectors = await embeddings.GenerateAsync(["A document"], cancellationToken: cancellationToken);
```

La construction est synchrone et ne fait aucun appel réseau. `CreateChatClient(settings)` suit `ChatProvider`. `isLocal: true` impose `LocalChat` ; `isLocal: false` impose `ApiChat`. Les embeddings suivent exclusivement `EmbeddingProvider` et `Embeddings`. Un chat API peut ainsi conserver les embeddings locaux, sans clé d'embeddings ni modification de modèle. Chaque fabrique valide uniquement le profil effectivement utilisé.

Dans un Generic Host ou un backend ASP.NET Core :

```csharp
var settings = ConnectionOptions.FromConfiguration(builder.Configuration);
builder.Services.AddDemoConnection(settings);
```

Les services sont des singletons créés à la première résolution. Le host les libère à l'arrêt. En utilisation directe, employer `using`. Le client possède son `HttpClient` et son handler ; un handler fourni explicitement transfère sa propriété au client retourné. Ne pas partager ce handler avec un autre propriétaire. Les transports sont séparés pour le chat et les embeddings. Il n'y a ni bascule fournisseur ni reprise automatique dans ce module ; les politiques de résilience viendront au Lot-010. Les redirections automatiques sont désactivées sur les transports par défaut.

## Profils et secrets

`shared/Configuration/ai-settings.json` contient les valeurs distribuées : chat `granite4:3b`, embeddings `embeddinggemma:300m`, Ollama `http://localhost:11434/`. Le modèle API est volontairement vide : choisir un modèle du fournisseur retenu avant activation. Le timeout HTTP par défaut est de 120 secondes, configurable entre 1 et 3600. Le timeout concerne chaque requête ; pour borner un scénario complet ou un flux, fournir aussi un token d'annulation avec échéance.

Exemple de sélection API dans la session PowerShell :

```powershell
$env:AI__ChatProvider = 'OpenAICompatible'
$env:AI__ApiChat__Endpoint = 'https://api.openai.com/v1/'
$env:AI__ApiChat__Model = '<model available in your API project>'
# Populate MAF_API_KEY from your secret manager in this process, without committing it.
dotnet demos/Demo020_ConnectionProfiles/Console/bin/Release/net10.0/Connection.Console.dll
```

`ApiKeyEnvironmentVariable` désigne la variable contenant la clé (`MAF_API_KEY` par défaut pour le chat). Un gestionnaire de secrets peut injecter cette variable au démarrage. Ne pas mettre la clé dans le JSON, les arguments de commande, une URL, une capture ou un journal. L'API OpenAI et les services compatibles utilisent la même intégration, basée sur Chat Completions et un endpoint incluant son préfixe `/v1/` ou équivalent. Un abonnement ChatGPT ne fournit pas cette authentification API.

Pour les embeddings API, sélectionner séparément `AI__EmbeddingProvider=OpenAICompatible`, puis `AI__Embeddings__Endpoint`, `AI__Embeddings__Model` et la variable `MAF_EMBEDDING_API_KEY` (ou le nom configuré). Aucun réglage de chat ne change ces valeurs. Les erreurs de configuration donnent une raison sans recopier la valeur invalide ni la clé. Les exemples affichent des diagnostics réseau génériques ; les exceptions brutes des SDK peuvent contenir des diagnostics du fournisseur et ne doivent pas être envoyées au navigateur ou journalisées sans examen.

## Embeddings

Le générateur utilise les lots de textes : `/api/embed`, `input` et `embeddings` pour Ollama ; `/embeddings`, `input` et `data[].embedding` pour l'API. Il demande des nombres flottants au mode API, remet les résultats dans l'ordre des index et vérifie le nombre de vecteurs, leur dimension non vide et homogène, et leurs valeurs finies. `EmbeddingDimensions`, facultatif, demande et contrôle la dimension attendue. Un modèle qui ne prend pas en charge une dimension demandée doit renvoyer une erreur. Sans dimension configurée, le contrôle de cohérence porte sur la réponse courante, pas sur plusieurs appels.

Ollama reçoit explicitement `truncate=false` : un texte trop long doit échouer. Le découpage du corpus appartient au Lot-007. Le générateur rejette explicitement les options supplémentaires et les overrides de modèle/dimension différents de la configuration. Il retourne le modèle configuré dans les métadonnées ; il ne certifie pas l'identité des poids derrière un alias serveur. Ne pas réutiliser un index historique `all-minilm` avec ces vecteurs. Les index, seuils et scénarios RAG restent à reconstruire et qualifier au Lot-007.

Références de protocole consultées le 23/10/2026 : [Ollama embed](https://docs.ollama.com/api/embed), [adaptateur Microsoft.Extensions.AI.OpenAI 10.11.0](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI/10.11.0). Le chat utilise les adaptateurs maintenus OllamaSharp et Microsoft.Extensions.AI.OpenAI ; le petit transport d'embeddings impose les validations ci-dessus.

## Exécution et contrôles

Depuis `BaseTechnique`, après compilation :

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
dotnet demos/Demo020_ConnectionProfiles/Console/bin/Release/net10.0/Connection.Console.dll
dotnet demos/Demo020_ConnectionProfiles/Console/bin/Release/net10.0/Connection.Console.dll --embed
dotnet demos/Demo020_ConnectionProfiles/Backend/bin/Release/net10.0/Connection.Backend.dll --urls=http://127.0.0.1:5187
```

Le backend expose `GET /health` (disponibilité du processus, sans test de connexion au modèle) et `POST /chat` avec `{"text":"Hello"}`. Il propage l'annulation HTTP au modèle. La console propage Ctrl+C. Le backend est un exemple local de connexion ; le backend applicatif, les sessions et les interfaces en streaming seront traités au Lot-012.

`./tools/Test-ConnectionLocal.ps1` effectue les appels réels console/chat/embedding/backend, vérifie un rejet HTTP 400 et arrête son backend de test. Il nécessite les deux modèles locaux installés et un port 5187 libre ; les fichiers de preuve sous `docs/lot-003` sont remplacés lors d'une nouvelle exécution. Les tests déterministes n'ont besoin ni d'Ollama ni de clé réelle.

La prise en charge d'un protocole par le module ne qualifie pas tous ses fournisseurs ou modèles. Les outils, sorties structurées, usages et autres capacités nécessaires aux agents seront qualifiés et documentés au Lot-004.

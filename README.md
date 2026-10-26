# Lumber Jack

Jogo em primeira pessoa de lenhador, feito em **Unity 6** (URP), com visual low poly estilizado (referência:
*Len's Island*). O jogador derruba árvores procedurais, corta a madeira em qualquer ponto, carrega os pedaços,
refina em tábuas e vende para comprar novas ferramentas. O mundo tem relógio, calendário, estações e uma floresta
que se renova sozinha.

> Projeto em desenvolvimento. O multiplayer está planejado, então a lógica de jogo é determinística (seeds
> explícitas e IDs estáveis).

## Funcionalidades

- **Árvores procedurais**: tronco, galhos (layouts em espiral, verticilo e bifurcação) e folhagem gerados por espécie
  (`TreeSpeciesDefinition`), com malhas flat shaded dentro do orçamento de triângulos.
- **Corte e queda**: qualquer galho ou tronco pode ser cortado com o machado; o corte divide a malha de verdade
  (`MeshSlicer`), a árvore cai com física calculada e os galhos quebram na queda. Prévia do corte com inclinação
  ajustável.
- **Madeira**: cada pedaço tem uma malha única, volume e valor conforme espécie e tamanho.
- **Carregar objetos**: o jogador agarra rigidbodies limitado pela força (`PlayerStats`), com indicador do ponto
  agarrado.
- **Economia**: ponto de venda de madeira, refinaria (madeira bruta → tábua de largura/altura ajustáveis), esteira,
  loja com caixa e detector de furto. Dinheiro sempre em centavos inteiros.
- **Inventário**: hotbar com 9 espaços (UI Toolkit) e ferramentas (`ToolDefinition`), como o machado de pedra.
- **Tempo do mundo**: relógio, calendário, estações, posição do sol pela latitude do mapa e ciclo dia/noite.
- **Floresta viva**: mudas nascem uma vez por dia do jogo, tocos apodrecem e algumas espécies rebrotam.
- **Veículos**: carro com `WheelCollider`, que o jogador dirige depois de entrar pela porta.
- **Áudio**: sons de machado, queda de árvore, passos por superfície e áudio espacial.

## Requisitos

| Item | Versão |
| --- | --- |
| Unity Editor | **6000.6.7f1** (instale pelo Unity Hub; outra versão pode reimportar e alterar assets) |
| Render pipeline | Universal Render Pipeline 17.6 |
| Input | Input System 1.20 (o `Input` legado não é usado) |
| Câmera | Cinemachine 6.6 |
| IDE | JetBrains Rider (recomendado) ou Visual Studio |

Os pacotes ficam em `Packages/manifest.json` e são baixados automaticamente ao abrir o projeto.

## Como abrir

1. Clone o repositório:
   ```bash
   git clone https://github.com/RSWeslei/lumber-jack.git
   ```
2. No Unity Hub, clique em **Add → Add project from disk** e escolha a pasta clonada.
3. Abra com o Editor **6000.6.7f1**. A primeira abertura gera a pasta `Library/` e pode demorar alguns minutos.
4. Abra a cena `Assets/Project/Scenes/Main.unity` e aperte **Play**.

## Controles

| Ação | Teclado e mouse | Gamepad |
| --- | --- | --- |
| Mover | `W` `A` `S` `D` / setas | Analógico esquerdo |
| Olhar | Mouse | Analógico direito |
| Correr | `Shift` esquerdo | Gatilho esquerdo |
| Pular | `Espaço` | Botão sul (A / ✕) |
| Golpear com a ferramenta | Botão esquerdo do mouse | |
| Interagir | `E` | |
| Agarrar / carregar (segurar) | `G` | |
| Inclinar o corte | `Q` / `R` | |
| Endireitar o corte | `C` | |
| Escolher espaço da hotbar | `1` a `9` / roda do mouse | |
| Veículo: dirigir / frear | `W` `A` `S` `D` / `Espaço` | |

Os mapeamentos ficam em `Assets/Project/Core/Input/*.inputactions`. Para mudar uma tecla, edite o `.inputactions`;
as classes C# geradas (`*Inputs.cs`) não devem ser editadas à mão.

## Estrutura do projeto

```
Assets/
├── Project/                  # Todo o código e conteúdo do jogo (assembly LumberJack)
│   ├── Core/                 # Base: input, interação, ranges, SeededRandom, dinheiro
│   ├── Shared/               # UI compartilhada, fontes, materiais, utilitários de malha
│   ├── Features/             # Uma pasta por funcionalidade
│   │   ├── Trees/            # Geração, corte, queda, folhas e crescimento das árvores
│   │   ├── Wood/             # Pedaços de madeira, fatiador de malha, prévia do corte
│   │   ├── Player/           # Movimento, câmera, mira, braços, carregar objetos
│   │   ├── Tools/            # Ferramentas (machados)
│   │   ├── Inventory/        # Itens e hotbar
│   │   ├── Machines/         # Refinaria e esteira
│   │   ├── Shop/             # Venda de madeira, loja e caixa
│   │   ├── WorldTime/        # Relógio, calendário, estações, sol, dia/noite
│   │   ├── Vehicles/         # Veículos
│   │   └── Audio/            # Definições de som e áudio espacial
│   ├── Editor/               # Ferramentas de editor (assembly LumberJack.Editor)
│   ├── Scenes/               # Main.unity e cenas de teste em Sandbox/
│   └── Settings/             # Assets do URP, volumes e iluminação
├── ThirdParty/               # Assets de terceiros (não editar)
└── TextMesh Pro/             # Pacote do TextMesh Pro (não editar)
```

### Cenas

| Cena | Uso |
| --- | --- |
| `Scenes/Main.unity` | Cena principal do jogo (única na build) |
| `Scenes/Sandbox/TreeSandbox.unity` | Visualização e ajuste das espécies de árvore |
| `Scenes/Sandbox/TreePhysicsTest.unity` | Teste da queda das árvores |
| `Scenes/Sandbox/WoodCuttingTest.unity` | Teste do corte de madeira |

### Ferramentas do editor

O menu **Lumber Jack** na barra do Unity reúne as ferramentas do projeto:

- **Aplicar iluminação do mundo**: configura sol, céu e volume da cena. Ajuste a iluminação por aqui (e as cores ao
  longo do dia no asset `DayNight`), não à mão na cena.
- **Configurar personagem do jogador**: monta o rig do jogador.
- **Criar cena de teste de árvores** / **Criar cena de corte de madeira**: geram as cenas de sandbox.
- **Testes**: corte e queda de árvores, fotos das árvores e dos braços em primeira pessoa.

O componente `ProceduralTree` também tem inspector próprio para gerar e visualizar a árvore no editor.

## Arquitetura

- **Definição, estado e view separados**: dados compartilhados em ScriptableObjects com sufixo `Definition`, estado
  em classes C# serializáveis e MonoBehaviours finos que só ligam a Unity à lógica.
- **Lógica pura em C#** (geração, cálculos, regras) em classes `sealed` testáveis, sem depender de cena.
- **Estratégias no inspector**: interfaces com várias implementações escolhidas via
  `[SerializeReference, SubclassSelector]` (ex.: `IBranchLayout`).
- **Comunicação**: referência direta dentro de uma feature, eventos C# ou interfaces entre features.
- **Mira única**: tudo que o jogador mira passa por `PlayerAim.TryFindTarget`; o que o machado corta implementa
  `IAxeTarget`.
- **Determinismo**: aleatoriedade com `SeededRandom` e seed explícita, para todos os clientes verem o mesmo mundo.
- **Desempenho**: meta de 0 bytes de alocação gerenciada por quadro durante o jogo.

### Camadas (layers)

| Layer | Uso |
| --- | --- |
| `Player` | Jogador |
| `Wood` | Toras, pedaços e tábuas |
| `Interactable` | Objetos usáveis (botões, caixas) |
| `Ground` | Chão onde as mudas podem nascer |
| `FirstPerson` | Braços e ferramenta em primeira pessoa |

## Padrões de código

- C# 9 (compilador da Unity): sem namespaces de arquivo, `global using` ou recursos do C# 10+.
- Formatação e nomes definidos no `.editorconfig`: campos privados `_camelCase`, membros públicos `PascalCase`,
  booleanos como pergunta (`IsGrounded`).
- Namespace `LumberJack.<Feature>`; código de editor em `Assets/Project/Editor/<Feature>/`.
- Identificadores em inglês; comentários, tooltips e textos de editor em português, com acentos.
- Scripts em UTF-8.

As regras completas estão em [`CLAUDE.md`](CLAUDE.md).

## Controle de versão

- O `.gitignore` já ignora `Library/`, `Temp/`, `Logs/`, `Build/` e os arquivos gerados de IDE.
- Sempre faça commit do arquivo `.meta` junto com o asset. Para mover ou renomear, faça pelo Unity ou com `git mv`
  do asset e do `.meta`.
- Feche a Unity antes de mudanças grandes de pastas feitas fora do editor.

## Build

1. **File → Build Profiles**.
2. Confirme que `Assets/Project/Scenes/Main.unity` está na lista de cenas.
3. Escolha a plataforma (Windows é a principal) e clique em **Build**, salvando em `Build/` (ignorada pelo git).

Problemas de desempenho que só aparecem no editor devem ser confirmados numa build, com o Profiler.

## Ideias futuras

As ideias anotadas para depois (biomas, viagens para outros países, efeitos visuais das estações, perks de força)
estão em [`Ideias.md`](Ideias.md).

## Créditos

Assets de terceiros em `Assets/ThirdParty/`: Kenney (modelos e áudio), Gridbox Prototype Materials, Low Poly Sky,
Low Poly Dungeons Lite, PSX First Person Arms, Medieval Weapon Sounds e Wood Chopping Sounds. Cada um segue a
licença do seu autor.

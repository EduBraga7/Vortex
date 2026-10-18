# VORTEX

> **Gênero:** Top-Down Action / Bullet Heaven (Survivor-like)  
> **Engine:** Unity 6 (Universal Render Pipeline - URP)  
> **Modelagem 3D:** Blender (Pipeline Scriptado Low-Poly Sci-Fi via MCP / Python `bpy`)  
> **Plataformas Alvo:** PC (Windows) e WebGL  

---

## 🌌 Visão Geral

**Vortex** é um jogo de sobrevivência em arena com visão superior (*top-down*). O jogador controla um núcleo mecanizado experimental preso em uma anomalia dimensional quântica. Cercado por hordas geométricas hostis, o jogador foca na movimentação espacial e esquiva, enquanto seu arsenal dispara e evolui automaticamente em partidas frenéticas de **10 minutos**.

---

## 🎮 Controles

| Ação | Tecla / Controle |
| :--- | :--- |
| **Movimentação** | `W`, `A`, `S`, `D` ou `Setas Direcionais` |
| **Interface / Upgrades** | `Mouse` (Clique esquerdo nos cartões de melhoria) |

---

## ⚡ Core Systems & Mecânicas

### 1. Arsenal de Armas (Evolução Nível 1 ao 5)
Todas as armas herdam de `WeaponBase` e possuem ciclos de disparo com suporte à redução de tempo de recarga (*Cooldown Reduction*):
* **Pulse Blaster** *(Arma Inicial)*: Varre inimigos em um raio de 12m e dispara rajadas lineares que evoluem com disparos duplos e perfuração (*Pierce*).
* **Orbiting Blades** *(Defensiva / Curto Alcance)*: Lâminas de plasma que orbitam continuamente o núcleo a 2.5m de distância, com velocidade hiper-rápida e empurrão (*Knockback*).
* **Vortex Shockwave** *(Controle de Grupo / Área)*: Pulso circular em área com anel expansivo gradativo, alta repulsão e pulso duplo que eletrifica o campo de batalha.

### 2. Inimigos & Escalonamento de Hordas
Gerenciamento de entidades 100% otimizado via `ObjectPooler` para alta taxa de quadros (60+ FPS):
* **Swarmer** (15 HP | Vel 4.5 | Dano 5): Inimigos leves e ágeis que atacam em bando direto em direção ao jogador.
* **Bruiser** (60 HP | Vel 2.2 | Dano 15): Tanques blindados pesados com 75% de resistência a empurrões.
* **Core Boss** (800 HP | Vel 3.5 | Dano 30 | Escala 3x): Titã dimensional que surge como clímax nos marcos de **5:00** e **10:00**.

### 3. Progressão & Level Up
* Coleta de **Cristais de Energia (XP)** com atração magnética configurável (*Pickup Radius*).
* Ao preencher a barra de XP, o tempo congela (`Time.timeScale = 0`) e 3 cartas randômicas de evolução são apresentadas ao jogador.

### 4. Ciclo de Partida (Game Loop)
* **Condição de Vitória:** Sobreviver por 10 minutos (600 segundos).
* **Condição de Derrota:** Ter o HP do núcleo reduzido a 0.
* **Painel de Fim de Jogo (EndGameUI):** Exibe o tempo sobrevivido, quantidade de inimigos derrotados e botão de reinício instantâneo.

---

## 📁 Estrutura do Projeto

```text
Vortex/
├── Assets/
│   ├── Art/
│   │   ├── Materials/         # Materiais URP com emissão neon (Lit/Unlit)
│   │   └── Models/            # Modelos 3D Low-Poly (.fbx) exportados do Blender
│   ├── Editor/
│   │   └── VortexSceneBuilder.cs  # Automação de cena em 1 clique
│   ├── Prefabs/               # Prefabs configurados (Player, Inimigos, Projéteis, XP)
│   ├── Scenes/                # Cena principal (SampleScene.unity)
│   └── Scripts/
│       ├── Camera/            # TopDownCameraFollow suave
│       ├── Combat/            # WeaponBase, PulseBlaster, OrbitingBlades, Shockwave
│       ├── Core/              # GameManager, ObjectPooler, EventBus
│       ├── Enemies/           # EnemyAI, EnemyHealth, WaveSpawner
│       ├── Player/            # PlayerMovement, PlayerStats, PlayerHealth
│       └── UI/                # HUDController, LevelUpUI, EndGameUI
├── AGENTS.md                  # Diretrizes de arquitetura para IA e desenvolvedores
├── GDD.MD                     # Documento de Game Design completo
└── README.md                  # Este documento
```

---

## 🚀 Como Executar o Projeto

1. Abra a pasta do projeto no **Unity 6** (ou Unity 2022 LTS com URP).
2. Abra a cena principal em: `Assets/Scenes/SampleScene.unity`.
3. Para reconstruir, conectar ou resetar todos os sistemas automaticamente, utilize o menu do editor:
   ```text
   Vortex Tools -> 4. Build Sprint 4 (Full Arsenal, Waves, 3D Models & EndGame)
   ```
4. Pressione o botão **Play** no topo do editor da Unity.

---

## 🛠️ Pipeline Técnica

* **Desacoplamento por EventBus:** Todos os sistemas comunicam dados de vida, experiência, contadores e fim de jogo através de eventos C# estáticos, eliminando acoplamentos rígidos entre UI e Gameplay.
* **Física & Alvo:** Utilização exclusiva de `Physics.OverlapSphereNonAlloc` com buffers pré-alocados para evitar qualquer alocação de lixo (Zero GC Alloc em loops de ataque).
* **Animações Procedurais:** Rotações angulares contínuas, flutuações e efeito visual de *squash and stretch* ao receber dano são calculados proceduralmente em C#, eliminando a necessidade de rigs ou clipes de animação pesados.

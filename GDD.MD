# Game Design Document (GDD) — VORTEX

**Versão:** 1.0 (MVP)  
**Gênero:** Top-Down Action / Bullet Heaven (Survivor-like)  
**Engine:** Unity 6 / 2022 LTS (Universal Render Pipeline - URP)  
**Modelagem 3D:** Blender (Pipeline Scriptado / Low-Poly Sci-Fi)  
**Plataformas Alvo:** PC (Windows) e WebGL (itch.io)  

---

## 1. Visão Geral (Elevator Pitch)
**Vortex** é um jogo de sobrevivência em arena com visão superior onde o jogador controla um núcleo mecanizado experimental preso em uma anomalia espacial. Cercado por hordas incessantes de entidades geométricas hostis, o jogador foca exclusivamente na movimentação e esquiva, enquanto seu arsenal dispara automaticamente e evolui a cada nível alcançado em partidas rápidas de 10 minutos.

---

## 2. Direção de Arte e Pipeline 3D (Blender -> Unity)
* **Estilo Visual:** Sci-Fi Minimalista / Low-Poly Geométrico.
* **Materiais:** Uso exclusivo de materiais URP Lit/Unlit com cores sólidas e *Emission* (brilho neon em ciano, magenta, laranja e roxo) combinado com Pós-Processamento (*Bloom*). Sem necessidade de mapeamento UV complexo ou texturas pintadas à mão.
* **Animações:** 100% procedurais via código C# (rotação contínua, flutuação com ondas senoidais `Mathf.Sin`, efeito de *squash and stretch* ao receber dano) para eliminar a necessidade de Rigging/Weight Painting no Blender.
* **Regras de Exportação (Blender via MCP):**
  * Escala: `1.0` (1 unidade Blender = 1 metro Unity).
  * Orientação: `-Z Forward`, `Y Up`.
  * Formato: `.fbx` salvo diretamente em `Assets/Art/Models/`.

---

## 3. Core Gameplay Loop (Fluxo Principal)
1. **Combate e Esquiva:** O jogador se move pela arena infinita/delimitada enquanto suas armas atacam automaticamente os inimigos no alcance.
2. **Eliminação e Coleta:** Inimigos derrotados deixam cair Cristais de Energia (XP).
3. **Level Up:** Ao preencher a barra de XP, o jogo pausa (`Time.timeScale = 0`) e exibe 3 opções randômicas de melhoria.
4. **Escalonamento:** A cada minuto, o *Wave Spawner* aumenta a quantidade e a vida dos inimigos.
5. **Condição de Vitória/Derrota:** Sobreviver até o marco de **10 minutos** (Vitória) ou ter o HP reduzido a **0** (Game Over).

---

## 4. Mecânicas e Sistemas do Jogador

### 4.1 Atributos Base (PlayerStats)
| Atributo | Valor Inicial | Descrição |
| :--- | :--- | :--- |
| **Max HP** | 100 | Vida máxima do núcleo. |
| **Move Speed** | 6.0 | Velocidade de deslocamento no plano XZ. |
| **Pickup Radius** | 3.0m | Raio magnético para atrair gemas de XP. |
| **Cooldown Reduction** | 0% | Redução global no tempo de recarga das armas. |

### 4.2 Controles
* **Teclado:** `WASD` ou `Setas Direcionais` para movimentação em 8 direções / 360 graus.
* **Mouse:** Usado apenas para clicar nos cartões de Upgrade e menus de UI.

---

## 5. Arsenal e Evoluções (MVP - 3 Armas)

Todas as armas herdam de uma classe base abstrata `WeaponBase` e utilizam `ScriptableObjects` para configurar seus níveis (Nível 1 ao 5).

### 1. Pulse Blaster (Arma Inicial)
* **Comportamento:** Localiza o inimigo mais próximo dentro de um raio de 12 metros e dispara um projétil linear de energia.
* **Progressão:**
  * Nv 1: 1 projétil, 15 de dano, 1.0s de recarga.
  * Nv 2: +25% velocidade de disparo.
  * Nv 3: Dispara +1 projétil adicional em sequência.
  * Nv 4: Projéteis atravessam +1 inimigo (*Pierce*).
  * Nv 5 (Evolução): Cadência dobrada e dano aumentado para 30.

### 2. Orbiting Blades (Defensiva / Curto Alcance)
* **Comportamento:** Esferas/lâminas de plasma que giram permanentemente ao redor do jogador a uma distância fixa de 2.5 metros.
* **Progressão:**
  * Nv 1: 2 lâminas orbitais, 10 de dano por contato.
  * Nv 2: +30% velocidade de rotação.
  * Nv 3: +1 lâmina orbital (total 3).
  * Nv 4: Aumenta o tamanho das lâminas em 25%.
  * Nv 5 (Evolução): 4 lâminas permanentes com rotação hiper-rápida e empurrão (*Knockback*).

### 3. Vortex Shockwave (Controle de Grupo / Área)
* **Comportamento:** Emite um pulso circular a partir do jogador que causa dano em todos os inimigos ao redor e os empurra para trás.
* **Progressão:**
  * Nv 1: Raio de 4m, 20 de dano, 3.5s de recarga.
  * Nv 2: Raio aumentado para 5.5m.
  * Nv 3: Recarga reduzida para 2.8s.
  * Nv 4: +50% de força de repulsão (*Knockback*).
  * Nv 5 (Evolução): Pulso duplo que deixa a área eletrificada por 2 segundos.

---

## 6. Inimigos e Sistema de Hordas

Todos os inimigos utilizam `Object Pooling` para evitar instanciamento e destruição custosos (`Instantiate`/`Destroy`) e garantir 60+ FPS com centenas de objetos em tela.

| Nome | Geometria Base (Blender) | HP | Velocidade | Dano | Comportamento |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Swarmer** | Pirâmide/Octaedro afiado | 15 | Rápida (4.5) | 5 | Persegue o jogador em linha reta. Surge em grandes grupos. |
| **Bruiser** | Cubo chanfrado pesado | 60 | Lenta (2.2) | 15 | Alta resistência a empurrões, avança implacavelmente. |
| **Core Boss** | Icosaedro com anéis | 800 | Média (3.5) | 30 | Surge aos 5 e 10 minutos. Tamanho 3x maior. |

### Regras do Spawner
* Os inimigos nascem em um círculo invisível fora do campo de visão da câmera (ex: raio de 22 metros a partir da posição atual do Player).
* **0:00 a 2:00:** Apenas *Swarmers* (1 a cada 1.5s).
* **2:00 a 5:00:** Hordas mistas de *Swarmers* e *Bruisers* (3 a cada 1.0s).
* **5:00 a 10:00:** Hordas densas contínuas + Aparição do *Core Boss*.

---

## 7. Interface de Usuário (UI / HUD)
* **HUD de Gameplay:**
  * Topo: Barra de XP horizontal ocupando toda a largura da tela + Texto de Nível Atual.
  * Abaixo da barra de XP: Cronômetro centralizado (`MM:SS`) e contador de abates no canto direito.
  * Sob o Jogador: Pequena barra de vida acoplada ao espaço do mundo (World Space Canvas).
* **Tela de Level Up:** Painel central contendo 3 botões verticais/horizontais com Ícone, Nome da Arma, Nível e Descrição da melhoria.
* **Tela de Fim de Jogo:** Mostra "Vitória" ou "Game Over", tempo sobrevivido, inimigos derrotados e botão de "Jogar Novamente".

---

## 8. Arquitetura Técnica (Regras para o Agente de IA)

Para manter a sanidade do projeto gerido via MCP/AntiGravity, todo código gerado deve obedecer às seguintes diretrizes:

1. **Estrutura de Diretórios:**
   * `Assets/Scripts/Core/` (GameManager, ObjectPooler, EventBus)
   * `Assets/Scripts/Player/` (PlayerMovement, PlayerStats, PlayerHealth)
   * `Assets/Scripts/Combat/` (WeaponBase, Projectile, Armas específicas)
   * `Assets/Scripts/Enemies/` (EnemyAI, EnemyHealth, WaveSpawner)
   * `Assets/Scripts/UI/` (HUDController, LevelUpUI)
   * `Assets/ScriptableObjects/` (Dados de Armas, Dados de Inimigos, Configs de Waves)
2. **Desacoplamento:** A UI nunca referencia o Player diretamente; utilize `C# Actions` / `UnityEvents` (ex: `OnXPChanged`, `OnPlayerLevelUp`, `OnPlayerDeath`).
3. **Performance:** Proibido usar `GameObject.Find` ou `GetComponent` dentro de loops `Update()`. Usar cache de referências no `Awake()` e `Physics.OverlapSphereNonAlloc` para detecção de alvos.
4. **Automação de Cena:** Como o usuário opera via MCP, sempre que possível gere scripts de Editor (`Assets/Editor/SceneSetupTools.cs`) com botões no menu do topo da Unity (`Vortex Tools -> Setup Scene`) que criam e conectam os GameObjects automaticamente na cena sem exigir configuração manual complexa no Inspector.
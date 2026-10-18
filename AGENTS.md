# AGENTS.md — Engineering & Game Design Directives (VORTEX)

This document defines mandatory directives for architecture, mechanics design, and tool operations (Unity & Blender via MCP) for any AI agent or developer contributing to this repository.

---

## 1. Core Engineering Principles: Anti-Overengineering & Anti-Abstraction

### The Conditional Abstraction Rule
> **Complex architectures, abstractions, or indirection layers are STRICTLY PROHIBITED unless explicitly mapped to a concrete future phase in the project roadmap.** If there is no documented future requirement, implement using the most direct, legible, and maintainable approach (Strict YAGNI).

* **No Speculative Coding:** Do not implement generic interfaces, complex inheritance hierarchies, abstract factory patterns, arbitrary dependency injection, or repository wrappers unless at least two concrete consumers exist today or are scheduled for the immediate next phase.
* **Favor Flat Composition over Inheritance Trees:** Deep class hierarchies degrade readability and complicate Unity component inspection. Prefer cohesive `MonoBehaviour` components combined with `[RequireComponent]`.
* **Zero Trivial Wrappers:** If Unity provides an API that accomplishes the goal (e.g., `AudioSource.PlayClipAtPoint`, `Physics.OverlapSphereNonAlloc`, `Mathf.MoveTowards`), use it directly. Never create boilerplate "Managers" whose only function is forwarding calls.

---

## 2. Design Interpretation: Intended Experience vs. Literal Mechanics

* **Focus on Feeling and Pacing (Experience-Driven):** When receiving a prompt, interpret *what gameplay sensation the user aims to create* (e.g., urgency, tension, power fantasy, spatial control, flow state) rather than blindly coding the mechanic as a literal, isolated checklist.
* **No Preemptive Mechanical Bias:** Never dismiss or judge a mechanic as inherently "good" or "bad" based on subjective intuition. Every mechanic serves a specific purpose depending on context and execution.
* **Mandatory Precedent Research:**
  * When encountering questions on tuning, cadence, damage curves, spawn rates, or mechanics interactions, **ALWAYS research proven precedents from commercially successful titles**.
  * **Strict Priority on Mechanical Reference:** References MUST be **FULLY or near-fully aligned with the GAMEPLAY MECHANICS** of the game (e.g., Bullet Heaven / Survivor-like, Top-Down Arena Shooter, Auto-attack survival), **NOT** with the narrative setting, lore, or visual aesthetic.
  * *Primary Mechanical References for Vortex:* *Vampire Survivors*, *Brotato*, *20 Minutes Till Dawn*, *Soulstone Survivors*, *Nova Drift*.

---

## 3. Tooling & MCP Pipeline Directives

### 3.1 Unity MCP (`unity-mcp`)
* **Inspection & Diagnostics:**
  * Use `Unity_GetProjectData`, `Unity_GetConsoleLogs`, and `Unity_ReadConsole` to verify scene state, project taxonomy, and compilation output before and after modifying code.
  * Validate compilation after script edits via `Unity_ValidateScript` or console logs.
* **Scene & Hierarchy Automation:**
  * Automate object setup, wiring, and component configuration using editor utilities or `[MenuItem("Vortex Tools/...")]` commands.
  * Avoid requiring manual inspector drag-and-drop actions from the user whenever possible.

### 3.2 Blender MCP (`blender`)
* **Procedural Low-Poly Asset Creation:**
  * Use `execute_blender_code` to generate parametric, low-poly geometry (e.g., chamfered cubes, octahedrons, pyramids, icosahedrons, orbit rings).
  * Verify scene hierarchy with `get_scene_info` and preview visual outputs using `look` when needed.
* **Export Standards for Unity Compatibility:**
  * **Scale:** `1.0` (1 Blender Unit = 1 Unity Meter).
  * **Axis Orientation:** `-Z Forward`, `Y Up`.
  * **Target Directory:** Export `.fbx` files directly to `Assets/Art/Models/` (or `Assets/Models/`).
  * **Procedural Transforms:** Keep models static in Blender; handle bobbing, spin, wobble, and squash-and-stretch procedurally in C# to ensure zero animation pipeline friction.

---

## 4. Agent Pre-Commit Checklist

Before generating code or creating assets, verify:
- [ ] Is this the most direct implementation delivering the intended player experience?
- [ ] Have all unnecessary indirection layers or speculative abstractions been removed?
- [ ] For combat balance, mechanics, or pacing questions, has a mechanically aligned Survivor-like precedent been referenced?
- [ ] Have MCP tools been used to validate compilation, asset paths, and scene integration?

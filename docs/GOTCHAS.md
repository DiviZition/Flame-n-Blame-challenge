# GOTCHAS.md — Mistakes, Restrictions & Rules

Things that bit us or will bite us. When we struggle with something, waste time on it, or
discover a hard restriction — it goes here so the next person (human or AI) doesn't repeat it.

**Rules:**
- Append an entry the moment the lesson is learned, not "later".
- Each entry states the **Rule** in one line — that's what people actually read.
- Mark `Resolved` or `Avoided` when no longer relevant, but keep the entry (it's history).

**Entry format:**
```
## G-XXX: <Short title>
- **Status:** Open | Resolved | Avoided
- **What happened / will happen:** ...
- **Rule:** ...
```

---

## G-001: Game asmdef references an editor-only assembly
- **Status:** Open
- **What happened / will happen:** `Assets/_Core/FlameNBlame.asmdef` line 9 references
  `GUID:b93f844d45cfcc44fa2b0eed5c9ec6bb` = `com.unity.ide.visualstudio` **editor** assembly.
  With no scripts in `_Core` it compiles fine, but the first script added there will make
  Unity refuse to compile the runtime assembly for player builds.
- **Rule:** Remove that reference before/when adding the first script under `_Core`; editor-only
  code goes in its own asmdef under `_Core/Editor` with `includePlatforms: ["Editor"]`.

## G-002: Invalid rootNamespace in FlameNBlame.asmdef
- **Status:** Open
- **What happened / will happen:** `"rootNamespace": "Flame-n-Blame-Challenge"` — hyphens are not
  valid in C# identifiers. Scripts created via the editor (which auto-insert the root namespace)
  will generate non-compiling code.
- **Rule:** Change `rootNamespace` to a valid identifier (e.g. `FlameNBlame`) before creating
  scripts through the editor. Manually written files must declare `namespace FlameNBlame…`
  themselves — see STYLE.md.

## G-003: Build settings point at a scene that doesn't exist
- **Status:** Open
- **What happened / will happen:** `EditorBuildSettings.asset` lists `Assets/Scenes/SampleScene.unity`
  (deleted). Building or some editor tooling may fail or silently build an empty scene list.
- **Rule:** After creating the first real scene, re-add it in Build Settings (File → Build Profiles).
  Note that this bites *harder* on WebGL: a build with an empty scene list produces a page that
  loads and shows nothing (D-007).

## G-004: Feel AI skills are not visible to Claude Code by default
- **Status:** Open
- **What happened / will happen:** The Feel skills in `Assets/Feel/Skills/` are only loaded if
  exposed via `.claude/skills/`. Without the junction, agents will guess Feel's component names
  and API instead of reading the skills.
- **Rule:** Create the junction once (from project root, PowerShell):
  `New-Item -ItemType Junction -Path .claude\skills -Target Assets\Feel\Skills`
  then start a fresh session. Before any Feel work, agents must read the matching SKILL.md.

## G-005: Non-standard Unity install location
- **Status:** Avoided
- **What happened / will happen:** Unity Hub's secondary install path is `X:\Programs\Unity\Editors`
  (not `C:\Program Files\Unity\Hub\Editor`), so the editor binary is at
  `X:/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe`.
- **Rule:** Use that full path for any CLI invocation (batchmode, tests); don't assume C:\.

## G-006: Store-asset folders get overwritten by updates
- **Status:** Avoided
- **What happened / will happen:** `Assets/Feel`, `Assets/Plugins/*`, `Assets/Pivot++`,
  `Assets/Cartoon FX Pack`, etc. are vendor-owned; an asset update replaces their contents.
- **Rule:** Never put our scripts, scenes, or modified vendor files in vendor folders. Our code
  lives in `Assets/_Core` only. If a vendor file must change, copy it into `_Core` and modify the copy.

## G-007: Generated files are ignored — don't track them
- **Status:** Avoided
- **What happened / will happen:** `Library/`, `Temp/`, `Logs/`, root `*.csproj`, `*.slnx` are
  Unity-generated and gitignored. `Library/` still appears on disk (fine). Two `.slnx` files are
  tracked because they were committed before the ignore rule existed.
- **Rule:** Never `git add` generated files. If a tracked-but-ignored file needs removing
  (e.g. the duplicate `Flame_Blame-challenge.slnx`), do it explicitly with `git rm --cached`.

## G-008: Test framework is transitive, not a direct dependency
- **Status:** Open
- **What happened / will happen:** `com.unity.test-framework` 1.8.0 is present in `packages-lock.json`
  only as a transitive dependency (depth 1), so the Test Runner works, but relying on a transitive
  dep is fragile — an unrelated package update could drop it.
- **Rule:** When the first tests are written, add `com.unity.test-framework` to `Packages/manifest.json`
  explicitly and create a `_Core/Tests` asmdef referencing `UnityEngine.TestRunner` +
  `UnityEditor.TestRunner`.

## G-009: `CLAUDE.md` cannot live in `docs/`
- **Status:** Avoided
- **What happened / will happen:** Moving the project docs into `docs/` (D-011) looks like it should
  include `CLAUDE.md`. It must not: Claude Code only auto-loads `CLAUDE.md` from the project root,
  so a `docs/CLAUDE.md` is simply never read — the loss is silent, and every future session then
  works without the commands, vendor boundaries or conventions, with nothing broken to notice by.
- **Rule:** `CLAUDE.md` stays at repo root as a thin entry point that points into `docs/`; the other
  docs move freely. When adding a doc, put it in `docs/` and add a pointer from `CLAUDE.md`.

## G-010: Renaming a render pipeline asset rebinds and retunes nothing
- **Status:** Open
- **What happened / will happen:** `PC_RPAsset` → `Web_RPAsset` (with `PC_Renderer` →
  `Web_Renderer`) renamed the files and kept their GUIDs, so it *looked* done. But the pipeline is
  bound through **quality levels**, not file names: `Web_RPAsset` is still attached only to the
  "PC" level, the "PC" level excludes WebGL (`QualitySettings.asset:119`), `WebGL` defaults to
  level 0 "Mobile" (`QualitySettings.asset:137`), and that level's pipeline GUID `5e6cbd92…` pointed
  at the deleted `Mobile_RPAsset` — so a web build has no pipeline (B-001). The editor shows none of
  this because `m_CurrentQuality: 1` runs the PC level. The asset also still carries PC-grade
  settings under its web name.
- **Rule:** After renaming/deleting any pipeline asset, switch the actual build target and check
  quality-level → pipeline routing per platform — the editor's current view proves nothing. Renaming
  buys a name, never a retune: budget a separate pass to strip HDR/shadows/post for web.

## G-011: Haptics don't exist on the shipping target
- **Status:** Open
- **What happened / will happen:** Nice Vibrations ships inside Feel and its modules are tempting
  because they are two clicks away in an `MMF_Player`. On WebGL there is no haptic device path at
  all, so anything tuned "by feel" via vibration is tuning a channel the player will never have —
  and adding an NV module to a feedback prefab also re-creates the code dependency D-010 dropped.
- **Rule:** Never gate game feel on haptics, and never add `NiceVibrations*` modules to an
  `MMF_Player`. Express impact through camera, audio, particles and time instead (D-007, D-010).

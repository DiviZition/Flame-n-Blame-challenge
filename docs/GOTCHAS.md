# GOTCHAS.md — Traps We Fell Into

Things that bit us, with the **Rule** that avoids them. One line of Rule is what people actually read.

**Rules:** append the moment the lesson is learned; each entry ≤ 5 lines (Status / What / Rule); mark
`Resolved`/`Avoided` when it no longer bites, keep the entry as history. Compressing verbosity is fine;
changing an ID, a status or a Rule is not (D-024).

**Format:** `## G-XXX: title` + `- **Status:** Open | Resolved | Avoided` + `- **What:**` + `- **Rule:**`

---

## G-001: A runtime asmdef referencing an editor-only assembly
- **Status:** Resolved 2026-11-19 (editor-only reference + `name` typo); the vendor-reference half stays Open
- **What:** `FlameNBlame.asmdef` referenced `com.unity.ide.visualstudio` (editor-only), which makes
  Unity refuse to compile the runtime assembly for player builds as soon as the first script appears.
  It also references `CartoonFX` — a vendor **demo** assembly.
- **Rule:** Editor-only code goes in `_Core/Editor` with `includePlatforms: ["Editor"]` in its own
  asmdef. Check the reference list against the **code**, not the docs: an asset-store update can delete
  a demo assembly and break our compile.

## G-002: `rootNamespace` with hyphens
- **Status:** Resolved 2026-11-17
- **What:** `"rootNamespace": "Flame-n-Blame-Challenge"` is not a valid C# identifier, so every
  editor-generated file silently fails to compile.
- **Rule:** Check `rootNamespace` before creating the first script in an assembly, and let the asmdef
  carry the convention instead of typing namespaces by hand. The root is `Flame_n_Blame_Challenge` (D-014).

## G-003: Build Settings point at a scene that doesn't exist
- **Status:** Open
- **What:** `EditorBuildSettings.asset` lists the deleted `Assets/Scenes/SampleScene.unity`.
- **Rule:** Add the real scene to Build Settings. On WebGL it bites harder: an empty scene list builds a
  page that loads and shows nothing (D-007).

## G-004: Feel's skills are not advertised to the agent
- **Status:** Open (the junction was dropped 2026-11-20, D-022)
- **What:** `Assets/Feel/Skills/` is not exposed as skills, so an agent guesses Feel's component names
  and API.
- **Rule:** Do **not** create the `.claude/skills` junction. Read the matching
  `Assets/Feel/Skills/<id>/SKILL.md` **before** any Feel work.

## G-005: Non-standard Unity install location
- **Status:** Avoided
- **What:** The editor lives at `X:/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe`, not under `C:\`.
- **Rule:** Use the full path for any batch-mode invocation. The Unity CLI is always
  `C:\Users\DiviZition Limited\AppData\Local\Unity\bin\unity.exe` — a different binary.

## G-006: Store-asset folders get overwritten by updates
- **Status:** Avoided
- **What:** `Assets/Feel`, `Assets/Plugins/*`, `Assets/Pivot++`, `Assets/Cartoon FX Pack`… are
  vendor-owned; an update replaces their contents.
- **Rule:** Our code lives in `Assets/_Core` only. If a vendor file must change, copy it into `_Core`
  and edit the copy.

## G-007: Generated files must not be tracked
- **Status:** Avoided
- **What:** `Library/`, `Temp/`, `Logs/`, root `*.csproj`/`*.slnx` are Unity-generated; two `.slnx`
  files were committed before the ignore rule existed.
- **Rule:** Never `git add` generated files. Removing a tracked one is an explicit `git rm --cached`.

## G-008: A test dependency present only transitively
- **Status:** Resolved 2026-11-19
- **What:** `com.unity.test-framework` 1.8.0 lived only in `packages-lock.json` as a transitive dep, so
  an unrelated package update could drop it and the tests with it.
- **Rule:** Declare the dependency in `Packages/manifest.json` explicitly, and give tests their own
  asmdef with `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, `overrideReferences: true` and
  `precompiledReferences: ["nunit.framework.dll"]`.

## G-009: `CLAUDE.md` cannot live in `docs/`
- **Status:** Avoided
- **What:** Claude Code auto-loads `CLAUDE.md` only from the repo root, so a `docs/CLAUDE.md` is never
  read — and nothing breaks, so nobody notices.
- **Rule:** `CLAUDE.md` stays at repo root, thin, pointing into `docs/`. New docs go in `docs/`.

## G-010: Renaming a render pipeline asset rebinds and retunes nothing
- **Status:** Open
- **What:** `PC_RPAsset` → `Web_RPAsset` looked done (GUIDs kept). The pipeline is bound through
  **quality levels**: `Web_RPAsset` is attached only to the "PC" level, that level excludes WebGL, and
  WebGL defaults to level 0 "Mobile", whose pipeline GUID points at the deleted `Mobile_RPAsset` — so a
  web build has no pipeline at all (B-001). The editor shows none of this (`m_CurrentQuality: 1`), and
  the renamed asset still carries PC-grade settings.
- **Rule:** After renaming/deleting any pipeline asset, switch the build target and check
  quality-level → pipeline routing **per platform**. Renaming buys a name, never a retune: budget a
  separate pass to strip HDR/shadows/post for web.

## G-011: Haptics don't exist on the shipping target
- **Status:** Open
- **What:** Nice Vibrations modules are two clicks away in an `MMF_Player`, and tuning "by feel" via
  vibration feels productive. On WebGL there is no haptic device path at all.
- **Rule:** Never gate game feel on haptics, never add a `NiceVibrations*` module to an `MMF_Player`
  (it also re-creates the code dependency D-010 dropped). Express impact through camera, audio,
  particles and time.

## G-012: `CharacterController` keeps no velocity — a force's timer is not a physics rule
- **Status:** Open
- **What:** A 2 s hydrant preset expired mid-flight and velocity dropped to **zero in mid-air**: fast,
  then slow, then dead stop — the feel of no inertia. `CharacterController.Move` teleports by the vector
  handed to it and stores nothing. Two more traps next to it: reporting only `isGrounded` hides wall
  hits, and one decay rate for ground and air makes the floor read as ice.
- **Rule:** **Never let a force's timer zero velocity — only contact zeroes momentum** (grounded absorbs
  vertical + push-Y; `CollisionFlags.Sides` absorbs the pushed X/Z and leaves the player's own control
  velocity). The shell reports `Sides`, not just `isGrounded`. Momentum lives in `PlayerMotion`, decays
  at authored `GroundDrag` (grounded) / `AirDrag` (airborne), saturates at `MaxPushSpeed`; `Duration`
  ends the *adding* only (D-016, D-020). A preset with no curve = a flat constant force, never a null.
  A re-fired impulse stays **one** entry (replace-per-`Type` resets progress).

## G-013: Unity's MCP server is deprecated, and the relay is fragile to drive by hand
- **Status:** Avoided (the bridge is not wired, D-022)
- **What:** Unity deprecates the MCP server in favour of the CLI. The bridge is a named pipe fed through
  a relay binary (`~/.unity/relay/relay_win.exe --mcp`); piping JSON-RPC lines makes the relay see EOF —
  it answers `initialize`, logs `discovered tools`, silently drops the queued `tools/list`, and Unity
  logs `Handshake failed: Connection closed during write`. Hand-driving needs redirected stdin +
  per-line timeouts, and approval under Project Settings ▸ AI.
- **Rule:** Do not wire the MCP bridge into any client: `unity status` → `unity cmd` covers all 54 tools.
  If a handshake is ever needed, keep stdin open and read stdout with a timeout — never pipe a file.

## G-014: Unity CLI vector parameters are JSON arrays
- **Status:** Avoided
- **What:** `--position "2.553 0.75 1.6"` and `"2.553,0.75,1.6"` are both rejected
  (`--position expects Single[]`). `gameobject find` has no `--path`.
- **Rule:** Vectors as JSON arrays: `--position "[x,y,z]"`, `--scale "[x,y,z]"`. Address objects by
  `--hierarchy-path "/Root/Child"` (the field `scene get_hierarchy` returns). Read parameters with
  `unity cmd <group> <verb> --help` before guessing; `unity cmd --query <term>` searches the command
  list (`unity cmd list` does not exist).

## G-015: There is no `unity skill uninstall`, and `sync` re-creates everything
- **Status:** Open
- **What:** `unity skill` has only `show/install/sync/refresh/disable/enable`. Removing skills means
  deleting folders; the CLI's tracking list keeps listing them, and `unity skill refresh` prunes the
  tracking **and re-renders** the tracked ones, while `unity skill sync <client>` installs the full set —
  so the habit of syncing after `unity self-update` silently brings back the 35 skills deleted on
  2026-11-20 (D-023).
- **Rule:** Trust the folders on disk, not `unity skills`. Never run `unity skill sync` or
  `unity skill refresh`. Need one skill for one task:
  `unity skill install opencode --local --skill <name>`, then delete its folder.

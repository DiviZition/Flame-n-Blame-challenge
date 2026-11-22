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

## G-016: A timed launch is eaten by the ground, and `CollisionFlags.Sides` has no axis
- **Status:** Resolved 2026-11-20 (D-027)
- **What:** Vertical launch momentum lived in the push channel, which the ground zeroes every grounded
  tick — so a 2 s hydrant force or a 0.5 s jump curve accumulates nothing. And `Sides` is a boolean: the
  rule built on it zeroed **both** X and Z, so sliding along a wall stopped the player dead.
- **Rule:** Keep vertical momentum (gravity + launch) in its own channel, and let the ground absorb it
  only when grounded **and not launched** — the landing ends the launch. The contact normal comes from
  `OnControllerColliderHit` (Unity fires it inside `Move`); absorb the push **along the normal** and keep
  the parallel component. A normal with y < 0.5 is a wall.

## G-017: An absolute convergence threshold in a test is unpassable
- **Status:** Resolved 2026-11-20 (D-027, B-003)
- **What:** `meanLate - meanEarly < 0.1` on an exponential ramp fails at any τ: the first second is always
  ~1τ below the asymptote. The test blamed the code (`MaxPushSpeed` "never clamped") while the clamp
  existed and the cap sat above the force/drag equilibrium, so clamping was never exercised.
- **Rule:** Assert the asymptote, not a difference of two windows: the late window on the analytic
  equilibrium, the series monotone, the plateau inside 3τ. To test a **cap**, author the equilibrium
  above it — if the cap is never reachable, the cap is not tested.

## G-018: An empty curve times a rotation is a diagonal launch
- **Status:** Recorded 2026-11-20 (D-028)
- **What:** `Quaternion * AnimationCurve` with an empty X/Y/Z curve evaluated `(1,1,1)`, so the launch ran
  along the diagonal of the nozzle's local space — up, right and forward at once. The player flew in a
  direction no one had authored, and the Inspector gave no way to see it: the preset's `Duration` and decay
  curve were ignored (`Kind = Impulse` reads neither), and `Applies` was dead.
- **Rule:** Direction is a **Vector3 from the source** (`PushDirection.forward`), the envelope is **one
  scalar curve** over progress. Never multiply a rotation by a curve, and never leave an Inspector field a
  kind cannot read — an Impulse preset has no Duration, no curve, no application flag to author.

## G-019: A serialized curve must be authored as YAML, not set through the CLI
- **Status:** Recorded 2026-11-20 (D-028)
- **What:** `component set_field` takes an enum, a Vector, an object reference and a `[Serializable]` struct,
  but an `AnimationCurve` field comes back as `unsupported` on read and as a 'String' on write. A curve in a
  preset asset is therefore authored as text: `asset write_text` with the serialized block Unity itself
  writes (`m_Curve` keyframes + `m_PreInfinity`/`m_PostInfinity`/`m_RotationOrder`), field names exactly as
  the private `[SerializeField]` names (`_type`, `_force`, `_curve`, `_duration`), `m_Script` set to the
  script's `.meta` GUID. The asset's own GUID stays stable across the rewrite, so scene references survive.
- **Rule:** Read back with `component get_fields` — the scalars return, the curve shows as `unsupported`,
  which is the signal that the YAML, not the CLI, owns it. A JSON `--value` must contain **no spaces**
  (the CLI splits on whitespace), and object references are `{\"guid\":…,\"fileId\":…}`.

## G-020: A sign-only ground rule eats a launch on its first ticks
- **Status:** Recorded 2026-11-20 (D-030)
- **What:** "the ground absorbs vertical when it points down" looks honest, and kills every timed launch.
  The grounded state is a **negative rest value** (`GroundedGravity = -2`), so a launch's first ticks read
  negative: `2000 N / 80 kg = 25 m/s²` against `19.62` is `+0.017 m/s` per tick at editor dt, and the sign
  rule resets it to `-2` every tick. Nothing accumulates, the jump is invisible, a 45° hydrant pushes only
  X/Z. The first fix — "no absorption while any push points up" — was a **crutch**: it ignores force
  magnitude, so a weak jet (300 N) is never absorbed and the body accelerates downward **through** the
  ground.
- **Rule:** The ground is a **normal force**: it can push only up, so it absorbs downward momentum **iff**
  the live vertical force ≤ `Mass * Gravity`. Force > weight breaks the contact and the launch accumulates;
  force < weight is cancelled, which is honest — a weak jet cannot lift you. Any rule about launch survival
  must be a force comparison, never a sign, and a launch needs `Force` above `Mass·Gravity` to lift at all
  (80 kg × 19.62 = **1569 N**; a 45° nozzle needs ≈ 2220 N).

## G-021: A `[Serializable]` class array in a ScriptableObject is not authorable
- **Status:** Recorded 2026-11-21 (D-031)
- **What:** `PushPresets` held `PushPresetData[]` (a `[Serializable]` class), with a
  `[CustomPropertyDrawer]` hiding the fields the authored `Kind` could not read. In the Inspector, adding an
  element to a class array creates a **null slot**, and a drawer cannot instantiate the instance, so the
  entry draws as an empty box and every change the designer made was lost. `PushKind` existed only to tell
  the drawer which fields to hide — a flag whose job is to hide fields that should not exist.
  **The same trap in the code:** the doc-comment promised `[SerializeReference]` and the field carried only
  `[SerializeField]`, so the array stayed a class array: `SerializedObject.FindProperty("_presets")` returned
  **null** (the code's `arr.arraySize` NRE'd), and `component get_fields` shows
  `{"unsupported":"ManagedReference"}` only when the attribute is present. A comment that describes an
  attribute which is not in the file is a claim, not a feature.
- **Rule:** A list of variants is `[SerializeReference] PushPresetData[]` over an abstract base with concrete
  subclasses: Unity's own Inspector draws a type dropdown, creates the instance, and shows only the fields
  that type has, so field visibility needs no flag and no drawer. A `[CustomPropertyDrawer]` on a
  serialized class is for **drawing**, never for making data exist.

## G-022: A .cs file written outside Unity with `autoRefresh: disabled` is never imported
- **Status:** Recorded 2026-11-21 (D-031)
- **What:** A new `.cs` created by an external tool gets a `.meta` with `fileFormatVersion` + `guid` and no
  `MonoImporter` block, and with `autoRefresh: disabled` nothing triggers `AssetDatabase.Refresh()`, so the
  file is never imported as a script. The compile then reports the new type as missing
  (`CS0246 … could not be found`) — and `script recompile` does not fix it, because it compiles against the
  asset database, it does not refresh it.
- **Rule:** Write the file **through Unity**: `unity cmd asset write_text --path … --contents $c --confirm
  true` (LF-only content) — Unity imports it, generates the proper `MonoImporter` `.meta`, and the type
  compiles. Do not hand-write `.meta` files.

## G-023: An epsilon guard swallows NaN, so a bad direction means "no push" and the console says nothing
- **Status:** Recorded 2026-11-22 (D-034)
- **What:** A jump direction built as `up + normalized * (magnitude / magnitude)` is NaN when the player
  stands (the denominator is 0). `AddActivePush` guards with `direction.sqrMagnitude > 0.0001f`, and
  `NaN > x` is **false**, so the guard accepted NaN as "zero direction": the push entry existed, its
  direction was `Vector3.zero`, the jump never fired, and no error appeared anywhere.
- **Rule:** Every epsilon comparison is also a NaN swallow — the guard is a zero-guard, not a NaN-guard.
  Never divide by a live value: use an authored constant as the denominator, or multiply by a coefficient, so
  the degenerate case is **zero** (a no-op the model can see) and not NaN (a no-op nobody sees).

## G-024: An object reference for `component set_field` is a plain path string, and the live asset object lags the file
- **Status:** Recorded 2026-11-22 (D-037)
- **What:** Setting a `ScriptableObject` reference on an asset: `--value '{"path":"Assets/…"}'` fails — the CLI
  stringifies the JSON and looks for an asset called `Assets/{path:Assets/…}`. Passing the path as a plain
  string (`--value "Assets/_Core/Physics Settings.asset"`) resolves. Second half: `asset write_text` writes the
  YAML and imports it, but `component get_fields` on the asset can read back the reference as **empty** for
  the next few calls — the loaded object in memory is not reloaded in the same dispatcher frame, so a
  `get_fields` check right after `write_text` is not a witness of the file.
- **Rule:** Two checks, and know which one you are reading: **the file** is verified with
  `unity cmd asset read_text` (the YAML you wrote is what Unity imports), **the live object** with
  `unity cmd component get_fields`. If the file has the reference and the object does not, set it with
  `set_field --value "<path>"` (a plain string) and read `get_fields` again — `set_field` serializes the asset,
  so the file stays consistent. References inside the YAML need the full form
  `{fileID: 11400000, guid: <asset guid>, type: 3, classID: 114, script: {fileID: 11500000, guid: <script guid>, type: 3}}`.

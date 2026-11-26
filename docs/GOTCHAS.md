# GOTCHAS.md — Traps We Fell Into

The **Rule** is the part that is read. Append the moment we get burned; the date is in git.

**Rules:** one entry = `## G-xxx: <the symptom, as it read>` + `- **Rule:** <the mechanism>`,
**≤ 4 lines per entry, ≤ 160 lines for the file.** Mark `Resolved` / `Avoided` / `Open` in the title; the
Rule states the mechanism, not the session — no measured numbers, no file lists, no CLI transcript. Do not
change an ID; a superseded rule is named in the title, its Rule compressed, never retold. Entries in ID order.

---

## G-001: A runtime asmdef referencing an editor-only assembly (Resolved 2026-11-19, the vendor-reference half Open)
- **Rule:** Editor-only code goes in `_Core/Editor` with `includePlatforms: ["Editor"]` in its own asmdef. Check the reference list against the **code**: an asset-store update can delete a vendor demo assembly and break our compile.

## G-002: `rootNamespace` with hyphens (Resolved 2026-11-17)
- **Rule:** Check `rootNamespace` before the first script: a hyphen is not a C# identifier, so every editor-generated file silently fails to compile. Let the asmdef carry the convention (D-014).

## G-003: Build Settings point at a scene that doesn't exist (Resolved 2026-11-26)
- **Rule:** Add the real scene to Build Settings. On WebGL it bites harder: an empty scene list builds a page that loads and shows nothing (D-007).

## G-004: Feel's skills are not advertised to the agent (Open)
- **Rule:** Never create the `.claude/skills` junction. Read `Assets/Feel/Skills/<id>/SKILL.md` **before** Feel work — guessing component names is the failure mode.

## G-005: Non-standard Unity install location (Avoided)
- **Rule:** Batch-mode needs the full editor path `X:/Programs/Unity/Editors/6000.6.5f1/Editor/Unity.exe`; the CLI is a **different** binary, at `%LOCALAPPDATA%\Unity\bin`.

## G-006: Store-asset folders get overwritten by updates (Avoided)
- **Rule:** Our code lives in `Assets/_Core` only; a vendor file that must change is copied into `_Core` and the copy edited.

## G-007: Generated files must not be tracked (Avoided)
- **Rule:** Never `git add` `Library/`, `Temp/`, `Logs/`, root `*.csproj`/`*.slnx`; removing a tracked one is `git rm --cached`.

## G-008: A test dependency present only transitively (Resolved 2026-11-19)
- **Rule:** Declare `com.unity.test-framework` in `Packages/manifest.json`; tests get their own asmdef with `UNITY_INCLUDE_TESTS`, `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll"]`.

## G-009: `CLAUDE.md` cannot live in `docs/` (Avoided)
- **Rule:** `CLAUDE.md` stays at repo root, thin, pointing into `docs/` — it is auto-loaded from the root only, and nothing breaks when it is moved, so nobody notices. New docs go in `docs/`.

## G-010: Renaming a render pipeline asset rebinds and retunes nothing (Open)
- **Rule:** The pipeline is bound through **quality levels**. After renaming/deleting a pipeline asset, switch the build target and check quality-level → pipeline routing **per platform** — the editor shows none of this.
- Renaming buys a name, never a retune: budget a separate pass to strip HDR/shadows/post for web (B-001).

## G-011: Haptics don't exist on the shipping target (Open)
- **Rule:** Never gate feel on haptics, never add a `NiceVibrations*` module to an `MMF_Player` — it re-creates the dependency D-010 dropped. Express impact through camera, audio, particles, time.

## G-012: `CharacterController` keeps no velocity — a force's timer is not a physics rule (Open)
- **Rule:** **Never let a force's timer zero velocity — only contact zeroes momentum.** Momentum lives in `PlayerMotion`: `GroundFriction` grounded (D-038), `AirDrag` airborne, capped at `MaxPushSpeed`; `Duration` ends the *adding* only.
- The shell reports the contact **normal**, not `isGrounded`; a re-fired impulse stays **one** entry, a preset with no curve is a flat constant force, never a null.

## G-013: Unity's MCP server is deprecated, the relay is fragile by hand (Avoided)
- **Rule:** Never wire the MCP bridge into a client — `unity status` → `unity cmd` covers it. Piping JSON-RPC lines makes the relay see EOF and silently drop the queued `tools/list`.

## G-014: Unity CLI vector parameters are JSON arrays (Avoided)
- **Rule:** `--position "[x,y,z]"`, `--scale "[x,y,z]"` — space- and comma-separated forms are rejected. Address objects by `--hierarchy-path`; `gameobject find` has no `--path`.
- Read `unity cmd <group> <verb> --help` before guessing, `unity cmd --query <term>` searches the list (`unity cmd list` does not exist).

## G-015: There is no `unity skill uninstall`, and `sync` re-creates everything (Open)
- **Rule:** Trust the folders on disk, not `unity skills`. Never run `unity skill sync` or `refresh` — both re-render the whole tracked tree. Need one skill for one task: install `--local --skill <name>`, then delete its folder.

## G-016: A timed launch is eaten by the ground, and `CollisionFlags.Sides` has no axis (Resolved 2026-11-20, D-027)
- **Rule:** Vertical momentum lives in its own channel; the ground absorbs it only when grounded **and not launched** — the landing ends the launch. Take the normal from `OnControllerColliderHit` (Unity fires it inside `Move`), absorb **along the normal**, keep the parallel. A normal with `y < 0.5` is a wall.

## G-017: An absolute convergence threshold in a test is unpassable (Resolved 2026-11-20, D-027/B-003)
- **Rule:** Assert the **asymptote**, not a difference of two windows: the late window on the analytic equilibrium, the series monotone, the plateau inside 3τ. To test a **cap**, author the equilibrium above it — if the cap is never reachable, the cap is not tested.

## G-018: An empty curve times a rotation is a diagonal launch (Recorded 2026-11-20, D-028)
- **Rule:** Direction is a **Vector3 from the source**, the envelope **one scalar curve** over progress. Never multiply a rotation by a curve, never leave an Inspector field a kind cannot read — an Impulse preset has no Duration, curve or application flag to author.

## G-019: A serialized curve must be authored as YAML, not set through the CLI (Recorded 2026-11-20, D-028)
- **Rule:** `component set_field` cannot read or write an `AnimationCurve` — author the block with `asset write_text` using the YAML Unity writes (`m_Curve` keyframes + `m_PreInfinity`/`m_PostInfinity`/`m_RotationOrder`), private `[SerializeField]` names, `m_Script` = the script's `.meta` GUID.
- A JSON `--value` has **no spaces** (the CLI splits on whitespace); object references are `{"guid":…,"fileId":…}`. The asset's own GUID stays stable, so scene references survive.

## G-020: A sign-only ground rule eats a launch on its first ticks (Recorded 2026-11-20, D-030)
- **Rule:** The ground is a **normal force** — it can push only up, so it absorbs downward momentum **iff** the live vertical force ≤ `Mass · Gravity`. Every launch-survival rule is a force comparison, never a sign.
- A launch needs `Force > Mass·Gravity` to lift (80 kg × 19.62 = **1569 N**, a 45° nozzle ≈ 2220 N). "No absorption while any push points up" is a crutch: force-blind, so a weak jet is never absorbed and the body accelerates **through** the ground.

## G-021: A `[Serializable]` class array in a ScriptableObject is not authorable (Recorded 2026-11-21, D-031)
- **Rule:** A list of variants is `[SerializeReference]` over an abstract base with concrete subclasses: Unity's Inspector draws a type dropdown, creates the instance, shows only that type's fields — so visibility needs no flag and no drawer.
- Adding an element to a **class** array creates a null slot no drawer can fill. A comment describing an attribute that is not in the file is a claim, not a feature.

## G-022: A `.cs` written outside Unity with `autoRefresh: disabled` is never imported (Recorded 2026-11-21, D-031)
- **Rule:** Write the file **through Unity**: `unity cmd script eval --file helper.cs`, the helper writing under `Assets/` with LF endings and calling `AssetDatabase.ImportAsset`. Never hand-write `.meta`.
- `script recompile` compiles against the asset database, it does not refresh it, so it cannot fix a missing type.

## G-023: An epsilon guard swallows NaN, so a bad direction means "no push" and the console says nothing (Recorded 2026-11-22, D-034)
- **Rule:** Every epsilon comparison is also a NaN swallow — `NaN > x` is false, so a guard accepted NaN as "zero direction" and the jump never fired, silently.
- Never divide by a live value: use an authored constant denominator, so the degenerate case is **zero** (a no-op the model can see), not NaN (a no-op nobody sees).

## G-024: An object reference for `component set_field` is a plain path string, and the live asset object lags the file (Recorded 2026-11-22, D-037)
- **Rule:** Two checks, know which you are reading: **the file** with `asset read_text`, **the live object** with `component get_fields`. Pass a `ScriptableObject` reference as a plain path, not as JSON.
- Right after `asset write_text`, `get_fields` can read a reference as empty — the loaded object is not reloaded in the same frame, so it is not a witness of the file. YAML references need the full `fileID`/`guid`/`classID` form.

## G-025: `capture` and `set_selection` kill the Pipeline connection (Recorded 2026-11-23, the user's rule: "хватит крашить юнити")
- **Rule:** The CLI is a **facts** tool — `unity status`, `console get`, `component get_fields`, `asset read_text`, `test run`, `script recompile` touch nothing. A visual check is the user playing, never a CLI capture.

## G-026: A pooled particle slot that is inactive between plays never plays (Recorded 2026-11-23, D-040)
- **Rule:** A pooled VFX slot has **two** owners of activation: the pool activates it on spawn (`SetActive(true)`), the prefab's `stopAction = Disable` retires it when the burst ends. `Play()` on an inactive GameObject fails **silently**.
- Check a pool by counting `activeSelf` children in Play Mode through `script eval`.

## G-027: `MeshRenderer` has no `sharedMesh` (Recorded 2026-11-23, D-040)
- **Rule:** Geometry lives on the `MeshFilter` of the same GameObject: read `sharedMesh.bounds` once in `Awake`, cached. The renderer owns the material.

## G-028: `script eval` returns a trailing expression only **without a semicolon**; `asset write_text` splits on whitespace (Recorded 2026-11-23, refines G-022)
- **Rule:** A project file cannot be pushed with `asset write_text` — it breaks at the first whitespace token. Push it through `script eval --file helper.cs`.
- In a probe file, `result;` is `CS0201` and the eval returns nothing; the bare `result` returns the value.

## G-029: `script recompile` triggers a domain reload that drops the Pipeline connection for 10–20 s (Recorded 2026-11-23, D-040)
- **Rule:** One CLI command per line, then `unity status` (the port table shows `ready` when the server is back), then the next command. A chain separated by `;` on one line reports a false failure.

## G-030: Unity 6.6 obsoletes `FindObjectsByType<T>(FindObjectsSortMode)` (Recorded 2026-11-23, D-040)
- **Rule:** Use the no-argument overload, namespace-qualified — a bare `Object.FindObjectsByType` is ambiguous. Check `console get --level warn` after a recompile: the project keeps the warning floor at zero.

## G-031: A douse stream must be **negative** — the sign belongs to the source (Recorded 2026-11-24, D-043)
- **Rule:** The shell copies, it decides nothing, so **the caller picks the sign**: a stream that douses passes a negative amount. When the VFX plays and the state does not move, check the sign before touching the rule.

## G-032: A stream douses only when `stream × FireResistance > 1` (Recorded 2026-11-24, superseded by G-034)
- **Rule:** The floor number moved with the preset (G-034); the shape stands — a douse can be **mathematically inert**, and no amount of tuning the VFX will make it read as water.

## G-033: Two `SerializeField` on one field is `CS0579` (Recorded 2026-11-24, D-043)
- **Rule:** **Exactly one** `SerializeField` per field, in the house form `[Tooltip("…"), SerializeField]`, the tooltip as an attribute, not a comment. `script recompile` then `console get --level warn`: `consoleErrors: 0` is the gate, not the editor window.

## G-034: The douse floor is `stream × FireResistance > MaxBurnIntensity` (Recorded 2026-11-24, D-045, supersedes G-032's number)
- **Rule:** A burning body feeds itself by **its own intensity**, so the floor moves with the preset: check every preset against the stream that is supposed to kill it, and state the floor in the tooltip. Below the floor a stream is decoration — do the arithmetic before the look (G-031).

## G-035: The char front is measured from `_BurnOrigin`, so a moving origin makes burnt black recede (Recorded 2026-11-24, D-045)
- **Rule:** The origin is written **once, on the first ignition** (`_originLocked`) and `Construct()` unlocks it with the char; a hand test sets `_cycleLength = 0` so the marks stand — a restart resets char and origin together, which reads the same.
- A mark per douse point needs a **list of points in the domain**; the axis is one number (D-042), so that is not the shape to reach for.

## G-036: The char front belongs to the fire, the splash to the water — one door, the sign picks the fact (Recorded 2026-11-24, D-046)
- **Rule:** `amount > 0` writes the char front at the contact (fire's mark), `amount < 0` plays the splash there (water's mark) and leaves the origin alone. If a feel check shows a VFX fact in the wrong place, check which sign the source sent before touching the VFX.

## G-037: `Get-Content`/`Set-Content` on a UTF-8 doc destroys every non-ASCII character (Recorded 2026-11-24)
- **Rule:** Edit docs with the **edit tool**, never shell line-index surgery: PowerShell reads BOM-less UTF-8 in the system codepage, so one `Set-Content` rewrote the whole file corrupted and a header row was lost by index arithmetic.
- The tell is Cyrillic letters inside English prose, and `grep` for the Latin-1 forms finds nothing. A corrupted file is reversible: re-encode codepage 1251 → UTF-8 (no BOM), check the `?` count is unchanged, then `git diff` and read the removed lines.

## G-038: The allocation test fails intermittently in EditMode (Recorded 2026-11-24, observed once)
- **Rule:** When a test fails, read **which** test it is and check it against the scope of the change; re-run once before treating an allocation test as a regression. If the second run is green, say so, rather than claiming a clean suite.

## G-039: A `[ContextMenu]` method cannot take a parameter (except `MenuCommand`) — the menu item never appears (Recorded 2026-11-24, D-047)
- **Rule:** Dev tooling is checked against the console, not the Inspector: a rig that logs a warning **per object** is a rig that does not do what it says. A ContextMenu is for parameterless methods only (`Construct` is the honest example); drive a parameterised door from `script eval` or from its owner.

## G-040: One puff pool for the scene, handed in at composition (Recorded 2026-11-24, D-048)
- **Rule:** A shared cosmetic service is **one object in the scene, referenced by every shell, injected into the session**. The budget is a **scene** number, not a per-prop number, so N pools is N × the budget.
- A `FindObjectsByType` returning an array of a service that owns a budget is the smell.

## G-041: A test run hangs on a modal the agent cannot see ("save the scene?", Yes/No) (Recorded 2026-11-26, the user's rule)
- **Rule:** A `test run` that does not return within 10–20 s is a **popup**, not a slow suite. Ask the user to answer it, or skip the run; a hung run burns 600 s of CLI timeout and its results are lost, so never wait it out.
- A run that returns `no_tests` afterwards: re-run once, the suite itself is fast (~30 s).

## G-042: A Button gives no hover fact, and `PointerEventData.object` no longer exists (Recorded 2026-11-26)
- **Rule:** uGUI 2.6 (Unity 6) gives `Button` only `onClick`; hover and press facts come from `IPointerEnterHandler`/`IPointerExitHandler`, and the object under the pointer is `data.hovered` (a **list**), never `data.object`.
- The handler is found by walking **up** the hierarchy and a `Button` is already a handler, so a pointer fact for a button must be taken on the **button's own** GameObject; a canvas-root shell never sees it. A TMP font reference is set as `guid:<guid>`.

## G-043: A new EventSystem with `StandaloneInputModule` throws every frame (Recorded 2026-11-26)
- **Rule:** The project runs the Input System (`activeInputHandler`), so `StandaloneInputModule` reads `UnityEngine.Input` and logs an exception **every frame**. Put `InputSystemUIInputModule` on the EventSystem, always.
- A Button is its own pointer handler, so a feel adapter goes on the **button's own** GameObject — a handler further up never gets the fact.

## G-044: A staged removal never flushes when the tick that owns it is gated (Recorded 2026-11-26)
- **Rule:** Removal is staged and applied by the next `UpdateLogic` **of that updater**. A root that gates the tick must flush the queue at teardown, or the disposed list stays registered.
- The scene reload is the case: Unity destroys the shells with the scene, the list does not die, and the next Playing ticks a destroyed shell.

# Rules for AI agents: Lumber Jack (Unity 6.6)

## Communication

- Talk to the developer in Brazilian Portuguese.
- Code identifiers in English. Code comments, tooltips and editor/UI text may be in Portuguese, always with correct accents (`não`, `é`, `árvore`, `direção`); never strip accents. Strings used as keys (paths, object names looked up in code, bindings) stay unaccented.

## Constraints

- C# 9 only (Unity's compiler): no file-scoped namespaces, no global usings, no C# 10+ features.
- Never use the legacy `UnityEngine.Input` class in new code; use the Input System.
- Never edit generated input classes (`Core/Input/*Inputs.cs`). Change the `.inputactions` instead.
- Never edit anything in `Assets/ThirdParty/` or `Assets/TextMesh Pro/`.
- Never add a DI framework or a new singleton without asking.

## Where code goes

- All project code lives in `Assets/Project/` (game assembly `LumberJack`). Editor-only code goes in `Assets/Project/Editor/` (assembly `LumberJack.Editor`) or behind `#if UNITY_EDITOR`.
- Put new code in its feature folder under `Features/`. Add `Scripts/ Prefabs/ Art/ Data/` subfolders only once a feature passes ~8-10 files.
- Put test/sandbox-only code in a `Sandbox/` subfolder of its feature. Test scenes go in `Scenes/Sandbox/`.
- Namespace = `LumberJack.<Feature>` (`Core/` uses `LumberJack.Core`, `Shared/` uses `LumberJack.Shared`, `Editor/` uses `LumberJack.Editor`). Never name a class like its namespace's last segment (e.g. `Wood` in `LumberJack.Wood`): other namespaces would resolve the name to the namespace.

## Code style

`.editorconfig` defines formatting and naming. Follow it, plus:

- Private fields `_camelCase` (including `[SerializeField]`); everything public `PascalCase`; booleans as questions (`IsGrounded`); events named for what happened (`Landed`), handlers `Handle` + event.
- New data ScriptableObjects use the `Definition` suffix.
- Member order: constants, serialized fields (`[Header]`), private fields, properties/events, Unity callbacks in execution order, public methods, private methods.
- `[SerializeField] private` instead of public fields; `[Tooltip]` on tunables; no magic numbers.
- Comments explain *why*, in one line when possible (two at most); tooltips are one short phrase. No commented-out code.
- All project code follows this standard. Keep it that way: don't restyle code outside the feature you are working on, and keep large refactors in their own commit.

## Architecture

- Communication: direct reference inside a feature, C# event or interface across features, ScriptableObject event channel for global events. Singletons only as a last resort.
- Split definition (ScriptableObject), state (plain serializable C# class) and view (thin MonoBehaviour). Put pure logic in plain C# so it can be tested.
- Input: subscribe to `InputManager` events for one-shot actions; poll `MoveInput`/`LookInput`/`PlayerInputs.<Map>.<Action>` for continuous values. Show keys with `InteractKeyLabel` etc., never hardcoded.
- Anything the player targets (interact, cut, grab, future tools) goes through `PlayerAim.TryFindTarget(layerMask, out hit)`; never cast separate rays from the camera. The thing that shows the hint must be the thing the action uses.
  - Anything the axe cuts (logs, standing trees) implements `IAxeTarget`; the axe and the cut preview only talk to that interface.
- Layers: the player is on `Player`, wood (logs, pieces, boards) on `Wood`, usable objects on `Interactable`. Get masks with `LayerMask.GetMask("Name")` in `Awake`.
- Keep gameplay actions as small deterministic commands with stable object IDs (multiplayer is planned).

## Code design

- Tunables shared by many instances live in a `Definition` ScriptableObject, not in the MonoBehaviour. Group related settings in `[Serializable]` plain classes (`TrunkSettings`) with a `Validate()` that `OnValidate` calls to clamp values.
- Use an interface where there are several interchangeable behaviors (strategy), and pick the implementation in the inspector with `[SerializeReference, SubclassSelector]`. Don't add an interface that has a single implementation.
- Pure logic (generation, calculations, rules) lives in plain `sealed` C# classes that take their inputs as parameters; MonoBehaviours only wire Unity to it.
- Value ranges are `FloatRange`/`IntRange` (with `[RangeLimits]`), never two loose min/max fields. Shape-over-length values are `AnimationCurve`s.
- Randomness in gameplay and generation uses `SeededRandom` with an explicit seed (same seed, same result on every client). Never `UnityEngine.Random` or `System.Random` there.
- Expose read-only state through properties and events; no public fields, no public setters on data a component owns.
- Editor tooling for a feature (custom inspectors, drawers, builders) goes in `Assets/Project/Editor/<Feature>/`. Inspector buttons call the component's public methods and record `Undo`.

## Unity rules

- Subscribe in `OnEnable`, unsubscribe in `OnDisable`. Never subscribe lambdas you cannot remove.
- Never use `?.`, `??` or `is null` on `UnityEngine.Object`; use `== null` or `if (obj)` (and see Performance for hot paths).
- Timed sequences use `async Awaitable` methods (not coroutines) that pass `destroyCancellationToken` to every await. Start them from an `async void` event handler that catches `OperationCanceledException`.
- Use `TryGetComponent` instead of `GetComponent` + null check.
- Compare tags with `CompareTag`, never `gameObject.tag == "..."` (allocates a string).
- No string-based calls: no `SendMessage`, `BroadcastMessage`, `Invoke("Name")`, `StartCoroutine("Name")`.
- Animator/material properties by cached ID: `Animator.StringToHash`, `Shader.PropertyToID` in `static readonly` fields.
- Don't leave empty Unity callbacks (`Update`, `LateUpdate`...): Unity still calls them every frame.
- Prefer events over polling: react when something changes instead of checking it every frame.
- Money in integer cents, never `float`.
- When renaming or removing a serialized field, update every scene/prefab that stores it (the key inside the component's YAML block and any `propertyPath` overrides). Don't leave `[FormerlySerializedAs]` or orphaned keys behind. When renaming a class, rename its file too.

## Procedural meshes

- Build meshes in a plain C# builder that owns and reuses its `List` buffers; never allocate per vertex or per triangle.
- Reuse the `Mesh` (`Clear()` then set vertices before triangles); destroy every mesh you create in `OnDestroy` (`DestroyImmediate` in edit mode). Edit-mode preview meshes use `HideFlags.DontSave`.
- Use `IndexFormat.UInt32` only when the vertex count passes 65535.
- Components that preview in the editor use `[ExecuteAlways]` and must have no per-frame callbacks. Never create, destroy or assign components inside `OnValidate`; defer with `EditorApplication.delayCall` and check the object still exists.

## Art style

- Low poly stylized (reference: Len's Island): simple silhouettes, faceted surfaces, detail through color and shape, not texture.
- Generated meshes are flat shaded (each triangle has its own vertices and face normal).
- Triangle budgets: tree up to 3000, prop up to 1000, character up to 5000.
- Materials: URP Lit, smoothness at most 0.1, warm saturated colors, no photographic textures. Prefer few shared materials.
- Scale: 1 unit = 1 m.
- World lighting comes from `Lumber Jack > Aplicar iluminação do mundo` (`WorldLightingSetup`); change values there, not by hand in the scene.

## Performance

Target: **0 bytes of managed allocation per frame** during gameplay (Unity's GC stops the game while it collects).

- Hot paths = `Update`/`FixedUpdate`/`LateUpdate`/`OnGUI`, coroutines, per-frame physics/collision callbacks, and everything they call (cost propagates to callers). Before finishing, walk each hot path's call graph and remove:
  - `GetComponent`/`Find*`/`AddComponent`/`Camera.main`: cache in `Awake`. For "what am I looking at", cache the last collider and call `TryGetComponent` only when it changes (`ReferenceEquals`).
  - Unity null checks (`== null`, `!= null`, `if (obj)` on `UnityEngine.Object`): track state with a `bool` (`_hasTarget`, `_isCarrying`); compare identity with `ReferenceEquals`. Keep a real null check only where the object can be destroyed while in use, with a comment saying why.
  - Allocations: no `new` lists/arrays, LINQ, `foreach` over interfaces, string concatenation/interpolation, boxing, or lambdas/closures (pass a cached `IComparer` to `Sort`). Reuse buffers owned by the caller.
  - Unity APIs that copy: `Mesh.vertices`/`triangles`/`normals`/`uv`, `GetComponents`, `Physics.RaycastAll`... Cache arrays (an immutable mesh can be cached once) and use `NonAlloc`/`List<T>` overloads.
  - `Debug.Log` (wrap diagnostics in `[Conditional]` methods or remove them).
  - Expensive one-off work (mesh slicing, generation): trigger it from an event or `Awaitable` code, never from `Update` or a coroutine.
  - UI updates that don't change anything: only touch UI when the value changes; use `TMP_Text.SetText` with format args for numbers.
- Objects spawned and destroyed many times per second (effects, projectiles, UI entries) use `UnityEngine.Pool.ObjectPool<T>` instead of `Instantiate`/`Destroy`. Cut wood pieces are not pooled: each one gets a new unique mesh.
- Physics queries: always pass a `LayerMask` and `QueryTriggerInteraction`; use `NonAlloc` variants.
- Rider's Unity performance highlights ("Expensive method invocation", "Comparison to 'null' is expensive") are bugs to fix, not noise.
- Measure before optimizing anything that isn't on this list: Profiler (CPU, GC Alloc column), in a build when the problem might be Editor overhead.

## Scene hierarchy

- The scene root holds only these groups, in this order: `Systems` (managers, EventSystem), `Lighting` (sun, Global Volume, probes), `Player` (player rig and cameras), `UI` (Canvas, UIDocuments), `Environment` (static scenery), `Gameplay` (interactive stations: shop, refinery, machines), `Vehicles`, `Props` (loose physics objects placed in the scene: logs, items, boxes). Add a group only when none fits, and add it to this list.
- Groups are empty GameObjects at the origin with zero rotation, scale 1 and no components. Never move, rotate or scale a group.
- No separator objects (`-----`) and no generic names (`GameObject`, `Cube (1)`, `Plane_01`). Name objects for what they are, in PascalCase. Prefab instances keep the prefab's name (Unity's ` (1)` suffix on copies is fine).
- Each station or machine is one object under `Gameplay`, named after it (`Refinery`, `Cashier`); its parts are its children.
- Mark everything under `Environment` that never moves as Static.
- Keep moving objects shallow: no Rigidbody or per-frame-moved object more than two levels deep, and never under another moving object unless it is attached to it. Spawn runtime objects (cut pieces, effects) next to their source or at the root, never under `Systems` or `UI`.
- Don't leave disabled or unused leftovers in a scene; ask the developer, then delete them. Test objects go in `Scenes/Sandbox/`, not in `Main`.

## Editing assets

- Move or rename an asset together with its `.meta` (`git mv` both). Ask the developer to close Unity before large moves.
- Scenes/prefabs may be edited as YAML: look up real fileIDs/GUIDs, preserve each file's line endings, back the file up to the scratchpad first.
- If a scene/prefab must reference a new script before Unity imports it, create the script's `.meta` yourself with a fresh 32-hex GUID.
- In editor scripts, reload asset references by path after `EditorSceneManager.NewScene()` (it unloads unused assets).
- Scripts are UTF-8 (Unity reads them as UTF-8; other encodings show broken symbols). Preserve each file's BOM and line endings when editing.

## Verifying

- Before asking the developer to open Unity, compile with Unity's bundled compiler: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe` running `...\DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll`, with `-langversion:9 -nostdlib -t:library -define:UNITY_EDITOR;ENABLE_INPUT_SYSTEM`, referencing `...\NetStandard\ref\2.1.0\netstandard.dll`, `...\Managed\UnityEngine\UnityEngine*.dll` and `Library/ScriptAssemblies/{Unity.InputSystem,Unity.TextMeshPro,UnityEngine.UI}.dll` (editor code also needs `UnityEditor*.dll` and the game dll). Pass arguments through a `.rsp` file with quoted Windows paths (the project path has a space).
- The compile must have no errors and no obsolete-API warnings (`CS0618`/`CS0612`); use the replacement the warning names.
- After Unity opens, check `Logs/Editor.log` inside the project for `error CS`, missing scripts and exceptions.
- Editor-only hitching is Editor overhead: confirm performance problems in a build.

## Git

- Work on `main`. Commit only when the developer explicitly asks for a commit in that request; a broad request ("do what's left", "migrate everything") is not permission to commit. Never push unless asked.
- Commit message: imperative summary line, then a short bullet list of the changes, then the attribution line the harness provides.

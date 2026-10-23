# Rules for AI agents: Lumber Jack (Unity 6.6)

## Communication

- Talk to the developer in Brazilian Portuguese.
- Code identifiers in English. Code comments may be in Portuguese.

## Constraints

- C# 9 only (Unity's compiler): no file-scoped namespaces, no global usings, no C# 10+ features.
- Never use the legacy `UnityEngine.Input` class in new code; use the Input System.
- Never edit generated input classes (`Core/Input/*Inputs.cs`). Change the `.inputactions` instead.
- Never edit anything in `Assets/ThirdParty/` or `Assets/TextMesh Pro/`.
- Never add a DI framework or a new singleton without asking.

## Where code goes

- All project code lives in `Assets/_Project/` (game assembly `LumberJack`). Editor-only code goes in `Assets/_Project/Editor/` (assembly `LumberJack.Editor`) or behind `#if UNITY_EDITOR`.
- Put new code in its feature folder under `Features/`. Add `Scripts/ Prefabs/ Art/ Data/` subfolders only once a feature passes ~8-10 files.
- Put test/sandbox-only code in a `Sandbox/` subfolder of its feature. Test scenes go in `Scenes/Sandbox/`.
- Namespace = `LumberJack.<Feature>` (Core code uses `LumberJack.Core`).

## Code style

`.editorconfig` defines formatting and naming. Follow it, plus:

- Private fields `_camelCase` (including `[SerializeField]`); everything public `PascalCase`; booleans as questions (`IsGrounded`); events named for what happened (`Landed`), handlers `Handle` + event.
- New data ScriptableObjects use the `Definition` suffix.
- Member order: constants, serialized fields (`[Header]`), private fields, properties/events, Unity callbacks in execution order, public methods, private methods.
- `[SerializeField] private` instead of public fields; `[Tooltip]` on tunables; no magic numbers.
- Comments explain *why*. No commented-out code.
- Legacy code is migrated feature by feature. Done: Core. Don't restyle code outside the feature you are working on; keep migrations in their own commit.

## Architecture

- Communication: direct reference inside a feature, C# event or interface across features, ScriptableObject event channel for global events. Singletons only as a last resort.
- Split definition (ScriptableObject), state (plain serializable C# class) and view (thin MonoBehaviour). Put pure logic in plain C# so it can be tested.
- Input: subscribe to `InputManager` events for one-shot actions; poll `MoveInput`/`LookInput`/`PlayerInputs.<Map>.<Action>` for continuous values. Show keys with `InteractKeyLabel` etc., never hardcoded.
- Keep gameplay actions as small deterministic commands with stable object IDs (multiplayer is planned).

## Unity rules

- Subscribe in `OnEnable`, unsubscribe in `OnDisable`. Never subscribe lambdas you cannot remove.
- Never use `?.`, `??` or `is null` on `UnityEngine.Object`; use `== null` or `if (obj)`.
- In `Update`/`FixedUpdate`/`LateUpdate`: no `GetComponent`, `Find`, allocations or `Debug.Log`; use `NonAlloc` physics queries; cache in `Awake`.
- Prefer `Awaitable` over coroutines in new async code.
- Money in integer cents, never `float`.
- When renaming a serialized field, add `[FormerlySerializedAs("oldName")]`. When renaming a class, rename its file too.

## Editing assets

- Move or rename an asset together with its `.meta` (`git mv` both). Ask the developer to close Unity before large moves.
- Scenes/prefabs may be edited as YAML: look up real fileIDs/GUIDs, preserve each file's line endings, back the file up to the scratchpad first.
- If a scene/prefab must reference a new script before Unity imports it, create the script's `.meta` yourself with a fresh 32-hex GUID.
- In editor scripts, reload asset references by path after `EditorSceneManager.NewScene()` (it unloads unused assets).
- Preserve each script's encoding, BOM and line endings. Don't assume UTF-8 (`Editor/Calculator.cs` is Windows-1252).

## Verifying

- Before asking the developer to open Unity, compile with Unity's bundled compiler: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\DotNetSdk\dotnet.exe` running `...\DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll`, with `-langversion:9 -nostdlib -t:library -define:UNITY_EDITOR;ENABLE_INPUT_SYSTEM`, referencing `...\NetStandard\ref\2.1.0\netstandard.dll`, `...\Managed\UnityEngine\UnityEngine*.dll` and `Library/ScriptAssemblies/{Unity.InputSystem,Unity.TextMeshPro,UnityEngine.UI}.dll` (editor code also needs `UnityEditor*.dll` and the game dll). Pass arguments through a `.rsp` file with quoted Windows paths (the project path has a space).
- After Unity opens, check `Logs/Editor.log` inside the project for `error CS`, missing scripts and exceptions.
- Measure before guessing on performance problems. Editor-only hitching is Editor overhead: confirm in a build.

## Git

- Work on `main`. Commit only when asked; never push unless asked.
- Commit message: imperative summary line, then a short bullet list of the changes, then the attribution line the harness provides.

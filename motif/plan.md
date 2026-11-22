# Motif.Bubbles: solution plan

*Exported by Motif Sketch from the project "Motif Bubbles". Stubs only: no functionality.*

## 1. Overview

- **System**: Motif.Bubbles (Blazor WebAssembly (GitHub Pages))
- **Purpose**: Draws a Motif app's architecture as a graph of bubbles, sized by its code and ringed with its Stryker results
- **Solution**: `Motif.Bubbles.sln`, targeting `net10.0`
- **Architecture**: ports and adapters. The application core (Domain and UseCases) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.
- **Ports**: 1 driving, 2 driven

## 2. Solution and project layout

Projects are grouped by role, and the solution folders in the `.sln` mirror the folders on disk. There are no `src/` or `tests/` folders.

```
Motif.Bubbles.sln
  Motif.Bubbles.App/
  Motif.Bubbles/
    Motif.Bubbles.Domain/
    Motif.Bubbles.UseCases/
  Adapters/
    Motif.Bubbles.ForOpeningRepos/
    Motif.Bubbles.ForOpeningFolders/
  Checks/
    Motif.Bubbles.Fakes/
    Motif.Bubbles.Specs/
```

| Project | Folder | Template | Namespace | Role |
|---|---|---|---|---|
| `Motif.Bubbles.App` | `Motif.Bubbles.App/` | `blazorwasm` | `Motif.Bubbles.App` | The app: runnable adapter on the User port |
| `Motif.Bubbles.Domain` | `Motif.Bubbles/Motif.Bubbles.Domain/` | `classlib` | `Motif.Bubbles` | Domain-specific business rules |
| `Motif.Bubbles.UseCases` | `Motif.Bubbles/Motif.Bubbles.UseCases/` | `classlib` | `Motif.Bubbles` | Application rules (use cases) and all port interfaces |
| `Motif.Bubbles.ForOpeningRepos` | `Adapters/Motif.Bubbles.ForOpeningRepos/` | `classlib` | `Motif.Bubbles.Adapters` | Adapters on the For opening repos port |
| `Motif.Bubbles.ForOpeningFolders` | `Adapters/Motif.Bubbles.ForOpeningFolders/` | `razorclasslib` | `Motif.Bubbles.Adapters` | Adapters on the For opening folders port |
| `Motif.Bubbles.Fakes` | `Checks/Motif.Bubbles.Fakes/` | `classlib` | `Motif.Bubbles.Fakes` | Fake driven adapters, for specs |
| `Motif.Bubbles.Specs` | `Checks/Motif.Bubbles.Specs/` | `xunit` | `Motif.Bubbles.Specs` | Specs: the checks on every driving port |

## 3. Project references (dependency rules)

| Project | References |
|---|---|
| `Motif.Bubbles.App` | `Motif.Bubbles.UseCases` |
| `Motif.Bubbles.Domain` | nothing |
| `Motif.Bubbles.UseCases` | `Motif.Bubbles.Domain` |
| `Motif.Bubbles.ForOpeningRepos` | `Motif.Bubbles.UseCases` |
| `Motif.Bubbles.ForOpeningFolders` | `Motif.Bubbles.UseCases` |
| `Motif.Bubbles.Fakes` | `Motif.Bubbles.UseCases`, `Motif.Bubbles.ForOpeningFolders` |
| `Motif.Bubbles.Specs` | `Motif.Bubbles.UseCases`, `Motif.Bubbles.Fakes`, `Motif.Bubbles.ForOpeningFolders` |

Rules:

- `Motif.Bubbles.Domain` references nothing.
- `Motif.Bubbles.UseCases` references `Motif.Bubbles.Domain` only.
- Every adapter project references `Motif.Bubbles.UseCases` only. **Adapters never reference each other.**
- The fakes project also references the adapter projects whose Seams it fakes, and the specs project references them too, so specs run the real adapters over fake Fittings.
- These references are the dependency rule. Don't add others.

## 4. Namespaces

- `Motif.Bubbles.Domain` and `Motif.Bubbles.UseCases` both use the plain `Motif.Bubbles` namespace, so the port interfaces are `Motif.Bubbles.ForDoing` and so on.
- Every other project uses the namespace in section 2. Real and simulated adapters share one namespace, and so do the fakes.
- Folders inside a project don't change its namespace.
- Set `RootNamespace` in each `.csproj` to its namespace, so new files created in an IDE get it.
- Every namespace sits inside `Motif.Bubbles`, so adapters, fakes and specs see the port interfaces without a `using`.

## 5. Ports

All port interfaces live in `Motif.Bubbles.UseCases`, namespace `Motif.Bubbles`, one file each. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.

```
Motif.Bubbles.UseCases/
  ForDoing.cs
  ForSeeing.cs
  MotifBubblesApplication.cs
  Ports/
    ForOpeningRepos.cs
    ForOpeningFolders.cs
```

### User (driving)

The User port: how people (and anything acting for them) use the application. It has two halves: `ForDoing`, the commands, and `ForSeeing`, the queries.

`ForDoing.cs`:

```csharp
namespace Motif.Bubbles;

public interface ForDoing
{
}
```

`ForSeeing.cs`:

```csharp
namespace Motif.Bubbles;

public interface ForSeeing
{
}
```

### For opening repos (driven)

Reads a public repo's code from its default branch, and its latest Stryker report from its Pages site if it has one

`Ports/ForOpeningRepos.cs`:

```csharp
namespace Motif.Bubbles;

public interface ForOpeningRepos
{
}
```

### For opening folders (driven)

Reads the code in a folder the Reviewer opens or drops in, and the latest Stryker report in its StrykerOutput/ folder

`Ports/ForOpeningFolders.cs`:

```csharp
namespace Motif.Bubbles;

public interface ForOpeningFolders
{
}
```

Also add one stub class, `MotifBubblesApplication` in `Motif.Bubbles.UseCases` (namespace `Motif.Bubbles`), implementing the driving interfaces (`ForDoing`, `ForSeeing`). It is the application itself; its constructor will later take the driven ports.

## 6. Adapters

Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally; **Checks** drive the application through a driving port and check what it does.

| Port | Adapter | Kind | Technology | Project | Class | Source |
|---|---|---|---|---|---|---|
| User | Web App | Real | Blazor WebAssembly UI | `Motif.Bubbles.App` | - | Context diagram: Reviewer |
| User | Specs | Checks | xUnit | `Motif.Bubbles.Specs` | - | Added in the sketch |
| For opening repos | GitHub | Real | HttpClient (api.github.com, raw.githubusercontent.com and github.io) | `Motif.Bubbles.ForOpeningRepos` | `ForOpeningReposViaGitHub` | Context diagram: GitHub, GitHub Pages |
| For opening repos | Fake GitHub | Fake | - | `Motif.Bubbles.Fakes` (`ForOpeningRepos/`) | `FakeGitHubForOpeningRepos` | Added in the sketch |
| For opening folders | Picker and Drop | Real | Folder picker and drag and drop (JS interop) | `Motif.Bubbles.ForOpeningFolders` | `ForOpeningFoldersViaPickerAndDrop` | Context diagram: Code Folders |

For each driven adapter, add the stub class named above implementing its port's interface, in its project's namespace and in the folder shown. For example:

```csharp
namespace Motif.Bubbles.Adapters;

public sealed class ForOpeningReposViaGitHub : ForOpeningRepos
{
}
```

Deep adapters: an adapter with a Seam keeps its own rules, and reaches the outside world through the Seam, an interface it owns. The Seam's Fitting makes one outside call per member, with no branches worth testing. Specs run the real adapter over its fake Fitting, so its port has no fake of its own. The Seam and its real Fitting go in the adapter's project and namespace; the fake Fitting goes in the fakes project, in the port's folder.

| Port | Adapters | Seam | Real Fitting | Fake Fitting |
|---|---|---|---|---|
| For opening folders | Picker and Drop | `LocalFolders` | `JsInteropLocalFolders` | `FakeLocalFolders` (`ForOpeningFolders/`) |

For each Seam, add the empty interface, its real Fitting and its fake Fitting. Each adapter with a Seam takes it in its one constructor. For example:

```csharp
namespace Motif.Bubbles.Adapters;

public interface LocalFolders
{
}

public sealed class JsInteropLocalFolders : LocalFolders
{
}

public sealed class ForOpeningFoldersViaPickerAndDrop(LocalFolders localFolders) : ForOpeningFolders
{
}
```

```csharp
using Motif.Bubbles.Adapters;

namespace Motif.Bubbles.Fakes;

public sealed class FakeLocalFolders : LocalFolders
{
}
```

Driving adapters (the app among them) keep their project template's default files. Checks and fakes on driving ports are the specs (section 7).

Adapter notes:

- **Web App** (`Motif.Bubbles.App`): The graph of bubbles, with a toggle per layer, hover for details, and pan and zoom
- **Specs** (`Motif.Bubbles.Specs`): Checks that drive the application through the User port, over the real adapters with their Fittings faked
- **GitHub** (`Motif.Bubbles.ForOpeningRepos`): Public repos' code, and the Stryker report their CI publishes to Pages
- **Picker and Drop** (`Motif.Bubbles.ForOpeningFolders`): A Motif repo's folder on the Reviewer's computer, opened or dropped in

## 7. Fakes and specs

- `Motif.Bubbles.Fakes` (`Checks/Motif.Bubbles.Fakes/`): every fake Fitting, and every fake of a port whose adapters are their own Fittings, with a folder per port (`ForOpeningRepos/`, `ForOpeningFolders/`). The namespace is flat: `Motif.Bubbles.Fakes`.
- `Motif.Bubbles.Specs` (`Checks/Motif.Bubbles.Specs/`): xUnit. Packages: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`, `AwesomeAssertions`. Keep the template's packages and add `AwesomeAssertions`.
  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the real adapters over their fake Fittings, and the other fakes, up by hand; there is no composition root yet.
- The browser adapters (`Motif.Bubbles.ForOpeningFolders`) also take `Microsoft.JSInterop`: each reaches the browser through a small JS module it ships as a static web asset.
- Use the latest stable package versions. Use AwesomeAssertions (`.Should()` style) in specs.

## 8. Not placed

Every element of the context diagram is on the sketch.

## 9. Build this

Instructions for the coding agent:

1. Create `Motif.Bubbles.sln` (classic `.sln` format: `dotnet new sln --format sln`).
2. Create every project in section 2 with its template, in its folder, and add it to the solution in the matching solution folder (`dotnet sln add <project> --solution-folder <folder>`, or `--in-root` for top-level projects). Set each project's `RootNamespace`.
3. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`, and the `razorclasslib` samples (`Component1.razor`, `ExampleJsInterop.cs`, the files in `wwwroot/`)), except `Class1.cs` in `Motif.Bubbles.Domain`: keep it, with its namespace changed to `Motif.Bubbles`, to show the namespace choice.
4. Add exactly the project references in section 3, and the packages in section 7.
5. Add a root `.editorconfig` with a naming rule that says interfaces are plain PascalCase, so the IDE doesn't ask for an `I` prefix:

   ```ini
   root = true

   [*.cs]
   dotnet_naming_rule.interfaces_no_prefix.symbols = interfaces
   dotnet_naming_rule.interfaces_no_prefix.style = pascal_case
   dotnet_naming_rule.interfaces_no_prefix.severity = warning

   dotnet_naming_symbols.interfaces.applicable_kinds = interface
   dotnet_naming_symbols.interfaces.applicable_accessibilities = *

   dotnet_naming_style.pascal_case.capitalization = pascal_case
   ```

6. Add the port interfaces and the application stub from section 5, and the adapter stub classes, and each Seam with its Fittings, from section 6.
7. Add the placeholder spec from section 7.
8. Check that `dotnet build` and `dotnet test` both succeed.

Ground rules:

- **Stubs only.** No functionality, no interface members, no wiring.
- **No composition root** (no host project, no dependency injection setup) in this version.
- **No `I` prefix** on interfaces. An adapter is named for its port, then its way (`ForStoringViaFileSystem`); other implementations carry the meaningful extra (`FileSystemFolder`), never `Impl`.
- **Two-letter acronyms stay in capitals** in code names (`IndexedDB`, `UI`).
- Don't add projects, references or packages the plan doesn't list.

# Contributing to DB TEAM

Thanks for taking the time to contribute.

## Getting started

1. **Fork** the repo and clone your fork
2. Create a branch: `git checkout -b feat/my-feature` or `fix/issue-123`
3. Ensure the build is clean: `dotnet build DBTeam.sln`
4. Make your changes
5. **Test** your changes by running the app: `dotnet run --project src/DBTeam.App`
6. Commit, push, open a Pull Request against `main`

## Project structure

- `src/DBTeam.Core` — domain models, abstractions, events. **No WPF / SQL dependencies.**
- `src/DBTeam.Data` — SQL Server implementations. **No UI.**
- `src/DBTeam.App` — WPF shell, DI, theme/lang services, AvalonDock layout
- `src/Modules/*` — feature modules, each exposes `ModuleRegistration.Register(IServiceCollection)`

## Conventions

### MVVM
- **ViewModels** derive from `ObservableObject` (CommunityToolkit.Mvvm)
- Use `[ObservableProperty]` for fields, `[RelayCommand]` for commands
- **Code-behind** restricted to view wiring (event handlers that can't go in VM). Never business logic.

### DI
- Register ViewModels and Views in your module's `ModuleRegistration`
- Resolve them from `ServiceLocator.TryGet<T>()` in code-behind (modules do **not** reference `DBTeam.App`)

### Cross-module communication
- Use `IEventBus` events. Define new ones in `DBTeam.Core/Events/AppEvents.cs`.
- Never reference another module directly from a module.

### Naming
- Files: `PascalCase.cs`, one type per file
- Folders: `Views/`, `ViewModels/`, `Engine/`, `Intellisense/`, `Converters/`…
- Namespaces = folder path

### Localization
- Any user-facing string must be a `{DynamicResource Key}` binding
- Add the key to **all** language files in `src/DBTeam.App/Lang/`
- Reuse existing keys when the concept already exists (e.g. `Designer.Connection`)

### Icons
- Use `MaterialDesignThemes` `PackIcon` with `Kind="..."`
- Prefer outlined variants for secondary actions
- Keep sizes consistent: 16 (tree nodes), 18 (icon buttons), 20 (toolbar), 28 (section headers), 32 (welcome cards), 48+ (empty states)

### Styles
- Reuse from `Themes/AppStyles.xaml`: `Card`, `SectionHeader`, `PrimaryButton`, `SuccessButton`, `DangerButton`, `IconButton`, `H1`, `H2`, `Caption`, `FieldLabel`, `Pill`
- Do **not** hardcode colors — use `{DynamicResource}` with `SystemControl*` (theme-aware) or `{StaticResource Brand.*}`

## Pull Request checklist

- [ ] Code builds with 0 errors / 0 warnings
- [ ] New user-facing strings added to **all** language files
- [ ] No hardcoded Background/Foreground colors in views
- [ ] New module (if any) registered in `App.xaml.cs → ConfigureServices`
- [ ] ViewModels do not reference WPF types (`Application`, `MessageBox` are OK in command handlers as last resort)
- [ ] README updated if a new feature is user-visible

## Adding a new module

1. `dotnet new wpflib -n DBTeam.Modules.YourName -f net8.0 -o src/Modules/DBTeam.Modules.YourName`
2. Add project to `DBTeam.sln`: `dotnet sln add <csproj>`
3. Reference Core + Data + UI: `dotnet add <csproj> reference ...`
4. Add NuGet deps (MaterialDesignThemes, CommunityToolkit.Mvvm) as needed
5. Create `ModuleRegistration.cs`, `ViewModels/YourViewModel.cs`, `Views/YourView.xaml(.cs)`
6. Register in `src/DBTeam.App/App.xaml.cs → ConfigureServices`
7. Wire a menu command in `MainViewModel` that publishes `OpenDocumentRequest`

## Questions

Open an issue tagged `question`.

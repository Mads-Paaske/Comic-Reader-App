# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 9 **.NET MAUI** single-project app (`ComicReaderApp/ComicReaderApp.csproj`) — a cross-platform comic reader. Target frameworks: `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, and `net9.0-windows10.0.19041.0` (the Windows TFM is only added when building *on* Windows). Tizen is present but commented out.

## Commands

Always target a single TFM with `-f`; building the `.sln` or the bare `.csproj` fans out to every TFM and is slow / fails on the wrong OS.

```powershell
dotnet workload restore                                             # first-time setup (installs maui workloads)
dotnet build ComicReaderApp/ComicReaderApp.csproj -f net9.0-windows10.0.19041.0
dotnet build ComicReaderApp/ComicReaderApp.csproj -f net9.0-android
dotnet build ComicReaderApp/ComicReaderApp.csproj -t:Run -f net9.0-windows10.0.19041.0   # build + launch on Windows
dotnet build ComicReaderApp/ComicReaderApp.csproj -t:Run -f net9.0-android               # build + deploy to running emulator/device
```

```powershell
./test.ps1                                            # run the whole unit test suite (xUnit) — extra args pass through to `dotnet test`
./test.ps1 --filter "FullyQualifiedName~LibraryViewModelTests"   # one class
./test.ps1 --filter "Name=AddBook_AssignsAnId"                   # one test
```

`./test.ps1` is a one-line wrapper around `dotnet test tests/ComicReaderApp.Tests/ComicReaderApp.Tests.csproj` — call that directly if you prefer. Don't run `dotnet test` on the solution: it drags in every app TFM.

No linter/formatter is configured. Verify changes by running the test suite and building a single TFM (Windows is the fastest local loop); run the app when a change is UI-visible.

`global.json` pins the .NET 9 SDK band (`rollForward: latestMajor`, prerelease allowed).

## Git etiquette

- **Never commit a new feature directly to `main`.** Create a feature branch off `main` first (e.g. `feature/lazy-pdf-paging`, `fix/shelf-line-recalc`), do the work there, and only merge to `main` once it builds and the change is complete.
- Small, self-contained fixes may go on a short-lived branch too; when in doubt, branch.
- **Commit as you go** — don't leave a large batch of uncommitted work. Commit each coherent step (a working slice, a refactor, a bug fix) with a clear, imperative-mood message that says *why*, not just *what* (e.g. `Add CbzComicSource and wire it into format dispatch`).
- Keep unrelated changes in separate commits.
- A feature branch isn't ready to merge until its unit tests exist and pass (see **Testing**).
- Don't `push` or open a PR unless asked. Assume local commits are fine; publishing is the user's call.

## Testing

- **Every new feature or bug fix must land with unit tests.** Cover the essential behaviour with **both success and failure cases** (valid input / happy path *and* invalid input, missing data, repository errors, boundary conditions). A feature without failing-case tests is not done.
- **Tests are sorted by layer, in separate folders** under `tests/ComicReaderApp.Tests/`:
  - `Frontend/` — view model / page-logic tests (`LibraryViewModelTests`, `AddBookViewModelTests`, `MainPageViewModelTests`).
  - `Backend/` — persistence and non-UI services (`BookDatabaseTests`; future `PdfComicSource` etc.).
  - `Shared/` — reusable test doubles (`FakeBookRepository`, `FakeNavigationService`, `FakeComicSource`/`FakeComicSourceFactory`, `FakeComicFilePicker`).
- Stack: **xUnit**. The project targets `net9.0-windows10.0.19041.0` with `<UseMaui>true</UseMaui>` (view models pull in `Microsoft.Maui.Controls` types); Resizetizer/asset processing is disabled in the csproj so it builds as a plain test library. Tests currently run on Windows only.
- **Testability pattern — inject a seam, never touch the real MAUI/IO type.** Each view model takes interfaces the app registers in `MauiProgram`; tests pass fakes from `Shared/`:
  - `IBookRepository` ← `BookDatabase` (Backend tests use the test-only `BookDatabase(string dbPath)` ctor against a throwaway SQLite file).
  - `INavigationService` ← `ShellNavigationService` (wraps every `Shell.Current` nav/dialog call; `FakeNavigationService` records them and returns canned dialog answers).
  - `IComicSourceFactory` ← `ComicSourceFactory` (the file-extension → `IComicSource` dispatch; the only place that knows formats).
  - `IComicFilePicker` ← `ComicFilePicker` (wraps the platform file picker).
  - Add the next seam the same way rather than reaching for `Shell.Current`, `FileSystem`, `FilePicker`, or `new PdfComicSource()` in a view model.
- **Async command handlers in tests:** the fakes hand back already-completed tasks, so `command.Execute(null)` on an `async void` handler runs to completion synchronously — assert straight after.
- **Known gaps (not yet unit-tested):** `MainPage.xaml.cs` gesture/zoom math and `LibraryPage.xaml.cs` shelf-line row grouping (both live in code-behind bound to the visual tree); real PDF rendering in `PdfComicSource` (needs a PDF + PDFium native).

## Architecture

### Composition & DI
`MauiProgram.CreateMauiApp()` is the composition root. Every page and view model is registered (`AddTransient`). The services are singletons: `IBookRepository`→`BookDatabase`, `INavigationService`→`ShellNavigationService`, `IComicSourceFactory`→`ComicSourceFactory`, `IComicFilePicker`→`ComicFilePicker` (the last three exist so the view models are testable — see **Testing**). `CommunityToolkit.Maui` is wired via `UseMauiCommunityToolkit()` — note this is the **UI** toolkit; there is **no `CommunityToolkit.Mvvm`**, so view models hand-roll `INotifyPropertyChanged` and use `Command` / `Command<T>` directly. Match that style; don't introduce `[ObservableProperty]` source generators without discussing it.

### Navigation (Shell + query-attribute passing)
- `AppShell.xaml` declares only `MainPage` as `ShellContent` (route `//MainPage`, the app's home/reader). `LibraryPage` and `AddBookPage` are registered as routes in `AppShell.xaml.cs` and reached with `GoToAsync(nameof(...))`.
- Pages exchange data by implementing `IQueryAttributable` and passing a `Dictionary<string, object>`. The **key names are the contract** between pages:
  - `SelectedBook` — LibraryPage → MainPage (open a book in the reader)
  - `NewBook` / `UpdatedBook` — AddBookPage → LibraryPage (sent via `GoToAsync("..")`; LibraryPage persists them, then removes the key so a nav replay can't double-apply)
  - `EditBook` — LibraryPage → AddBookPage (prefill the form for editing)
- `LibraryViewModel` / `AddBookViewModel` own navigation *intent* through `INavigationService` (never `Shell.Current` directly); receiving pages do the DB work in `ApplyQueryAttributes` / `OnAppearing`.

### Persistence
`BookDatabase` (implements `IBookRepository`) wraps a `SQLiteAsyncConnection` (`sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`). DB file: `FileSystem.AppDataDirectory/comicreader.db3`. The constructor blocks on `CreateTableAsync<Book>().Wait()`. `Book` (table `Books`) is the *entire* schema — all metadata fields are `string`, plus `FilePath` / `CoverImagePath`. There is **no reading-progress field**: the reader always opens at page 0. Adding "resume where I left off" means a schema change here.

### Comic rendering
`IComicSource` (`PageCount`, `GetPageAsync(int) -> ImageSource`) is the format abstraction. `PdfComicSource` is the **only** implementation: `LoadAsync` uses `PDFtoImage` (PDFium) to eagerly rasterize **every** page into an in-memory `List<SKBitmap>` — memory-heavy for large files, and a known place to add lazy/paged loading. Android pulls in the native `bblanchon.PDFium.Android` package (conditional `PackageReference`).

**Format dispatch lives in exactly one place:** `ComicSourceFactory.CreateAsync` switches on `Path.GetExtension(filePath)` (`MainPageViewModel` just calls `IComicSourceFactory`). To support CBZ/CBR/etc., add an `IComicSource` implementation and a `case` there — nothing else needs to know about formats.

### The two custom-drawn / gesture-heavy screens
- **`MainPage.xaml.cs`** — hand-written pinch-zoom, pan, and double-tap-to-reset logic (scale clamped 1–4×, translation bounds-clamped to keep the image covering the viewport). Left/right tap zones turn pages but only when not zoomed. This gesture state lives in code-behind fields, *not* the view model; the VM only exposes `CurrentPageImage`, `PageIndicator`, and next/prev commands. Zoom resets when `CurrentPageImage` changes.
- **`LibraryPage.xaml.cs` + `ShelfLineDrawable`** — books are a `FlexLayout`; a `GraphicsView` behind them draws horizontal "shelf" lines between wrapped rows. `RecalculateShelfLines` groups children by rounded `Y` to detect rows and must be re-run (via `Dispatcher.Dispatch`, to defer past the current layout pass) on size changes and collection changes. Column/slot width is computed from available width against a 100px minimum.
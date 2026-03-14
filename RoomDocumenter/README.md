# RoomDocumenter — Revit 2024 Addin

A Revit 2024 C# addin that automates room finish documentation in three commands:

1. **Finish Mapping** — maps finish parameter strings to Revit family types
2. **Document Rooms** — creates / reconciles floors, ceilings, and baseboards
3. **Interior Elevations** — creates 4-way elevation views per room

---

## Build Requirements

| Property | Value |
|---|---|
| Target Framework | `net48` |
| Platform | `x64` |
| Language Version | `9.0` |
| WPF | `true` |
| Revit Version | 2024 |

### Revit API References

Both references must be set to **Copy Local = False** so they are not copied to the output folder (Revit loads them from its install directory at runtime).

| Assembly | Default path |
|---|---|
| `RevitAPI.dll` | `C:\Program Files\Autodesk\Revit 2024\RevitAPI.dll` |
| `RevitAPIUI.dll` | `C:\Program Files\Autodesk\Revit 2024\RevitAPIUI.dll` |

The path is controlled by `$(RevitInstallPath)` in `Directory.Build.props` at the repository root. Override by setting the `REVIT_INSTALL_PATH` environment variable.

### NuGet Dependency

| Package | Version | Purpose |
|---|---|---|
| `System.Text.Json` | 8.0.5 | JSON serialisation of FinishMapping to Extensible Storage |

No other third-party dependencies are permitted.

---

## Deployment

1. Build the project in **Debug** configuration.
2. The post-build target `DeployToRevitAddins` automatically copies `RoomDocumenter.dll` and `RoomDocumenter.addin` to:
   ```
   %AppData%\Autodesk\Revit\Addins\2024\
   ```
3. Launch Revit 2024. The **PDG Tools** ribbon tab will contain a **Room Documenter** panel with three stacked buttons.

For **Release** builds, copy the DLL and `.addin` file to the target machine's Revit addins folder manually.

---

## Recommended Workflow

### Step 1 — Finish Mapping

Run **Finish Mapping** once per project (or whenever finishes change):

- The dialog reads all unique finish strings from `ROOM_FINISH_FLOOR`, `ROOM_FINISH_CEILING`, `ROOM_FINISH_WALL`, and `ROOM_BASE_FINISH` parameters across all placed rooms.
- Assign a Revit family type to each string on the Floor, Ceiling, and Baseboard tabs.
- Wall finishes are shown for reference but do not drive geometry in this version.
- Click **Save** to persist the mapping to Extensible Storage on the document.

### Step 2 — Document Rooms

Run **Document Rooms** to create or reconcile finish elements:

- Reads the saved mapping from Extensible Storage.
- Processes rooms from the current selection, or all placed rooms on the active level if nothing is selected.
- For each room: creates or reconciles Floor, Ceiling, and WallSweep baseboard elements.
- Existing elements of the correct type are left untouched (logged as "unchanged").
- Mismatched elements are replaced (logged as "updated").
- A summary TaskDialog is shown on completion.

**Run this command a second time to reconcile** — it will not create duplicates.

### Step 3 — Interior Elevations

Run **Interior Elevations** to generate 4-way elevation views:

- Opens a searchable room picker. Select rooms and set the crop offset (default 300 mm).
- Creates an ElevationMarker at each room's centroid and generates four elevation views (indices 0–3).
- Crop boxes are sized to the room boundary plus the specified offset on all sides.
- View names follow the pattern: `{Room Number} - {Room Name} - {Direction}`.
- Directions (North/South/East/West) are resolved from the building's stored `ProjectNorthOrientation` — see below.

---

## OrientationSchemaConstants.cs — Shared Dependency

`Helpers/OrientationSchemaConstants.cs` contains the Extensible Storage Schema GUID and field name used to read the building's cardinal orientation, which was originally persisted by the **PDG Elevation Builder** addin via `ProjectParametersUtil` in `PDG_Shared_Methods`.

### Important: GUID must match the orientation tool

The default GUID in this file is a placeholder:

```csharp
public static readonly Guid SchemaGuid =
    new Guid("3C4E7F1A-0B2D-4A8C-9E6F-5D1B8C2A4F3E");

public const string FieldName = "ProjectNorthOrientationValue";
```

**Before deploying**, open `ProjectParametersUtil.cs` in the `PDG_Shared_Methods` project, locate the `Schema.Lookup` / `SchemaBuilder` call, and copy the exact GUID into `OrientationSchemaConstants.cs`.

If the GUIDs do not match, `InteriorElevationCmd` will fall back to index-based direction labels (0=North, 1=South, 2=East, 3=West) and log a warning in the results summary — it will not crash.

### Sharing the file across projects

`OrientationSchemaConstants.cs` is designed as the single source of truth. If `RoomDocumenter` and the PDG Elevation Builder live in separate Visual Studio projects, reference this file using one of:

- **Linked file** — right-click the `PDG_Shared_Methods` project → Add → Existing Item → Add As Link
- **Shared class library** — extract `OrientationSchemaConstants.cs` into a small `PDG.Revit.Shared` project that both addins reference

---

## Project Structure

```
RoomDocumenter/
├── Commands/
│   ├── FinishMappingCmd.cs           IExternalCommand — opens FinishMappingDialog
│   ├── RoomDocumentationCmd.cs       IExternalCommand — resolves selection; calls RoomDocumentationService
│   └── InteriorElevationCmd.cs       IExternalCommand — opens RoomPickerDialog; calls ElevationService
├── Services/
│   ├── FinishMappingService.cs       Reads/writes FinishMapping via Extensible Storage
│   ├── RoomDocumentationService.cs   Orchestrates reconciliation + creation per room
│   ├── FloorService.cs               Reconcile + Floor.Create() logic
│   ├── CeilingService.cs             Reconcile + Ceiling.Create() logic
│   ├── BaseboardService.cs           Reconcile + WallSweep.Create() per bounding wall
│   └── ElevationService.cs           ElevationMarker, crop box, view naming logic
├── Models/
│   ├── FinishMapping.cs              POCO: Dictionary<string, long> per category
│   ├── RoomData.cs                   Room snapshot: number, name, finish strings, boundary, level
│   ├── DocumentationResult.cs        Per-run counts: created / updated / unchanged / skipped
│   └── ElevationResult.cs            Per-run counts: views created, skip reasons
├── UI/
│   ├── FinishMappingDialog.xaml      Tabbed WPF: Floor / Ceiling / Baseboard / Wall DataGrids
│   ├── FinishMappingDialog.xaml.cs   Code-behind (minimal — wires CloseRequested to DialogResult)
│   ├── FinishMappingViewModel.cs     Finish table rows, type dropdowns, Save command
│   ├── RoomPickerDialog.xaml         Searchable multi-select room list + offset input
│   ├── RoomPickerDialog.xaml.cs      Code-behind (minimal — wires CloseRequested to DialogResult)
│   └── RoomPickerViewModel.cs        Room list, search filter, selection, offset value
├── Helpers/
│   ├── CurveLoopHelper.cs            Build CurveLoop from BoundarySegments; centroid; point-in-polygon
│   └── OrientationSchemaConstants.cs Shared constants: Schema GUID + field name (see note above)
├── App.cs                            IExternalApplication — adds three stacked PushButtons
├── RoomDocumenter.addin              Revit manifest
└── RoomDocumenter.csproj             SDK-style project file (net48, x64, UseWPF)
```

---

## Edge Case Handling

| Scenario | Behaviour |
|---|---|
| Room area = 0 / unplaced | Skipped; logged as "not placed" |
| Finish parameter empty | Feature skipped; logged |
| Finish string not in mapping | Feature skipped; logged as "no mapping for '{value}'" |
| Mapped ElementId not in project | Feature skipped; logged |
| Room boundary < 3 segments | Floor/ceiling skipped; baseboard attempts per available walls |
| Room separation line (ElementId invalid) | Baseboard segment silently skipped |
| Linked-model wall (GetElement = null) | Baseboard segment skipped; logged once per room |
| No "Interior Elevation" ViewFamilyType | InteriorElevationCmd aborts with TaskDialog error |
| ElevationMarker creation exception | Caught per room; logged; command continues |
| Crop box zero/inverted | Clamped to 100 mm minimum; logged as warning |
| Orientation unreadable | Falls back to index-based labels (0=North…); logged as warning |
| RoomPickerDialog with zero rooms selected | Inline validation; dialog stays open |
| RoomDocumentationCmd run twice | Reconciliation prevents duplicates |

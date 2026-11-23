# Platinum cross-platform native migration policy

**Status:** binding architecture and migration plan; this document is not a declaration that migration is complete.

## Binding product decisions

- Runtime and application code: C# on .NET 10 or later.
- Desktop UI: **Avalonia UI with XAML**, selected from the beginning for a cross-platform application architecture.
- Target operating systems: Windows, Linux and macOS.
- Release strategy: **Windows first**. Linux and macOS follow in later releases after their platform-specific validation and packaging gates pass.
- JSON policy: **zero JSON** in the delivered product, source tree, tests, build/release workflows, project files, persistence, configuration, catalogues, diagnostics, or application-owned network messages.
- Project persistence: versioned `.platinum` packages containing XML documents. XML schemas and migration rules must be versioned.
- Exclusions: no JMRI and no TrainController dependencies, adapters, protocol bridges, compatibility modes, product-specific UI, copied code, or required runtime services.
- No JavaScript, HTML, CSS, Node.js, npm, Electron, browser runtime, WebView, or web-hosted UI in the delivered application or its build/test path.

These decisions supersede older documentation and implementation notes that describe other targets, including WPF.

## Cross-platform architecture principles

1. Keep `Platinum.Domain` and `Platinum.Application` independent of Avalonia and operating-system APIs.
2. Keep `Platinum.Infrastructure` behind application-owned interfaces. Isolate filesystem, serial/USB, networking, device discovery, native interop and OS integration behind explicit platform services.
3. Use Avalonia only in the presentation layer. Views and view models must not contain domain rules or directly issue hardware commands.
4. Prefer portable .NET APIs and managed dependencies. Any native dependency must document supported operating systems, architectures, licence and packaging requirements.
5. Avoid OS-specific assumptions in paths, case sensitivity, line endings, serial-port naming, permissions, service management, display scaling, input and file dialogs.
6. Keep platform-specific code in small, explicit adapters selected at composition time. Unsupported capabilities must be reported clearly; never silently simulate hardware support.
7. Maintain one shared application and domain codebase. Do not fork product behaviour by operating system.

## Release and support sequence

### Phase 1 — Windows-first foundation

- Build the Avalonia application and shared .NET solution on Windows.
- Establish Windows installer/package, signing, upgrade and clean-install validation.
- Keep project files, application services, persistence and tests portable so later operating systems do not require a UI rewrite.
- Record platform assumptions and prevent Windows-only APIs from leaking into shared layers.

### Phase 2 — Linux enablement

- Add Linux CI/build and supported distribution/package targets.
- Validate permissions, device access, serial/USB discovery, desktop integration, file associations and upgrade/uninstall behaviour.
- Publish only after the selected distribution matrix and hardware support are tested and documented.

### Phase 3 — macOS enablement

- Add macOS CI/build and signed/notarized distribution.
- Validate Apple Silicon and any declared Intel support, sandbox/permissions, device access, signing, notarization, updates and uninstall behaviour.
- Publish only after packaging and hardware compatibility gates pass.

Windows-first is a release-order decision, not permission to build a Windows-only architecture. Linux and macOS are explicit product targets from the start, but are not promised as available in the first release.

## Migration principles

1. Preserve the original `main` branch. All migration work stays on the dedicated migration branch.
2. Port behaviour into independently designed C# domain and application services. Do not mechanically translate web UI code or reproduce third-party product interfaces.
3. Keep domain logic independent of Avalonia. Use the existing Domain / Application / Infrastructure / Presentation separation where suitable, renaming or replacing the WPF presentation project as part of the migration.
4. Replace JSON persistence with explicit, versioned XML DTOs and a documented package manifest. Reject unknown or unsafe package content; validate IDs, references, topology and version compatibility before loading.
5. Do not silently discard unsupported project data. Provide deterministic import diagnostics and preserve source files outside the installed application where conversion cannot be completed.
6. Keep physical train control fail-safe. Unknown sensor state, stale communications, invalid topology or incomplete verification must inhibit actuation. Simulation must be visibly distinct from physical control.
7. Integrate only independently documented protocols after licence, patent, interoperability and safety review. No protocol is presumed legally or technically cleared by this plan.
8. Keep third-party catalogue data only when its provenance and licence permit redistribution; otherwise replace it with original data or user-imported content.

## Repository areas requiring migration or removal

- Legacy web runtime and UI: root `*.js`, `*.css`, `index.html` and `electron/`.
- Legacy data and schemas: `*.json`, `config/*.json` and `schema/*.json`.
- Legacy Node test suite: `tests/*.cjs` and npm metadata.
- Native application: `src/Platinum.Domain`, `src/Platinum.Application`, `src/Platinum.Infrastructure`, and the current WPF presentation project, which must be replaced by an Avalonia presentation project.
- .NET tests, CI, documentation, installers and release workflows must support the Windows-first release plan while preserving Linux/macOS portability.

Legacy source must not be deleted until its required user-visible behaviour has a native implementation and equivalent automated coverage. It must not be shipped or loaded by the native application.

## Required workstreams

1. **Inventory and traceability:** enumerate every legacy feature, data field, command, validation rule, workflow, asset and test; map each to a native component and acceptance test.
2. **Domain parity:** layout geometry, levels, track primitives, ports and junctions, topology validation, blocks, sensors, signals, routes, locomotive state, traffic management, automation, diagnostics, catalogue and undo/redo.
3. **Application services:** use-case APIs, validation, import/export, project lifecycle, command orchestration, eventing and safety gates.
4. **Persistence:** XML package manifest, versioned XML documents, atomic writes, backups, integrity checks, schema validation, upgrade paths and round-trip tests. No JSON serialization fallback.
5. **Infrastructure:** portable simulation and separately reviewed native hardware adapters for each supported OS. No JMRI or TrainController bridge.
6. **Avalonia presentation:** native editor, dispatcher, catalogue, signalling, locomotive, automation, diagnostics, project and settings views; keyboard access, accessibility, DPI/display scaling and platform-appropriate interactions.
7. **Quality and delivery:** .NET-only CI; shared unit/integration tests; Windows UI/package validation first; Linux and macOS build, UI, hardware and packaging jobs added before those releases.
8. **Legal and security review:** dependency and licence inventory, provenance review, protocol terms, patent counsel review where appropriate, threat model and hardware safety validation.

## Definition of done

The migration is complete only when all of the following are true:

- A clean checkout restores, builds and tests using the documented .NET SDK.
- The Windows release builds, installs, upgrades and uninstalls on supported Windows versions.
- Linux and macOS builds and release packages pass their documented OS-specific validation before those platforms are declared supported or released.
- Shared domain/application behaviour is OS-independent and the presentation layer uses Avalonia rather than WPF.
- The shipped application and its build/test/release workflows contain no JavaScript, HTML, CSS, JSON, Node/npm, Electron, browser or WebView dependency.
- No JMRI or TrainController name, integration, API, adapter, protocol bridge, copied code or required service remains in product code, UI, package, installer, workflows or user-facing documentation.
- All supported project data round-trips through the versioned XML `.platinum` format; legacy import has explicit coverage and diagnostics.
- Every inventoried user-facing feature is implemented natively or explicitly removed from product scope with a documented decision.
- Tests cover domain rules, persistence, migration, failure modes, safety interlocks and UI-critical flows on the declared release platforms.
- The application cannot issue physical commands until the relevant hardware, topology, detection and emergency-stop validation gates pass.
- Dependency, licence, provenance and protocol reviews are recorded. This engineering work is not a legal opinion and cannot guarantee absence of copyright or patent risk.

## Current status

This commit updates the binding decision from WPF to Avalonia and records the cross-platform architecture with a Windows-first release sequence. The repository still contains legacy web/JSON/Node assets and a partial native implementation; no completed cross-platform migration, build or platform certification is claimed. The pull request must remain a draft until the workstreams and definition-of-done checks are completed and CI evidence is available.

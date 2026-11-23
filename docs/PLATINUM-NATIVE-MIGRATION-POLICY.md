# Platinum native migration policy

**Status:** migration baseline and acceptance contract; not a declaration of completion.

## Binding product decisions

- Runtime and application code: C# on .NET 10 or later.
- Desktop UI: Windows Presentation Foundation (WPF) with XAML.
- JSON policy: **zero JSON** in the delivered product, source tree, tests, build/release workflows, project files, persistence, configuration, catalogues, diagnostics, or application-owned network messages.
- Project persistence: a versioned `.platinum` package containing XML documents. XML schemas and migration rules must be versioned and validated.
- Exclusions: no JMRI and no TrainController dependencies, adapters, protocol bridges, compatibility modes, product-specific UI, copied code, or required runtime services.
- No JavaScript, HTML, CSS, Node.js, npm, Electron, browser runtime, WebView, or web-hosted UI in the delivered application or its build/test path.

These decisions supersede older documentation and implementation notes that describe other targets.

## Migration principles

1. Preserve the original `main` branch. All work is performed on `migration/native-wpf-dotnet10-zero-json`.
2. Port behaviour into independently designed C# domain and application services. Do not mechanically translate web UI code or reproduce third-party product interfaces.
3. Keep domain logic independent of WPF. Use the existing Domain / Application / Infrastructure / Presentation separation where it remains suitable.
4. Replace JSON persistence with explicit, versioned XML DTOs and a documented package manifest. Reject unknown or unsafe package content; validate IDs, references, topology, and version compatibility before loading.
5. Do not silently discard unsupported project data. Provide deterministic import diagnostics and preserve source files outside the installed application where conversion cannot be completed.
6. Keep physical train control fail-safe. Unknown sensor state, stale communications, invalid topology, or incomplete verification must inhibit actuation. Simulation must be visibly distinct from physical control.
7. Integrate only independently documented protocols after licence, patent, interoperability, and safety review. No protocol is presumed legally or technically cleared by this plan.
8. Keep third-party catalogue data only when its provenance and licence permit redistribution; otherwise replace it with original data or user-imported content.

## Repository areas requiring migration or removal

- Legacy web runtime and UI: root `*.js`, `*.css`, `index.html`, and `electron/`.
- Legacy data and schemas: `*.json`, `config/*.json`, and `schema/*.json`.
- Legacy Node test suite: `tests/*.cjs` and npm metadata.
- Native application: `src/Platinum.Domain`, `src/Platinum.Application`, `src/Platinum.Infrastructure`, `src/Platinum.Presentation.Wpf`, and .NET tests.
- Documentation, installer and workflows must describe and build only the supported native product.

Legacy source must not be deleted until its required user-visible behaviour has a native implementation and equivalent automated coverage. It must not be shipped or loaded by the native application.

## Required workstreams

1. **Inventory and traceability:** enumerate every legacy feature, data field, command, validation rule, workflow, asset and test; map each to a native component and acceptance test.
2. **Domain parity:** layout geometry, levels, track primitives, ports and junctions, topology validation, blocks, sensors, signals, routes, locomotive state, traffic management, automation, diagnostics, catalogue and undo/redo.
3. **Application services:** use-case APIs, validation, import/export, project lifecycle, command orchestration, eventing and safety gates.
4. **Persistence:** XML package manifest, versioned XML documents, atomic writes, backups, integrity checks, schema validation, upgrade paths and round-trip tests. No JSON serialization fallback.
5. **Infrastructure:** local simulation and separately reviewed native hardware adapters. No JMRI or TrainController bridge.
6. **WPF presentation:** native editor, dispatcher, catalogue, signalling, locomotive, automation, diagnostics, project and settings views; keyboard access and accessibility.
7. **Quality and delivery:** .NET-only CI, unit/integration/UI tests, Windows packaging, upgrade tests, signing and release documentation.
8. **Legal and security review:** dependency and licence inventory, provenance review, protocol terms, patent counsel review where appropriate, threat model and hardware safety validation.

## Definition of done

The migration is complete only when all of the following are true:

- A clean checkout builds and tests using the documented .NET SDK on a supported Windows runner.
- The shipped application and its build/test/release workflows contain no JavaScript, HTML, CSS, JSON, Node/npm, Electron, browser, or WebView dependency.
- No JMRI or TrainController name, integration, API, adapter, protocol bridge, copied code, or required service remains in product code, UI, package, installer, workflows, or user-facing documentation.
- All supported project data round-trips through the versioned XML `.platinum` format; legacy import has explicit coverage and diagnostics.
- Every inventoried user-facing feature is implemented natively or explicitly removed from the product scope with a documented decision.
- Tests cover domain rules, persistence, migration, failure modes, safety interlocks, and UI-critical flows.
- The application cannot issue physical commands until the relevant hardware, topology, detection and emergency-stop validation gates pass.
- Dependency, licence, provenance and protocol reviews are recorded. This engineering work is not a legal opinion and cannot guarantee absence of copyright or patent risk.

## Current status

This commit establishes the binding migration policy only. The repository still contains legacy web/JSON/Node assets and the native WPF implementation is partial. A draft pull request must remain open until the workstreams and definition-of-done checks above are completed and CI evidence is available.

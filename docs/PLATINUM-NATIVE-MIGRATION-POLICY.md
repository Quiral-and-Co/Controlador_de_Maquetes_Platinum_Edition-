# Platinum cross-platform native migration policy

**Status:** binding architecture and acceptance contract; not a declaration that migration is complete.

## Binding product decisions

- Runtime and application code: C# on .NET 10 or later, using supported cross-platform APIs.
- Desktop UI: **Avalonia UI with XAML**, from the start. WPF is not the target UI.
- Platform architecture: Windows, Linux and macOS are first-class targets. Keep domain, application and infrastructure layers independent of operating-system and UI-specific APIs.
- Release strategy: **simultaneous Windows, Linux and macOS releases**. No platform is designated as a later phase.
- JSON policy: **zero JSON** in the delivered product, source tree, tests, build/release workflows, project files, persistence, configuration, catalogues, diagnostics, or application-owned network messages.
- Project persistence: versioned `.platinum` packages containing XML documents. XML schemas and migration rules must be versioned and validated.
- Exclusions: no JMRI and no TrainController dependencies, adapters, protocol bridges, compatibility modes, product-specific UI, copied code, or required runtime services.
- No JavaScript, HTML, CSS, Node.js, npm, Electron, browser runtime, WebView, or web-hosted UI in the delivered application or its build/test path.

These decisions supersede older documentation and implementation notes that describe other targets.

## Cross-platform architecture principles

1. Keep Domain and Application projects free of Avalonia, WPF, Windows-only APIs and platform-specific filesystem or device assumptions.
2. Define hardware, filesystem, serial/USB, networking, notifications and other OS-facing capabilities behind interfaces in the appropriate abstraction layer. Implement and test platform-specific adapters independently.
3. Use Avalonia/XAML for the presentation layer, with MVVM and UI-independent view models/application services. Do not introduce WPF dependencies.
4. Treat Windows, Linux and macOS as equal supported targets in architecture, build automation, test planning, packaging and documentation.
5. Establish a platform capability matrix for hardware interfaces, permissions, device discovery, serial ports, packaging, signing/notarization and update mechanisms. Unsupported capabilities must fail clearly and safely rather than silently degrading.
6. Build and test all three operating systems in CI. Use native runners where required for packaging, signing, notarization and platform-specific integration tests.
7. Release all three platform packages together from the same version and release candidate. A release is blocked for all platforms if a critical shared feature or safety requirement fails on any supported platform.

## Migration principles

1. Preserve the original `main` branch. All work is performed on the dedicated migration branch.
2. Port behaviour into independently designed C# domain and application services. Do not mechanically translate web UI code or reproduce third-party product interfaces.
3. Keep domain logic independent of Avalonia and operating-system APIs. Use the existing Domain / Application / Infrastructure / Presentation separation where it remains suitable; adapt the presentation project to Avalonia.
4. Replace JSON persistence with explicit, versioned XML DTOs and a documented package manifest. Reject unknown or unsafe package content; validate IDs, references, topology and version compatibility before loading.
5. Do not silently discard unsupported project data. Provide deterministic import diagnostics and preserve source files outside the installed application where conversion cannot be completed.
6. Keep physical train control fail-safe. Unknown sensor state, stale communications, invalid topology or incomplete verification must inhibit actuation. Simulation must be visibly distinct from physical control.
7. Integrate only independently documented protocols after licence, patent, interoperability and safety review. No protocol is presumed legally or technically cleared by this plan.
8. Keep third-party catalogue data only when its provenance and licence permit redistribution; otherwise replace it with original data or user-imported content.

## Repository areas requiring migration or removal

- Legacy web runtime and UI: root `*.js`, `*.css`, `index.html` and `electron/`.
- Legacy data and schemas: `*.json`, `config/*.json` and `schema/*.json`.
- Legacy Node test suite: `tests/*.cjs` and npm metadata.
- Native application: `src/Platinum.Domain`, `src/Platinum.Application`, `src/Platinum.Infrastructure`, and the presentation project, which must be migrated from WPF to Avalonia.
- Documentation, installers and workflows must describe and build the supported native product on Windows, Linux and macOS.

Legacy source must not be deleted until its required user-visible behaviour has a native implementation and equivalent automated coverage. It must not be shipped or loaded by the native application.

## Required workstreams

1. **Inventory and traceability:** enumerate every legacy feature, data field, command, validation rule, workflow, asset and test; map each to a native component and acceptance test.
2. **Domain parity:** layout geometry, levels, track primitives, ports and junctions, topology validation, blocks, sensors, signals, routes, locomotive state, traffic management, automation, diagnostics, catalogue and undo/redo.
3. **Application services:** use-case APIs, validation, import/export, project lifecycle, command orchestration, eventing and safety gates.
4. **Persistence:** XML package manifest, versioned XML documents, atomic writes, backups, integrity checks, schema validation, upgrade paths and round-trip tests. No JSON serialization fallback.
5. **Cross-platform infrastructure:** simulation plus separately reviewed hardware adapters; abstract OS-specific capabilities and verify them on all three operating systems. No JMRI or TrainController bridge.
6. **Avalonia presentation:** migrate the UI to Avalonia/XAML, native editor, dispatcher, catalogue, signalling, locomotive, automation, diagnostics, project and settings views; keyboard access, accessibility, scaling and platform conventions.
7. **Cross-platform quality and delivery:** .NET-only CI on Windows, Linux and macOS; unit, integration and UI tests; packaging and installation tests for all three; signing/notarization where required; synchronized release documentation.
8. **Legal and security review:** dependency and licence inventory, provenance review, protocol terms, patent counsel review where appropriate, threat model and hardware safety validation.

## Definition of done

The migration is complete only when all of the following are true:

- A clean checkout restores, builds and tests using the documented .NET SDK on Windows, Linux and macOS.
- The native Avalonia application launches and its critical UI flows pass automated tests on all three platforms.
- The shipped application and its build/test/release workflows contain no JavaScript, HTML, CSS, JSON, Node/npm, Electron, browser, WebView or WPF dependency.
- No JMRI or TrainController name, integration, API, adapter, protocol bridge, copied code or required service remains in product code, UI, package, installer, workflows or user-facing documentation.
- All supported project data round-trips through the versioned XML `.platinum` format; legacy import has explicit coverage and diagnostics.
- Every inventoried user-facing feature is implemented natively or explicitly removed from product scope with a documented decision.
- Tests cover domain rules, persistence, migration, failure modes, safety interlocks, cross-platform infrastructure and UI-critical flows.
- The application cannot issue physical commands until the relevant hardware, topology, detection and emergency-stop validation gates pass on the applicable platform.
- Installers/packages are produced and validated for Windows, Linux and macOS from the same release version; release publication is simultaneous.
- Dependency, licence, provenance and protocol reviews are recorded. This engineering work is not a legal opinion and cannot guarantee absence of copyright or patent risk.

## Current status

This commit updates the binding target from WPF to Avalonia and establishes simultaneous Windows, Linux and macOS support and release as architectural requirements. It does not implement or complete the migration. The repository still contains legacy web/JSON/Node assets and the native implementation is partial. The draft pull request must remain open until the workstreams and definition-of-done checks above are completed and CI evidence is available.

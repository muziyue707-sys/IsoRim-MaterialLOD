# Current public release

Use `IsoRim-MaterialLOD-0.3.1-release.zip`. Workshop item 3835669507 is Public; Test Build labels were removed at the user's request on 2026-11-08. Version and runtime binaries remain 0.3.1, with verified Workshop API61 support. [Remote publication verification](../review/workshop-public-release-20261108.json). Full private upload receipts remain local. Earlier packages and status descriptions below are historical.

# Previous API 61 package

Local historical archive `IsoRim-MaterialLOD-0.3.1-api61-workshop.zip` (not published here): updated Workshop IsoRim API 61 was verified against the accepted DLL and runtime resources on 2026-11-08. Manual GitHub installation is no longer required. Runtime binaries remain 0.3.1. See `../API61-INSTALL.md`, `../review/workshop-host-api61-comparison.json` and `../review/workshop-api61-ready-20261108.json`. The earlier `IsoRim-MaterialLOD-0.3.1-api61.zip` retains the historical manual-install guidance. No host/game DLL is included.

# Candidate package

**Current floor candidate:** `IsoRim-MaterialLOD-0.3.0-floors.zip` includes the independent compiled addon, optional crate example, bilingual About/Workshop assets and three detail panels. It requires the matching candidate host API branch. See `../review/floor-validation.md` and `../review/floor-provenance.json` for current validation and hashes. No host/game DLL or test harness is redistributed. API numbering remains for the maintainer to assign. Steam publication has not been performed.

The archives below are historical; they do not include floor support.

`IsoRim-MaterialLOD-0.3.0-bilingual.zip` is the metadata/artwork update: bilingual main/example About files, an in-game preview, and bilingual Workshop publishing assets. Runtime binaries are byte-identical to the original candidate. See `../review/bilingual-media-check.json` for checks and the new archive hash. Steam publication has not been performed. The original archive and its provenance remain unchanged.

`IsoRim-MaterialLOD-0.3.0.zip` contains the independent compiled addon, source, optional crate example, configuration documentation and licenses. No host/game DLLs or test harness are included.

This is a review candidate for the companion IsoRim API branch, not a Workshop-ready universal release. The exact host DLL pin and archive SHA256 are recorded in `../review/build-provenance.json`. Build both host and addon from source for a different host binary; do not remove the compatibility gate. Load order: Harmony, candidate IsoRim, addon, optional example.

Unpack `IsoRimMaterialLOD` and optionally `IRMEExample` into a separate test installation's Mods directory. Replace the old 0.2 addon if present; do not enable both versions. See the root README and VALIDATION for limits.

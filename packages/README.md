# Candidate package

**Current floor candidate:** `IsoRim-MaterialLOD-0.3.0-floors.zip` includes the independent compiled addon, optional crate example, bilingual About/Workshop assets and three detail panels. It requires the matching candidate host API branch. See `../review/floor-validation.md` and `../review/floor-provenance.json` for current validation and hashes. No host/game DLL or test harness is redistributed. API numbering remains for the maintainer to assign. Steam publication has not been performed.

The archives below are historical; they do not include floor support.

`IsoRim-MaterialLOD-0.3.0-bilingual.zip` is the metadata/artwork update: bilingual main/example About files, an in-game preview, and bilingual Workshop publishing assets. Runtime binaries are byte-identical to the original candidate. See `../review/bilingual-media-check.json` for checks and the new archive hash. Steam publication has not been performed. The original archive and its provenance remain unchanged.

`IsoRim-MaterialLOD-0.3.0.zip` contains the independent compiled addon, source, optional crate example, configuration documentation and licenses. No host/game DLLs or test harness are included.

This is a review candidate for the companion IsoRim API branch, not a Workshop-ready universal release. The exact host DLL pin and archive SHA256 are recorded in `../review/build-provenance.json`. Build both host and addon from source for a different host binary; do not remove the compatibility gate. Load order: Harmony, candidate IsoRim, addon, optional example.

Unpack `IsoRimMaterialLOD` and optionally `IRMEExample` into a separate test installation's Mods directory. Replace the old 0.2 addon if present; do not enable both versions. See the root README and VALIDATION for limits.

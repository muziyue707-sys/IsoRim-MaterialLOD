# Candidate package

`IsoRim-MaterialLOD-0.3.0.zip` contains the independent compiled addon, source, optional crate example, configuration documentation and licenses. No host/game DLLs or test harness are included.

This is a review candidate for the companion IsoRim API branch, not a Workshop-ready universal release. The exact host DLL pin and archive SHA256 are recorded in `../review/build-provenance.json`. Build both host and addon from source for a different host binary; do not remove the compatibility gate. Load order: Harmony, candidate IsoRim, addon, optional example.

Unpack `IsoRimMaterialLOD` and optionally `IRMEExample` into a separate test installation's Mods directory. Replace the old 0.2 addon if present; do not enable both versions. See the root README and VALIDATION for limits.

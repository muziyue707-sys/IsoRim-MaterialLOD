# Author update recheck — 2026-11-08

Read-only check requested after initial Workshop publication.

- GitHub master still resolves to `75e80b98e19cb2b754d40cf6bb908dfd2a28277d` (API 61), the already validated host. Verified through GitHub connector against master.
- Live SteamUGC query with cached responses disabled reports item `3819577715` updated at Unix `1794090114`: **2026-11-08 06:21:54 +08:00**. Remote size **83,521,519 bytes**, public, not banned, result OK.
- Steam website/changelog crawler still returns the previous update; it is stale relative to the live SteamUGC result.
- Steam GetItemInstallInfo points to `E:\SteamLibrary\steamapps\workshop\content\294100\3819577715`, installed timestamp `1794001812`, size **83,482,558 bytes**. Local Source/Api.cs still says version 60.
- Installed DLL SHA256 remains `CCE1341B979EA0F0B0360CFC2BF37F3EB00332B5BB6841366D8C2A71DE89D3AF`.
- Addon 0.3.1 expects validated API61 DLL SHA256 `3A22565D697E78A5D15210C7B861F0FEBFD80C62C8D9D409D6529E2F990B1861`.
- Remote UGC details expose neither a legacy downloadable file handle nor a file URL; no new binary was downloaded for this check. No Workshop files, formal state, addon runtime code or published content changed.

Conclusion: the author's Workshop update is confirmed, but the updated Workshop binary is not yet present locally. GitHub/API61 compatibility evidence remains valid for the previously tested exact binary. Workshop compatibility and removal of the manual-install notice require the newly downloaded Workshop DLL hash (and relevant resource comparison); do not infer binary identity from the update timestamp.

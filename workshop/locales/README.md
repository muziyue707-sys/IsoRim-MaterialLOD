# Workshop page languages

Steam native localized metadata uses `schinese` for Simplified Chinese and `english` for English. The title and description files in this directory are the publishing sources. The author credit stays 水表狂魔 in both languages. The shared cover and gallery are unchanged.

Update both language variants with:

```powershell
./scripts/PublishWorkshopLanguages.ps1 -Mode Upload -ExpectedSteamId <your Steam ID>
./scripts/PublishWorkshopLanguages.ps1 -Mode Verify -ExpectedSteamId <your Steam ID>
```

This updates only page titles/descriptions and verifies each language through a language-specific SteamUGC query. It preserves the content download, visibility and gallery. `PublishWorkshop.ps1` publishes the English default from this directory; rerun the language script when the translated page content changes. The older combined `workshop/title.txt` and `workshop/description.bbcode.txt` are retained as bilingual reference copies, not the current page publishing inputs.

Steam chooses the display language from the visitor's Steam/site language preference, not nationality. Direct preview links:

- [简体中文](https://steamcommunity.com/sharedfiles/filedetails/?id=3835669507&l=schinese)
- [English](https://steamcommunity.com/sharedfiles/filedetails/?id=3835669507&l=english)

API reference: [SetItemUpdateLanguage / SetLanguage](https://partner.steamgames.com/doc/api/ISteamUGC#SetItemUpdateLanguage).

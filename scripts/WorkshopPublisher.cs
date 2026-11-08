using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Steamworks;

// Uses the user's running Steam client. Call only from the scoped publication script.
public static class IrmeWorkshopPublisher
{
    public static T Wait<T>(SteamAPICall_t call, int seconds = 300) where T : struct
    {
        bool done = false, failed = false; T value = default(T);
        using (var result = CallResult<T>.Create((T r, bool io) => { value = r; failed = io; done = true; }))
        {
            result.Set(call); var watch = Stopwatch.StartNew();
            while (!done && watch.Elapsed.TotalSeconds < seconds) { SteamAPI.RunCallbacks(); Thread.Sleep(100); }
            if (!done) throw new Exception("Steam timeout. Inspect the saved item ID and remote state before retrying.");
            if (failed) throw new Exception("Steam call IO failure.");
            return value;
        }
    }
    static void Check(bool ok, string name) { if (!ok) throw new Exception("Steam rejected " + name); }
    public static CreateItemResult_t Create() => Wait<CreateItemResult_t>(SteamUGC.CreateItem(new AppId_t(294100), EWorkshopFileType.k_EWorkshopFileTypeCommunity));
    public static SubmitItemUpdateResult_t Upload(ulong id, string title, string description, string content, string preview, string[] gallery, bool publiclyVisible, string changeNote)
    {
        var h = SteamUGC.StartItemUpdate(new AppId_t(294100), new PublishedFileId_t(id));
        Check(SteamUGC.SetItemUpdateLanguage(h, "english"), "language");
        Check(SteamUGC.SetItemTitle(h, title), "title");
        Check(SteamUGC.SetItemDescription(h, description), "description");
        Check(SteamUGC.SetItemTags(h, new List<string> { "Mod", "1.7" }), "tags");
        Check(SteamUGC.SetItemVisibility(h, publiclyVisible ? ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic : ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted), "visibility");
        Check(SteamUGC.SetItemContent(h, content), "content");
        Check(SteamUGC.SetItemPreview(h, preview), "cover");
        // Gallery is only added on the initial successful content upload; retries query remote previews first.
        foreach (var image in gallery) Check(SteamUGC.AddItemPreviewFile(h, image, EItemPreviewType.k_EItemPreviewType_Image), "gallery");
        return Wait<SubmitItemUpdateResult_t>(SteamUGC.SubmitItemUpdate(h, changeNote), 900);
    }
    public static AddUGCDependencyResult_t Dependency(ulong id) => Wait<AddUGCDependencyResult_t>(SteamUGC.AddDependency(new PublishedFileId_t(id), new PublishedFileId_t(3819577715)));
    public static SubmitItemUpdateResult_t UpdateTranslation(ulong id, string language, string title, string description)
    {
        var h = SteamUGC.StartItemUpdate(new AppId_t(294100), new PublishedFileId_t(id));
        Check(SteamUGC.SetItemUpdateLanguage(h, language), "language");
        Check(SteamUGC.SetItemTitle(h, title), "localized title");
        Check(SteamUGC.SetItemDescription(h, description), "localized description");
        return Wait<SubmitItemUpdateResult_t>(SteamUGC.SubmitItemUpdate(h, null));
    }
    public static SteamUGCDetails_t Query(ulong id, out uint previews, out ulong[] dependencies, string language = "english")
    {
        var h = SteamUGC.CreateQueryUGCDetailsRequest(new[] { new PublishedFileId_t(id) }, 1);
        Check(SteamUGC.SetLanguage(h, language), "query language");
        SteamUGC.SetReturnChildren(h, true); SteamUGC.SetReturnAdditionalPreviews(h, true);
        SteamUGC.SetReturnLongDescription(h, true); SteamUGC.SetAllowCachedResponse(h, 0);
        try
        {
            var result = Wait<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(h));
            if (result.m_eResult != EResult.k_EResultOK) throw new Exception("Query failed: " + result.m_eResult);
            SteamUGCDetails_t d; Check(SteamUGC.GetQueryUGCResult(h, 0, out d), "query result");
            previews = SteamUGC.GetQueryUGCNumAdditionalPreviews(h, 0);
            var children = new PublishedFileId_t[d.m_unNumChildren];
            if (children.Length > 0) Check(SteamUGC.GetQueryUGCChildren(h, 0, children, (uint)children.Length), "dependencies");
            dependencies = new ulong[children.Length]; for (int i = 0; i < children.Length; i++) dependencies[i] = children[i].m_PublishedFileId;
            return d;
        }
        finally { SteamUGC.ReleaseQueryUGCRequest(h); }
    }
}

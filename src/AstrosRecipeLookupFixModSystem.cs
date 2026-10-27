using HarmonyLib;
using Vintagestory.API.Common;

namespace AstrosRecipeLookupFix;

public class AstrosRecipeLookupFixModSystem : ModSystem
{
    private Harmony? harmony;

    /// <summary>
    /// patches the crafting grid once per process. in singleplayer the client and the server
    /// both start this mod in the same process, so we shouldnt be patching twice
    /// </summary>
    /// <param name="api">the api of the side that is starting</param>
    public override void Start(ICoreAPI api)
    {
        if (Harmony.HasAnyPatches(Mod.Info.ModID))
        {
            return;
        }

        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll(typeof(AstrosRecipeLookupFixModSystem).Assembly);
    }

    /// <summary>
    /// undo patches..
    /// </summary>
    public override void Dispose()
    {
        harmony?.UnpatchAll(Mod.Info.ModID);
    }
}
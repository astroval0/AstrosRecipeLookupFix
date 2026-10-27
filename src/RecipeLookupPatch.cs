using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace AstrosRecipeLookupFix;

[HarmonyPatch(typeof(InventoryCraftingGrid), "FindMatchingRecipe")]
public static class RecipeLookupPatch
{
    private static readonly AccessTools.FieldRef<InventoryCraftingGrid, ItemSlot[]> SlotsField =
        AccessTools.FieldRefAccess<InventoryCraftingGrid, ItemSlot[]>("slots");

    private static readonly AccessTools.FieldRef<InventoryCraftingGrid, ItemSlotCraftingOutput> OutputSlotField =
        AccessTools.FieldRefAccess<InventoryCraftingGrid, ItemSlotCraftingOutput>("outputSlot");

    private static readonly AccessTools.FieldRef<InventoryCraftingGrid, int> GridSizeField =
        AccessTools.FieldRefAccess<InventoryCraftingGrid, int>("GridSize");

    private static readonly AccessTools.FieldRef<InventoryCraftingGrid, int> GridSizeSqField =
        AccessTools.FieldRefAccess<InventoryCraftingGrid, int>("GridSizeSq");

    private static readonly Action<InventoryCraftingGrid, GridRecipe> FoundMatch =
        AccessTools.MethodDelegate<Action<InventoryCraftingGrid, GridRecipe>>(
            AccessTools.Method(typeof(InventoryCraftingGrid), "FoundMatch"));

    /// <summary>
    /// replaces the games recipe lookup. the game checks every filled slot against every ingredient and dedupes the
    /// recipes it finds with a list, and with planks in 1000s of recipes that lags the server. we dedupe with a hashset
    /// instead and skip stacks that are the same as one we already checked
    /// </summary>
    /// <param name="__instance">the crafting grid</param>
    /// <returns>false to skip the game's version</returns>
    public static bool Prefix(InventoryCraftingGrid __instance)
    {
        ItemSlot[] slots = SlotsField(__instance);
        __instance.MatchingRecipe = null;
        OutputSlotField(__instance).Itemstack = null;
        IWorldAccessor world = __instance.Api.World;
        List<GridRecipe> candidates = [];
        HashSet<GridRecipe> foundCandidates = [];
        List<ItemStack> scannedStacks = [];

        foreach (ItemSlot slot in slots)
        {
            ItemStack? stack = slot.Itemstack;
            if (stack == null || stack.StackSize == 0 || WasScanned(scannedStacks, stack, world))
            {
                continue;
            }

            scannedStacks.Add(stack);
            foreach (KeyValuePair<IRecipeIngredientBase, List<IRecipeBase>> entry in
                     world.FastSearchRecipesByIngredient)
            {
                if (!entry.Key.SatisfiesAsIngredient(stack, checkStackSize: false))
                {
                    continue;
                }

                foreach (IRecipeBase recipe in entry.Value)
                {
                    if (recipe is GridRecipe { Enabled: true } gridRecipe && foundCandidates.Add(gridRecipe))
                    {
                        candidates.Add(gridRecipe);
                    }
                }
            }
        }

        IPlayer player = __instance.Player;
        int gridSize = GridSizeField(__instance);
        foreach (GridRecipe recipe in candidates)
        {
            if (!recipe.Shapeless && recipe.Matches(player, world, slots, gridSize))
            {
                FoundMatch(__instance, recipe);
                return false;
            }
        }

        foreach (GridRecipe recipe in candidates)
        {
            if (recipe.Shapeless && recipe.Matches(player, world, slots, gridSize))
            {
                FoundMatch(__instance, recipe);
                return false;
            }
        }

        __instance.dirtySlots.Add(GridSizeSqField(__instance));
        return false;
    }

    /// <summary>
    /// checks if an identical stack already got scanned in this lookup. same item and same attributes means it is an
    /// ingredient of the same recipes, and those are candidates already, only the stack size can differ and te lookup
    /// ignores that
    /// </summary>
    /// <param name="scannedStacks">the stacks scanned so far</param>
    /// <param name="stack">the stack to check</param>
    /// <param name="world">the world, for comparing attributes</param>
    /// <returns>true if an identical stack got scanned already</returns>
    private static bool WasScanned(List<ItemStack> scannedStacks, ItemStack stack, IWorldAccessor world)
    {
        foreach (ItemStack scannedStack in scannedStacks)
        {
            if (stack.Class == scannedStack.Class && stack.Id == scannedStack.Id && stack.Attributes.Equals(world, scannedStack.Attributes))
            {
                return true;
            }
        }

        return false;
    }
}
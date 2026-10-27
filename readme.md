# Astro's Recipe Lookup Fix

a Vintage Story mod that stops crafting from freezing the server.

## what it does

- every time something in the crafting grid changes, the game looks up the recipe on the main thread. it checks every filled slot against every ingredient of every recipe, and with planks in over 1000 recipes that freezes the whole server when someone mass crafts.
this mod fixes that by only checking each kind of item once and keeping track of the recipes it found with a hashset.

## performance
- tested with a server's 58 mods and 34k recipes, looking up a full crafting grid went from about 10ms to about 0.5ms, so much faster. as well as the worst lag spikes going from 700ms to 90ms

### compatible with mods that add recipes too.
- if you find any incompatibilities then please make an issue.

## server side 
- the client doesnt need it but it cant hurt for them to have it.

## building

needs the .NET 10 SDK and Vintage Story 1.22 installed in the default place (`%APPDATA%\Vintagestory`). building copies the mod straight into `VintagestoryData\Mods\AstrosRecipeLookupFix`, so close the game first.

## license

LGPL 2.1, see `LICENSE`.

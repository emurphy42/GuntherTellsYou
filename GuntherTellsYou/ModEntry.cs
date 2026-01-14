using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace GuntherTellsYou
{
    public class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();

            ObjectPatches.ModMonitor = this.Monitor;

            var harmony = new Harmony(this.ModManifest.UniqueID);

            // detect start of donation
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Locations.LibraryMuseum), nameof(StardewValley.Locations.LibraryMuseum.OpenDonationMenu)),
               postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.LibraryMuseum_OpenDonationMenu_Postfix))
            );

            // detect start of rearrange
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Locations.LibraryMuseum), nameof(StardewValley.Locations.LibraryMuseum.OpenRearrangeMenu)),
                postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.LibraryMuseum_OpenRearrangeMenu_Postfix))
            );

            // detect when items are donated            
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Menus.MuseumMenu), nameof(StardewValley.Menus.MuseumMenu.receiveLeftClick)),
               postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.MuseumMenu_receiveLeftClick_Postfix))
            );

            // display dialogue after items were donated
            // must target IClickableMenu because that's where exitThisMenu() is implemented
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Menus.IClickableMenu), nameof(StardewValley.Menus.IClickableMenu.exitThisMenu)),
               postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.MuseumMenu_exitThisMenu_Postfix))
            );

            // also display dialogue after closing dialogue, in case multiple items were donated at once
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Menus.DialogueBox), nameof(StardewValley.Menus.DialogueBox.closeDialogue)),
               postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.DialogueBox_closeDialogue_Postfix))
            );
        }
    }
}

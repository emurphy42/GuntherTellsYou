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
            ObjectPatches.ItemDelimiter = this.Config.ItemDelimiter;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            // detect when items are donated            
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Menus.MuseumMenu), nameof(StardewValley.Menus.MuseumMenu.receiveLeftClick)),
               prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.MuseumMenu_receiveLeftClick_Prefix)),
               postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.MuseumMenu_receiveLeftClick_Postfix))
            );
            // display dialogue after items were donated
            // must target IClickableMenu because that's where exitThisMenu() is implemented
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Menus.IClickableMenu), nameof(StardewValley.Menus.IClickableMenu.exitThisMenu)),
               postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.MuseumMenu_exitThisMenu_Postfix))
            );
        }
    }
}

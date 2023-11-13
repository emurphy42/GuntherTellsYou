using StardewValley.Locations;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Force.DeepCloner;
using StardewValley.Network;
using Netcode;
using StardewModdingAPI;
using StardewValley.Objects;

namespace GuntherTellsYou
{
    internal class ObjectPatches
    {
        // unclear how to use __state when standard function has optional parameters
        static NetVector2Dictionary<int, NetInt> museumPiecesBeforeAction;

        // wait till user exits menu, then display dialogues for any donated items
        // also, displaying dialogue during MuseumMenu_receiveLeftClick_Postfix() leaves UI in a stuck state
        static readonly List<int> newlyDonatedItems = new List<int>();

        // initialized by ModEntry.cs
        public static IMonitor ModMonitor; // allow patches to call ModMonitor.Log()
        public static string ItemDelimiter; // separates name/description of multiple items donated at once

        public static bool MuseumMenu_receiveLeftClick_Prefix(int x, int y, bool playSound = true)
        {
            try
            {
                // retain copy of museum donations before the action
                museumPiecesBeforeAction = null;
                // ShallowClone is apparently insufficient
                museumPiecesBeforeAction = (Game1.currentLocation as LibraryMuseum).museumPieces.DeepClone<NetVector2Dictionary<int, NetInt>>();
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[Gunther Tells You] Exception in MuseumMenu_receiveLeftClick_Prefix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
            }

            // run standard function
            return true;
        }

        public static void MuseumMenu_receiveLeftClick_Postfix(int x, int y, bool playSound = true)
        {
            try
            {
                // did we succeed in retaining copy of museum donations before the action?
                if (museumPiecesBeforeAction == null)
                {
                    ModMonitor.Log($"[Gunther Tells You] Issue in MuseumMenu_receiveLeftClick_Postfix: museumPiecesBeforeAction is null", LogLevel.Debug);
                    return;
                }

                // did museum donations change?
                var museumPiecesAfterAction = (Game1.currentLocation as LibraryMuseum).museumPieces;
                if (museumPiecesAfterAction.Count() == museumPiecesBeforeAction.Count())
                {
                    return;
                }

                // find and record new item
                foreach (var key in museumPiecesAfterAction.Keys)
                {
                    if (!museumPiecesBeforeAction.ContainsKey(key))
                    {
                        var itemID = museumPiecesAfterAction[key];
                        if (!newlyDonatedItems.Contains(itemID))
                        {
                            newlyDonatedItems.Add(itemID);
                            // https://stardewcommunitywiki.com/Modding:Object_data
                            var itemName = Game1.objectInformation[itemID].Split('/')[0];
                            var itemDescription = Game1.objectInformation[itemID].Split('/')[5];
                            ModMonitor.Log($"[Gunther Tells You] Recorded donated item: {itemID} - {itemName} - {itemDescription}", LogLevel.Debug);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[Gunther Tells You] Exception in MuseumMenu_receiveLeftClick_Postfix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
            }
        }

        // display dialogue(s) after exiting menu
        public static void MuseumMenu_exitThisMenu_Postfix(bool playSound = true)
        {
            try
            {
                if (newlyDonatedItems.Count == 0)
                {
                    return;
                }

                // calling drawDialogue() or drawDialogueNoTyping() multiple times causes only one to be effective
                // so instead, combine multiple items into one longer dialogue block
                // DialogueBox() uses '#' as a delimiter, but that also causes only one to be effective
                // newlines are munged to spaces, so punt and default to something that typically wraps onto its own line
                var dialogueText = "";
                foreach (var itemID in newlyDonatedItems)
                {
                    var itemName = Game1.objectInformation[itemID].Split('/')[0];
                    var itemDescription = Game1.objectInformation[itemID].Split('/')[5];
                    if (dialogueText != "")
                    {
                        dialogueText += $" {ItemDelimiter} ";
                    }
                    dialogueText += $"{itemName} - {itemDescription}";
                    ModMonitor.Log($"[Gunther Tells You] Added donated item to dialogue: {itemID} - {itemName} - {itemDescription}", LogLevel.Debug);
                }
                Game1.drawDialogue(Game1.getCharacterFromName("Gunther"), Game1.parseText(dialogueText));

                newlyDonatedItems.Clear();
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[Gunther Tells You] Exception in MuseumMenu_exitThisMenu_Postfix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
            }
        }
    }
}

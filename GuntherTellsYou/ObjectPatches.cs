﻿using StardewValley.Locations;
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
using StardewValley.TokenizableStrings;

namespace GuntherTellsYou
{
    internal class ObjectPatches
    {
        // unclear how to use __state when standard function has optional parameters
        static bool rearrangingInProgress = false;
        static readonly List<string> museumIDsBeforeAction = new();

        // wait till user exits menu, then display dialogues for any donated items
        // also, displaying dialogue during MuseumMenu_receiveLeftClick_Postfix() leaves UI in a stuck state
        static readonly List<string> newlyDonatedItems = new();

        // initialized by ModEntry.cs
        public static IMonitor ModMonitor; // allow patches to call ModMonitor.Log()

        public static void LibraryMuseum_answerDialogueAction_Postfix(string questionAndAnswer, string[] questionParams)
        {
            try
            {
                rearrangingInProgress = (questionAndAnswer == "Museum_Rearrange_Yes");
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[Gunther Tells You] Exception in LibraryMuseum_rearrange_Postfix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
            }
        }

        public static bool MuseumMenu_receiveLeftClick_Prefix(int x, int y, bool playSound = true)
        {
            try
            {
                if (!rearrangingInProgress)
                {
                    // retain copy of museum donations before the action
                    // ShallowClone is apparently insufficient, DeepClone ran into an error
                    museumIDsBeforeAction.Clear();
                    var museumPieces = (Game1.currentLocation as LibraryMuseum).museumPieces;
                    foreach (var key in museumPieces.Keys)
                    {
                        var itemID = museumPieces[key];
                        museumIDsBeforeAction.Add(itemID);
                    }
                }
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
                if (rearrangingInProgress)
                {
                    return;
                }

                // did we succeed in retaining copy of museum donations before the action?
                if (museumIDsBeforeAction == null)
                {
                    ModMonitor.Log($"[Gunther Tells You] Issue in MuseumMenu_receiveLeftClick_Postfix: museumIDsBeforeAction is null", LogLevel.Debug);
                    return;
                }

                // did museum donations change?
                var museumPiecesAfterAction = (Game1.currentLocation as LibraryMuseum).museumPieces;
                if (museumPiecesAfterAction.Count() == museumIDsBeforeAction.Count)
                {
                    return;
                }

                // find and record new item
                foreach (var key in museumPiecesAfterAction.Keys)
                {
                    var itemID = museumPiecesAfterAction[key];
                    if (!museumIDsBeforeAction.Contains(itemID) && !newlyDonatedItems.Contains(itemID))
                    {
                        newlyDonatedItems.Add(itemID);
                        var itemName = TokenParser.ParseText(Game1.objectData[itemID].DisplayName);
                        var itemDescription = TokenParser.ParseText(Game1.objectData[itemID].Description);
                        ModMonitor.Log($"[Gunther Tells You] Recorded donated item: {itemID} - {itemName} - {itemDescription}", LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[Gunther Tells You] Exception in MuseumMenu_receiveLeftClick_Postfix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
            }
        }

        // display dialogue if relevant
        private static void displayDialogue()
        {
            if (newlyDonatedItems.Count == 0)
            {
                return;
            }

            var itemID = newlyDonatedItems[0];
            var itemName = TokenParser.ParseText(Game1.objectData[itemID].DisplayName);
            var itemDescription = TokenParser.ParseText(Game1.objectData[itemID].Description);
            var dialogueText = $"{itemName} - {itemDescription}";
            ModMonitor.Log($"[Gunther Tells You] Added donated item to dialogue: {itemID} - {itemName} - {itemDescription}", LogLevel.Debug);
            Game1.DrawDialogue(new Dialogue(
                speaker: Game1.getCharacterFromName("Gunther"),
                translationKey: null,
                dialogueText: Game1.parseText(dialogueText)
            ));

            newlyDonatedItems.Remove(itemID);
        }

        // display dialogue after exiting menu
        public static void MuseumMenu_exitThisMenu_Postfix(bool playSound = true)
        {
            try
            {
                displayDialogue();
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[Gunther Tells You] Exception in MuseumMenu_exitThisMenu_Postfix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
            }
        }

        // in case multiple items were donated at once:
        // (in 1.5) calling drawDialogue() or drawDialogueNoTyping() multiple times causes only one to be effective
        // so also display dialogue after closing dialogue
        public static void DialogueBox_closeDialogue_Postfix()
        {
            try
            {
                displayDialogue();
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[Gunther Tells You] Exception in DialogueBox_closeDialogue_Postfix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
            }
        }
    }
}

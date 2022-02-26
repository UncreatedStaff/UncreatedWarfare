
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Commands
{
    public class Command_I : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "item";
        public string Help => "spawns in an item";
        public string Syntax => "/item";
        public List<string> Aliases => new List<string>(1) { "i"};
        public List<string> Permissions => new List<string>(1) { "uc.i" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;

            int amount = 1;
            string itemName = "";
            ushort shortID;
            ItemAsset? asset = null;

            bool amountWasValid = true;

            int similarNamesCount = 0;

            if (command.Length > 0)
            {

                if (command.Length == 1) // if there is only one argument, we only have to check a single word
                {
                    // first check if single-word argument is a short ID
                    if (ushort.TryParse(command[0], out shortID) && (Assets.find(EAssetType.ITEM, shortID) is ItemAsset asset2))
                    {
                        // success
                        asset = asset2;
                        goto SpawnItem;
                    }
                    else
                    {
                        itemName = command[0];
                        asset = UCAssetManager.FindItemAsset(itemName, out similarNamesCount, true);
                        if (asset != null)
                        {
                            // success
                            goto SpawnItem;
                        }
                        else
                        {
                            //failure
                            player.Message($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'".Colorize("8f9494"));
                        }
                    }
                }
                else // there are at least 2 arguments
                {
                    // first try treat 1st argument as a short ID, and 2nd argument as an amount
                    if (command.Length == 2 &&
                        ushort.TryParse(command[0], out shortID) && 
                        (Assets.find(EAssetType.ITEM, shortID) is ItemAsset asset2))
                    {
                        if (!int.TryParse(command[1], out amount))
                        {
                            amount = 1;
                            amountWasValid = false;
                        }

                        // success
                        asset = asset2;
                        goto SpawnItem;
                    }
                    else
                    {
                        // now try find an asset using all arguments except the last, which could be treated as a number.
                        

                        if (int.TryParse(command.Last(), out amount)) // if this runs, then the last ID is treated as an amount
                        {
                            for (int i = 0; i < command.Length - 1; i++)
                                itemName += command[i] + ' ';

                            asset = UCAssetManager.FindItemAsset(itemName.Trim(), out similarNamesCount, true);
                            if (asset != null)
                            {
                                // success

                                goto SpawnItem;
                            }
                            else
                            {
                                // failure
                                player.Message($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'".Colorize("8f9494"));
                            }

                        }
                        else
                        {
                            amount = 1;

                            for (int i = 0; i < command.Length; i++)
                                itemName += command[i] + ' ';

                            asset = UCAssetManager.FindItemAsset(itemName.Trim(), out similarNamesCount, true);
                            if (asset != null)
                            {
                                // success
                                goto SpawnItem;
                            }
                            else
                            {
                                //failure
                                player.Message($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'".Colorize("8f9494"));
                            }
                        }
                    }
                }

            }
            else
                player.Message($"Usage: <color=#c7a29f><i>/i <item id | item name | item folder name></i></color>".Colorize("8f9494"));



            SpawnItem:
            if (asset == null) return;
            Item itemFromID = new Item(asset.id, true);
            for (int i = 0; i < amount; i++)
            {
                if (!player.Player.inventory.tryAddItem(itemFromID, true))
                    ItemManager.dropItem(itemFromID, player.Position, true, true, true);
            }

            string message = $"Giving you {(amount == 1 ? "a" : (amount.ToString() + "x").Colorize("9dc9f5"))} <color=#ffdf91>{asset.itemName}</color> - <color=#a7b6c4>{asset.id}</color>".Colorize("bfb9ac");

            if (!amountWasValid)
                message += $" (the amount your tried to put was invalid)".Colorize("7f8182");
            else if (similarNamesCount > 0)
                message += $" ({similarNamesCount} similarly named items exist)".Colorize("7f8182");

            player.Message(message);

        }
    }
}

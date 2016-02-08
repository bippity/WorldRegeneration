using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace WorldRegeneration
{
    public static class Commands
    {
        public static void SaveWorld(CommandArgs args)
        {
            string schematicPath = Path.Combine("worldregen", String.Format("world-{0}.twd", Main.worldID));
            Utilities.SaveWorldSection(0, 0, Main.maxTilesX, Main.maxTilesY, schematicPath);
        }

        public static void LoadWorld(CommandArgs args)
        {
            if (args.Parameters.Count == 1)
            {
                string schematicPath = Path.Combine("worldregen", String.Format("world-{0}.twd", args.Parameters[0]));
                if (!File.Exists(schematicPath))
                {
                    args.Player.SendErrorMessage("Invalid world file '{0}'!", args.Parameters[0]);
                    return;
                }
                Utilities.LoadWorldSection(schematicPath);
            }
            else
                args.Player.SendErrorMessage("Proper syntax: /loadworld <worldid>");
        }

        public static void WorldRegen(CommandArgs args)
        {
            string cmd = "help";
            if (args.Parameters.Count > 0)
            {
                cmd = args.Parameters[0].ToLower();
            }
            switch (cmd)
            {
                case "time":
                    TimeSpan NextRegen = WorldRegeneration.WorldRegenCheck - DateTime.UtcNow.AddSeconds(-WorldRegeneration.WorldRegenConfig.RegenerationInterval);
                    args.Player.SendInfoMessage("World Regeneration will be in {0} Minute{1} and {2} Second{3}.", NextRegen.Minutes, NextRegen.Minutes == 1 ? "" : "s", NextRegen.Seconds, NextRegen.Seconds == 1 ? "" : "s");
                    break;
                case "force":
                    args.Player.SendInfoMessage("You forced World Regeneration.");
                    WorldRegeneration.WorldRegenCheck = DateTime.UtcNow.AddSeconds(-WorldRegeneration.WorldRegenConfig.RegenerationInterval + 60);
                    break;
                default:
                    {
                        int pageNumber;
                        int pageParamIndex = 0;
                        if (args.Parameters.Count > 1)
                            pageParamIndex = 1;

                        if (cmd != "help")
                        {
                            if (!PaginationTools.TryParsePageNumber(args.Parameters, pageParamIndex, args.Player, out pageNumber))
                            {
                                args.Player.SendErrorMessage("Proper syntax: /search <option> <name>");
                                return;
                            }
                        }
                        else pageNumber = 1;

                        List<string> lines = new List<string> {
                        "time - Information on next world regeneration.",
                        "force - Force the world regeneration.",
                    };
                        PaginationTools.SendPage(
                            args.Player, pageNumber, lines,
                            new PaginationTools.Settings
                            {
                                HeaderFormat = "Available [c/ffffff:World Regen] Sub-Commands ({0}/{1}):",
                                FooterFormat = "Type {0}worldregen {{0}} for more sub-commands.".SFormat(TShockAPI.Commands.Specifier)
                            }
                        );
                        return;
                    }
            }
        }
    }
}

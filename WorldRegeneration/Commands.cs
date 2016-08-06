using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using TShockAPI;

namespace WorldRegeneration
{
    public static class Commands
    {
        public static void SaveWorld(CommandArgs args)
        {
            string worldid = Main.worldID.ToString();
            if (args.Parameters.Count > 0)
                worldid = args.Parameters[0];

            string schematicPath = Path.Combine("worldregen", string.Format("world-{0}.twd", worldid));
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
                int.TryParse(args.Parameters[0], out WorldRegeneration.lastWorldID);
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
                        TimeSpan NextRegen = WorldRegeneration.WorldRegenCheck - DateTime.UtcNow.AddSeconds(-WorldRegeneration.Config.RegenerationInterval);
                        args.Player.SendInfoMessage("World Regeneration will be in{0}{1}{2}.", NextRegen.Hours > 0 ? NextRegen.Hours == 1 ? " " + NextRegen.Hours + " Hour" : " " + NextRegen.Hours + " Hours" : "", NextRegen.Minutes > 0 ? NextRegen.Minutes == 1 ? " " + NextRegen.Minutes + " Minute" : " " + NextRegen.Minutes + " Minutes" : "", NextRegen.Seconds > 0 ? NextRegen.Seconds == 1 ? " " + NextRegen.Seconds + " Second" : " " + NextRegen.Seconds + " Seconds" : "");
                    break;
                case "force":
                    int time = 300;
                    if (args.Parameters.Count == 2 && args.Parameters[1] == "1")
                        time = 60;
                    args.Player.SendInfoMessage("You forced World Regeneration.");
                    WorldRegeneration.WorldRegenCheck = DateTime.UtcNow.AddSeconds(-WorldRegeneration.Config.RegenerationInterval + time + 1);
                    break;
                case "list":
                    if (args.Parameters.Count > 2)
                    {
                        args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /worldregen list [page]");
                        return;
                    }

                    int page;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page))
                        return;

                    var schematics = from s in Directory.EnumerateFiles("worldregen", "world-*.twd")
                                     select s.Substring(17, s.Length - 21);
                    PaginationTools.SendPage(args.Player, page, PaginationTools.BuildLinesFromTerms(schematics),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Worlds ({0}/{1}):",
                            FooterFormat = "Type /worldregen list {0} for more."
                        });
                    args.Player.SendInfoMessage("Last World Regenerated: {0}.", WorldRegeneration.lastWorldID);
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
                        "force [1] - Force the world regeneration to 5 minutes, or last minute.",
                        "list - List available world IDs.",
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

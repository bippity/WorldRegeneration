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
    }
}

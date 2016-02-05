using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace WorldRegeneration
{
    [ApiVersion(1, 22)]
    public class WorldRegeneration : TerrariaPlugin
    {
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public override string Name
        {
            get { return "WorldRegeneration"; }
        }
        public override string Author
        {
            get { return "Marcus101RR, WhiteXZ"; }
        }
        public override string Description
        {
            get { return "Regenerate a world from a save template with chests and sign information."; }
        }

        public static ConfigFile WorldRegenConfig { get; set; }

        private static DateTime WorldRegenCheck = DateTime.UtcNow;
        private static bool hasWorldRegenerated = false;

        public WorldRegeneration(Main game)
			: base(game)
		{
            WorldRegenConfig = new ConfigFile();
            Order = 10;
        }

        public override void Initialize()
        {
            ConfigFile.SetupConfig();

            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            GetDataHandlers.InitGetDataHandler();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);

                PlayerHooks.PlayerCommand -= OnPlayerCommand;
            }
            base.Dispose(disposing);
        }

        private void OnPlayerCommand(PlayerCommandEventArgs args)
        {
            if (args.Handled || args.Player == null)
                return;

            Command command = args.CommandList.FirstOrDefault();
            if (command == null || (command.Permissions.Any() && !command.Permissions.Any(s => args.Player.Group.HasPermission(s))))
                return;
        }

        private void OnInitialize(EventArgs args)
        {
            Directory.CreateDirectory("worldregen");

            PlayerHooks.PlayerCommand -= OnPlayerCommand;
            #region Commands
            Action<Command> Add = c =>
            {
                TShockAPI.Commands.ChatCommands.RemoveAll(c2 => c2.Names.Select(s => s.ToLowerInvariant()).Intersect(c.Names.Select(s => s.ToLowerInvariant())).Any());
                TShockAPI.Commands.ChatCommands.Add(c);
            };

            Add(new Command(Permissions.immunetoban, Commands.SaveWorld, "saveworld")
            {
                AllowServer = true,
                HelpText = "Save the world and its contents."
            });

            Add(new Command(Permissions.immunetoban, Commands.LoadWorld, "loadworld")
            {
                AllowServer = true,
                HelpText = "Load the world and its contents."
            });
            #endregion
        }

        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - WorldRegenCheck).TotalSeconds >= WorldRegenConfig.RegenerationInterval - 60 && !hasWorldRegenerated)
            {
                TSPlayer.All.SendMessage(string.Format("The world will regenerate in 1 Minute(s)."), 50, 255, 130);
                hasWorldRegenerated = true;
            }
            if ((DateTime.UtcNow - WorldRegenCheck).TotalSeconds >= WorldRegenConfig.RegenerationInterval)
            {
                WorldRegenCheck = DateTime.UtcNow;
                var worldData = from s in Directory.EnumerateFiles("worldregen", "world-*.twd")
                                select s.Substring(17, s.Length - 21);

                if (worldData.Count() > 0)
                {
                    int selectedWorld = Main.rand.Next(0, worldData.Count());
                    string worldPath = Path.Combine("worldregen", String.Format("world-{0}.twd", worldData.ElementAt(selectedWorld)));
                    Utilities.RegenerateWorld(worldPath);
                    hasWorldRegenerated = false;
                }
            }
        }
    }
}

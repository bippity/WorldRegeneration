using System;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace WorldRegeneration
{
    [ApiVersion(2, 0)]
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

        public static Config Config { get; set; }

        static readonly Timer RegenTimer = new Timer(1000);
        public static DateTime WorldRegenCheck = DateTime.UtcNow;
        public static int WorldRegenCountdown = 5;
        public static int lastWorldID = 0;
        private static bool hasWorldRegenerated = false;

        public WorldRegeneration(Main game)
			: base(game)
		{
            Order = 10;
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            GeneralHooks.ReloadEvent += OnReload;
            GetDataHandlers.InitGetDataHandler();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                GeneralHooks.ReloadEvent -= OnReload;
                RegenTimer.Elapsed -= OnWorldRegeneration;
                RegenTimer.Stop();
            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs args)
        {
            Directory.CreateDirectory("worldregen");

            string path = Path.Combine(TShock.SavePath, "WorldRegeneration.json");
            Config = Config.Read(path);
            if (!File.Exists(path))
            {
                Config.Write(path);
            }

            #region Commands
            Action<Command> Add = c =>
            {
                TShockAPI.Commands.ChatCommands.RemoveAll(c2 => c2.Names.Select(s => s.ToLowerInvariant()).Intersect(c.Names.Select(s => s.ToLowerInvariant())).Any());
                TShockAPI.Commands.ChatCommands.Add(c);
            };

            Add(new Command(Permissions.saveworld, Commands.SaveWorld, "saveworld")
            {
                AllowServer = true,
                HelpText = "Save the world and its contents."
            });

            Add(new Command(Permissions.loadworld, Commands.LoadWorld, "loadworld")
            {
                AllowServer = true,
                HelpText = "Load the world and its contents."
            });

            Add(new Command(Permissions.worldregen, Commands.WorldRegen, "worldregen")
            {
                AllowServer = true,
                HelpText = "Various sub-commands for world regeneration."
            });
            #endregion

            RegenTimer.Elapsed += OnWorldRegeneration;
            RegenTimer.Start();
        }

        private void OnWorldRegeneration(object Sender, EventArgs args)
        {
            if ((DateTime.UtcNow - WorldRegenCheck).TotalMinutes >= (Config.RegenerationInterval / 60) - 6 && !hasWorldRegenerated)
            {
                TimeSpan RegenSpan = WorldRegenCheck.AddSeconds(Config.RegenerationInterval) - DateTime.UtcNow;
                if(RegenSpan.Minutes > 0 && RegenSpan.Minutes < 6 && RegenSpan.Seconds == 0)
                {
                    TSPlayer.All.SendMessage(string.Format("The world will regenerate in {0} minute{1}.", RegenSpan.Minutes, RegenSpan.Minutes == 1 ? "" : "s"), 50, 255, 130);
                    TShock.Log.ConsoleInfo(string.Format("The world will regenerate in {0} minute{1}.", RegenSpan.Minutes, RegenSpan.Minutes == 1 ? "" : "s"));
                }
                if (RegenSpan.Minutes == 0)
                    hasWorldRegenerated = true;
            }
            if ((DateTime.UtcNow - WorldRegenCheck).TotalSeconds >= Config.RegenerationInterval)
            {
                WorldRegenCheck = DateTime.UtcNow;
                var worldData = from s in Directory.EnumerateFiles("worldregen", "world-*.twd")
                                select s.Substring(17, s.Length - 21);

                if (worldData.Count() > 0)
                {
                    Random w = new Random();
                    int selectedWorld = w.Next(0, worldData.Count()-1);
                    string worldPath = Path.Combine("worldregen", String.Format("world-{0}.twd", worldData.ElementAt(selectedWorld)));
                    Utilities.RegenerateWorld(worldPath);
                    hasWorldRegenerated = false;
                    int.TryParse(worldData.ElementAt(selectedWorld), out lastWorldID);
                }
            }
        }

        public void OnReload(ReloadEventArgs args)
        {
            string path = Path.Combine(TShock.SavePath, "WorldRegeneration.json");
            Config = Config.Read(path);
            if (!File.Exists(path))
            {
                Config.Write(path);
            }
            args.Player.SendSuccessMessage("[World Regeneration] Reloaded configuration file and data!");
        }
    }
}

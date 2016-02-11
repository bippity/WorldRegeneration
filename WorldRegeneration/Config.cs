using System;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace WorldRegeneration
{
    public class ConfigFile
    {
        public int RegenerationInterval = 21600;
        public bool IgnoreChests = false;

        public static ConfigFile Read(string path)
        {
            if (!File.Exists(path))
                return new ConfigFile();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static ConfigFile Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<ConfigFile>(sr.ReadToEnd());
                if (ConfigRead != null)
                    ConfigRead(cf);
                return cf;
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<ConfigFile> ConfigRead;

        internal static string ConfigPath { get { return Path.Combine(TShock.SavePath, "WorldRegeneration.json"); } }

        public static void SetupConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                    WorldRegeneration.WorldRegenConfig = Read(ConfigPath);
                /* Add all the missing config properties in the json file */

                WorldRegeneration.WorldRegenConfig.Write(ConfigPath);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("Config Exception: Error in config file");
                TShock.Log.Error(ex.ToString());
            }
        }
    }
}
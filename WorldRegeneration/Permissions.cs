using System.ComponentModel;

namespace WorldRegeneration
{
    public static class Permissions
    {
        [Description("Save a (.twd) terraria world data file.")]
        public static readonly string saveworld = "worldregen.main.save";

        [Description("Load a (.twd) terraria world data file.")]
        public static readonly string loadworld = "worldregen.main.load";       

    }
}

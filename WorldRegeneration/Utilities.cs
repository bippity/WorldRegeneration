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
    public static class Utilities
    {
        const int BUFFER_SIZE = 1048576;
        public static void SaveWorldSection(int x, int y, int x2, int y2, string path)
        {
            // GZipStream is already buffered, but it's much faster to have a 1 MB buffer.
            using (var writer =
                new BinaryWriter(
                    new BufferedStream(
                        new GZipStream(File.Open(path, FileMode.Create), CompressionMode.Compress), BUFFER_SIZE)))
            {
                writer.Write(Main.worldSurface);
                writer.Write(Main.rockLayer);
                writer.Write(Main.dungeonX);
                writer.Write(Main.dungeonY);
                writer.Write(WorldGen.crimson);

                writer.Write(x);
                writer.Write(y);
                writer.Write(x2);
                writer.Write(y2);

                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        writer.Write(Main.tile[i, j] ?? new Tile());
                    }
                }
                TSPlayer.All.SendInfoMessage("Tile Data Saved...");

                int totalChests = 0;
                for (int i = 0; i < 1000; i++)
                {
                    Chest chest = Main.chest[i];
                    if (chest != null)
                    {
                        totalChests++;
                    }
                }

                writer.Write(totalChests);
                for (int i = 0; i < 1000; i++)
                {
                    Chest chest = Main.chest[i];
                    if(chest != null)
                        writer.WriteChest(chest);
                }
                TSPlayer.All.SendInfoMessage("{0} Chest Data Saved...", totalChests);

                int totalSigns = 0;
                for (int i = 0; i < 1000; i++)
                {
                    Sign sign = Main.sign[i];
                    if (sign != null)
                    {
                        totalSigns++;
                    }
                }

                writer.Write(totalSigns);
                for (int i = 0; i < 1000; i++)
                {
                    Sign sign = Main.sign[i];
                    if(sign != null && sign.text != null)
                        writer.WriteSign(sign);
                }
                TSPlayer.All.SendInfoMessage("{0} Sign Data Saved...", totalSigns);
            }
        }

        public static void Write(this BinaryWriter writer, Tile tile)
        {
            writer.Write(tile.sTileHeader);
            writer.Write(tile.bTileHeader);
            writer.Write(tile.bTileHeader2);

            if (tile.active())
            {
                writer.Write(tile.type);
                if (Main.tileFrameImportant[tile.type])
                {
                    writer.Write(tile.frameX);
                    writer.Write(tile.frameY);
                }
            }
            writer.Write(tile.wall);
            writer.Write(tile.liquid);
        }

        public static void WriteChest(this BinaryWriter writer, Chest chest)
        {
            writer.Write(chest.x);
            writer.Write(chest.y);
            //writer.Write(chest.name);
            for (int l = 0; l < 40; l++)
            {
                Item item = chest.item[l];
                if (item != null && item.stack > 0)
                {
                    writer.Write((short)item.stack);
                    writer.Write(item.netID);
                    writer.Write(item.prefix);
                }
                else
                {
                    writer.Write((short)0);
                }
            }
        }

        public static void WriteSign(this BinaryWriter writer, Sign sign)
        {
            writer.Write(sign.text);
            writer.Write(sign.x);
            writer.Write(sign.y);
        }

        public static void LoadWorldSection(string path)
        {
            using (var reader = new BinaryReader(new GZipStream(new FileStream(path, FileMode.Open), CompressionMode.Decompress)))
            {
                Main.worldSurface = reader.ReadDouble();
                Main.rockLayer = reader.ReadDouble();
                Main.dungeonX = reader.ReadInt32();
                Main.dungeonY = reader.ReadInt32();
                WorldGen.crimson = reader.ReadBoolean();

                reader.ReadInt32();
                reader.ReadInt32();

                int x = 0;
                int y = 0;

                int x2 = reader.ReadInt32();
                int y2 = reader.ReadInt32();

                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        Tile tile = reader.ReadTile();
                        if (i >= 0 && j >= 0 && i < Main.maxTilesX && j < Main.maxTilesY)
                        {
                            if (TShock.Regions.InAreaRegion(i, j).Any(r => r != null && r.Z > 99))
                            {
                                continue;
                            }
                            else
                            {
                                Main.tile[i, j] = tile; // Paste Tiles
                            }
                        }
                    }
                }
                ResetSection(x, y, x2, y2);
                TSPlayer.All.SendInfoMessage("Tile Data Loaded...");

                int totalChests = reader.ReadInt32();
                int chests = 0;
                int index = 0;
                for (int a = 0; a < totalChests; a++)
                {
                    Chest chest = reader.ReadChest();
                    for (int c = index; c < 1000; c++)
                    {
                        if (TShock.Regions.InAreaRegion(chest.x, chest.y).Any(r => r != null && r.Z > 99))
                        {
                            break;
                        }
                        else if(Main.chest[c] != null && TShock.Regions.InAreaRegion(Main.chest[c].x, Main.chest[c].y).Any(r => r != null && r.Z > 99))
                        {
                            index++;
                            continue;
                        }
                        else
                        {
                            Main.chest[c] = chest;
                            index++;
                            chests++;
                            break;
                        }
                    }
                }
                TSPlayer.All.SendInfoMessage("{0} of {1} Chest Data Loaded...", chests, totalChests);

                int totalSigns = reader.ReadInt32();
                int signs = 0;
                index = 0;
                for (int a = 0; a < totalSigns; a++)
                {
                    Sign sign = reader.ReadSign();
                    for (int s = index; s < 1000; s++)
                    {
                        if (TShock.Regions.InAreaRegion(sign.x, sign.y).Any(r => r != null && r.Z > 99))
                        {
                            break;
                        }
                        else if (Main.sign[s] != null && TShock.Regions.InAreaRegion(Main.sign[s].x, Main.sign[s].y).Any(r => r != null && r.Z > 99))
                        {
                            index++;
                            continue;
                        }
                        else
                        {
                            Main.sign[s] = sign;
                            index++;
                            signs++;
                            break;
                        }
                    }
                }
                TSPlayer.All.SendInfoMessage("{0} of {1} Signs Data Loaded...", signs, totalSigns);
            }
        }

        public static Tile ReadTile(this BinaryReader reader)
        {
            Tile tile = new Tile();
            tile.sTileHeader = reader.ReadInt16();
            tile.bTileHeader = reader.ReadByte();
            tile.bTileHeader2 = reader.ReadByte();

            // Tile type
            if (tile.active())
            {
                tile.type = reader.ReadUInt16();
                if (Main.tileFrameImportant[tile.type])
                {
                    tile.frameX = reader.ReadInt16();
                    tile.frameY = reader.ReadInt16();
                }
            }
            tile.wall = reader.ReadByte();
            tile.liquid = reader.ReadByte();
            return tile;
        }

        public static Chest ReadChest(this BinaryReader reader)
        {
            Chest chest = new Chest(false);
            chest.x = reader.ReadInt32();
            chest.y = reader.ReadInt32();
            chest.name = "World Chest";
            for (int l = 0; l < 40; l++)
            {
                Item item = new Item();
                int stack = reader.ReadInt16();
                if (stack > 0)
                {
                    int netID = reader.ReadInt32();
                    byte prefix = reader.ReadByte();
                    item.netDefaults(netID);
                    item.stack = stack;
                    item.Prefix(prefix);
                }
                chest.item[l] = item;
            }            
            return chest;
        }

        public static Sign ReadSign(this BinaryReader reader)
        {
            Sign sign = new Sign();
            sign.text = reader.ReadString();
            sign.x = reader.ReadInt32();
            sign.y = reader.ReadInt32();
            return sign;
        }

        public static void ResetSection(int x, int y, int x2, int y2)
        {
            int lowX = Netplay.GetSectionX(x);
            int highX = Netplay.GetSectionX(x2);
            int lowY = Netplay.GetSectionY(y);
            int highY = Netplay.GetSectionY(y2);
            foreach (RemoteClient sock in Netplay.Clients.Where(s => s.IsActive))
            {
                for (int i = lowX; i <= highX; i++)
                {
                    for (int j = lowY; j <= highY; j++)
                        sock.TileSections[i, j] = false;
                }
            }
        }

        public static void RegenerateWorld(string path)
        {
            using (var reader = new BinaryReader(new GZipStream(new FileStream(path, FileMode.Open), CompressionMode.Decompress)))
            {
                Main.worldSurface = reader.ReadDouble();
                Main.rockLayer = reader.ReadDouble();
                Main.dungeonX = reader.ReadInt32();
                Main.dungeonY = reader.ReadInt32();
                WorldGen.crimson = reader.ReadBoolean();

                reader.ReadInt32();
                reader.ReadInt32();

                int x = 0;
                int y = 0;

                int x2 = reader.ReadInt32();
                int y2 = reader.ReadInt32();

                for (int i = x; i <= x2; i++)
                {
                    for (int j = y; j <= y2; j++)
                    {
                        Tile tile = reader.ReadTile();
                        if (i >= 0 && j >= 0 && i < Main.maxTilesX && j < Main.maxTilesY)
                        {
                            if (TShock.Regions.InAreaRegion(i, j).Any(r => r != null && r.Z > 99))
                            {
                                continue;
                            }
                            else
                            {
                                Main.tile[i, j] = tile;
                            }
                        }
                    }
                }
                ResetSection(x, y, x2, y2);

                int totalChests = reader.ReadInt32();
                int chests = 0;
                int index = 0;
                for (int a = 0; a < totalChests; a++)
                {
                    Chest chest = reader.ReadChest();
                    for (int c = index; c < 1000; c++)
                    {
                        if (TShock.Regions.InAreaRegion(chest.x, chest.y).Any(r => r != null && r.Z > 99))
                        {
                            break;
                        }
                        else if (Main.chest[c] != null && TShock.Regions.InAreaRegion(Main.chest[c].x, Main.chest[c].y).Any(r => r != null && r.Z > 99))
                        {
                            index++;
                            continue;
                        }
                        else
                        {
                            Main.chest[c] = chest;
                            index++;
                            chests++;
                            break;
                        }
                    }
                }

                int totalSigns = reader.ReadInt32();
                int signs = 0;
                index = 0;
                for (int a = 0; a < totalSigns; a++)
                {
                    Sign sign = reader.ReadSign();
                    for (int s = index; s < 1000; s++)
                    {
                        if (TShock.Regions.InAreaRegion(sign.x, sign.y).Any(r => r != null && r.Z > 99))
                        {
                            break;
                        }
                        else if (Main.sign[s] != null && TShock.Regions.InAreaRegion(Main.sign[s].x, Main.sign[s].y).Any(r => r != null && r.Z > 99))
                        {
                            index++;
                            continue;
                        }
                        else
                        {
                            Main.sign[s] = sign;
                            index++;
                            signs++;
                            break;
                        }
                    }
                }
                TSPlayer.All.SendMessage(string.Format("The world has regenerated..."), 50, 255, 130);
            }
        }
    }
}

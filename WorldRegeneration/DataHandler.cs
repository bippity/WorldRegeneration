using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.GameContent.Events;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace WorldRegeneration
{
	public delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);
	public class GetDataHandlerArgs : EventArgs
	{
		public TSPlayer Player { get; private set; }
		public MemoryStream Data { get; private set; }

		public Player TPlayer
		{
			get { return Player.TPlayer; }
		}

		public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
		{
			Player = player;
			Data = data;
		}
	}

	public class Counters
	{
		public static int iMobKills = 0;
		public static int iExpertKills = 0;
	}

	public static class GetDataHandlers
	{
		private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;

		public static void InitGetDataHandler()
		{
			GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
			{
					{ PacketTypes.MassWireOperation, HandleMassWireOperation },
			};
		}

		public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
		{
			GetDataHandlerDelegate handler;
			if (GetDataHandlerDelegates.TryGetValue(type, out handler))
			{
				try
				{
					return handler(new GetDataHandlerArgs(player, data));
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}
			return false;
		}

		private static bool HandleMassWireOperation(GetDataHandlerArgs args)
		{
			int x1 = args.Data.ReadInt16();
			int y1 = args.Data.ReadInt16();
			int x2 = args.Data.ReadInt16();
			int y2 = args.Data.ReadInt16();

			if(WorldRegeneration.awaitingSelection)
			{
				args.Player.TempPoints[0].X = x1;
				args.Player.TempPoints[0].Y = y1;
				args.Player.TempPoints[1].X = x2;
				args.Player.TempPoints[1].Y = y2;

				args.Player.SendMessage("Selection Done.", Color.Yellow);
				WorldRegeneration.awaitingSelection = false;

				int x = Math.Min(x1, x2);
				int y = Math.Min(y1, y2);
				int width = Math.Max(x1, x2) - x;
				int height = Math.Max(y1, y2) - y;
				Rectangle rect = new Rectangle(x, y, width+1, height+1);

				Utilities.LoadWorldSection(WorldRegeneration.lastPath, rect, true);
			}
			return false;
		}
	}

}

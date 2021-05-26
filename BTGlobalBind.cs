using System;
using System.Collections.Generic;
using TShockAPI;

namespace BindTools
{
	public struct BTGlobalBind
	{
		public string Name { get; set; }
		public int ItemID { get; set; }
		public string[] Commands { get; set; }
		public string Permission { get; set; }
		public int Slot { get; set; }
		public int Prefix { get; set; }
		public bool Looping { get; set; }
		public bool Awaiting { get; set; }
		private int[] Count { get; set; }

		public BTGlobalBind(bool Empty)
		{
			Name = null;
			ItemID = -1;
			Commands = new string[0];
			Permission = null;
			Slot = -1;
			Prefix = -1;
			Looping = false;
			Awaiting = false;
			Count = new int[0];
		}

		public BTGlobalBind(string Name, int ItemID, string[] Commands, string Permission,
			int Slot = -1, int Prefix = -1, bool Looping = false, bool Awaiting = false)
		{
			this.Name = Name;
			this.ItemID = ItemID;
			this.Commands = Commands;
			this.Permission = Permission;
			this.Slot = Slot;
			this.Prefix = Prefix;
			this.Looping = Looping;
			this.Awaiting = Awaiting;
			Count = new int[255];
			for (int i = 0; i < 255; i++)
			{ Count[i] = 0; }
		}

		public void DoCommand(TSPlayer player)
		{
			try
			{
				if (Looping)
				{
					if (Count[player.Index] >= Commands.Length)
					{ Count[player.Index] = 0; }
					if (Awaiting)
					{
						BindTools.BTPlayers[player.Index].AddCommand(Commands[Count[player.Index]]);
						player.SendInfoMessage("Command {0} added in queue! Use '{1}bindwait' or '{1}bw' to see current awaiting command.", Commands[Count[player.Index]], TShock.Config.Settings.CommandSpecifier);
					}
					else { TShockAPI.Commands.HandleCommand(player, Commands[Count[player.Index]]); }
					Count[player.Index]++;
				}
				else
				{
					foreach (string cmd in Commands)
					{
						if (Awaiting)
						{
							BindTools.BTPlayers[player.Index].AddCommand(cmd);
							player.SendInfoMessage("Command {0} added in queue! Use '{1}bindwait' or '{1}bw' to see current awaiting command.", Commands[Count[player.Index]], TShock.Config.Settings.CommandSpecifier);
						}
						else { TShockAPI.Commands.HandleCommand(player, cmd); }
					}
				}
			}
			catch (Exception ex) { TShock.Log.ConsoleError(ex.ToString()); }
		}
	}
	public struct BTPrefix
	{
		public string Name { get; set; }
		public string Permission { get; set; }
		public List<int> AllowedPrefixes { get; set; }

		public BTPrefix(bool Empty)
		{
			Name = null;
			Permission = null;
			AllowedPrefixes = new List<int>();
		}

		public BTPrefix(string Name, string Permission, List<int> AllowedPrefixes)
		{
			this.Name = Name;
			this.Permission = Permission;
			this.AllowedPrefixes = AllowedPrefixes;
		}
	}
}
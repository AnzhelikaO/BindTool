using System;
using System.Collections.Generic;
using TShockAPI;

namespace BindTools
{
	public class BindTool
	{
		public List<string> commands;
		public bool awaiting;
		public int item;
		public bool looping;
		public int slot;
		public int prefix;
		public bool database;
		private int count;
		public BindTool(int item, int slot, List<string> commands, bool awaiting = false, bool looping = false, int prefix = -1, bool database = false)
		{
			this.item = item;
			this.commands = commands;
			this.awaiting = awaiting;
			this.looping = looping;
			this.slot = slot;
			this.prefix = prefix;
			this.database = database;
			count = 0;
		}
		public void DoCommand(TSPlayer player)
		{
			try
			{
				if (looping)
				{
					if (awaiting)
					{
						BindTools.BTPlayers[player.Index].AddCommand(commands[count]);
						player.SendInfoMessage("Command {0} added in queue! Use '{1}bindwait' or '{1}bw' to see current awaiting command.", commands[count], TShock.Config.Settings.CommandSpecifier);
					} else { Commands.HandleCommand(player, commands[count]); }
					count++;
					if (count >= commands.Count)
						count = 0;
				}
				else
				{
					foreach (string cmd in commands)
					{
						if (awaiting)
						{
							BindTools.BTPlayers[player.Index].AddCommand(cmd);
							player.SendInfoMessage("Command {0} added in queue! Use '{1}bindwait' or '{1}bw' to see current awaiting command.", commands[count], TShock.Config.Settings.CommandSpecifier);
						}
						else { Commands.HandleCommand(player, cmd); }
					}
				}
			}
			catch (Exception ex) { TShock.Log.ConsoleError(ex.ToString()); }
		}
	}
}
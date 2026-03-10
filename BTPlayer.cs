using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace BindTools
{
	public class BTPlayer
	{
		public TSPlayer tsPlayer { get; set; }
		public List<BindTool> bindTools { get; set; }
		public List<string> awaitingCommands { get; set; }
		private List<int> globalCount { get; set; }
		public bool hasAwaitingCommands { get { return (awaitingCommands.Count > 0); } }
		public string awaitingCommand { get { return ((awaitingCommands.Count > 0) ? awaitingCommands[0] : null); } }

		public BTPlayer(int Index)
		{
			tsPlayer = TShock.Players[Index];
			// fucking shit but i don't care how user or tsplayer becomes null
			bindTools = (tsPlayer?.Account is UserAccount user)
						? BTDatabase.BTGet(user.ID)
						: new List<BindTool>();
			awaitingCommands = new List<string>();
		}

		public void AddBindTool(BindTool NewBT, bool Database)
		{
			List<BindTool> Removed = new List<BindTool>();
			foreach (BindTool PT in bindTools)
			{
				if ((PT.item == NewBT.item)
					&& ((PT.slot == NewBT.slot)
						|| ((PT.slot != -1) && (NewBT.slot == -1))
						|| ((PT.slot == -1) && (NewBT.slot != -1)))
					&& ((PT.prefix == NewBT.prefix)
						|| ((PT.prefix != -1) && (NewBT.prefix == -1))
						|| ((PT.prefix == -1) && (NewBT.prefix != -1))))
				{
					Removed.Add(PT);
					if (Database && tsPlayer.IsLoggedIn)
					{ BTDatabase.BTDelete(tsPlayer.Account.ID, PT.item, PT.slot, PT.prefix); }
				}
			}
			bindTools = (from BindTool b in bindTools
						 where !Removed.Contains(b)
						 select b).ToList();
			bindTools.Add(NewBT);
			if (Database && tsPlayer.IsLoggedIn)
			{ BTDatabase.BTAdd(tsPlayer.Account.ID, NewBT); }
		}
		public BindTool GetBindTool(Item item, int Slot)
		{
			foreach (BindTool bt in bindTools)
			{
				if ((bt.item == item.type) && ((bt.slot == -1) || (bt.slot == Slot))
					&& ((bt.prefix == -1) || (bt.prefix == item.prefix))) { return bt; }
			}
			return null;
		}
		public void RemoveBindTool(int item, int slot, int prefix)
		{
			if ((slot == -1) && (prefix == -1))
			{
				if (tsPlayer.IsLoggedIn)
				{ BTDatabase.BTDelete(tsPlayer.Account.ID, item); }
				bindTools = (from BindTool b in bindTools
								where (b.item != item)
								select b).ToList();
			}
			else if (slot == -1)
			{
				if (tsPlayer.IsLoggedIn)
				{ BTDatabase.BTDelete(tsPlayer.Account.ID, item, slot); }
				bindTools = (from BindTool b in bindTools
							 where !((b.item == item)
								 && (b.prefix == prefix))
							 select b).ToList();
			}
			else if (prefix == -1)
			{
				if (tsPlayer.IsLoggedIn)
				{ BTDatabase.BTDelete(tsPlayer.Account.ID, item, prefix, false); }
				bindTools = (from BindTool b in bindTools
							 where !((b.item == item)
								 && (b.prefix == prefix))
							 select b).ToList();
			}
			else
			{
				if (tsPlayer.IsLoggedIn)
				{ BTDatabase.BTDelete(tsPlayer.Account.ID, item, slot, prefix); }
				bindTools = (from BindTool b in bindTools
							 where !((b.item == item)
								 && (b.slot == slot)
								 && (b.prefix == prefix))
							 select b).ToList();
			}
		}

		public BTGlobalBind GetGlobalBind(Item item, int Slot)
		{
			foreach (BTGlobalBind gb in BindTools.GlobalBinds)
			{
				if ((gb.ItemID == item.type) && tsPlayer.HasPermission(gb.Permission)
					&& ((gb.Slot == -1) || (gb.Slot == Slot))
					&& ((gb.Prefix == -1) || (gb.Prefix == item.prefix))) { return gb; }
			}
			return new BTGlobalBind(true);
		}
		public BTGlobalBind GetGlobalBind(int item, int Slot, int Prefix)
		{
			foreach (BTGlobalBind gb in BindTools.GlobalBinds)
			{
				if ((gb.ItemID == item) && tsPlayer.HasPermission(gb.Permission)
					&& ((gb.Slot == -1) || (gb.Slot == Slot))
					&& ((gb.Prefix == -1) || (gb.Prefix == Prefix))) { return gb; }
			}
			return new BTGlobalBind(true);
		}

		public void AddCommand(string Format)
		{ awaitingCommands = (new List<string> { Format }).Concat(awaitingCommands).ToList(); }
		public bool ExecuteCommand(params string[] args)
		{
			try
			{
				string Command = string.Format(awaitingCommands[0], args);
				Commands.HandleCommand(tsPlayer, Command);
				awaitingCommands.RemoveAt(0);
				return true;
			}
			catch { return false; }
		}
	}
}
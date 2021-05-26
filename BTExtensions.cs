using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using static BindTools.BindTools;

namespace BindTools
{
	class BTExtensions
	{
		public static BTGlobalBind GetGlobalBind(Item item, int Slot)
		{
			foreach (BTGlobalBind gb in GlobalBinds)
			{
				if ((gb.ItemID == item.netID) && ((gb.Slot == -1) || (gb.Slot == Slot))
					&& ((gb.Prefix == -1) || (gb.Prefix == item.prefix))) { return gb; }
			}
			return new BTGlobalBind(true);
        }
        public static bool AnyGBMatch(BindTool BindTool)
        {
            foreach (BTGlobalBind GlobalBind in GlobalBinds)
            {
                if ((GlobalBind.ItemID == BindTool.item)
                && ((GlobalBind.Slot == BindTool.slot)
                    || ((GlobalBind.Slot != -1) && (BindTool.slot == -1))
                    || ((GlobalBind.Slot == -1) && (BindTool.slot != -1)))
                && ((GlobalBind.Prefix == BindTool.prefix)
                    || ((GlobalBind.Prefix != -1) && (BindTool.prefix == -1))
                    || ((GlobalBind.Prefix == -1) && (BindTool.prefix != -1))))
                { return true; }
            }
            return false;
		}

        public static void ManageGlobalBinds(CommandArgs args)
		{
			bool Add = args.Parameters[0].ToLower() == "add";
			if (Add)
			{
				if (args.Parameters.Count < 9)
				{
					args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier + "bindglobal add [Name] [ItemID] [Permission] [SlotID] [PrefixID] [Looping] [Awaiting] commands; separated; by semicolon\r\n" +
						"SlotID: -1 for any; 1-10 - hotbar; 100 for cursor\r\n" +
						"PrefixID: -1 for any; Looping: true/false; Awaiting: true/false");
					return;
				}
				string Name = args.Parameters[1];
				if (GlobalBinds.Any(b => b.Name == Name))
				{
					args.Player.SendErrorMessage("Global bind with '{0}' name already exists.", Name);
					return;
				}
				if (!int.TryParse(args.Parameters[2], out int ItemID)
					|| (ItemID < 0) || (ItemID > (Main.maxItemTypes - 1)))
				{
					args.Player.SendErrorMessage("Invalid ItemID!");
					return;
				}
				string Permission = args.Parameters[3];
				if (!int.TryParse(args.Parameters[4], out int Slot)
					|| ((Slot < 1) && (Slot != -1)) || ((Slot > 10) && (Slot != 100)))
				{
					args.Player.SendErrorMessage("Invalid SlotID!");
					return;
				}
				if (!int.TryParse(args.Parameters[5], out int Prefix)
					|| ((Prefix < 0) && (Prefix != -1)) || (Prefix > (Lang.prefix.Length - 1)))
				{
					args.Player.SendErrorMessage("Invalid PrefixID!");
					return;
				}
				if (!bool.TryParse(args.Parameters[6], out bool Looping))
				{
					args.Player.SendErrorMessage("Invalid Looping value!");
					return;
				}
				if (!bool.TryParse(args.Parameters[7], out bool Awaiting))
				{
					args.Player.SendErrorMessage("Invalid Awaiting value!");
					return;
				}

				string NewMsg = string.Join(" ", args.Message.Replace("\"", "\\\"").Split(' ').Skip(9));
				string[] Commands = string.Join(" ", ParseParameters(NewMsg)).Split(';');

				for (int i = 0; i < Commands.Length; i++)
				{ Commands[i] = Commands[i].TrimStart(' '); }

				var GB = new BTGlobalBind(Name, ItemID, Commands, Permission,
						((Slot == 100) ? 58 : (Slot == -1) ? -1 : (Slot - 1)), Prefix, Looping, Awaiting);
				GlobalBinds.Add(GB);
				BTDatabase.GBAdd(GB);
				args.Player.SendSuccessMessage("Successfully added new global bind with given name: {0}.", Name);
			}
			else
			{
				if (args.Parameters.Count != 2)
				{
					args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier + "bindglobal del [Name]");
					return;
				}
				string Name = args.Parameters[1];
				bool Success = false;
				foreach (var b in GlobalBinds)
				{
					if (b.Name == Name)
					{
						Success = true;
						GlobalBinds.Remove(b);
						break;
					}
				}
				if (Success)
				{
					args.Player.SendSuccessMessage("Successfully deleted global bind with given name: {0}.", Name);
					BTDatabase.GBDelete(Name);
				}
				else { args.Player.SendErrorMessage("Invalid GlobalBind name!"); }
			}
		}
		public static void ManagePrefixGroups(CommandArgs args)
		{
			bool Add = args.Parameters[0].ToLower() == "add";
			if (Add)
			{
				if (args.Parameters.Count < 5)
				{
					args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier + "bprefix add group [Name] [Permission] [AllowedPrefixes (1 3 10...)]");
					return;
				}
				string Name = args.Parameters[2];
				if (Prefixes.Any(b => b.Name == Name))
				{
					args.Player.SendErrorMessage("Prefix group with '{0}' name already exists.", Name);
					return;
				}
				string Permission = args.Parameters[3];
				List<int> AllowedPrefixes = new List<int>();
				for (int i = 4; i < args.Parameters.Count; i++)
				{
					if (!int.TryParse(args.Parameters[i], out int PrefixID)
						|| (PrefixID < 0) || (PrefixID > (Lang.prefix.Length - 1)))
					{
						args.Player.SendErrorMessage("Invalid PrefixID '{0}'!", args.Parameters[i]);
						continue;
					}
					AllowedPrefixes.Add(PrefixID);
				}

				var BTP = new BTPrefix(Name, Permission, AllowedPrefixes);
				Prefixes.Add(BTP);
				BTDatabase.PAdd(BTP);
				args.Player.SendSuccessMessage("Successfully added new prefix group with given name: {0}.", Name);
			}
			else
			{
				if (args.Parameters.Count != 3)
				{
					args.Player.SendErrorMessage(TShock.Config.Settings.CommandSpecifier + "bprefix del group [Name]");
					return;
				}
				string Name = args.Parameters[2];
				bool Success = false;
				foreach (var p in Prefixes)
				{
					if (p.Name == Name)
					{
						Success = true;
						Prefixes.Remove(p);
						break;
					}
				}
				if (Success)
				{
					args.Player.SendSuccessMessage("Successfully deleted prefix group with given name: {0}.", Name);
					BTDatabase.PDelete(Name);
				}
				else { args.Player.SendErrorMessage("Invalid PrefixGroup name!"); }
			}
		}
		public static void ManagePrefixesInPrefixGroups(CommandArgs args)
		{
			bool Add = (args.Parameters[0] == "add");
			if (args.Parameters.Count < 3)
			{
				args.Player.SendErrorMessage("{0}bprefix " + (Add ? "add" : "del") + " prefix [PrefixGroupName] [PrefixID]", TShock.Config.Settings.CommandSpecifier);
				return;
			}

			int Index = -1;
			string Name = args.Parameters[2];
			for (int i = 0; i < Prefixes.Count; i++)
			{
				if (Prefixes[i].Name == Name)
				{
					Index = i;
					break;
				}
			}

			if (Index == -1)
			{
				args.Player.SendErrorMessage("Invalid PrefixGroup name!");
				return;
			}

			if (!int.TryParse(args.Parameters[3], out int Prefix)
				|| (Prefix < 0) || (Prefix > (Lang.prefix.Length - 1)))
			{
				args.Player.SendErrorMessage("Invalid PrefixID!");
				return;
			}

			if (Add) { Prefixes[Index].AllowedPrefixes.Add(Prefix); }
			else { Prefixes[Index].AllowedPrefixes.Remove(Prefix); }
			BTDatabase.PUpdate(Prefixes[Index]);

			args.Player.SendSuccessMessage("Successfully {0} prefix", (Add ? "added new" : "deleted"));
		}

		public static bool IsAdmin(TSPlayer Player)
		{
			bool isAdmin = Player.HasPermission("bindtools.admin");
			if (!isAdmin) { Player.SendErrorMessage("You do not have access to this command."); }
			return isAdmin;
		}

		public static Tuple<bool, List<int>> AvailablePrefixes(TSPlayer Player)
		{
			if (Player.HasPermission(BTPermissions.AllowAllPrefixes)) { return new Tuple<bool, List<int>>(true, null); }
			List<int> Allowed = new List<int>();
			foreach (BTPrefix p in Prefixes)
			{
				if (Player.HasPermission(p.Permission))
				{ Allowed.AddRange(p.AllowedPrefixes); }
			}
			return new Tuple<bool, List<int>>(false, Allowed);
		}

		#region TShock parser code

		public static List<String> ParseParameters(string str)
		{
			var ret = new List<string>();
			var sb = new StringBuilder();
			bool instr = false;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				if (c == '\\' && ++i < str.Length)
				{
					if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
						sb.Append('\\');
					sb.Append(str[i]);
				}
				else if (c == '"')
				{
					instr = !instr;
					if (!instr)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
					else if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else if (IsWhiteSpace(c) && !instr)
				{
					if (sb.Length > 0)
					{
						ret.Add(sb.ToString());
						sb.Clear();
					}
				}
				else
					sb.Append(c);
			}
			if (sb.Length > 0)
				ret.Add(sb.ToString());

			return ret;
		}

		public static bool IsWhiteSpace(char c)
		{ return c == ' ' || c == '\t' || c == '\n'; }

		#endregion
	}
}
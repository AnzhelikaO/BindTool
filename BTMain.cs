using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;
using Terraria.Localization;

namespace BindTools
{
	[ApiVersion(2, 1)]
	public class BindTools : TerrariaPlugin
	{
		public static BTPlayer[] BTPlayers = new BTPlayer[255];
		public static List<BTGlobalBind> GlobalBinds = new List<BTGlobalBind>();
		public static List<BTPrefix> Prefixes = new List<BTPrefix>();
		public override string Name { get { return "BindTools"; } }
		public override string Author { get { return "by Jewsus & Anzhelika"; } }
		public override string Description { get { return "Enables command binding to tools. Rewrite of InanZen's AdminTools"; } }
		public override Version Version { get { return new Version("1.3"); } }
		public BindTools(Main game) : base(game) { }

		public override void Initialize()
		{
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			PlayerHooks.PlayerPostLogin += OnLogin;
			GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
			GeneralHooks.ReloadEvent += OnReload;
			Commands.ChatCommands.Add(new Command(BTPermissions.BTool, BindToolCMD, "bindtool", "bt")
			{
				AllowServer = false,
				HelpText = string.Format("Use '{0}bt help'.", TShock.Config.Settings.CommandSpecifier)
			});
			Commands.ChatCommands.Add(new Command(BTPermissions.BWait, BindWaitCMD, "bindwait", "bw")
			{
				AllowServer = false,
				HelpText = string.Format("Use '{0}bw help'.", TShock.Config.Settings.CommandSpecifier)
			});
			Commands.ChatCommands.Add(new Command(BTPermissions.BGlobal, BindGlobalCMD, "bindglobal", "bgl")
			{
				HelpText = string.Format("Use '{0}bgl help'.", TShock.Config.Settings.CommandSpecifier)
			});
			Commands.ChatCommands.Add(new Command(BTPermissions.BPrefix, BindPrefixCMD, "bprefix", "bindprefix", "bpr")
			{
				AllowServer = false,
				HelpText = string.Format("Use '{0}bgl help'.", TShock.Config.Settings.CommandSpecifier)
			});
			BTDatabase.DBConnect();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				PlayerHooks.PlayerPostLogin -= OnLogin;
				GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
				GeneralHooks.ReloadEvent -= OnReload;
			}
			base.Dispose(disposing);
		}

		private void OnGreet(GreetPlayerEventArgs args)
		{ BTPlayers[args.Who] = new BTPlayer(args.Who); }
		private void OnLogin(PlayerPostLoginEventArgs args)
		{
            if (args?.Player == null) { return; }
            BTPlayers[args.Player.Index] = new BTPlayer(args.Player.Index);
        }
		private void OnLeave(LeaveEventArgs args)
		{ BTPlayers[args.Who] = null; }
		private void OnReload(ReloadEventArgs args)
		{ BTDatabase.GBGet(); BTDatabase.PGet(); }

		private void BindToolCMD(CommandArgs args)
		{
			var player = BTPlayers[args.Player.Index];
			if ((player == null) || (player.tsPlayer == null)) return;

			if (args.Parameters.Count == 0)
			{
				args.Player.SendMessage("BindTool usage:", Color.LightSalmon);
				args.Player.SendMessage(string.Format("{0}bindtool [-flags] commands; separated; by semicolon", TShock.Config.Settings.CommandSpecifier), Color.BurlyWood);
				args.Player.SendMessage("This will bind those commands to the current item in hand.", Color.BurlyWood);
				args.Player.SendMessage(string.Format("Type {0}bt help for flag info.", TShock.Config.Settings.CommandSpecifier), Color.BurlyWood);
				args.Player.SendMessage(string.Format("Type {0}bt list for current bind list.", TShock.Config.Settings.CommandSpecifier), Color.BurlyWood);
				return;
			}

			if (args.Parameters[0].ToLower() == "help")
			{
				List<string> Help = new List<string>
				{
					"-l will loop trough commands in order",
					"[c/aaaa00:-s will bind item only at certain slot]",
					"-p will bind item only with certain prefix",
					"[c/aaaa00:-d will add bind to database, so it will be saved and can be used after rejoin]",
					"You can combine flags: -spd = slot + prefix + database",
					string.Format("[c/aaaa00:-w instead of execution will add command to queue, so you could add parameters later] (write {0}bt help wait for more info)", TShock.Config.Settings.CommandSpecifier),
					"-c will clear all commands from the item at certain slot with certain prefix",
					"[c/aaaa00:-csp = clear any bind on item; -cs = clear binds on item with certain prefix, but any slot; -cp = clear binds on item with certain slot, but any prefix]"
				};
				int page = 1;
				if ((args.Parameters.Count > 1) && (!int.TryParse(args.Parameters[1], out page)))
				{
					if (args.Parameters[1].ToLower() == "wait")
					{
						page = 1;
						Help = new List<string>
						{
							"Text format uses {Num} syntax. Parameters start from 0.",
							string.Format("[c/aaaa00:For example ' {0}bt {0}region allow ", TShock.Config.Settings.CommandSpecifier) + "\"{0}\" " + string.Format("\"Region Name\" ' will allow you to use {0}bw Player1, {0}bw \"Player 2\", etc.]", TShock.Config.Settings.CommandSpecifier),
							"You need to fill all {Num} fields used in your bind command."
						};
					}
					else { args.Player.SendErrorMessage("\"{0}\" is not a valid page number.", args.Parameters[1]); return; }
				}
				PaginationTools.SendPage(args.Player, page, Help,
						new PaginationTools.Settings
						{
							HeaderFormat = "Bindtools help ({0}/{1}):",
							FooterFormat = "Type {0}bt help {{0}} for more info.".SFormat(TShock.Config.Settings.CommandSpecifier)
						}
					);
				return;
			}
			else if (args.Parameters[0].ToLower() == "list")
			{
				int page = 1;
				if ((args.Parameters.Count > 1)
					&& (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page)))
				{ return; }
				var Normal = BTPlayers[args.Player.Index].bindTools.Select
				(
					b => (string.Format("Item: [i:{0}]. Commands: {1}. Awaiting: {2}. Looping: {3}. Slot: {4}. Prefix: {5}. Database: {6}.",
						b.item,
						string.Join("; ", b.commands),
						b.awaiting,
						b.looping,
						((b.slot == -1) ? "Any" : (b.slot == 58) ? "Cursor" : "Hotbar-" + (b.slot + 1)),
						((b.prefix == -1) ? "Any" : (b.prefix == 0) ? "None" : Lang.prefix[b.prefix].Value),
						b.database))
				).ToList();
				PaginationTools.SendPage(args.Player, page, Normal,
						new PaginationTools.Settings
						{
							HeaderFormat = "Current binds ({0}/{1}):",
							FooterFormat = "Type {0}bt list {{0}} for more info.".SFormat(TShock.Config.Settings.CommandSpecifier),
							NothingToDisplayString = "You do not have any binds."
						}
					);
				return;
			}

			if ((args.Player.TPlayer.selectedItem > 9) && (args.Player.TPlayer.selectedItem != 58))
			{
				args.Player.SendMessage("Please select an item from your hotbar or cursor", Color.Red);
				return;
			}

			byte flagmod = 0;
			bool awaiting = false, looping = false, slot = false, clear = false;
			bool prefix = false, database = false;

			if (args.Parameters[0].StartsWith("-"))
			{
				flagmod = 1;
				for (int i = 1; i < args.Parameters[0].Length; i++)
				{
					if ((args.Parameters[0][i] == 'w') || ((args.Parameters[0][i] == 'W')))
						awaiting = true;
					else if ((args.Parameters[0][i] == 'l') || ((args.Parameters[0][i] == 'L')))
						looping = true;
					else if ((args.Parameters[0][i] == 's') || ((args.Parameters[0][i] == 'S')))
						slot = true;
					else if ((args.Parameters[0][i] == 'c') || ((args.Parameters[0][i] == 'C')))
						clear = true;
					else if ((args.Parameters[0][i] == 'p') || ((args.Parameters[0][i] == 'P')))
						prefix = true;
					else if ((args.Parameters[0][i] == 'd') || ((args.Parameters[0][i] == 'D')))
						database = true;
					else
					{
						args.Player.SendMessage("Invalid BindTool flag.", Color.LightSalmon);
						args.Player.SendMessage("Valid flags are 'w' [awaiting], 'l' [looping], 's' [slot], 'p' [prefix], " +
							"'d' [database], 'c' [clear], 'ca' [clear any], 'ce' [clear everything]", Color.BurlyWood);
						args.Player.SendMessage("You can combine flags: -spd = slot + prefix + database, -csp = clear any bind on item; -cs = clear binds on item with certain prefix, but any slot", Color.BurlyWood);
						return;
					}
				}
			}

			var item = args.Player.TPlayer.inventory[args.Player.TPlayer.selectedItem];

			if (clear)
			{
				string _prefix = Lang.prefix[item.prefix].Value;
				player.RemoveBindTool(item.netID, ((slot) ? -1 : args.Player.TPlayer.selectedItem), ((prefix) ? -1 : item.prefix));
				args.Player.SendMessage(string.Format("All commands have been removed from [i:{0}]{1}{2}", item.netID,
					((slot) ? "" : " at " + ((args.Player.TPlayer.selectedItem > 9) ? "cursor" : "hotbar-" + (args.Player.TPlayer.selectedItem + 1)) + " slot"),
					((prefix) ? "" : (" with " + (_prefix == "" ? "no" : _prefix) + " prefix"))), Color.BurlyWood);
				return;
			}

			else if (args.Parameters.Count < 1) { args.Player.SendMessage("Missing commands", Color.LightSalmon); return; }

			string NewMsg = string.Join(" ", args.Message.Replace("\"", "\\\"").Split(' ').Skip(1));
			List<string> NewArgs = BTExtensions.ParseParameters(NewMsg);

			var cmdstring = string.Join(" ", NewArgs.GetRange(flagmod, NewArgs.Count - flagmod));
			List<string> cmdlist = cmdstring.Split(';').ToList();

			for (int i = 0; i < cmdlist.Count; i++)
			{ cmdlist[i] = cmdlist[i].TrimStart(' '); }

			BindTool BindTool = new BindTool(item.netID, (slot ? args.Player.TPlayer.selectedItem : -1), cmdlist, awaiting, looping, (prefix ? item.prefix : -1), database);
			
			if (BTExtensions.AnyGBMatch(BindTool) && !args.Player.HasPermission(BTPermissions.Overwrite))
			{
				args.Player.SendErrorMessage("You can't overwrite global binds!");
				return;
			}

			player.AddBindTool(BindTool, database);

			string Prefix = Lang.prefix[item.prefix].Value;

			StringBuilder builder = new StringBuilder();
			builder.Append("Bound");
			foreach (string cmd in cmdlist) { builder.AppendFormat(" '{0}'", cmd); }
			builder.AppendFormat(" to {0}{1}{2} (Database: {3})", item.Name,
				(slot ? (" at " + ((args.Player.TPlayer.selectedItem > 9) ? "cursor" : "hotbar-" + (args.Player.TPlayer.selectedItem + 1)) + " slot") : ""),
				(prefix ? (" with " + ((Prefix == "") ? "no" : Prefix) + " prefix") : ""), database);

			args.Player.SendMessage(builder.ToString(), Color.BurlyWood);
		}

		private void BindWaitCMD(CommandArgs args)
		{
			var player = BTPlayers[args.Player.Index];
			if ((player == null) || (player.tsPlayer == null)) return;
			if (args.Parameters.Count == 0)
			{
				if (player.hasAwaitingCommands) { player.tsPlayer.SendSuccessMessage("Current command format: {0}", player.awaitingCommand); }
				else { player.tsPlayer.SendSuccessMessage("You do not have any awaiting commands."); }
				return;
			}
			else if (args.Parameters[0].ToLower() == "help")
			{
				List<string> Help = new List<string>
				{
					string.Format("[c/aaaa00:'{0}bindwait list' or '{0}bw list' - shows all awaiting commands]", TShock.Config.Settings.CommandSpecifier),
					string.Format("'{0}bindwait skip <Count/\"all\">' - skips commands in queue", TShock.Config.Settings.CommandSpecifier),
					string.Format("[c/aaaa00:'{0}bindwait \"Argument1\" \"Argument2\" \"Argument3\" ...' - executes current awaiting command with certain arguments]", TShock.Config.Settings.CommandSpecifier),
					"You need to fill all argument fields."
				};
				PaginationTools.SendPage(args.Player, 1, Help, new PaginationTools.Settings { HeaderFormat = "Bindwait help ({0}/{1}):" });
				return;
			}
			else if (args.Parameters[0].ToLower() == "list")
			{
				int page = 1;
				if ((args.Parameters.Count > 1)
					&& (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page)))
				{ return; }
				PaginationTools.SendPage(args.Player, page, player.awaitingCommands,
						new PaginationTools.Settings
						{
							HeaderFormat = "Binds queue ({0}/{1}):",
							FooterFormat = "Type {0}bw list {{0}} for more info.".SFormat(TShock.Config.Settings.CommandSpecifier),
							NothingToDisplayString = "You do not have any awaiting commands."
						}
					);
				return;
			}
			else if (args.Parameters[0].ToLower() == "skip")
			{
				int count = 1;
				if ((args.Parameters.Count > 1) && (!int.TryParse(args.Parameters[1], out count)))
				{
					if (args.Parameters[1].ToLower() == "all") { count = player.awaitingCommands.Count; }
					else { args.Player.SendErrorMessage("Invalid skip count!"); return; }
				}
				if (count > player.awaitingCommands.Count) { count = player.awaitingCommands.Count; }
				player.awaitingCommands.RemoveRange(0, count);
				args.Player.SendSuccessMessage("Successfully skiped {0} commands.", count);
				return;
			}

			if (!player.ExecuteCommand(args.Parameters.ToArray()))
			{ args.Player.SendErrorMessage("Invalid text format!"); }
		}

		private void BindGlobalCMD(CommandArgs args)
		{
			string Parameter = (args.Parameters.Count == 0) ? "help" : args.Parameters[0].ToLower();
			switch (Parameter)
			{
				case "add":
				case "del":
					{
						if (!BTExtensions.IsAdmin(args.Player)) { return; }
						BTExtensions.ManageGlobalBinds(args);
						break;
					}
				case "list":
					{
						int page = 1;
						if ((args.Parameters.Count > 1)
							&& (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page)))
						{ return; }

						var GBinds = (from BTGlobalBind b in GlobalBinds
										where args.Player.HasPermission(b.Permission)
										select (string.Format("Item: [i:{0}]. Name: {1}. Commands: {2}. Permission: {3}. Awaiting: {4}. Looping: {5}. Slot: {6}. Prefix: {7}.",
											b.ItemID,
											b.Name,
											string.Join("; ", b.Commands),
											b.Permission,
											b.Awaiting,
											b.Looping,
											((b.Slot == -1) ? "Any" : (b.Slot == 58) ? "Cursor" : "Hotbar-" + (b.Slot + 1)),
											((b.Prefix == -1) ? "Any" : (b.Prefix == 0) ? "None" : Lang.prefix[b.Prefix].Value)))).ToList();

						PaginationTools.SendPage(args.Player, page, GBinds,
								new PaginationTools.Settings
								{
									HeaderFormat = "Global binds ({0}/{1}):",
									FooterFormat = "Type {0}bgl list b {{0}} for more info.".SFormat(TShock.Config.Settings.CommandSpecifier),
									NothingToDisplayString = "There are currently no global binds you allowed to use."
								}
							);
						break;
					}
				case "help":
					{
						List<string> Help = new List<string>
						{
							string.Format("{0}bgl list [page]", TShock.Config.Settings.CommandSpecifier),
						};
						if (args.Player.HasPermission("bindtools.admin"))
						{
							List<string> Help2 = new List<string>
							{
								string.Format("{0}bgl add [Name] [ItemID] [Permission] [SlotID] [PrefixID] [Looping] [Awaiting] commands; separated; by semicolon", TShock.Config.Settings.CommandSpecifier),
								string.Format("{0}bgl del [Name]", TShock.Config.Settings.CommandSpecifier),
								"SlotID: -1 for any; 1-10 - hotbar; 100 for cursor",
								"PrefixID: -1 for any; Looping: true/false; Awaiting: true/false",
							};
							Help.AddRange(Help2);
						}
						int page = 1;
						if ((args.Parameters.Count > 1)
							&& (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page)))
						{ return; }
						PaginationTools.SendPage(args.Player, page, Help,
								new PaginationTools.Settings
								{
									HeaderFormat = "BindGlobal help ({0}/{1}):",
									FooterFormat = "Type {0}bgl help {{0}} for more info.".SFormat(TShock.Config.Settings.CommandSpecifier)
								}
							);
						break;
					}
			}
		}

		private void BindPrefixCMD(CommandArgs args)
		{
			string Parameter = (args.Parameters.Count == 0) ? "help" : args.Parameters[0].ToLower();
			switch (Parameter)
			{
				case "add":
				case "del":
					{
						if (!BTExtensions.IsAdmin(args.Player)) { return; }
						string Parameter2 = (args.Parameters.Count == 1) ? "help" : args.Parameters[1].ToLower();
						switch (Parameter2)
						{
							case "g":
							case "group":
								{
									BTExtensions.ManagePrefixGroups(args);
									return;
								}
							case "p":
							case "prefix":
								{
									BTExtensions.ManagePrefixesInPrefixGroups(args);
									return;
								}
							default:
								{
                                    args.Player.SendSuccessMessage("BindPrefix help (1/1):");
									args.Player.SendInfoMessage("{0}bprefix add group [Name] [Permission] [AllowedPrefixes (1 3 10...)]\r\n" +
										"{0}bprefix del group [Name]\r\n" +
										"{0}bprefix <add/del> prefix [Name] [PrefixID]", TShock.Config.Settings.CommandSpecifier);
									return;
								}
						}
					}
				case "list":
					{
						var Available = BTExtensions.AvailablePrefixes(args.Player);
						if (Available.Item1) { args.Player.SendSuccessMessage("All prefixes available."); }
						else if (Available.Item2.Count == 0) { args.Player.SendSuccessMessage("No prefixes available."); }
						else { args.Player.SendSuccessMessage("Available prefixes: {0}.", string.Join(", ", Available.Item2)); }
						return;
					}
				case "listgr":
					{
						if (!BTExtensions.IsAdmin(args.Player)) { return; }
						args.Player.SendSuccessMessage("Available prefix groups:");
						args.Player.SendInfoMessage(string.Join("\r\n", Prefixes.Select(p =>
							string.Format("Name: {0}. Permission: {1}. Prefixes: {2}.",
								p.Name, p.Permission, string.Join(", ", p.AllowedPrefixes)))));
						return;
					}
				case "help":
					{
						List<string> Help = new List<string>
						{
							string.Format("{0}bpr [PrefixID]", TShock.Config.Settings.CommandSpecifier),
							string.Format("{0}bpr list [page]", TShock.Config.Settings.CommandSpecifier)
						};
						if (args.Player.HasPermission("bindtools.admin"))
						{
							List<string> Help2 = new List<string>
							{
								string.Format("{0}bpr listgr [page]", TShock.Config.Settings.CommandSpecifier),
								string.Format("{0}bpr add <group/g> [Name] [Permission] [AllowedPrefixes (1 3 10...)]", TShock.Config.Settings.CommandSpecifier),
								string.Format("{0}bpr del <group/g> [Name]", TShock.Config.Settings.CommandSpecifier),
								string.Format("{0}bpr <add/del> <prefix/p> [Name] [PrefixID]", TShock.Config.Settings.CommandSpecifier)
							};
							Help.AddRange(Help2);
						}
						int page = 1;
						if ((args.Parameters.Count > 1)
							&& (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out page)))
						{ return; }
						PaginationTools.SendPage(args.Player, page, Help,
								new PaginationTools.Settings
								{
									HeaderFormat = "BindPrefix help ({0}/{1}):",
									FooterFormat = "Type {0}bpr help {{0}} for more info.".SFormat(TShock.Config.Settings.CommandSpecifier)
								}
							);
						return;
					}
			}
			
			Tuple<bool, List<int>> Allowed = BTExtensions.AvailablePrefixes(args.Player);
			if (((Allowed.Item2 == null) || (Allowed.Item2.Count == 0)) && !Allowed.Item1)
			{
				args.Player.SendErrorMessage("No prefixes allowed.");
				return;
			}
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("/bpr [PrefixID]");
				return;
			}
			if ((args.Player.TPlayer.selectedItem > 9) && (args.Player.TPlayer.selectedItem != 58))
			{
				args.Player.SendMessage("Please select an item from your hotbar or cursor", Color.Red);
				return;
			}
			if (!int.TryParse(args.Parameters[0], out int Prefix)
				|| (Prefix < 0) || (Prefix > (Lang.prefix.Length - 1)))
			{
				args.Player.SendErrorMessage("Invalid PrefixID!");
				return;
			}
			if (((Allowed.Item2 == null) || (!Allowed.Item2.Contains(Prefix))) && !Allowed.Item1)
			{
				args.Player.SendErrorMessage("This prefix is not allowed!");
				return;
			}

			bool SSC = Main.ServerSideCharacter;
			if (!SSC)
			{
				Main.ServerSideCharacter = true;
				NetMessage.SendData((int)PacketTypes.WorldInfo, args.Player.Index, -1, NetworkText.Empty);
			}

			Item Item = args.Player.TPlayer.inventory[args.Player.TPlayer.selectedItem];
			Item.prefix = (byte)Prefix;
			args.Player.TPlayer.inventory[args.Player.TPlayer.selectedItem] = Item;
			NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(Item.Name), args.Player.Index, args.Player.TPlayer.selectedItem, Prefix);
			NetMessage.SendData((int)PacketTypes.PlayerSlot, args.Player.Index, -1, NetworkText.FromLiteral(Item.Name), args.Player.Index, args.Player.TPlayer.selectedItem, Prefix);

			if (!SSC)
			{
				Main.ServerSideCharacter = false;
				NetMessage.SendData((int)PacketTypes.WorldInfo, args.Player.Index, -1, NetworkText.Empty);
			}

			args.Player.SendSuccessMessage("Successfully changed [i:{0}]'s prefix to {1} ({2})", args.Player.TPlayer.inventory[args.Player.TPlayer.selectedItem].netID, Prefix, Lang.prefix[Prefix].Value);
		}

		private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
		{
			BTPlayer player = BTPlayers[args.PlayerId];
			if ((player == null) || (player.tsPlayer == null)
				|| ((player.bindTools.Count == 0) && (GlobalBinds.Count == 0))) return;
			if (args.Control.IsUsingItem)
			{
				try
				{
					Item Selected = Main.player[args.PlayerId].inventory[args.SelectedItem];
					var GB = BTExtensions.GetGlobalBind(Selected, args.SelectedItem);
					var BT = player.GetBindTool(Selected, args.SelectedItem);

					if ((GB.Name != null) && (player.tsPlayer.HasPermission(GB.Permission)) && (BT == null))
					{ GB.DoCommand(player.tsPlayer); }

					else if (BT != null)
					{ BT.DoCommand(player.tsPlayer); }
				}
				catch (Exception ex) { TShock.Log.ConsoleError(ex.ToString()); }
			}
		}
	}
}
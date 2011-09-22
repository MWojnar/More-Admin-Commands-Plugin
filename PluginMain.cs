using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using Community.CsharpSqlite.SQLiteClient;
using MySql.Data.MySqlClient;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaAPI;
using TerrariaAPI.Hooks;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Threading;

namespace PluginTemplate
{
    [APIVersion(1, 8)]
    public class PluginTemplate : TerrariaPlugin
    {
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        public static bool timeFrozen = false;
        public static double timeToFreezeAt = 1000;
        public static bool freezeDayTime = true;
        public static bool[] isGhost = new bool[256];
        public static bool[] isHeal = new bool[256];
        public static bool[] flyMode = new bool[256];
        public static List<List<PointF>> carpetPoints = new List<List<PointF>>();
        public static int[] carpetY = new int[256];
        public static bool[] upPressed = new bool[256];
        public static bool cansend = false;
        public override string Name
        {
            get { return "MoreAdminCommands"; }
        }
        public override string Author
        {
            get { return "Created by DaGamesta"; }
        }
        public override string Description
        {
            get { return ""; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override void Initialize()
        {
            GameHooks.Initialize += OnInitialize;
            GameHooks.Update += OnUpdate;
            ServerHooks.Chat += OnChat;
            NetHooks.SendData += OnSendData;
            ServerHooks.Leave += OnLeave;
            NetHooks.GetData += OnGetData;
        }
        public override void DeInitialize()
        {
            GameHooks.Initialize -= OnInitialize;
            GameHooks.Update -= OnUpdate;
            ServerHooks.Chat -= OnChat;
            NetHooks.SendData -= OnSendData;
            ServerHooks.Leave -= OnLeave;
            NetHooks.GetData -= OnGetData;
        }
        public PluginTemplate(Main game)
            : base(game)
        {
            Order = -1;
        }

        public void OnInitialize()
        {
            bool morecommands = false;
            foreach (Group group in TShock.Groups.groups)
            {
                if (group.Name != "superadmin")
                {
                    if (group.HasPermission("ghostmode"))
                        morecommands = true;
                }
            }
            List<string> permlist = new List<string>();
            if (!morecommands)
                permlist.Add("ghostmode");
            TShock.Groups.AddPermissions("trustedadmin", permlist);
            for (int i = 0; i < 256; i++)
            {

                isGhost[i] = false;
                isHeal[i] = false;
                flyMode[i] = false;
                upPressed[i] = false;
                carpetPoints.Add(new List<PointF>());

            }
            Commands.ChatCommands.Add(new Command("ghostmode", Ghost, "ghost"));
            Commands.ChatCommands.Add(new Command("time",FreezeTime,"freezetime"));
            Commands.ChatCommands.Add(new Command("spawnmob", SpawnMobPlayer, "spawnmobplayer"));
            Commands.ChatCommands.Add(new Command("heal", AutoHeal, "autoheal"));
            Commands.ChatCommands.Add(new Command("fly", Fly, "fly"));
            Commands.ChatCommands.Add(new Command("flymisc", Fetch, "fetch"));
            Commands.ChatCommands.Add(new Command("flymisc", CarpetBody, "carpetbody"));
            Commands.ChatCommands.Add(new Command("flymisc", CarpetSides, "carpetsides"));
        }

        private DateTime LastCheck = DateTime.UtcNow;

        private void OnLeave(int ply)
        {
            isGhost[ply] = false;
            isHeal[ply] = false;
            flyMode[ply] = false;
            foreach (PointF entry in carpetPoints[ply])
            {

                Main.tile[(int)entry.X, (int)entry.Y].active = false;

            }
            carpetPoints[ply] = new List<PointF>();
        }

        void OnGetData(GetDataEventArgs e)
        {
            if (e.MsgID == PacketTypes.PlayerHp)
            {

                using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    var reader = new BinaryReader(data);
                    var playerID = reader.ReadByte();
                    var theHP = reader.ReadInt16();
                    var theMaxHP = reader.ReadInt16();
                    if (isHeal[playerID])
                    {

                        Item heart = Tools.GetItemById(58);
                        Item star = Tools.GetItemById(184);
                        if (theHP <= theMaxHP / 2)
                        {

                            for (int i = 0; i < 20; i++)
                                TShock.Players[playerID].GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                            for (int i = 0; i < 10; i++)
                                TShock.Players[playerID].GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                            TShock.Players[playerID].SendMessage("You just got healed!");
                        }

                    }

                }

            }
            if (e.MsgID == PacketTypes.PlayerMana)
            {

                using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                {
                    var reader = new BinaryReader(data);
                    var playerID = reader.ReadByte();
                    var theMana = reader.ReadInt16();
                    var theMaxMana = reader.ReadInt16();
                    if (isHeal[playerID])
                    {

                        Item heart = Tools.GetItemById(58);
                        Item star = Tools.GetItemById(184);
                        if (theMana <= theMaxMana / 2)
                        {

                            for (int i = 0; i < 20; i++)
                                TShock.Players[playerID].GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                            for (int i = 0; i < 10; i++)
                                TShock.Players[playerID].GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                            TShock.Players[playerID].SendMessage("You just got healed!");
                        }

                    }

                }

            }
        }

        public void OnSendData(SendDataEventArgs e)
        {
            try
            {
                List<int> ghostIDs = new List<int>();
                for (int i = 0; i < 256; i++)
                {

                    if (isGhost[i])
                    {

                        ghostIDs.Add(i);

                    }

                }
                switch (e.MsgID)
                {
                        
                    case PacketTypes.DoorUse:
                    case PacketTypes.EffectHeal:
                    case PacketTypes.EffectMana:
                    case PacketTypes.PlayerDamage:
                    case PacketTypes.Zones:
                    case PacketTypes.PlayerAnimation:
                    case PacketTypes.PlayerTeam:
                    case PacketTypes.PlayerSpawn:
                        if ((ghostIDs.Contains(e.number)) && (isGhost[e.number]))
                            e.Handled = true;
                        break;
                    case PacketTypes.ProjectileNew:
                    case PacketTypes.ProjectileDestroy:
                        if ((ghostIDs.Contains(e.ignoreClient)) && (isGhost[e.ignoreClient]))
                            e.Handled = true;
                        break;
                    default: break;

                }
                if ((e.number >= 0) && (e.number <= 255) && (isGhost[e.number]))
                {

                    if ((!cansend) && (e.MsgID == PacketTypes.PlayerUpdate))
                    {

                        e.Handled = true;

                    }
                }
            }
            catch (Exception) { }

        }

        public static void Fly(CommandArgs args)
        {

            flyMode[args.Player.Index] = !flyMode[args.Player.Index];
            carpetY[args.Player.Index] = args.Player.TileY;
            if (flyMode[args.Player.Index])
            {

                args.Player.SendMessage("Flying carpet activated.");

            }
            else
            {

                foreach (PointF entry in carpetPoints[args.Player.Index])
                {

                    Main.tile[(int)entry.X, (int)entry.Y].active = false;
                    TSPlayer.All.SendTileSquare((int)entry.X, (int)entry.Y, 1);
                    //carpetPoints.Remove(entry);

                }
                args.Player.SendMessage("Flying carpet deactivated.");

            }

        }

        public static void Fetch(CommandArgs args)
        {

            if (flyMode[args.Player.Index])
            {

                List<PointF> tilesToUpdate = new List<PointF>();
                foreach (PointF entry in carpetPoints[args.Player.Index])
                {

                    Main.tile[(int)entry.X, (int)entry.Y].active = false;
                    tilesToUpdate.Add(new PointF(entry.X, entry.Y));
                    carpetY[args.Player.Index] = args.Player.TileY;

                }
                foreach (PointF entry in tilesToUpdate)
                {

                    TSPlayer.All.SendTileSquare((int)entry.X, (int)entry.Y, 3);
                    carpetPoints[args.Player.Index].Remove(entry);

                }
                args.Player.SendMessage("Carpet Fetched.");

            }
            else
            {

                args.Player.SendMessage("You have no flying carpet activated.", System.Drawing.Color.Red);

            }

        }

        public static void CarpetBody(CommandArgs args)
        {



        }

        public static void CarpetSides(CommandArgs args)
        {



        }

        public static void AutoHeal(CommandArgs args) 
        {

            isHeal[args.Player.Index] = !isHeal[args.Player.Index];
            if (isHeal[args.Player.Index]) {

                args.Player.SendMessage("Auto Heal Mode is now on.");

            } else {
                
                args.Player.SendMessage("Auto Heal Mode is now off.");

            }

        }

        public static void Ghost(CommandArgs args)
        {

            int tempTeam = args.Player.TPlayer.team;
            args.Player.TPlayer.team = 0;
            NetMessage.SendData(45, -1, -1, "", args.Player.Index);
            args.Player.TPlayer.team = tempTeam;
            if (!isGhost[args.Player.Index])
            {

                args.Player.SendMessage("Ghost Mode activated!");

            }
            else
            {

                args.Player.SendMessage("Ghost Mode deactivated!");

            }
            isGhost[args.Player.Index] = !isGhost[args.Player.Index];
            args.Player.TPlayer.position.X = 0;
            args.Player.TPlayer.position.Y = 0;
            cansend = true;
            NetMessage.SendData(13, -1, -1, "", args.Player.Index);
            cansend = false;

        }

        private void OnUpdate()
        {

            if ((DateTime.UtcNow - LastCheck).TotalSeconds >= 1)
            {
                LastCheck = DateTime.UtcNow;
                if (timeFrozen)
                {

                    if (Main.dayTime != freezeDayTime)
                    {

                        if (timeToFreezeAt > 10000)
                        {

                            timeToFreezeAt -= 100;

                        }
                        else
                        {

                            timeToFreezeAt += 100;

                        }

                    }
                    TSPlayer.Server.SetTime(freezeDayTime, timeToFreezeAt);

                }
            }
            for (int i = 0; i < 256; i++)
            {

                if (flyMode[i])
                {

                    try
                    {

                        List<PointF> tilesToUpdate = new List<PointF>();
                        if ((TShock.Players[i].TileY < carpetY[i] - 9) || ((TShock.Players[i].TileY > carpetY[i]) && (TShock.Players[i].TPlayer.velocity.Y == 0)))
                        {

                            foreach (PointF entry in carpetPoints[i])
                            {

                                Main.tile[(int)entry.X, (int)entry.Y].active = false;
                                tilesToUpdate.Add(new PointF(entry.X, entry.Y));
                                carpetY[i] = TShock.Players[i].TileY + 3;

                            }

                        }
                        foreach (PointF entry in carpetPoints[i])
                        {

                            if ((Main.tile[(int)entry.X, (int)entry.Y].type == 54) || (Main.tile[(int)entry.X, (int)entry.Y].type == 30))
                            {
                                if ((entry.Y < TShock.Players[i].TileY + 3) || (entry.Y != carpetY[i] + 3) || (Math.Abs(entry.X - TShock.Players[i].TileX) > 5))
                                {

                                    if ((entry.Y != carpetY[i] + 2) || (Math.Abs(entry.X - TShock.Players[i].TileX) > 6) || (Math.Abs(entry.X - TShock.Players[i].TileX) < 6))
                                    {
                                        Main.tile[(int)entry.X, (int)entry.Y].active = false;
                                        tilesToUpdate.Add(new PointF(entry.X, entry.Y));
                                    }

                                }
                            }
                            else if ((entry.Y == TShock.Players[i].TileY + 3) && (TShock.Players[i].TPlayer.velocity.Y == 0))
                            {

                                carpetY[i] = TShock.Players[i].TileY;
                                Main.tile[(int)entry.X, (int)entry.Y].type = 54;
                                tilesToUpdate.Add(new PointF(entry.X, entry.Y));

                            }
                            else if ((entry.X < TShock.Players[i].TileX - 1) || (entry.X > TShock.Players[i].TileX + 2) || (entry.Y != carpetY[i] - 1))
                            {

                                Main.tile[(int)entry.X, (int)entry.Y].active = false;
                                tilesToUpdate.Add(new PointF(entry.X, entry.Y));

                            }

                        }
                        if (TShock.Players[i].TileY >= carpetY[i])
                        {
                            if (TShock.Players[i].TPlayer.controlDown)
                            {

                                carpetY[i] += 4;

                            }
                        }
                        for (int j = -5; j <= 5; j++)
                        {

                            if (!Main.tile[TShock.Players[i].TileX + j, carpetY[i] + 3].active)
                            {

                                Main.tile[TShock.Players[i].TileX + j, carpetY[i] + 3].type = 54;
                                Main.tile[TShock.Players[i].TileX + j, carpetY[i] + 3].active = true;
                                tilesToUpdate.Add(new PointF(TShock.Players[i].TileX + j, carpetY[i] + 3));
                                carpetPoints[i].Add(new PointF(TShock.Players[i].TileX + j, carpetY[i] + 3));

                            }

                        }
                        if (!Main.tile[TShock.Players[i].TileX + 6, carpetY[i] + 2].active)
                        {

                            Main.tile[TShock.Players[i].TileX + 6, carpetY[i] + 2].type = 30;
                            Main.tile[TShock.Players[i].TileX + 6, carpetY[i] + 2].active = true;
                            tilesToUpdate.Add(new PointF(TShock.Players[i].TileX + 6, carpetY[i] + 2));
                            carpetPoints[i].Add(new PointF(TShock.Players[i].TileX + 6, carpetY[i] + 2));

                        }
                        if (!Main.tile[TShock.Players[i].TileX - 6, carpetY[i] + 2].active)
                        {

                            Main.tile[TShock.Players[i].TileX - 6, carpetY[i] + 2].type = 30;
                            Main.tile[TShock.Players[i].TileX - 6, carpetY[i] + 2].active = true;
                            tilesToUpdate.Add(new PointF(TShock.Players[i].TileX - 6, carpetY[i] + 2));
                            carpetPoints[i].Add(new PointF(TShock.Players[i].TileX - 6, carpetY[i] + 2));

                        }
                        for (int j = -1; j <= 2; j++)
                        {

                            if (!Main.tile[TShock.Players[i].TileX + j, carpetY[i] - 1].active)
                            {

                                Main.tile[TShock.Players[i].TileX + j, carpetY[i] - 1].type = 19;
                                Main.tile[TShock.Players[i].TileX + j, carpetY[i] - 1].active = true;
                                tilesToUpdate.Add(new PointF(TShock.Players[i].TileX + j, carpetY[i] - 1));
                                carpetPoints[i].Add(new PointF(TShock.Players[i].TileX + j, carpetY[i] - 1));

                            }

                        }
                        foreach (PointF entry in tilesToUpdate)
                        {

                            TSPlayer.All.SendTileSquare((int)entry.X, (int)entry.Y, 3);
                            if (!Main.tile[(int)entry.X, (int)entry.Y].active)
                                carpetPoints[i].Remove(entry);

                        }

                    }
                    catch (Exception) {  }

                }

            }

        }

        public void FreezeTime(CommandArgs args)
        {

            timeFrozen = !timeFrozen;
            freezeDayTime = Main.dayTime;
            timeToFreezeAt = Main.time;
            if (timeFrozen)
            {

                Tools.Broadcast(args.Player.Name.ToString() + " froze time.");

            }
            else
            {

                Tools.Broadcast(args.Player.Name.ToString() + " unfroze time.");

            }

        }

        public void OnChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
        {
            
        }

        public static void SpawnMobPlayer(CommandArgs args)
        {
            if (args.Parameters.Count < 1 || args.Parameters.Count > 3)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount] [username]", System.Drawing.Color.Red);
                return;
            }
            if (args.Parameters[0].Length == 0)
            {
                args.Player.SendMessage("Missing mob name/id", System.Drawing.Color.Red);
                return;
            }
            int amount = 1;
            if (args.Parameters.Count == 3 && !int.TryParse(args.Parameters[1], out amount))
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /spawnmob <mob name/id> [amount] [username]", System.Drawing.Color.Red);
                return;
            }

            amount = Math.Min(amount, Main.maxNPCs);

            var npcs = Tools.GetNPCByIdOrName(args.Parameters[0]);
            var players = Tools.FindPlayer(args.Parameters[2]);
            if (players.Count == 0)
            {
                args.Player.SendMessage("Invalid player!", System.Drawing.Color.Red);
            }
            else if (players.Count > 1)
            {
                args.Player.SendMessage("More than one player matched!", System.Drawing.Color.Red);
            }
            else if (npcs.Count == 0)
            {
                args.Player.SendMessage("Invalid mob type!", System.Drawing.Color.Red);
            }
            else if (npcs.Count > 1)
            {
                args.Player.SendMessage(string.Format("More than one ({0}) mob matched!", npcs.Count), System.Drawing.Color.Red);
            }
            else
            {
                var npc = npcs[0];
                if (npc.type >= 1 && npc.type < Main.maxNPCTypes)
                {
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, players[0].TileX, players[0].TileY, 50, 20);
                    Tools.Broadcast(string.Format("{0} was spawned {1} time(s) by {2}.", npc.name, amount, players[0].Name));
                }
                else
                    args.Player.SendMessage("Invalid mob type!", System.Drawing.Color.Red);
            }
        }
    }
}
﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using TerRoguelike.NPCs;
using TerRoguelike.Projectiles;
using TerRoguelike.Systems;
using TerRoguelike.TerPlayer;
using TerRoguelike.World;
using static TerRoguelike.Utilities.TerRoguelikeUtils;
using static TerRoguelike.Systems.MusicSystem;
using static TerRoguelike.Managers.SpawnManager;
using static TerRoguelike.Schematics.SchematicManager;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.GameContent;
using Terraria.UI.Chat;
using Terraria.ID;
using TerRoguelike.Schematics;
using rail;
using Terraria.ModLoader.Core;
using System.Collections.Generic;
using TerRoguelike.Rooms;
using TerRoguelike.Floors;
using TerRoguelike.MainMenu;
using TerRoguelike.Packets;
using static TerRoguelike.Packets.TeleportToPositionPacket;
using static TerRoguelike.Packets.EscapePacket;

namespace TerRoguelike.Managers
{
    public class Room
    {
        public static int forceLoopCalculation = -1; //if greater than -1, elite spawning logic is done with this interger instead of TerRoguelikeWorld.currentLoop
        private static Texture2D wallTex = null;

        // base room class used by all rooms
        public virtual string Key => null; //schematic key
        public virtual string Filename => null; //schematic filename
        public int ID = -1; //ID in RoomID list
        public virtual int AssociatedFloor => -1; // what floor this room is associated with
        public virtual bool CanExitRight => false; // if room is capable of exiting right
        public virtual bool CanExitDown => false; // if room is capable of exiting down
        public virtual bool CanExitUp => false; // if room is capable of exiting up
        public virtual bool IsBossRoom => false; //if room is the end to a floor
        public virtual bool IsStartRoom => false; // if room is the start of a floor
        public virtual bool IsPillarRoom => false; // if room uses the pillar clearing mechanic (lunar floor)
        public virtual bool IsRoomVariant => false; // if room uses the schematic of another room
        public virtual bool HasTransition => false; // if room is preceeded by a transition room
        public virtual int TransitionDirection => -1; // -1 if not a transition room. 0: right, 1: Down, 2: Up
        public virtual bool ActivateNewFloorEffects => true;
        public virtual bool IsSanctuary => false;
        public virtual int[] CantExitInto => []; // for exempting very specific rooms from being chosen as the connector to this one to avoid softlocks.
        public int myRoom; // index in RoomList
        public bool initialized = false; // whether initialize has run yet
        public bool escapeInitialized = false; // whether initialize has been run yet in the escape sequence
        public bool awake = false; // whether a player has ever stepped into this room
        public bool active = true; // whether the room has been completed or not
        public bool haltSpawns = false; // if true, prevents any more enemies from spawning in that room
        public Vector2 RoomDimensions; // dimensions of the room
        public int roomTime; // time the room has been active
        public int closedTime; // time the room has been completed
        public int waveStartTime;
        public int waveCount;
        public int currentWave;
        public int waveClearGraceTime;
        public const int RoomSpawnCap = 200;
        public virtual Vector2 bossSpawnPos => Vector2.Zero;
        public Vector2 RoomPosition; //position of the room
        public bool bossDead;
        public bool entered = false;
        public virtual Point WallInflateModifier => new Point(0, 0); // amount to expand a room's wall collision by, in tiles. can shrink with negative.
        public virtual bool AllowWallDrawing => true;
        public Rectangle GetRect()
        {
            Vector2 pos = RoomPosition16;
            Vector2 dimensions = RoomDimensions16;
            return new Rectangle((int)pos.X, (int)pos.Y, (int)dimensions.X, (int)dimensions.Y);
        }
        public Vector2 RoomPosition16 { get { return RoomPosition * 16f; } }
        public Vector2 RoomCenter16 { get { return RoomDimensions * 8f; } }

        public Vector2 RoomDimensions16 { get { return RoomDimensions * 16f; } }
        public Vector2 TopLeft { get { return Vector2.Zero; } }
        public Vector2 Top { get { return new Vector2((int)(RoomDimensions.X * 0.5f), 0); } }
        public Vector2 TopRight { get { return new Vector2(RoomDimensions.X - 1, 0); } }
        public Vector2 Left { get { return new Vector2(0, (int)(RoomDimensions.Y * 0.5f)); } }
        public Vector2 Center { get { return new Vector2((int)(RoomDimensions.X * 0.5f), (int)(RoomDimensions.Y * 0.5f)); } }
        public Vector2 Right { get { return new Vector2(RoomDimensions.X - 1, (int)(RoomDimensions.Y * 0.5f)); } }
        public Vector2 BottomLeft { get { return new Vector2(0, RoomDimensions.Y - 1); } }
        public Vector2 Bottom { get { return new Vector2((int)(RoomDimensions.X * 0.5f), RoomDimensions.Y - 1); } }
        public Vector2 BottomRight { get { return new Vector2(RoomDimensions.X - 1, RoomDimensions.Y - 1); } }
        public Vector2 PercentPosition(float x, float y) => new Vector2(RoomDimensions.X * 16f * x, RoomDimensions.Y * 16f * y);
        public Vector2 MakeEnemySpawnPos(Vector2 anchor, int addTileX, int addTileY, float autoAddX = 8f, float autoAddY = 8f)
        {
            return ((anchor + new Vector2(addTileX, addTileY)) * 16f) + new Vector2(autoAddX, autoAddY);
        }

        public virtual bool AllowSettingPlayerCurrentRoom => active;

        //potential NPC variables
        public Vector2[] NPCSpawnPosition = new Vector2[RoomSpawnCap];
        public int[] NPCToSpawn = new int[RoomSpawnCap];
        public int[] TimeUntilSpawn = new int[RoomSpawnCap];
        public int[] TelegraphDuration = new int[RoomSpawnCap];
        public float[] TelegraphSize = new float[RoomSpawnCap];
        public bool[] NotSpawned = new bool[RoomSpawnCap];
        public int[] AssociatedWave = new int[RoomSpawnCap];

        public bool anyAlive = true; // whether any associated npcs are alive
        public int roomClearGraceTime = -1; // time gap of 1 seconds after the last enemy has spawned where a room cannot be considered cleared to prevent any accidents happening
        public int lastTelegraphDuration; // used for roomClearGraceTime
        public bool wallActive = false; // whether the barriers of the room are active
        public virtual void AddRoomNPC(Vector2 npcSpawnPosition, int npcToSpawn, int timeUntilSpawn, int telegraphDuration, float telegraphSize = 0, int wave = 0)
        {
            if (TerRoguelikeWorld.currentLoop > 0 && FloorID[AssociatedFloor].Stage != 5 && Main.rand.NextFloat() < 0.02f)
            {
                List<int> lunarSpawnSelect = [];
                lunarSpawnSelect.AddRange(new LunarPillarRoomBottomLeft().SpawnSelection);
                lunarSpawnSelect.AddRange(new LunarPillarRoomBottomRight().SpawnSelection);
                lunarSpawnSelect.AddRange(new LunarPillarRoomTopLeft().SpawnSelection);
                lunarSpawnSelect.AddRange(new LunarPillarRoomTopRight().SpawnSelection);
                npcToSpawn = lunarSpawnSelect[Main.rand.Next(lunarSpawnSelect.Count)];
            }
            for (int i = 0; i < RoomSpawnCap; i++)
            {
                if (!NotSpawned[i])
                {
                    NPCSpawnPosition[i] = npcSpawnPosition + (RoomPosition * 16f);
                    NPCToSpawn[i] = npcToSpawn;
                    TimeUntilSpawn[i] = timeUntilSpawn;
                    TelegraphDuration[i] = telegraphDuration;
                    if (telegraphSize == 0)
                        telegraphSize = 1f;
                    TelegraphSize[i] = telegraphSize;
                    NotSpawned[i] = true;
                    AssociatedWave[i] = wave;
                    if (wave > waveCount)
                        waveCount = wave;
                    break;
                }
            }
            
        }
        public virtual void AddBoss(Vector2 npcSpawnPosition, int npcToSpawn)
        {
            if (TerRoguelike.mpClient)
                return;

            SpawnNPCTerRoguelike(NPC.GetSource_NaturalSpawn(), npcSpawnPosition + RoomPosition16, npcToSpawn, myRoom);
        }
        public virtual void Update()
        {
            if (!StartCondition()) // not been touched yet? return
                return;

            if (!initialized && !(TerRoguelikeWorld.escaped && FloorID[AssociatedFloor].Stage != -1)) // initialize the room
                InitializeRoom();

            if (closedTime <= 60 && (TerRoguelikeWorld.escapeTime > TerRoguelikeWorld.escapeTimeSet - 180 || !TerRoguelikeWorld.escape || (TerRoguelikeWorld.escape && IsBossRoom && FloorID[AssociatedFloor].jstcProgress >= Floor.JstcProgress.Boss))) //wall is visually active up to 1 second after room clear
                wallActive = true;

            if (TerRoguelikeWorld.escaped && FloorID[AssociatedFloor].Stage != -1)
                wallActive = false;

            if (!active) // room done, closed time increments
            {
                closedTime++;
                return;
            }
            WallUpdate(); // update wall logic
            PlayerItemsUpdate(); // update items from all players

            roomTime++; //time room is active
            if (TerRoguelike.mpClient && !TerRoguelikeWorld.escape && roomTime % 180 == 179)
            {
                RoomPacket.Send(ID);
            }

            if (!haltSpawns)
            {
                for (int i = 0; i < RoomSpawnCap; i++)
                {
                    if (AssociatedWave[i] > currentWave || !NotSpawned[i])
                        continue;

                    if (TimeUntilSpawn[i] - roomTime + waveStartTime <= 0) //spawn pending enemy that has reached it's time
                    {
                        var eliteVars = new TerRoguelikeGlobalNPC.EliteVars();
                        int currentLoop = forceLoopCalculation >= 0 ? forceLoopCalculation : TerRoguelikeWorld.currentLoop;
                        if (currentLoop > 0)
                        {
                            EliteCredits += 0.34f + 0.66f * currentLoop;
                            float eliteRoll = (float)Math.Pow(EliteCredits, 2) / (EliteCredits + 15);
                            if (Main.rand.NextFloat(15) < eliteRoll)
                            {
                                EliteCredits = 0;
                                switch (Main.rand.Next(3))
                                {
                                    default:
                                    case 0:
                                        eliteVars.tainted = true;
                                        break;
                                    case 1:
                                        eliteVars.slugged = true;
                                        break;
                                    case 2:
                                        eliteVars.burdened = true;
                                        break;
                                }
                            }
                        }

                        SpawnEnemy(NPCToSpawn[i], NPCSpawnPosition[i], myRoom, TelegraphDuration[i], TelegraphSize[i], eliteVars);

                        lastTelegraphDuration = TelegraphDuration[i];
                        waveClearGraceTime = roomTime;
                        roomClearGraceTime = -1;
                        NotSpawned[i] = false;
                    }
                }
            }
            
            // if there is still an enemy yet to be spawned, do not continue with room clear logic
            bool cancontinue = roomTime - waveClearGraceTime > 60;
            bool encourageNextWave = false;
            if (!haltSpawns)
            {
                if (currentWave < waveCount && roomTime - waveClearGraceTime > lastTelegraphDuration + 60)
                {
                    encourageNextWave = !TerRoguelikeWorld.escape;

                    if (encourageNextWave)
                    {
                        for (int j = 0; j < RoomSpawnCap; j++)
                        {
                            if (NotSpawned[j] == true && AssociatedWave[j] <= currentWave)
                            {
                                encourageNextWave = false;
                                break;
                            }
                        }
                    }
                    if (encourageNextWave)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC npc = Main.npc[i];
                            if (npc == null)
                                continue;
                            if (!npc.active)
                                continue;

                            TerRoguelikeGlobalNPC modNPC = npc.ModNPC();

                            if (!modNPC.isRoomNPC)
                                continue;

                            if (modNPC.sourceRoomListID == myRoom && !modNPC.hostileTurnedAlly)
                            {
                                encourageNextWave = false;
                                break;
                            }
                        }
                    }
                }
                for (int i = 0; i < RoomSpawnCap; i++)
                {
                    if (TerRoguelikeWorld.escape && AssociatedWave[i] != 0)
                        continue;

                    if (NotSpawned[i] == true || currentWave < waveCount)
                    {
                        cancontinue = false;
                        break;
                    }
                }
            }

            if (encourageNextWave)
            {
                currentWave++;
                waveStartTime = roomTime;
                waveClearGraceTime = roomTime;
                if (Main.dedServ)
                    RoomPacket.Send(ID);
            }
            if (cancontinue && !TerRoguelike.mpClient)
            {
                // start checking if any npcs in the world are active and associated with this room
                if (roomClearGraceTime == -1)
                {
                    roomClearGraceTime += lastTelegraphDuration + 60;
                }
                if (roomClearGraceTime > 0)
                    roomClearGraceTime--;

                anyAlive = false;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc == null)
                        continue;
                    if (!npc.active)
                        continue;

                    TerRoguelikeGlobalNPC modNPC = npc.ModNPC();

                    if (!modNPC.isRoomNPC || modNPC.ignoreForRoomClearing)
                        continue;

                    if (modNPC.sourceRoomListID == myRoom && !modNPC.hostileTurnedAlly)
                    {
                        anyAlive = true;
                        break;
                    }
                }
            }
            if (ClearCondition()) // all associated enemies are gone. room cleared.
            {
                active = false;
                RoomClearReward();
                if (Main.dedServ)
                    RoomPacket.Send(ID);
            }
        }
        public virtual void InitializeRoom()
        {
            initialized = true;
            //sanity check for all npc slots
            for (int i = 0; i < NotSpawned.Length; i++)
            {
                NotSpawned[i] = false;
            }
        }
        public void WallUpdate()
        {
            if (!wallActive)
                return;

            if (roomTime == 0 && Main.netMode != NetmodeID.SinglePlayer)
            {
                Rectangle roomRect = GetRect();
                roomRect.Inflate(WallInflateModifier.X * 16, WallInflateModifier.Y * 16);
                int teleportTarget = -1;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (roomRect.Contains(Main.player[i].getRect()))
                    {
                        teleportTarget = i;
                        break;
                    }
                }
                if (teleportTarget >= 0 && !RoomSystem.activatedTeleport)
                {
                    RoomSystem.activatedTeleportCooldown = 180;
                    TeleportToPositionPacket.Send(Main.player[teleportTarget].Center, TeleportContext.Room, ID, -1, -1);
                    foreach (Player player in Main.ActivePlayers)
                    {
                        player.Center = Main.player[teleportTarget].Center;
                    }
                }
                    
            }
            Vector2 playerCollisionShrink = WallInflateModifier.ToVector2() * 16;
            for (int playerID = 0; playerID < Main.maxPlayers; playerID++) // keep players in the fucking room
            {
                var player = Main.player[playerID];
                
                bool boundLeft = !player.pulley && (player.position.X + player.velocity.X + playerCollisionShrink.X) < (RoomPosition.X + 1f) * 16f;
                bool boundRight = !player.pulley && (player.position.X + (float)player.width + player.velocity.X - playerCollisionShrink.X) > (RoomPosition.X - 1f + RoomDimensions.X) * 16f;
                bool boundTop = (player.position.Y + player.velocity.Y + playerCollisionShrink.Y) < (RoomPosition.Y + 1f) * 16f;
                bool boundBottom = (player.position.Y + (float)player.height + player.velocity.Y - playerCollisionShrink.Y) > (RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f;
                bool boundProc = false;
                Vector2 playerPreBoundPosition = player.position;
                if (boundLeft)
                {
                    player.position.X = (RoomPosition.X + 1f) * 16f - playerCollisionShrink.X;
                    player.velocity.X = 0;
                    boundProc = true;
                }
                if (boundRight)
                {
                    player.position.X = ((RoomPosition.X - 1f + RoomDimensions.X) * 16f + playerCollisionShrink.X) - (float)player.width;
                    player.velocity.X = 0;
                    boundProc = true;
                }
                if (boundTop)
                {
                    player.position.Y = (RoomPosition.Y + 1f) * 16f - playerCollisionShrink.Y;
                    player.velocity.Y = 0.01f;
                    player.jump = 0;
                    boundProc = true;
                }
                if (boundBottom)
                {
                    player.position.Y = ((RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f + playerCollisionShrink.Y) - (float)player.height;
                    player.velocity.Y = 0;
                    boundProc = true;
                }
                if (boundProc)
                {
                    player.HeldItem.position += player.position - playerPreBoundPosition;
                }
            }
            for (int npcID = 0; npcID < Main.maxNPCs; npcID++) // keep npcs in the fucking room
            {
                var npc = Main.npc[npcID];
                if (npc == null)
                    continue;
                if (!npc.active)
                    continue;
                TerRoguelikeGlobalNPC modNPC = npc.ModNPC();

                if (!modNPC.isRoomNPC)
                    continue;
                if (modNPC.sourceRoomListID != myRoom)
                    continue;
                if (modNPC.IgnoreRoomWallCollision)
                    continue;

                Vector2 shrink = modNPC.RoomWallCollisionShrink + WallInflateModifier.ToVector2() * 16;

                bool boundLeft = (npc.position.X + npc.velocity.X + shrink.X) < (RoomPosition.X + 1f) * 16f;
                bool boundRight = (npc.position.X + (float)npc.width + npc.velocity.X - shrink.X) > (RoomPosition.X - 1f + RoomDimensions.X) * 16f;
                bool boundTop = (npc.position.Y + npc.velocity.Y + shrink.Y) < (RoomPosition.Y + 1f) * 16f;
                bool boundBottom = (npc.position.Y + (float)npc.height + npc.velocity.Y - shrink.Y) > (RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f;
                if (boundLeft)
                {
                    npc.position.X = (RoomPosition.X + 1f) * 16f - shrink.X;
                    if (npc.velocity.X < 0)
                        npc.velocity.X = 0;
                    npc.collideX = true;
                }
                if (boundRight)
                {
                    npc.position.X = ((RoomPosition.X - 1f + RoomDimensions.X) * 16f + shrink.X) - (float)npc.width;
                    if (npc.velocity.X > 0)
                        npc.velocity.X = 0;
                    npc.collideX = true;
                }
                if (boundTop)
                {
                    npc.position.Y = (RoomPosition.Y + 1f) * 16f - shrink.Y;
                    if (npc.velocity.Y < 0)
                        npc.velocity.Y = 0;
                    npc.collideY = true;
                }
                if (boundBottom)
                {
                    npc.position.Y = ((RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f + shrink.Y) - (float)npc.height;
                    if (npc.velocity.Y > 0)
                        npc.velocity.Y = 0;
                    npc.collideY = true;
                }
            }
        }
        public Rectangle CheckRectWithWallCollision(Rectangle rect)
        {
            bool boundLeft = (rect.X) < (RoomPosition.X + 1f) * 16f;
            bool boundRight = (rect.X + rect.Width) > (RoomPosition.X - 1f + RoomDimensions.X) * 16f;
            bool boundTop = (rect.Y) < (RoomPosition.Y + 1f) * 16f;
            bool boundBottom = (rect.Y + rect.Height) > (RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f;
            if (boundLeft)
            {
                rect.X = (int)((RoomPosition.X + 1f) * 16f);
            }
            if (boundRight)
            {
                rect.X = (int)((RoomPosition.X - 1f + RoomDimensions.X) * 16f) - rect.Width;
            }
            if (boundTop)
            {
                rect.Y = (int)((RoomPosition.Y + 1f) * 16f);
            }
            if (boundBottom)
            {
                rect.Y = (int)((RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f) - rect.Height;
            }

            return rect;
        }
        public virtual void RoomClearReward()
        {
            if (TerRoguelikeWorld.escape || TerRoguelikeWorld.escaped)
                return;

            ClearGhosts();
            ClearSpecificProjectiles();

            void RewardItemLogic(int owner = -1)
            {
                // reward. boss rooms give higher tiers.
                int chance = Main.rand.Next(1, 101);
                int itemType;
                int itemTier;

                if (IsBossRoom)
                {
                    if (chance <= 80)
                    {
                        itemType = ItemManager.GiveUncommon(false);
                        itemTier = 1;
                    }
                    else
                    {
                        itemType = ItemManager.GiveRare(false);
                        itemTier = 2;
                    }
                    SpawnManager.SpawnItem(itemType, FindAirNearRoomCenter(), itemTier, 75, 0.5f, owner);
                    return;
                }

                if (ItemManager.RoomRewardCooldown > 0)
                {
                    ItemManager.RoomRewardCooldown--;
                    return;
                }

                if (chance <= 80)
                {
                    itemType = ItemManager.GiveCommon();
                    itemTier = 0;
                }
                else if (chance <= 98)
                {
                    itemType = ItemManager.GiveUncommon();
                    itemTier = 1;
                }
                else
                {
                    itemType = ItemManager.GiveRare();
                    itemTier = 2;
                }
                SpawnManager.SpawnItem(itemType, FindAirNearRoomCenter(), itemTier, 75, 0.5f, owner);
            }

            int itemCount = RoomSystem.playerCount;
            int rewardedCount = 0;
            foreach (Player player in Main.ActivePlayers)
            {
                if (rewardedCount >= itemCount)
                    break;

                int who = player.whoAmI;
                var modPlayer = player.ModPlayer();
                if (modPlayer != null && !modPlayer.allowedToExist)
                    continue;

                RewardItemLogic(who);
                
                rewardedCount++;
            }
            for (int i = rewardedCount; i < itemCount; i++)
            {
                RewardItemLogic();
            }
        }
        public virtual bool CanDescend(Player player, TerRoguelikePlayer modPlayer)
        {
            return closedTime > 180 && IsBossRoom && player.position.X + player.width >= ((RoomPosition.X + RoomDimensions.X) * 16f) - 22f && !player.dead && !TerRoguelikeWorld.escape;
        }
        public virtual bool CanAscend(Player player, TerRoguelikePlayer modPlayer)
        {
            return IsStartRoom && player.position.X <= (RoomPosition.X * 16f) + 22f && !player.dead && TerRoguelikeWorld.escape;
        }
        public void Descend(Player player)
        {
            var modPlayer = player.ModPlayer();
            int nextStage = !IsSanctuary ? modPlayer.currentFloor.Stage + 1 : TerRoguelikeWorld.currentStage;
            if (nextStage >= RoomManager.FloorIDsInPlay.Count) // if FloorIDsInPlay overflows, send back to the start
            {
                nextStage = 0;
                TerRoguelikeWorld.currentStage = 0;
            }
            else
            {
                if (nextStage > TerRoguelikeWorld.currentStage)
                {
                    TerRoguelikeWorld.currentStage = nextStage;
                    StageCountPacket.Send();
                }
            }

            Floor nextFloor;
            Room targetRoom;
            if (!IsSanctuary)
            {
                nextFloor = FloorID[RoomManager.FloorIDsInPlay[nextStage]];
                targetRoom = RoomID[nextFloor.StartRoomID];
                foreach (Player p in Main.ActivePlayers)
                {
                    var modp = p.ModPlayer();
                    if (modp == null)
                        continue;
                    modp.cacheRoomListWarp = targetRoom.myRoom;
                }
                modPlayer.cacheRoomListWarp = targetRoom.myRoom;
            }
            else
            {
                targetRoom = RoomSystem.RoomList[modPlayer.cacheRoomListWarp];
                nextFloor = FloorID[targetRoom.AssociatedFloor];
            }
            

            if (!IsSanctuary && TerRoguelikeWorld.TryWarpToSanctuary())
            {
                modPlayer.currentFloor = FloorID[FloorDict["Sanctuary"]];
                targetRoom = RoomID[modPlayer.currentFloor.StartRoomID];
                nextFloor = modPlayer.currentFloor;
                if (TerRoguelikeWorld.totalLunarCharm > 0)
                {
                    foreach (Player p in Main.ActivePlayers)
                    {
                        var modp = p.ModPlayer();
                        if (modp == null) continue;
                        if (modp.lunarCharm > 0)
                            modp.LunarCharmLogic(targetRoom.RoomPosition16 + targetRoom.RoomCenter16);
                    }
                }
            }
            else
            {
                modPlayer.currentFloor = nextFloor;
            }
            player.Center = targetRoom.DescendTeleportPosition();
            if (!IsSanctuary)
            {
                TerRoguelikePlayer.HealthUpIndicator(player);
            }
            if (targetRoom.IsSanctuary)
            {
                if (Main.dedServ)
                    TeleportToPositionPacket.Send(player.Center, TeleportContext.Sanctuary, targetRoom.ID);
            }
            else
            {
                if (Main.dedServ)
                {
                    TeleportToPositionPacket.Send(player.Center, TeleportContext.NewFloor, targetRoom.ID);
                    foreach (Player p in Main.ActivePlayers)
                    {
                        var modp = p.ModPlayer();
                        if (modp == null) continue;
                        RoomSystem.NewFloorEffects(targetRoom, modp);
                    }
                }
                    
            }
            if (nextFloor.Name != "Lunar")
            {
                SetCalm(nextFloor.Soundtrack.CalmTrack);
                SetCombat(nextFloor.Soundtrack.CombatTrack);
                SetMusicMode(nextFloor.Name == "Sanctuary" ? MusicStyle.AllCalm : MusicStyle.Dynamic);
                CombatVolumeInterpolant = 0;
                CalmVolumeInterpolant = 0;
                CalmVolumeLevel = nextFloor.Soundtrack.Volume;
                CombatVolumeLevel = nextFloor.Soundtrack.Volume;
            }

            if (!Main.dedServ)
                RoomSystem.NewFloorEffects(targetRoom, modPlayer);
        }
        public virtual void Ascend(Player player)
        {
            var modPlayer = player.ModPlayer();
            int nextStage = modPlayer.currentFloor.Stage - 1;
            if (nextStage < 0) // if FloorIDsInPlay underflows, send back to the start
            {
                nextStage = 0;
                TerRoguelikeWorld.escape = false;
            }

            var nextFloor = FloorID[RoomManager.FloorIDsInPlay[nextStage]];
            Room potentialRoom = null;
            for (int j = 0; j < nextFloor.BossRoomIDs.Count; j++)
            {
                potentialRoom = RoomSystem.RoomList.Find(x => x.ID == nextFloor.BossRoomIDs[j]);
                if (potentialRoom != null)
                    break;
            }
            var targetRoom = potentialRoom;
            if (TerRoguelikeWorld.escape)
            {
                if (myRoom >= 2)
                {
                    Room jumpstartRoom = RoomSystem.RoomList[myRoom - 2];
                    if (!jumpstartRoom.initialized)
                    {
                        jumpstartRoom.awake = true;
                        jumpstartRoom.InitializeRoom();
                    }
                }

                player.Center = (targetRoom.RoomPosition + (targetRoom.RoomDimensions / 2f)) * 16f;
                player.BottomRight = modPlayer.FindAirToPlayer((targetRoom.RoomPosition + targetRoom.RoomDimensions) * 16f);
                modPlayer.currentFloor = nextFloor;

                if (Main.dedServ)
                    TeleportToPositionPacket.Send(player.Center, TeleportContext.NewFloor, targetRoom.ID);

                modPlayer.escapeArrowTime = 300;
                var newFloorStartRoom = RoomSystem.RoomList.Find(x => x.ID == nextFloor.StartRoomID);
                modPlayer.escapeArrowTarget = newFloorStartRoom.RoomPosition16 + Vector2.UnitY * newFloorStartRoom.RoomDimensions.Y * 8f;

                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (npc == null)
                        continue;
                    if (!npc.active)
                        continue;
                    if (npc.life <= 0)
                        continue;

                    TerRoguelikeGlobalNPC modNPC = npc.ModNPC();
                    if (!modNPC.isRoomNPC)
                        continue;
                    if (modNPC.sourceRoomListID < 0)
                        continue;

                    if (modNPC.sourceRoomListID > targetRoom.myRoom)
                        npc.active = false;
                }
                for (int t = targetRoom.myRoom + 1; t < RoomSystem.RoomList.Count; t++)
                {
                    Room roomToClear = RoomSystem.RoomList[t];
                    if (!roomToClear.active)
                        continue;

                    for (int p = 0; p < roomToClear.NotSpawned.Length; p++)
                    {
                        roomToClear.NotSpawned[p] = false;
                    }
                }
                for (int s = 0; s < SpawnManager.pendingEnemies.Count; s++)
                {
                    var pendingEnemy = SpawnManager.pendingEnemies[s];
                    if (pendingEnemy.RoomListID > targetRoom.myRoom)
                    {
                        pendingEnemy.spent = true;
                    }
                }

                foreach (Player p in Main.ActivePlayers)
                {
                    var modp = p.ModPlayer();
                    if (modp == null) continue;
                    RoomSystem.NewFloorEffects(targetRoom, modp);
                }
            }
            else
            {
                targetRoom = RoomID[FloorID[FloorDict["Sanctuary"]].StartRoomID];
                if (TerRoguelikeWorld.totalLunarCharm > 0)
                {
                    foreach (Player p in Main.ActivePlayers)
                    {
                        var modp = p.ModPlayer();
                        if (modp == null) continue;
                        if (modp.lunarCharm > 0)
                            modp.LunarCharmLogic(targetRoom.RoomPosition16 + targetRoom.RoomCenter16);
                    }
                }
                player.Center = targetRoom.RoomPosition16 + targetRoom.RoomDimensions16 * new Vector2(0.9f, 0.5f);
                player.BottomRight = modPlayer.FindAirToPlayer((targetRoom.RoomPosition + targetRoom.RoomDimensions) * 16f);
                foreach (Player p in Main.ActivePlayers)
                {
                    var modp = p.ModPlayer();
                    if (modp == null)
                        continue;
                    modp.escaped = true;
                }
                TerRoguelikeWorld.escaped = true;
                EscapePacket.Send(EscapeContext.Complete);
                if (Main.dedServ)
                    TeleportToPositionPacket.Send(player.Center, TeleportContext.Sanctuary);
                modPlayer.escapeArrowTime = 0;
                for (int L = 0; L < RoomSystem.RoomList.Count; L++)
                {
                    RoomSystem.ResetRoomID(RoomSystem.RoomList[L].ID);
                }

                SetBossTrack(FinalBoss2PreludeTheme, 2);
                CombatVolumeInterpolant = 0;

                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (npc == null)
                        continue;
                    if (!npc.active)
                        continue;
                    if (npc.life <= 0)
                        continue;

                    TerRoguelikeGlobalNPC modNPC = npc.ModNPC();
                    if (!modNPC.isRoomNPC)
                        continue;
                    if (modNPC.sourceRoomListID < 0)
                        continue;

                    npc.active = false;
                }

                for (int s = 0; s < SpawnManager.pendingEnemies.Count; s++)
                {
                    var pendingEnemy = SpawnManager.pendingEnemies[s];
                    pendingEnemy.spent = true;

                }
            }
        }
        public virtual Vector2 DescendTeleportPosition()
        {
            return (RoomPosition + (RoomDimensions / 2f)) * 16f;
        }
        public virtual void OnEnter()
        {
            if (!active)
                return;
            if (IsStartRoom && !IsBossRoom)
                return;
            if (IsSanctuary)
                return;
            if (IsPillarRoom && TerRoguelikeWorld.escape)
                return;

            if (IsBossRoom && TerRoguelikeWorld.escape && FloorID[AssociatedFloor].jstcProgress < Floor.JstcProgress.Boss)
            {
                entered = false;
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null)
                    continue;
                if (!player.active)
                    continue;

                TerRoguelikePlayer modPlayer = player.GetModPlayer<TerRoguelikePlayer>();
                if (modPlayer == null)
                    continue;

                if (modPlayer.attackPlan > 0 && i == Main.myPlayer)
                {
                    int rocketCount = 4 + (4 * modPlayer.attackPlan);
                    RoomSystem.attackPlanRocketBundles.Add(new AttackPlanRocketBundle(RoomPosition16 + RoomCenter16, rocketCount, player.whoAmI, myRoom));
                }
                if (modPlayer.stimPack > 0)
                {
                    modPlayer.stimPackTime = 600;
                }
                
            }
        }
        public virtual void PreResetRoom()
        {

        }
        public void PlayerItemsUpdate()
        {
            if (TerRoguelikeWorld.escape || bossDead || IsSanctuary || IsStartRoom || TransitionDirection >= 0)
                return;
            if (TerRoguelikeWorld.escaped && FloorID[AssociatedFloor].Stage != -1)
                return;

            int totalAutomaticDefibrillator = 0;
            Vector2 roomCenter = new Vector2(RoomPosition.X + (RoomDimensions.X * 0.5f), RoomPosition.Y + (RoomDimensions.Y * 0.5f)) * 16f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null)
                    continue;
                if (!player.active)
                    continue;

                TerRoguelikePlayer modPlayer = player.GetModPlayer<TerRoguelikePlayer>();
                totalAutomaticDefibrillator += modPlayer.automaticDefibrillator;
            }

            if (totalAutomaticDefibrillator > 0)
            {
                int healTime = (int)(1500 * (4 / (float)(totalAutomaticDefibrillator + 4)));
                if (healTime <= 0)
                    healTime = 1;
                if (roomTime % healTime == 0 && roomTime > 0 && !(IsStartRoom && IsBossRoom && CutsceneSystem.cutsceneActive))
                {
                    RoomSystem.healingPulses.Add(new HealingPulse(roomCenter));
                }
            }
        }
        public static void ClearSpecificProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active)
                    continue;

                TerRoguelikeGlobalProjectile modProj = proj.ModProj();
                if (modProj != null && modProj.killOnRoomClear)
                {
                    proj.Kill();
                    continue;
                }
                if (proj.type == ModContent.ProjectileType<PlanRocket>() || proj.type == ModContent.ProjectileType<Missile>())
                {
                    proj.timeLeft = 60;
                }
            }
        }
        public void ClearGhosts()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                var modNPC = npc.ModNPC();
                if (modNPC == null)
                    continue;
                if (modNPC.hostileTurnedAlly && modNPC.isRoomNPC && modNPC.sourceRoomListID == myRoom)
                    npc.StrikeInstantKill();
            }
        }
        public virtual void PostDrawTilesRoom()
        {
            if (TerRoguelikeWorld.IsDebugWorld)
            {
                string s = Key;
                if (IsRoomVariant)
                    s += "Var";
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.DeathText.Value, s, RoomPosition16 + (Top * 5) + new Vector2(0, -80) - Main.screenPosition, Color.White, 0f, Vector2.Zero, new Vector2(0.5f));
            }
            if (RoomSystem.debugDrawNotSpawnedEnemies)
            {
                if (!initialized && !IsBossRoom)
                    InitializeRoom();

                StartAlphaBlendSpritebatch();
                Color color = Color.HotPink;
                Vector3 colorHSL = Main.rgbToHsl(color);
                GameShaders.Misc["TerRoguelike:BasicTint"].UseOpacity(0.4f);
                GameShaders.Misc["TerRoguelike:BasicTint"].UseColor(Main.hslToRgb(1 - colorHSL.X, colorHSL.Y, colorHSL.Z));
                GameShaders.Misc["TerRoguelike:BasicTint"].Apply();
                for (int i = 0; i < NotSpawned.Length; i++)
                {
                    bool notSpawned = NotSpawned[i];
                    if (!notSpawned)
                        continue;

                    int npcType = NPCToSpawn[i];

                    Texture2D texture = TextureAssets.Npc[npcType].Value;
                    int frameCount = Main.npcFrameCount[NPCToSpawn[i]];
                    int height = (int)(texture.Height / frameCount);
                    Rectangle frame = new Rectangle(0, 0, texture.Width, height);
                    Main.EntitySpriteDraw(texture, NPCSpawnPosition[i] - Main.screenPosition, frame, color, 0f, frame.Size() * 0.5f, 1f, SpriteEffects.None);
                }
                StartAlphaBlendSpritebatch();
            }

            wallTex ??= TextureManager.TexDict["TemporaryBlock"];

            if (IsSanctuary)
            {
                if (!Main.LocalPlayer.ModPlayer().escaped)
                    RightPortal(1f);
                else
                    LeftPortal();
            }
            else if (IsStartRoom && TerRoguelikeWorld.escape)
            {
                //Draw the blue wall portal
                LeftPortal();
            }

            if (closedTime > 60)
            {
                if (IsBossRoom && !TerRoguelikeWorld.escape && AllowWallDrawing)
                {
                    //Draw the blue wall portal
                    RightPortal((closedTime - 120) / 60f);
                }
            }

            void RightPortal(float completion)
            {
                StartAdditiveSpritebatch();
                for (float i = 0; i < RoomDimensions.Y; i++)
                {
                    Vector2 targetBlock = RoomPosition + new Vector2(RoomDimensions.X - 2, i);
                    int tileType = Main.tile[targetBlock.ToPoint()].TileType;
                    if (Main.tile[targetBlock.ToPoint()].IsTileSolidGround(true))
                        continue;

                    Color color = Color.Cyan;

                    color.A = (byte)MathHelper.Clamp(MathHelper.Lerp(0, 255, completion), 0, 255); ;

                    Vector2 drawPosition = targetBlock * 16f - Main.screenPosition - new Vector2(0, -16f);
                    float rotation = MathHelper.PiOver2;

                    Main.EntitySpriteDraw(wallTex, drawPosition, null, color, rotation, wallTex.Size(), 1f, SpriteEffects.None);

                    float scale = MathHelper.Clamp(MathHelper.Lerp(0.85f, 0.75f, completion), 0.75f, 0.85f);

                    if (Main.rand.NextBool((int)MathHelper.Clamp(MathHelper.Lerp(30f, 8f, completion), 8f, 20f)))
                        Dust.NewDustDirect((targetBlock * 16f) + new Vector2(10f, 0), 2, 16, 206, Scale: scale);
                }
                StartAlphaBlendSpritebatch();
            }

            void LeftPortal()
            {
                StartAdditiveSpritebatch();
                for (float i = 0; i < RoomDimensions.Y; i++)
                {
                    Vector2 targetBlock = RoomPosition + new Vector2(1, i);
                    if (Main.tile[targetBlock.ToPoint()].IsTileSolidGround(true))
                        continue;

                    Color color = Color.Cyan;

                    color.A = 255;

                    Vector2 drawPosition = targetBlock * 16f - Main.screenPosition + (Vector2.UnitX * 16f);
                    float rotation = -MathHelper.PiOver2;

                    Main.EntitySpriteDraw(wallTex, drawPosition, null, color, rotation, wallTex.Size(), 1f, SpriteEffects.None);

                    float scale = 0.75f;

                    if (Main.rand.NextBool(8))
                        Dust.NewDustDirect((targetBlock * 16f) + new Vector2(2f, 0), 2, 16, 206, Scale: scale);
                }
                StartAlphaBlendSpritebatch();
            }

            return;

            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (TopLeft * 16) - Main.screenPosition, null, Color.White, 0f, Vector2.Zero, 0.25f, SpriteEffects.None);
            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (Top * 16) - Main.screenPosition, null, Color.White, MathHelper.PiOver4, wallTex.Size() * 0.5f, 0.25f, SpriteEffects.None);
            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (TopRight * 16) - Main.screenPosition, null, Color.White, MathHelper.PiOver4 * 2, wallTex.Size() * 0.5f, 0.25f, SpriteEffects.None);
            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (Right * 16) - Main.screenPosition, null, Color.White, MathHelper.PiOver4 * 3, wallTex.Size() * 0.5f, 0.25f, SpriteEffects.None);
            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (BottomRight * 16) - Main.screenPosition, null, Color.White, MathHelper.PiOver4 * 4, wallTex.Size() * 0.5f, 0.25f, SpriteEffects.None);
            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (Bottom * 16) - Main.screenPosition, null, Color.White, MathHelper.PiOver4 * 5, wallTex.Size() * 0.5f, 0.25f, SpriteEffects.None);
            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (BottomLeft * 16) - Main.screenPosition, null, Color.White, MathHelper.PiOver4 * 6, wallTex.Size() * 0.5f, 0.25f, SpriteEffects.None);
            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (Left * 16) - Main.screenPosition, null, Color.White, MathHelper.PiOver4 * 7, wallTex.Size() * 0.5f, 0.25f, SpriteEffects.None);
            Main.EntitySpriteDraw(wallTex, RoomPosition16 + (Center * 16) - Main.screenPosition, null, Color.White, MathHelper.PiOver4 * 7, wallTex.Size() * 0.5f, 0.25f, SpriteEffects.None);
        }
        public virtual bool ClearCondition()
        {
            return (!anyAlive && roomClearGraceTime == 0) || (TerRoguelikeWorld.escape && IsBossRoom && bossDead && FloorID[AssociatedFloor].jstcProgress >= Floor.JstcProgress.Boss);
        }
        public virtual bool StartCondition()
        {
            if (IsBossRoom && TerRoguelikeWorld.escape && (int)FloorID[AssociatedFloor].jstcProgress < (int)(Floor.JstcProgress.Boss))
            {
                awake = false;
            }
            return awake;
        }
        public Vector2 FindAirNearRoomCenter()
        {
            Point centerTile = new Point((int)(RoomPosition.X + (RoomDimensions.X * 0.5f)), (int)(RoomPosition.Y + (RoomDimensions.Y * 0.5f)));
            if (!ParanoidTileRetrieval(centerTile.X, centerTile.Y).IsTileSolidGround(true))
                return RoomPosition16 + RoomCenter16;

            for (int i = 0; i < 200; i++)
            {
                int direction = i % 4 > 1 ? (i % 2 == 0 ? 2 : -2) : (i % 2 == 0 ? 1 : -1);
                if (Math.Abs(direction) == 2)
                {
                    direction = Math.Sign(direction);
                    if (ParanoidTileRetrieval(centerTile.X, centerTile.Y + ((i / 4) * direction)).IsTileSolidGround(true))
                        continue;

                    return new Vector2(centerTile.X, centerTile.Y + ((i / 4) * direction)) * 16f + new Vector2(8, 8);
                }
                else
                {
                    if (ParanoidTileRetrieval(centerTile.X + ((i / 4) * direction), centerTile.Y).IsTileSolidGround(true))
                        continue;

                    return new Vector2(centerTile.X + ((i / 4) * direction), centerTile.Y) * 16f + new Vector2(8, 8);
                }
            }

            return RoomPosition16 + RoomCenter16;
        }
        public Vector2 FindPlayerAirNearRoomCenter()
        {
            Point centerTile = new Point((int)(RoomPosition.X + (RoomDimensions.X * 0.5f)), (int)(RoomPosition.Y + (RoomDimensions.Y * 0.5f)));

            for (int i = 3; i < 200; i++)
            {
                int magnitude = (i / 4);
                bool xCheck = i % 2 == 0;
                int direction = i % 4 < 2 ? 1 : -1;
                Point checkTile = centerTile + new Point(xCheck ? direction * magnitude : 0, !xCheck ? direction * magnitude : 0);

                bool allow = true;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (ParanoidTileRetrieval(checkTile + new Point(x, y)).IsTileSolidGround(true))
                        {
                            allow = false;
                            break;
                        }
                    }
                    if (!allow)
                        break;
                }
                if (!allow)
                    continue;

                return checkTile.ToWorldCoordinates(16, 24);
            }

            return RoomPosition16 + RoomCenter16;
        }
    }
}

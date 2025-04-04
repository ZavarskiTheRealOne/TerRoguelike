﻿using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using TerRoguelike.Managers;
using TerRoguelike.Particles;
using TerRoguelike.Projectiles;
using TerRoguelike.Systems;
using TerRoguelike.Utilities;
using TerRoguelike.World;
using static Terraria.GameContent.PlayerEyeHelper;
using static TerRoguelike.Managers.TextureManager;
using static TerRoguelike.Schematics.SchematicManager;
using static TerRoguelike.Systems.MusicSystem;
using static TerRoguelike.Systems.RoomSystem;
using static TerRoguelike.Utilities.TerRoguelikeUtils;
using static TerRoguelike.Systems.EnemyHealthBarSystem;
using static TerRoguelike.NPCs.Enemy.Boss.MoonLord;
using System.IO;

namespace TerRoguelike.NPCs.Enemy.Boss
{
    public class MoonLordHead : BaseRoguelikeNPC
    {
        public Entity target;
        public Vector2 spawnPos;
        public bool ableToHit = false;
        public bool canBeHit = true;
        public override int modNPCID => ModContent.NPCType<MoonLordHead>();
        public override string Texture => "TerRoguelike/NPCs/Enemy/Boss/MoonLordSideEye";
        public bool goreProc = false;
        public override List<int> associatedFloors => new List<int>() { FloorDict["Lunar"] };
        public override int CombatStyle => -1;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            modNPC.TerRoguelikeBoss = true;
            NPC.width = 46;
            NPC.height = 76;
            NPC.aiStyle = -1;
            NPC.damage = 36;
            NPC.lifeMax = 10000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.knockBackResist = 0f;
            modNPC.drawCenter = new Vector2(0, 0);
            modNPC.IgnoreRoomWallCollision = true;
            modNPC.OverrideIgniteVisual = true;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            modNPC.AdaptiveArmorEnabled = true;
        }
        public override void OnSpawn(IEntitySource source)
        {
            NPC.ai[0] = -1;
            if (source is EntitySource_Parent parentSource)
            {
                if (parentSource.Entity is NPC)
                {
                    NPC.ai[0] = parentSource.Entity.whoAmI;
                    NPC npc = Main.npc[(int)NPC.ai[0]];
                    if (!npc.active || npc.type != ModContent.NPCType<MoonLord>())
                    {
                        NPC.ai[0] = -1;
                        NPC.StrikeInstantKill();
                        NPC.active = false;
                        return;
                    }

                }
            }

            if (NPC.ai[0] == -1)
            {
                NPC.StrikeInstantKill();
                NPC.active = false;
            }

            NPC.immortal = true;
            NPC.dontTakeDamage = true;
            spawnPos = NPC.Center;
            ableToHit = false;
            NPC.direction = -1;
            NPC.spriteDirection = -1;
        }
        public override void PostAI()
        {

        }
        public override void AI()
        {
            NPC.netSpam = 0;
            NPC parent = Main.npc[(int)NPC.ai[0]];
            if (!parent.active || parent.type != ModContent.NPCType<MoonLord>())
            {
                NPC.dontTakeDamage = false;
                NPC.immortal = false;
                NPC.StrikeInstantKill();
                NPC.active = false;
                return;
            }

            NPC.dontTakeDamage = parent.localAI[0] <= -30;
            NPC.immortal = parent.localAI[0] <= -30;
            canBeHit = parent.localAI[3] == 1;
            if (NPC.life <= 1)
            {
                CheckDead();
            }
        }
       
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return ableToHit;
        }
        public override bool CanHitNPC(NPC target)
        {
            return canBeHit;
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return canBeHit ? null : false;
        }

        public override bool CheckDead()
        {
            if (NPC.ai[0] < 0 || NPC.ai[0] == 1)
            {
                NPC.immortal = false;
                NPC.dontTakeDamage = false;
                NPC.StrikeInstantKill();
                return true;
            }
                
            if (NPC.ai[3] == 0)
            {
                modNPC.ignitedStacks.Clear();
                modNPC.bleedingStacks.Clear();
                modNPC.ballAndChainSlow = 0;
            }
            NPC.ai[3]++;
            NPC parent = Main.npc[(int)NPC.ai[0]];
            if (parent.active)
            {
                NPC.active = true;
                NPC.life = 1;
                NPC.immortal = true;
                NPC.dontTakeDamage = true;
                return false;
            }
            NPC.StrikeInstantKill();
            return true;
        }
        
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 1)
            {
                if (!CheckDead())
                {
                    if (!goreProc)
                    {
                        NPC parent = Main.npc[(int)NPC.ai[2]];
                        SoundEngine.PlaySound(SoundID.NPCHit57 with { Volume = 0.5f }, parent.Center + new Vector2(0, -300));
                        SoundEngine.PlaySound(SoundID.NPCDeath62 with { Volume = 0.8f }, NPC.Center);
                        goreProc = true;
                    }
                }
            }
            else
            {
                for (int i = 0; (double)i < hit.Damage / (double)NPC.lifeMax * 1000.0; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Vortex, hit.HitDirection, -1f, 0, default, 0.5f);
                    Main.dust[d].noLight = true;
                    Main.dust[d].noLightEmittence = true;
                }
            }
        }
        public override void OnKill()
        {

        }
        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            boundingBox = NPC.Hitbox;
        }
        public override void FindFrame(int frameHeight)
        {
            
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.life > 1)
                DrawDeathrayForNPC(NPC, Main.npc[(int)NPC.ai[0]]);
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(spawnPos);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            spawnPos = reader.ReadVector2();
        }
    }
}

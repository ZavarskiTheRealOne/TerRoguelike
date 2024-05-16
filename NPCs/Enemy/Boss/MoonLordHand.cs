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

namespace TerRoguelike.NPCs.Enemy.Boss
{
    public class MoonLordHand : BaseRoguelikeNPC
    {
        public Entity target;
        public Vector2 spawnPos;
        public bool ableToHit = true;
        public bool canBeHit = true;
        public override int modNPCID => ModContent.NPCType<MoonLordHand>();
        public override string Texture => "TerRoguelike/Projectiles/InvisibleProj";
        public override List<int> associatedFloors => new List<int>() { FloorDict["Lunar"] };
        public override int CombatStyle => -1;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.width = 70;
            NPC.height = 70;
            NPC.aiStyle = -1;
            NPC.damage = 36;
            NPC.lifeMax = 25000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.knockBackResist = 0f;
            modNPC.drawCenter = new Vector2(0, 0);
            modNPC.IgnoreRoomWallCollision = true;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
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
        }
        public override void PostAI()
        {
            
        }
        public override void AI()
        {
            NPC parent = Main.npc[(int)NPC.ai[0]];
            if (!parent.active || parent.type != ModContent.NPCType<MoonLord>())
            {
                NPC.dontTakeDamage = false;
                NPC.immortal = false;
                NPC.StrikeInstantKill();
                NPC.active = false;
                return;
            }

            NPC.dontTakeDamage = false;
            NPC.immortal = false;
            canBeHit = true;
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
            return ableToHit;
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            return canBeHit ? null : false;
        }

        public override bool CheckDead()
        {
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
                CheckDead();
            }
        }
        public override void OnKill()
        {
            
        }
        public override void FindFrame(int frameHeight)
        {
            
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            
            return false;
        }
    }
}

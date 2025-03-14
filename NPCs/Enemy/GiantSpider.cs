﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerRoguelike;
using TerRoguelike.TerPlayer;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.GameContent;
using TerRoguelike.NPCs;
using TerRoguelike.Projectiles;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using static TerRoguelike.Schematics.SchematicManager;
using Terraria.DataStructures;
using static TerRoguelike.Utilities.TerRoguelikeUtils;

namespace TerRoguelike.NPCs.Enemy
{
    public class GiantSpider : BaseRoguelikeNPC
    {
        public override int modNPCID => ModContent.NPCType<GiantSpider>();
        public override List<int> associatedFloors => new List<int>() { FloorDict["Corrupt"] };
        public override int CombatStyle => 0;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 4;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.width = 36;
            NPC.height = 36;
            NPC.aiStyle = -1;
            NPC.damage = 30;
            NPC.lifeMax = 600;
            NPC.HitSound = SoundID.NPCHit29;
            NPC.DeathSound = SoundID.NPCDeath32;
            NPC.knockBackResist = 0.25f;
            modNPC.drawCenter = new Vector2(0, -9);
            NPC.noGravity = true;
        }
        public override void AI()
        {
            modNPC.RogueSpiderAI(NPC, 1.7f, 0.08f, 120, 45, 120, 120f);

            if (NPC.ai[1] == 2)
            {
                NPC.frameCounter += 0.14d;
                float direction = 0;
                if (modNPC.targetNPC != -1)
                {
                    direction = (Main.npc[modNPC.targetNPC].Center - NPC.Center).ToRotation();
                }
                else if (modNPC.targetPlayer != -1)
                {
                    direction = (Main.player[modNPC.targetPlayer].Center - NPC.Center).ToRotation();
                }
                NPC.rotation = NPC.rotation.AngleLerp(direction, 0.1f);
            }
            else if (NPC.velocity.Length() > 0.5f)
            {
                NPC.frameCounter += 0.14d;
                NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.ToRotation(), 0.1f);
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0)
            {
                for (int i = 0; (double)i < hit.Damage / (double)NPC.lifeMax * 100.0; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 18, hit.HitDirection, -1f);
                }
            }
            else
            {
                for (int i = 0; i < 50; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 18, 2.5f * (float)hit.HitDirection, -2.5f);
                }
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 207);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 208);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 208);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 208);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 208);
            }
            
        }
        public override void OnSpawn(IEntitySource source)
        {
            NPC.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }
        public override bool? CanFallThroughPlatforms()
        {
            return true;
        }
        public override void FindFrame(int frameHeight)
        {
            NPC.gfxOffY = 6;
            int currentFrame = (int)(NPC.frameCounter % Main.npcFrameCount[Type]);
            NPC.frame = new Rectangle(0, currentFrame * frameHeight, NpcTexWidth(Type), frameHeight - 1);
        }
    }
}

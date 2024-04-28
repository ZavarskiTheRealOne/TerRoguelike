﻿using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Animations;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using TerRoguelike.Managers;
using TerRoguelike.Particles;
using TerRoguelike.Projectiles;
using TerRoguelike.Systems;
using TerRoguelike.Utilities;
using static TerRoguelike.Managers.TextureManager;
using static TerRoguelike.Schematics.SchematicManager;
using static TerRoguelike.Systems.MusicSystem;
using static TerRoguelike.Systems.RoomSystem;
using static TerRoguelike.Utilities.TerRoguelikeUtils;
using Terraria.Graphics.Effects;

namespace TerRoguelike.NPCs.Enemy.Boss
{
    public class QueenBee : BaseRoguelikeNPC
    {
        public Entity target;
        public Vector2 spawnPos;
        public bool ableToHit = true;
        public bool canBeHit = true;
        public override int modNPCID => ModContent.NPCType<QueenBee>();
        public override List<int> associatedFloors => new List<int>() { FloorDict["Jungle"] };
        public override int CombatStyle => -1;
        public int currentFrame;
        public bool CollisionPass = false;
        public List<ExtraHitbox> hitboxes = new List<ExtraHitbox>()
        {
            new ExtraHitbox(new Point(60, 60), new Vector2(0)),
            new ExtraHitbox(new Point(40, 40), new Vector2(0, -46)),
            new ExtraHitbox(new Point(50, 50), new Vector2(46, 0)),
        };
        public Texture2D squareTex;

        public int deadTime = 0;
        public int cutsceneDuration = 120;
        public int deathCutsceneDuration = 120;

        public static Attack None = new Attack(0, 0, 75);
        public static Attack Shotgun = new Attack(1, 30, 180);
        public static Attack BeeSwarm = new Attack(2, 30, 180);
        public static Attack Charge = new Attack(3, 30, 180);
        public static Attack HoneyVomit = new Attack(4, 30, 180);
        public static Attack Summon = new Attack(5, 30, 180);
        public int shotgunFireRate = 24;
        public float shotgunRecoilInterpolant = 0;
        public Vector2 shotgunRecoilAnchorPos = Vector2.Zero;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[modNPCID] = 12;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.width = 66;
            NPC.height = 66;
            NPC.aiStyle = -1;
            NPC.damage = 30;
            NPC.lifeMax = 28000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.knockBackResist = 0f;
            modNPC.drawCenter = new Vector2(0, -32);
            modNPC.IgnoreRoomWallCollision = true;
            modNPC.SpecialProjectileCollisionRules = true;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            squareTex = TexDict["Square"].Value;
        }
        public override void OnSpawn(IEntitySource source)
        {
            NPC.Opacity = 0;
            NPC.immortal = true;
            NPC.dontTakeDamage = true;
            currentFrame = 0;
            NPC.localAI[0] = -(cutsceneDuration + 30);
            NPC.direction = -1;
            NPC.spriteDirection = -1;
            spawnPos = NPC.Center;
            NPC.ai[2] = None.Id;
            ableToHit = false;
        }
        public override void PostAI()
        {
            NPC.width = 60;
            NPC.height = 60;
            
            bool dashingFrames = false;
            if (dashingFrames)
            {
                currentFrame = (int)NPC.frameCounter % 4 + 8;
            }
            else
            {
                currentFrame = (int)NPC.frameCounter % 8;
            }

            switch (currentFrame)
            {
                default:
                case var expression when (currentFrame < 8):
                    hitboxes[1].active = true;
                    hitboxes[2].active = false;
                    break;
                case var expression when (currentFrame >= 8):
                    hitboxes[1].active = false;
                    hitboxes[2].active = true;
                    break;
            }
            NPC.spriteDirection = NPC.direction;
            for (int i = 0; i < hitboxes.Count; i++)
            {
                var hitbox = hitboxes[i];
                hitbox.offset.X = Math.Abs(hitbox.offset.X) * -NPC.spriteDirection;
            }
        }
        public override void AI()
        {
            NPC.frameCounter += 0.25d;

            if (deadTime > 0)
            {
                CheckDead();
                return;
            }
            if (modNPC.isRoomNPC && NPC.localAI[0] == -(cutsceneDuration + 30))
            {
                SetBossTrack(IceQueenTheme);
            }

            ableToHit = NPC.localAI[0] >= 0;
            canBeHit = true;

            if (NPC.localAI[0] < 0)
            {
                target = modNPC.GetTarget(NPC);

                if (NPC.localAI[0] == -cutsceneDuration)
                {
                    CutsceneSystem.SetCutscene(spawnPos, cutsceneDuration, 30, 30, 2.5f);
                }
                NPC.localAI[0]++;

                if (NPC.localAI[0] == -30)
                {
                    NPC.Opacity = 1;
                    NPC.immortal = false;
                    NPC.dontTakeDamage = false;
                    NPC.ai[1] = 0;
                }
            }
            else
            {
                NPC.localAI[0]++;
                BossAI();
                if (NPC.ai[0] == None.Id)
                {
                    NPC.rotation = NPC.rotation.AngleLerp(0, 0.025f).AngleTowards(0, 0.015f);
                }
            }
        }
        public void BossAI()
        {
            target = modNPC.GetTarget(NPC);

            NPC.ai[1]++;
            NPC.velocity *= 0.98f;

            if (NPC.ai[0] == None.Id)
            {
                UpdateDirection();

                if (NPC.ai[1] >= None.Duration)
                {
                    ChooseAttack();
                }
                else
                {
                    if (target != null)
                    {
                        Vector2 targetPos = target.Center + new Vector2(0, -240);
                        if (NPC.velocity.Length() < 10)
                            NPC.velocity += (targetPos - NPC.Center).SafeNormalize(Vector2.UnitY) * 0.15f;
                        if (NPC.velocity.Length() > 10)
                            NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * 10;
                    }
                }
            }

            if (NPC.ai[0] == Shotgun.Id)
            {
                NPC.velocity *= 0.97f;
                UpdateDirection();
                if (NPC.direction != NPC.oldDirection)
                {
                    NPC.rotation += MathHelper.PiOver2 * 0.4f * NPC.direction;
                }
                Vector2 targetPos = target == null ? NPC.Center : target.Center;
                float buttRot = (MathHelper.PiOver2 * 0.8f * NPC.direction);
                float targetAngle = target == null ? -buttRot : (target.Center - NPC.Center).ToRotation() - buttRot;
                targetAngle -= (NPC.direction == -1 ? MathHelper.PiOver2 * 2f : 0);
                NPC.rotation = NPC.rotation.AngleLerp(targetAngle, 0.05f).AngleTowards(targetAngle, 0.03f);
                float fireDirection = NPC.rotation + buttRot + (NPC.direction == -1 ? MathHelper.PiOver2 * 2f : 0);

                if (NPC.ai[1] > 0 && NPC.ai[1] % shotgunFireRate == 0)
                {
                    shotgunRecoilAnchorPos = NPC.Center;
                    shotgunRecoilInterpolant = 1f;
                    Vector2 baseOffset = new Vector2(6 * NPC.direction, 16);
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 1f }, NPC.Center);
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 projSpawnPos = baseOffset + ((Vector2.UnitX * 16).RotatedBy(i * MathHelper.TwoPi / 12f) * new Vector2(1f, 0.6f));
                        projSpawnPos = projSpawnPos.RotatedBy(NPC.rotation);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + projSpawnPos, (fireDirection.ToRotationVector2() * Main.rand.NextFloat(7, 7.5f)).RotatedBy(Main.rand.NextFloat(-0.2f, 0.2f)), ModContent.ProjectileType<Stinger>(), NPC.damage, 0, -1, 1);
                    }
                    NPC.Center -= fireDirection.ToRotationVector2() * 12;
                }
                if (shotgunRecoilInterpolant > 0)
                {
                    NPC.Center += (shotgunRecoilAnchorPos - NPC.Center) * (1 - shotgunRecoilInterpolant) * 0.25f;
                    Vector2 offset = (targetPos - NPC.Center).SafeNormalize(Vector2.Zero) * 0.25f;
                    shotgunRecoilAnchorPos += offset;
                    NPC.Center += offset;

                    shotgunRecoilInterpolant -= 0.05f;
                }

                if (NPC.ai[1] >= Shotgun.Duration)
                {
                    shotgunRecoilInterpolant = 0;
                    NPC.ai[0] = None.Id;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = Shotgun.Id;
                }
            }
            else if (NPC.ai[0] == BeeSwarm.Id)
            {
                if (NPC.ai[1] >= BeeSwarm.Duration)
                {
                    NPC.ai[0] = None.Id;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = BeeSwarm.Id;
                }
            }
            else if (NPC.ai[0] == Charge.Id)
            {
                if (NPC.ai[1] >= Charge.Duration)
                {
                    NPC.ai[0] = None.Id;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = Charge.Id;
                }
            }
            else if (NPC.ai[0] == HoneyVomit.Id)
            {
                if (NPC.ai[1] >= HoneyVomit.Duration)
                {
                    NPC.ai[0] = None.Id;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = HoneyVomit.Id;
                }
            }
            else if (NPC.ai[0] == Summon.Id)
            {
                if (NPC.ai[1] >= Summon.Duration)
                {
                    NPC.ai[0] = None.Id;
                    NPC.ai[1] = 0;
                    NPC.ai[2] = Summon.Id;
                }
            }
        }
        public void ChooseAttack()
        {
            NPC.ai[1] = 0;
            int chosenAttack = 0;

            List<Attack> potentialAttacks = new List<Attack>() { Shotgun, BeeSwarm, Charge, HoneyVomit, Summon };
            potentialAttacks.RemoveAll(x => x.Id == (int)NPC.ai[2]);

            int totalWeight = 0;
            for (int i = 0; i < potentialAttacks.Count; i++)
            {
                totalWeight += potentialAttacks[i].Weight;
            }
            int chosenRandom = Main.rand.Next(totalWeight);

            for (int i = potentialAttacks.Count - 1; i >= 0; i--)
            {
                Attack attack = potentialAttacks[i];
                chosenRandom -= attack.Weight;
                if (chosenRandom < 0)
                {
                    chosenAttack = attack.Id;
                    break;
                }
            }
            chosenAttack = Shotgun.Id;
            NPC.ai[0] = chosenAttack;
        }
        public void UpdateDirection()
        {
            if (target != null)
            {
                if (target.Center.X > NPC.Center.X)
                    NPC.direction = 1;
                else
                    NPC.direction = -1;
            }
            else
            {
                NPC.direction = Math.Sign(NPC.velocity.X);
                if (NPC.direction == 0)
                    NPC.direction = -1;
            }
        }
        public override bool? CanFallThroughPlatforms()
        {
            return true;
        }
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            Rectangle targetBox = target.Hitbox;
            for (int i = 0; i < hitboxes.Count; i++)
            {
                if (!hitboxes[i].active)
                    continue;

                bool pass = targetBox.Intersects(hitboxes[i].GetHitbox(NPC.Center, NPC.rotation));
                if (pass)
                {
                    CollisionPass = ableToHit;
                    return ableToHit;
                }
            }
            CollisionPass = false;
            return false;
        }
        public override bool CanHitNPC(NPC target)
        {
            Rectangle targetBox = target.Hitbox;
            for (int i = 0; i < hitboxes.Count; i++)
            {
                if (!hitboxes[i].active)
                    continue;

                bool pass = targetBox.Intersects(hitboxes[i].GetHitbox(NPC.Center, NPC.rotation));
                if (pass)
                {
                    CollisionPass = ableToHit;
                    return ableToHit;
                }
            }
            CollisionPass = false;
            return false;
        }
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            if (CollisionPass)
            {
                npcHitbox = new Rectangle(0, 0, Main.maxTilesX * 16, Main.maxTilesY * 16);
            }
            return CollisionPass;
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if ((projectile.hostile && !NPC.friendly) || (projectile.friendly && NPC.friendly))
                return false;

            for (int i = 0; i < hitboxes.Count; i++)
            {
                if (!hitboxes[i].active)
                    continue;

                bool pass = projectile.Colliding(projectile.getRect(), hitboxes[i].GetHitbox(NPC.Center, NPC.rotation));
                if (pass)
                {
                    projectile.ModProj().ultimateCollideOverride = true;
                    return canBeHit ? null : false;
                }
            }

            return false;
        }

        public override bool CheckDead()
        {
            if (deadTime >= deathCutsceneDuration - 30)
            {
                return true;
            }

            NPC.ai[0] = None.Id;
            NPC.ai[1] = 1;

            modNPC.OverrideIgniteVisual = true;
            NPC.life = 1;
            NPC.immortal = true;
            NPC.dontTakeDamage = true;
            NPC.active = true;


            if (deadTime == 0)
            {
                NPC.velocity *= 0;
                NPC.rotation = 0;
                modNPC.ignitedStacks.Clear();
                if (modNPC.isRoomNPC)
                {
                    ActiveBossTheme.endFlag = true;
                    Room room = RoomList[modNPC.sourceRoomListID];
                    room.bossDead = true;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (i == NPC.whoAmI)
                            continue;

                        NPC childNPC = Main.npc[i];
                        if (childNPC == null)
                            continue;
                        if (!childNPC.active)
                            continue;

                        TerRoguelikeGlobalNPC modChildNPC = childNPC.ModNPC();
                        if (modChildNPC == null)
                            continue;
                        if (modChildNPC.isRoomNPC && modChildNPC.sourceRoomListID == modNPC.sourceRoomListID)
                        {
                            childNPC.StrikeInstantKill();
                            childNPC.active = false;
                        }
                    }
                }
                CutsceneSystem.SetCutscene(NPC.Center, deathCutsceneDuration, 30, 30, 2.5f);
            }
            deadTime++;

            
            if (deadTime >= deathCutsceneDuration - 30)
            {
                NPC.immortal = false;
                NPC.dontTakeDamage = false;
                NPC.StrikeInstantKill();
            }

            return deadTime >= cutsceneDuration - 30;
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0 && deadTime > 0)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath66 with { Volume = 1f }, NPC.Center);
                SoundEngine.PlaySound(SoundID.NPCDeath1 with { Volume = 1f }, NPC.Center);
            }
        }
        public override void OnKill()
        {
            
        }
        public override void FindFrame(int frameHeight)
        {
            Texture2D tex = TextureAssets.Npc[Type].Value;

            NPC.frame = new Rectangle(0, currentFrame * frameHeight, tex.Width, frameHeight);
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = TextureAssets.Npc[Type].Value;
            Color color = Color.Lerp(drawColor, Color.White, 0.2f);

            modNPC.drawCenter.X = -12 * NPC.spriteDirection;

            Main.EntitySpriteDraw(tex, NPC.Center - Main.screenPosition + modNPC.drawCenter.RotatedBy(NPC.rotation), NPC.frame, color * NPC.Opacity, NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally);

            bool drawHitboxes = false;
            if (drawHitboxes)
            {
                for (int i = 0; i < hitboxes.Count; i++)
                {
                    if (!hitboxes[i].active)
                        continue;

                    Rectangle hitbox = hitboxes[i].GetHitbox(NPC.Center, NPC.rotation);
                    for (int d = 0; d <= 1; d++)
                    {
                        for (int x = 0; x < hitbox.Width; x++)
                        {
                            Main.EntitySpriteDraw(squareTex, hitbox.Location.ToVector2() + new Vector2(x, hitbox.Height * d) - Main.screenPosition, null, Color.Red, 0, squareTex.Size() * 0.5f, 0.5f, SpriteEffects.None);
                        }
                        for (int y = 0; y < hitbox.Height; y++)
                        {
                            Main.EntitySpriteDraw(squareTex, hitbox.Location.ToVector2() + new Vector2(hitbox.Width * d, y) - Main.screenPosition, null, Color.Red, 0, squareTex.Size() * 0.5f, 0.5f, SpriteEffects.None);
                        }
                    }
                }
            }
            return false;
        }
    }
}

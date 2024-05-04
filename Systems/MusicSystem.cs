﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using TerRoguelike.Managers;
using TerRoguelike.Systems;
using TerRoguelike.Rooms;
using Terraria.WorldBuilding;
using static tModPorter.ProgressUpdate;
using Terraria.GameContent.Generation;
using tModPorter;
using Terraria.Localization;
using TerRoguelike.World;
using TerRoguelike.MainMenu;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using ReLogic.Utilities;
using TerRoguelike.TerPlayer;
using Terraria.ModLoader.IO;
using System.Threading;
using ReLogic.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace TerRoguelike.Systems
{
    public class MusicSystem : ModSystem
    {
        public static bool Initialized = false;
        public static bool PlayedAllSounds = false;
        public static int BlankMusicSlotId = -1;
        public static float CalmVolumeInterpolant = 0;
        public static float CombatVolumeInterpolant = 0;
        public static float CalmVolumeLevel = 1f;
        public static float CombatVolumeLevel = 1f;
        public static double BossIntroDuration = 0;
        public static double BossIntroProgress = 0;
        public static float BossIntroPreviousGlobalTime = 0;
        public static SoundEffectInstance CalmMusic;
        public static SoundEffectInstance CombatMusic;
        public static BossTheme ActiveBossTheme;
        public static MusicStyle MusicMode = MusicStyle.Dynamic;
        public static Dictionary<string, Asset<SoundEffect>> MusicDict = new Dictionary<string, Asset<SoundEffect>>();
        public enum MusicStyle
        {
            Dynamic = 0,
            AllCalm = 1,
            AllCombat = 2,
            Silent = 3,
            Boss = 4,
        }
        public static bool BufferCalmSilence = false;
        public static bool BufferCombatSilence = false;

        public static string Silence = "TerRoguelike/Tracks/Blank";
        public static string FinalStage = "TerRoguelike/Tracks/FinalStage";
        public static string FinalBoss = "TerRoguelike/Tracks/FinalBoss";
        public static string Escape = "TerRoguelike/Tracks/Escape";

        public static FloorSoundtrack BaseTheme = new(
            "TerRoguelike/Tracks/Calm",
            "TerRoguelike/Tracks/Combat",
            0.18f);

        public static BossTheme PaladinTheme = new(
            "TerRoguelike/Tracks/PaladinTheme",
            "TerRoguelike/Tracks/PaladinThemeStart",
            "TerRoguelike/Tracks/PaladinThemeEnd",
            0.33f);

        public static BossTheme BrambleHollowTheme = new(
            "TerRoguelike/Tracks/BrambleHollowTheme",
            "TerRoguelike/Tracks/BrambleHollowThemeStart",
            "TerRoguelike/Tracks/BrambleHollowThemeEnd",
            0.4f);

        public static BossTheme CrimsonVesselTheme = new(
            "TerRoguelike/Tracks/CrimsonVesselTheme",
            "TerRoguelike/Tracks/CrimsonVesselThemeStart",
            "TerRoguelike/Tracks/CrimsonVesselThemeEnd",
            0.8f);
        public static BossTheme CorruptionParasiteTheme = new(
            "TerRoguelike/Tracks/CorruptionParasiteTheme",
            "TerRoguelike/Tracks/CorruptionParasiteThemeStart",
            "TerRoguelike/Tracks/CorruptionParasiteThemeEnd",
            0.36f);
        public static BossTheme IceQueenTheme = new(
            "TerRoguelike/Tracks/IceQueenTheme",
            "TerRoguelike/Tracks/IceQueenThemeStart",
            "TerRoguelike/Tracks/IceQueenThemeEnd",
            0.6f);
        public static BossTheme PharaohSpiritTheme = new(
            "TerRoguelike/Tracks/PharaohSpiritTheme",
            "TerRoguelike/Tracks/PharaohSpiritThemeStart",
            "TerRoguelike/Tracks/PharaohSpiritThemeEnd",
            0.67f);
        public static BossTheme QueenBeeTheme = new(
            "TerRoguelike/Tracks/QueenBeeTheme",
            "TerRoguelike/Tracks/QueenBeeThemeStart",
            "TerRoguelike/Tracks/QueenBeeThemeEnd",
            0.4f);
        public static BossTheme WallOfFleshTheme = new(
            "TerRoguelike/Tracks/WallOfFleshTheme",
            "TerRoguelike/Tracks/WallOfFleshThemeStart",
            "TerRoguelike/Tracks/WallOfFleshThemeEnd",
            0.44f);


        public static void PlayAllSounds()
        {
            if (PlayedAllSounds)
                return;

            PlayedAllSounds = true;

            List<string> pathList = new List<string>()
            {
                Silence,
                FinalStage,
                FinalBoss,
                Escape,
                BaseTheme.CalmTrack,
                BaseTheme.CombatTrack,
                PaladinTheme.BattleTrack,
                PaladinTheme.StartTrack,
                PaladinTheme.EndTrack,
                BrambleHollowTheme.BattleTrack,
                BrambleHollowTheme.StartTrack,
                BrambleHollowTheme.EndTrack,
                CrimsonVesselTheme.BattleTrack,
                CrimsonVesselTheme.StartTrack,
                CrimsonVesselTheme.EndTrack,
                CorruptionParasiteTheme.BattleTrack,
                CorruptionParasiteTheme.StartTrack,
                CorruptionParasiteTheme.EndTrack,
                IceQueenTheme.BattleTrack,
                IceQueenTheme.StartTrack,
                IceQueenTheme.EndTrack,
                PharaohSpiritTheme.BattleTrack,
                PharaohSpiritTheme.StartTrack,
                PharaohSpiritTheme.EndTrack,
                QueenBeeTheme.BattleTrack,
                QueenBeeTheme.StartTrack,
                QueenBeeTheme.EndTrack,
                WallOfFleshTheme.BattleTrack,
                WallOfFleshTheme.StartTrack,
                WallOfFleshTheme.EndTrack
            };
            foreach (string path in pathList)
            {
                AddMusic(path);
            }
        }
        internal static void AddMusic(string path)
        {
            MusicDict.Add(path, ModContent.Request<SoundEffect>(path, AssetRequestMode.AsyncLoad));
        }
 
        public override void OnModLoad()
        {
            MusicLoader.AddMusic(TerRoguelike.Instance, "Tracks/Blank");
        }
        public override void Unload()
        {
            MusicDict = null;
        }
        public override void SetStaticDefaults()
        {
            PlayAllSounds();
            BlankMusicSlotId = MusicLoader.GetMusicSlot(TerRoguelike.Instance, "Tracks/Blank");
        }
        public static void SetBossTrack(BossTheme bossTheme)
        {
            ActiveBossTheme = new BossTheme(bossTheme);
            ActiveBossTheme.startFlag = true;
            SoundEffect introTrack = MusicDict[bossTheme.StartTrack].Value;
            BossIntroDuration = introTrack.Duration.TotalSeconds;
            BossIntroProgress = 0;
            BossIntroPreviousGlobalTime = Main.GlobalTimeWrappedHourly;

            SetCombat(introTrack, false);
            SetMusicMode(MusicStyle.Boss);
            CombatVolumeLevel = bossTheme.Volume;
        }
        public static void SetMusicMode(MusicStyle newMode)
        {
            MusicMode = newMode;
            if (newMode != MusicStyle.Boss)
                ActiveBossTheme = null;

            BufferCalmSilence = newMode == MusicStyle.AllCombat || newMode == MusicStyle.Silent || newMode == MusicStyle.Boss;
            BufferCombatSilence = newMode == MusicStyle.AllCalm || newMode == MusicStyle.Silent;
        }
        public override void PostUpdateEverything()
        {
            MusicUpdate();
        }
        public override void PostDrawFullscreenMap(ref string mouseText)
        {
            if (!Main.hasFocus || Main.gamePaused)
                MusicUpdate();
        }
        public override void PostDrawTiles()
        {
            if (!Main.hasFocus || Main.gamePaused)
                MusicUpdate();
        }
        public static void SetCalm(string track, bool loop = true)
        {
            SetCalm(MusicDict[track].Value, loop);
        }
        public static void SetCalm(SoundEffect track, bool loop = true)
        {
            if (CalmMusic != null)
                CalmMusic.Dispose();

            CalmMusic = track.CreateInstance();
            CalmMusic.IsLooped = loop;
            CalmMusic.Play();
            CalmMusic.Volume = 0;
        }
        public static void SetCombat(string track, bool loop = true)
        {
            SetCombat(MusicDict[track].Value, loop);
        }
        public static void SetCombat(SoundEffect track, bool loop = true)
        {
            if (CombatMusic != null)
                CombatMusic.Dispose();

            CombatMusic = track.CreateInstance();
            CombatMusic.IsLooped = loop;
            CombatMusic.Play();
            CombatMusic.Volume = 0;
        }
        public void MusicUpdate()
        {
            if (!TerRoguelikeWorld.IsTerRoguelikeWorld)
                return;

            TerRoguelikePlayer modPlayer = Main.player[Main.myPlayer].GetModPlayer<TerRoguelikePlayer>();
            if (!Initialized && modPlayer != null && modPlayer.currentFloor != null)
            {
                SetMusicMode(MusicStyle.Dynamic);
                FloorSoundtrack soundtrack = modPlayer.currentFloor.Soundtrack;

                SetCalm(soundtrack.CalmTrack);
                SetCombat(soundtrack.CombatTrack);
                CalmVolumeInterpolant = 1;
                CombatVolumeInterpolant = 0;
                CalmVolumeLevel = soundtrack.Volume;
                CombatVolumeLevel = soundtrack.Volume;
                Initialized = true;
            }
            if (!Initialized)
                return;

            if (!Main.hasFocus)
            {
                if (CalmMusic.State == SoundState.Playing)
                    CalmMusic.Pause();
                if (CombatMusic.State == SoundState.Playing)
                    CombatMusic.Pause();
            }
            else
            {
                if (CalmMusic.State == SoundState.Paused)
                    CalmMusic.Resume();
                if (CombatMusic.State == SoundState.Paused)
                    CombatMusic.Resume();
            }
            
            if (MusicMode == MusicStyle.Dynamic)
            {
                if (modPlayer.currentRoom == -1)
                {
                    float calmInterpolant = CalmVolumeInterpolant + (1f / 120f);

                    CalmVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, calmInterpolant), 0f, 1f);

                    float combatInterpolant = CombatVolumeInterpolant - (1f / 120f);

                    CombatVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, combatInterpolant), 0f, 1f);
                }
                else
                {
                    float calmInterpolant = CalmVolumeInterpolant - (1f / 60f);

                    CalmVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, calmInterpolant), 0f, 1f);

                    float combatInterpolant = CombatVolumeInterpolant + (1f / 60f);

                    CombatVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, combatInterpolant), 0f, 1f);
                }
            }
           
            
            if (MusicMode == MusicStyle.AllCalm)
            {
                float calmInterpolant = CalmVolumeInterpolant + (1f / 120f);

                CalmVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, calmInterpolant), 0f, 1f);


                float combatInterpolant = CombatVolumeInterpolant - (1f / 120f);

                CombatVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, combatInterpolant), 0f, 1f);

                if (BufferCombatSilence)
                {
                    if (CombatMusic.Volume <= 0)
                    {
                        SetCombat(Silence);
                        BufferCombatSilence = false;
                    }
                }
            }

            if (MusicMode == MusicStyle.AllCombat)
            {
                float calmInterpolant = CalmVolumeInterpolant - (1f / 60f);

                CalmVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, calmInterpolant), 0f, 1f);

                if (BufferCalmSilence)
                {
                    if (CalmMusic.Volume <= 0)
                    {
                        SetCalm(Silence);
                        BufferCalmSilence = false;
                    }
                }

                float combatInterpolant = CombatVolumeInterpolant + (1f / 60f);

                CombatVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, combatInterpolant), 0f, 1f);
            }

            if (MusicMode == MusicStyle.Silent)
            {
                float calmInterpolant = CalmVolumeInterpolant - (1f / 180f);

                CalmVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, calmInterpolant), 0f, 1f);

                if (BufferCalmSilence)
                {
                    if (CalmMusic.Volume <= 0)
                    {
                        SetCalm(Silence);
                        BufferCalmSilence = false;
                    }
                }

                float combatInterpolant = CombatVolumeInterpolant - (1f / 180f);

                CombatVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, combatInterpolant), 0f, 1f);

                if (BufferCombatSilence)
                {
                    if (CombatMusic.Volume <= 0)
                    {
                        SetCombat(Silence);
                        BufferCombatSilence = false;
                    }
                }
            }

            if (MusicMode == MusicStyle.Boss)
            {
                float calmInterpolant = CalmVolumeInterpolant - (1f / 60f);

                CalmVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, calmInterpolant), 0f, 1f);

                if (BufferCalmSilence)
                {
                    if (CalmMusic.Volume <= 0)
                    {
                        SetCalm(Silence);
                        BufferCalmSilence = false;
                    }
                }

                if (ActiveBossTheme.startFlag)
                {
                    float currentTime = Main.GlobalTimeWrappedHourly;
                    if (currentTime < BossIntroPreviousGlobalTime)
                        currentTime += 3600;
                    float difference = currentTime - BossIntroPreviousGlobalTime;

                    if (Main.hasFocus && !Main.gamePaused)
                    {
                        BossIntroProgress += difference;
                        if (BossIntroProgress + difference >= BossIntroDuration)
                        {
                            SetCombat(ActiveBossTheme.BattleTrack);
                            ActiveBossTheme.startFlag = false;
                        }
                    }

                    BossIntroPreviousGlobalTime = currentTime;
                }
                if (CombatMusic != null && !CombatMusic.IsDisposed && CombatMusic.State != SoundState.Stopped)
                {
                    float combatInterpolant = CombatVolumeInterpolant + (1f / 60f);

                    CombatVolumeInterpolant = MathHelper.Clamp(MathHelper.Lerp(0, 1f, combatInterpolant), 0f, 1f);

                    if (ActiveBossTheme.endFlag)
                    {
                        SetCombat(ActiveBossTheme.EndTrack, false);
                        ActiveBossTheme.endFlag = false;
                        ActiveBossTheme.startFlag = false;
                    }
                }
                else
                {
                    if (ActiveBossTheme.startFlag)
                    {
                        SetCombat(ActiveBossTheme.BattleTrack);
                        ActiveBossTheme.startFlag = false;
                    }
                    else
                    {
                        SetCombat(Silence);
                        CombatVolumeInterpolant = 0;
                        SetMusicMode(MusicStyle.Silent);
                    }   
                }
            }

            float musicFade = Main.musicFade[BlankMusicSlotId];
            CalmMusic.Volume = CalmVolumeInterpolant * Main.musicVolume * CalmVolumeLevel * musicFade;
            CombatMusic.Volume = CombatVolumeInterpolant * Main.musicVolume * CombatVolumeLevel * musicFade;
        }
        public override void PreSaveAndQuit()
        {
            ClearMusic();
        }
        public override void ClearWorld()
        {
            ClearMusic();
        }
        public static void ClearMusic()
        {
            Initialized = false;
            if (CalmMusic != null)
                CalmMusic.Dispose();
            if (CombatMusic != null)
                CombatMusic.Dispose();
        }
    }
    public class BossTheme
    {
        public string BattleTrack;
        public string EndTrack;
        public string StartTrack;
        public bool endFlag = false;
        public bool startFlag = true;
        public float Volume;
        public BossTheme(string battleTrack, string startTrack, string endTrack, float volume)
        {
            BattleTrack = battleTrack;
            StartTrack = startTrack;
            EndTrack = endTrack;
            Volume = volume;
        }
        public BossTheme(BossTheme bossTheme)
        {
            BattleTrack = bossTheme.BattleTrack;
            StartTrack = bossTheme.StartTrack;
            EndTrack = bossTheme.EndTrack;
            Volume = bossTheme.Volume;
        }
    }
    public class FloorSoundtrack
    {
        public string CalmTrack;
        public string CombatTrack;
        public float Volume;
        public FloorSoundtrack(string calmTrack, string combatTrack, float volume)
        {
            CalmTrack = calmTrack;
            CombatTrack = combatTrack;
            Volume = volume;
        }
        public FloorSoundtrack (FloorSoundtrack floorSoundtrack)
        {
            CalmTrack = floorSoundtrack.CalmTrack;
            CombatTrack = floorSoundtrack.CombatTrack;
            Volume = floorSoundtrack.Volume;
        }
    }
}

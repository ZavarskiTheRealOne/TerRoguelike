﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerRoguelike.Rooms;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using TerRoguelike.Schematics;
using TerRoguelike.Managers;
using TerRoguelike.Systems;
using TerRoguelike.Items;
using TerRoguelike.Items.Common;
using TerRoguelike.Items.Uncommon;
using TerRoguelike.Items.Rare;
using Terraria.ModLoader.Core;
using TerRoguelike.NPCs.Enemy;
using TerRoguelike.NPCs;
using static TerRoguelike.Schematics.SchematicManager;

namespace TerRoguelike.Managers
{
    public class NPCManager
    {
        internal static void Load()
        {
            AllNPCs = new List<BaseRoguelikeNPC>()
            {
                new Splinter(),
                new Spookrow(),
                new UndeadGuard()
            };
        }
        internal static void Unload()
        {
            AllNPCs = null;
        }
        public static List<BaseRoguelikeNPC> AllNPCs;
        /// <summary>
        /// Choose a random enemy that has an associated floor ID and combat style.
        /// </summary>
        /// <param name="floorID"></param> 
        /// <param name="combatStyle"></param>
        /// <returns>Matching enemy. if no matching combat style, chooses any style. if absolutely no options, returns NPCID.None</returns>
        public static int ChooseEnemy(int floorID, int combatStyle)
        {
            List<BaseRoguelikeNPC> enemyPool = AllNPCs.FindAll(x => x.associatedFloors.Contains(floorID) && x.CombatStyle == combatStyle);
            if (!enemyPool.Any())
            {
                enemyPool = AllNPCs.FindAll(x => x.associatedFloors.Contains(floorID) && x.CombatStyle >= 0);
            }
            if (enemyPool.Any())
            {
                int randIndex = Main.rand.Next(enemyPool.Count);
                return enemyPool[randIndex].modNPCID;
            }
            return 0;
        }
    }
}
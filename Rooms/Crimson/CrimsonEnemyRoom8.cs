﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerRoguelike.Managers;
using TerRoguelike.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using static TerRoguelike.Managers.NPCManager;
using static TerRoguelike.Schematics.SchematicManager;
using Terraria.ModLoader;
using TerRoguelike.NPCs.Enemy;

namespace TerRoguelike.Rooms
{
    public class CrimsonEnemyRoom8 : Room
    {
        public override int AssociatedFloor => FloorDict["Crimson"];
        public override string Key => "CrimsonEnemyRoom8";
        public override string Filename => "Schematics/RoomSchematics/CrimsonEnemyRoom8.csch";
        public override bool CanExitRight => true;
        public override bool CanExitDown => true;
        public override bool CanExitUp => true;
        public override void InitializeRoom()
        {
            base.InitializeRoom();
            AddRoomNPC(MakeEnemySpawnPos(BottomRight, -3, -3), ChooseEnemy(AssociatedFloor, 0), 60, 120, 0.45f, 0);
            AddRoomNPC(MakeEnemySpawnPos(Bottom, -1, -8, 12), ChooseEnemy(AssociatedFloor, 1), 60, 120, 0.45f, 0);
            AddRoomNPC(MakeEnemySpawnPos(Center, -1, -1, 12), ChooseEnemy(AssociatedFloor, 0), 60, 120, 0.45f, 0);
            AddRoomNPC(MakeEnemySpawnPos(TopLeft, 6, 6), ModContent.NPCType<Crimator>(), 60, 120, 0.45f, 0);

            AddRoomNPC(MakeEnemySpawnPos(Left, 4, 4), ChooseEnemy(AssociatedFloor, 2), 60, 120, 0.45f, 1);
            AddRoomNPC(MakeEnemySpawnPos(Right, -5, -3), ChooseEnemy(AssociatedFloor, 2), 60, 120, 0.45f, 1);
        }
    }
}
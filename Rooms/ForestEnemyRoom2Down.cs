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

namespace TerRoguelike.Rooms
{
    public class ForestEnemyRoom2Down : Room
    {
        public override int ID => 35;
        public override string Key => "ForestEnemyRoom2Down";
        public override string Filename => "Schematics/RoomSchematics/ForestEnemyRoom2Down.csch";
        public override bool CanExitRight => true;
        public override bool CanExitDown => true;
        public override void InitializeRoom()
        {
            base.InitializeRoom();
            AddRoomNPC(0, new Vector2(RoomDimensions.X * 8f, RoomDimensions.Y * 8f), NPCID.Splinterling, 60, 120, 0.45f);

        }
    }
}

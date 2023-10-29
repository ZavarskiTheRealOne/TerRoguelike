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
    public class CorruptBossRoom1 : Room
    {
        public override int AssociatedFloor => 3;
        public override string Key => "CorruptBossRoom1";
        public override string Filename => "Schematics/RoomSchematics/CorruptBossRoom1.csch";
        public override bool IsBossRoom => true;
        public override void InitializeRoom()
        {
            base.InitializeRoom();
            AddRoomNPC(new Vector2(RoomDimensions.X * 8f, (RoomDimensions.Y * 16f) - 120f), NPCID.BigMimicCorruption, 60, 120, 0.9f);
        }
    }
}
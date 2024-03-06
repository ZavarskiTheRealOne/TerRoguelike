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

namespace TerRoguelike.Rooms
{
    public class BaseBossRoom1 : Room
    {
        public override int AssociatedFloor => FloorDict["Base"];
        public override string Key => "BaseBossRoom1";
        public override string Filename => "Schematics/RoomSchematics/BaseBossRoom1.csch";
        public override bool IsBossRoom => true;
        public override void InitializeRoom()
        {
            base.InitializeRoom();
            AddRoomNPC(new Vector2(RoomDimensions.X * 8f, (RoomDimensions.Y * 16f) - 72f), NPCID.Paladin, 60, 120, 0.9f);
        }
    }
}

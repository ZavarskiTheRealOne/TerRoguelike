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
    public class BaseEnemyRoom4Down : Room
    {
        public override string Key => "BaseEnemyRoom4Down";
        public override string Filename => "Schematics/RoomSchematics/BaseEnemyRoom4Down.csch";
        public override bool CanExitRight => true;
        public override bool CanExitDown => true;
        public override void InitializeRoom()
        {
            base.InitializeRoom();
            AddRoomNPC(new Vector2(48f, 64f), NPCID.MartianTurret, 60, 30, 0.45f);
            AddRoomNPC(new Vector2((RoomDimensions.X * 16f) - 48f, 64f), NPCID.MartianTurret, 60, 30, 0.45f);
            AddRoomNPC(new Vector2(48f, (RoomDimensions.Y * 16f) / 2f), NPCID.MartianTurret, 60, 30, 0.45f);
            AddRoomNPC(new Vector2((RoomDimensions.X * 16f) - 48f, (RoomDimensions.Y * 16f) / 2f), NPCID.MartianTurret, 60, 30, 0.45f);
        }
    }
}
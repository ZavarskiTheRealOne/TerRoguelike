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
using static TerRoguelike.Schematics.SchematicManager;

namespace TerRoguelike.Rooms
{
    public class CrimsonStartRoom : Room
    {
        public override int AssociatedFloor => FloorDict["Crimson"];
        public override string Key => "CrimsonStartRoom";
        public override string Filename => "Schematics/RoomSchematics/CrimsonStartRoom.csch";
        public override bool CanExitRight => true;
        public override bool CanExitDown => true;
        public override bool CanExitUp => true;
        public override bool IsStartRoom => true;
        public override void InitializeRoom()
        {
            active = false;
        }
        public override void Update()
        {
            active = false;
        }
    }
}

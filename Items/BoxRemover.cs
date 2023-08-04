using Terraria.ModLoader;
using Terraria.ID;
using System;
using Terraria;
using Terraria.Audio;
using System.Collections.Generic;

namespace Boxes.Items
{
   public class BoxRemover: ModItem
   {
      public override void SetDefaults()
      {
         Item.width = 20;
         Item.height = 20;
         Item.maxStack = 1;
         Item.value = 0;
         Item.rare = ItemRarityID.Red;
         Item.consumable = false;
         Item.useStyle = ItemUseStyleID.HoldUp;
         Item.useTime = 20;
         Item.useAnimation = 15;
         Item.autoReuse = false;
      }

      public override bool? UseItem(Player player)
      {
         var gridSystem = ModContent.GetInstance<BoxesSystem>();
         var checkedPos = BoxesSystem.getChoosenGrid(Player.tileTargetX, Player.tileTargetY);
         if (gridSystem.unlockedCells.Contains(checkedPos))
         {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
               gridSystem.unlockedCells.Remove(checkedPos);
               SoundEngine.PlaySound(SoundID.Item14, player.position);
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
               var packet = ModContent.GetInstance<Boxes>().GetPacket();
               packet.Write((byte)Packet.OnDestroyBox);
               packet.Write((int)checkedPos.Item1);
               packet.Write((int)checkedPos.Item2);
               packet.Write((int)player.position.X);
               packet.Write((int)player.position.Y);
               packet.Send();
            }
         }
         return true;
      }
   }
}

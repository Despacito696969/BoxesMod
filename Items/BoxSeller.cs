using Terraria.ModLoader;
using Terraria.ID;
using System;
using Terraria;
using Terraria.Audio;
using System.Collections.Generic;

namespace Boxes.Items
{
   public class BoxSeller : ModItem
   {
      public override void SetDefaults()
      {
         Item.width = 20;
         Item.height = 20;
         Item.maxStack = 1;
         Item.value = 0;
         Item.rare = ItemRarityID.Blue;
         Item.consumable = false;
         Item.useStyle = ItemUseStyleID.HoldUp;
         Item.useTime = 20;
         Item.useAnimation = 15;
         Item.autoReuse = false;
      }
      public override void ModifyTooltips(List<TooltipLine> tooltips)
      {
         var mod = ModContent.GetInstance<Boxes>();
         if (tooltips[tooltips.Count - 1].Name == "CostLine")
         {
            tooltips.RemoveAt(tooltips.Count - 1);
         }
         var boxesSystem = ModContent.GetInstance<BoxesSystem>();
         tooltips.Add(new TooltipLine(mod, "CostLine", "Next box will cost: " + boxesSystem.getCostString()));
      }


      public override void AddRecipes()
      {
         CreateRecipe().Register();
      }

      public override bool? UseItem(Player player)
      {
         var gridSystem = ModContent.GetInstance<BoxesSystem>();
         var checkedPos = BoxesSystem.getChoosenGrid(Player.tileTargetX, Player.tileTargetY);
         if (!gridSystem.unlockedCells.Contains(checkedPos) && player.CanAfford(gridSystem.getCost()))
         {
            if (!gridSystem.isBoxBuyable(checkedPos.Item1, checkedPos.Item2))
            {
               return true;
            }
            player.BuyItem(gridSystem.getCost());
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
               gridSystem.unlockedCells.Add(checkedPos);
               SoundEngine.PlaySound(SoundID.Item4, player.position);
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
               var packet = ModContent.GetInstance<Boxes>().GetPacket();
               packet.Write((byte)Packet.OnCreateBox);
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

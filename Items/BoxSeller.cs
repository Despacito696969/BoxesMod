using Terraria.ModLoader;
using Terraria.ID;
using System;
using Terraria;
using Terraria.Audio;

namespace Boxes.Items
{
	public class BoxSeller : ModItem
	{
      public override void SetStaticDefaults()
      {
			DisplayName.SetDefault("Box Buying Interface");
         Tooltip.SetDefault("Allows to buy boxes");
      }
      public override void SetDefaults()
      {
         Item.width = 20;
         Item.height = 20;
         Item.maxStack = 1;
         Item.value = 0;
         Item.rare = ItemRarityID.Blue;
         Item.consumable = false;
         Item.useStyle = ItemUseStyleID.HoldUp;
         Item.useTime = 0;
      }
      public override void AddRecipes()
      {
         CreateRecipe().Register();
      }
      public Tuple<int, int> getChoosenGrid()
      {
         var x = Player.tileTargetX;
         var y = Player.tileTargetY;

         var gridSystem = ModContent.GetInstance<BoxesSystem>();
         x -= gridSystem.baseCellCornerX;
         y -= gridSystem.baseCellCornerY;
         int cell_x = (int)Math.Floor((float)x / (float)gridSystem.cellWidth);
         int cell_y = (int)Math.Floor((float)y / (float)gridSystem.cellHeight);
         return new Tuple<int, int>(cell_x, cell_y);
      }
      public override bool? UseItem(Player player)
      {
         var gridSystem = ModContent.GetInstance<BoxesSystem>();
         var checkedPos = getChoosenGrid();
         if (!gridSystem.unlockedCells.Contains(checkedPos) && player.CanBuyItem(gridSystem.getCost()))
         {
            if (
               !gridSystem.unlockedCells.Contains(new Tuple<int, int>(checkedPos.Item1 - 1, checkedPos.Item2)) &&
               !gridSystem.unlockedCells.Contains(new Tuple<int, int>(checkedPos.Item1 + 1, checkedPos.Item2)) &&
               !gridSystem.unlockedCells.Contains(new Tuple<int, int>(checkedPos.Item1, checkedPos.Item2 - 1)) &&
               !gridSystem.unlockedCells.Contains(new Tuple<int, int>(checkedPos.Item1, checkedPos.Item2 + 1)))
            {
               return null;
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
               packet.Write((byte)Packet.OnItemUseSync);
               packet.Write((int)checkedPos.Item1);
               packet.Write((int)checkedPos.Item2);
               packet.Write((int)player.position.X);
               packet.Write((int)player.position.Y);
               packet.Send();
            }
         }
         return null;
      }
   }
}

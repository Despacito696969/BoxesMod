using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using Terraria;
using Terraria.GameContent;
using System.Collections.Generic;
using System;
using MonoMod.Cil;
using Terraria.ID;
using System.Text;

namespace Boxes
{
	public class BoxesSystem : ModSystem
	{
      public HashSet<Tuple<int, int>> unlockedCells = new HashSet<Tuple<int, int>>();
      public const string WORLD_CELLS = "WorldCells";

      GraphicsDevice device;

      public override void Load()
      {
         if (Main.dedServ) { return; }
         Terraria.IL_Main.DrawInfernoRings += DrawBordersILEdit;
         device = Main.instance.GraphicsDevice;
      }

      private void DrawBordersILEdit(ILContext il)
      {
         if (Main.dedServ) { return; }
         var c = new ILCursor(il);
         c.EmitDelegate<Action>(() =>
         {
            ModContent.GetInstance<BoxesSystem>().DrawBorders();
         });
      }

      // TODO I'm not sure if I should memoize the instance
      public int cellWidth
      {
         get => ModContent.GetInstance<BoxesConfig>().BoxWidth;
      }

      public int cellHeight
      {
         get => ModContent.GetInstance<BoxesConfig>().BoxHeight;
      }

      // Those are coordinates of top left corner of (0, 0) Cell
      public int baseCellCornerX
      {
         get => Main.spawnTileX - cellWidth / 2;
      }

      public int baseCellCornerY
      {
         get => Main.spawnTileY - 2 - cellHeight / 2;
      }

      // Calculates cost of next cell in copper coins
      public int getCost()
      {
         var Config = ModContent.GetInstance<BoxesConfig>();
         return (((unlockedCells.Count - 1) / Config.boxesPerIncrease) * Config.costIncreaseSilver + Config.costSilver) * 100;
      }

      // Translates position to cell identificator
      public Tuple<int, int> getCell(Vector2 pos)
      {
         var centerOfGrid = new Vector2(
               (float)baseCellCornerX, (float)baseCellCornerY
         );
         centerOfGrid *= 16.0f;
         pos = pos - centerOfGrid;

         pos /= 16.0f;
         pos.X /= (float)(cellWidth);
         pos.Y /= (float)(cellHeight);

         return new Tuple<int, int>(
               (int)Math.Floor(pos.X),
               (int)Math.Floor(pos.Y));
      }

      public bool hasBox(int x, int y) 
      {
         return unlockedCells.Contains(new Tuple<int, int>(x, y));
      }

      public bool isBoxBuyable(int x, int y)
      {
         return !(!hasBox(x, y) &&
                  !hasBox(x - 1, y) &&
                  !hasBox(x + 1, y) &&
                  !hasBox(x, y - 1) &&
                  !hasBox(x, y + 1));
      }

      public static Tuple<int, int> getChoosenGrid(int tileX, int tileY)
      {
         var x = tileX;
         var y = tileY;

         var gridSystem = ModContent.GetInstance<BoxesSystem>();
         x -= gridSystem.baseCellCornerX;
         y -= gridSystem.baseCellCornerY;
         int cell_x = (int)Math.Floor((float)x / (float)gridSystem.cellWidth);
         int cell_y = (int)Math.Floor((float)y / (float)gridSystem.cellHeight);
         return new Tuple<int, int>(cell_x, cell_y);
      }

      public void DrawBorders()
      {
         var screenPos = Main.screenPosition - new Vector2((float)baseCellCornerX, (float)baseCellCornerY) * 16.0f;
         int x_min = (int)Math.Floor((float)screenPos.X / (float)(cellWidth * 16));
         int y_min = (int)Math.Floor((float)screenPos.Y / (float)(cellHeight * 16));
         int x_max = (int)Math.Floor((float)(screenPos.X + Main.screenWidth) / (float)(cellWidth * 16));
         int y_max = (int)Math.Floor((float)(screenPos.Y + Main.screenHeight) / (float)(cellHeight * 16));

         if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<Items.BoxSeller>()) {
            var grid = BoxesSystem.getChoosenGrid(Player.tileTargetX, Player.tileTargetY);
            var g_x = grid.Item1;
            var g_y = grid.Item2;
            if (!hasBox(g_x, g_y) &&
               (hasBox(g_x, g_y + 1) || hasBox(g_x, g_y - 1)
               || hasBox(g_x + 1, g_y) || hasBox(g_x - 1, g_y))) { 

               var pos = new Vector2(
                     (float)(baseCellCornerX + g_x * cellWidth), 
                     (float)(baseCellCornerY + g_y * cellHeight));

               pos = pos * 16.0f - Main.screenPosition;

               var pos_x = (int)pos.X;
               var pos_y = (int)pos.Y;
               var width = cellWidth * 16;
               var height = cellHeight * 16;

               Main.spriteBatch.Draw(
                     TextureAssets.MagicPixel.Value, 
                     new Rectangle(pos_x, pos_y, width, height), null, 
                     Color.Lerp(Color.Transparent, Color.Green, 0.2f));
            }
         }
         for (int x = x_min; x <= x_max; ++x)
         {
            for (int y = y_min; y <= y_max; ++y)
            {
               int box_x = x;
               int box_y = y;

               if (hasBox(x, y))
               {
                  continue;
               }

               var pos = new Vector2(
                     (float)(baseCellCornerX + box_x * cellWidth), 
                     (float)(baseCellCornerY + box_y * cellHeight));
               pos = pos * 16.0f - Main.screenPosition;

               Color color = Color.Lerp(Color.Transparent, Color.Blue, 0.2f);
               // TODO This could look better
               if (hasBox(x - 1, y))
               {
                  var pos_x = (int)pos.X;
                  var pos_y = (int)pos.Y;
                  var width = 8;
                  var height = cellHeight * 16;

                  Main.spriteBatch.Draw(
                        TextureAssets.MagicPixel.Value, 
                        new Rectangle(pos_x, pos_y, width, height), null, color);
               }
               if (hasBox(x + 1, y))
               {
                  var pos_x = (int)pos.X + cellWidth * 16 - 8;
                  var pos_y = (int)pos.Y;
                  var width = 8;
                  var height = cellHeight * 16;

                  Main.spriteBatch.Draw(
                        TextureAssets.MagicPixel.Value, 
                        new Rectangle(pos_x, pos_y, width, height), null, color);
               }
               if (hasBox(x, y - 1))
               {
                  var pos_x = (int)pos.X;
                  var pos_y = (int)pos.Y;
                  var width = cellWidth * 16;
                  var height = 8;

                  if (hasBox(x - 1, y - 1) && hasBox(x - 1, y))
                  {
                     pos_x += 8;
                     width -= 8;
                  }

                  if (hasBox(x + 1, y - 1) && hasBox(x + 1, y))
                  {
                     width -= 8;
                  }

                  Main.spriteBatch.Draw(
                        TextureAssets.MagicPixel.Value, 
                        new Rectangle(pos_x, pos_y, width, height), null, color);
               }
               if (hasBox(x, y + 1))
               {
                  var pos_x = (int)pos.X;
                  var pos_y = (int)pos.Y + cellHeight * 16 - 8;
                  var width = cellWidth * 16;
                  var height = 8;

                  if (hasBox(x - 1, y + 1) && hasBox(x - 1, y))
                  {
                     pos_x += 8;
                     width -= 8;
                  }

                  if (hasBox(x + 1, y + 1) && hasBox(x + 1, y))
                  {
                     width -= 8;
                  }

                  Main.spriteBatch.Draw(
                        TextureAssets.MagicPixel.Value, 
                        new Rectangle(pos_x, pos_y, width, height), null, color);
               }
            }
         }
      }

      public string getCostString()
      { 
         int cost = getCost();
         int silver = ((cost / 100) % 100);
         int gold = ((cost / 100 / 100) % 100);
         int platinum = (cost / 100 / 100 / 100);
         StringBuilder sb = new StringBuilder();

         Action<int, int> addCoin = (value, id) =>
         {
            if (value == 0)
            {
               return;
            }
            sb.Append("[i/s");
            sb.Append(value);
            sb.Append(':');
            sb.Append(id);
            sb.Append(']');
         };

         addCoin(platinum, ItemID.PlatinumCoin);
         addCoin(gold, ItemID.GoldCoin);
         addCoin(silver, ItemID.SilverCoin);
         return sb.ToString();
      }

      public override void PostDrawInterface(SpriteBatch batch)
      {
         if (Main.dedServ) { return; }
         if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<Items.BoxSeller>())
         {
            int cost = getCost();
            float lineDist = 30.0f;
            float linePos = lineDist;

            var gridSystem = ModContent.GetInstance<BoxesSystem>();
            var checkedPos = BoxesSystem.getChoosenGrid(Player.tileTargetX, Player.tileTargetY);
            if (gridSystem.unlockedCells.Contains(checkedPos)) {
               Utils.DrawBorderString(
                     batch,
                     "Select a box",
                     new Vector2((float)Main.mouseX, (float)Main.mouseY - linePos),
                     Color.Linen);
               return;
            }

            bool cantAfford = !Main.LocalPlayer.CanAfford(cost);

            if (gridSystem.isBoxBuyable(checkedPos.Item1, checkedPos.Item2))
            {
               if (cantAfford)
               {
                  Utils.DrawBorderString(
                        batch,
                        "You need: " + getCostString(),
                        new Vector2((float)Main.mouseX, (float)Main.mouseY - linePos),
                        Color.Red);
                  linePos += lineDist;
               }
               else
               {
                  Utils.DrawBorderString(
                        batch,
                        "Buy: " + getCostString(),
                        new Vector2((float)Main.mouseX, (float)Main.mouseY - linePos),
                        Color.White);
                  linePos += lineDist;
               }
            }
            else
            { 
               Utils.DrawBorderString(
                     batch,
                     "You can only buy neighbouring boxes",
                     new Vector2((float)Main.mouseX, (float)Main.mouseY - linePos),
                     Color.Red);
               linePos += lineDist;
            }

         }
      }

      public void setDefaultCells()
      { 
         unlockedCells = new HashSet<Tuple<int, int>>();
         unlockedCells.Add(new Tuple<int, int>(0, 0));
      }

      public override void OnWorldLoad()
      {
         setDefaultCells();
         if (Main.netMode == NetmodeID.MultiplayerClient)
         {
            var packet = ModContent.GetInstance<Boxes>().GetPacket();
            packet.Write((byte)Packet.OnJoinSync);
            packet.Send();
         }
      }

      public override void LoadWorldData(TagCompound tag)
      {
         if (tag.ContainsKey(WORLD_CELLS))
         {
            var list = tag.GetList<Tuple<int, int>>(WORLD_CELLS);
            foreach (var element in list)
            {
               unlockedCells.Add(element);
            }
         }
      }

      public override void OnWorldUnload()
      {
         setDefaultCells();
      }

      public override void SaveWorldData(TagCompound tag)
      {  
         var list = new List<Tuple<int, int>>();
         foreach (var element in unlockedCells)
         {
            list.Add(element);
         }
         tag.Add(WORLD_CELLS, list);
      }

      public class PointSerializer : TagSerializer<Tuple<int, int>, TagCompound>
      {
         public override TagCompound Serialize(Tuple<int, int> point) => new TagCompound {
            ["x"] = point.Item1,
            ["y"] = point.Item2,
         };

         public override Tuple<int, int> Deserialize(TagCompound tag) => new Tuple<int, int>(
               tag.GetInt("x"), tag.GetInt("y")
         );
      }
	}
}

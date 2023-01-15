using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using Terraria;
using Terraria.GameContent;
using System.Collections.Generic;
using System;

namespace Boxes
{
	public class BoxesSystem : ModSystem
	{
      public HashSet<Tuple<int, int>> unlockedCells = new HashSet<Tuple<int, int>>();
      public const string WORLD_CELLS = "WorldCells";

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
      public int GetCost()
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


      public override void PostDrawTiles()
      {
         Main.spriteBatch.Begin(
               SpriteSortMode.Deferred, 
               BlendState.AlphaBlend,
               Main.DefaultSamplerState, 
               DepthStencilState.None, 
               Main.Rasterizer, 
               (Effect)null,
               Main.Transform
         );
         
         var screenPos = Main.screenPosition - new Vector2((float)baseCellCornerX, (float)baseCellCornerY) * 16.0f;
         int x_min = (int)Math.Floor((float)screenPos.X / (float)(cellWidth * 16));
         int y_min = (int)Math.Floor((float)screenPos.Y / (float)(cellHeight * 16));
         int x_max = (int)Math.Floor((float)(screenPos.X + Main.screenWidth) / (float)(cellWidth * 16));
         int y_max = (int)Math.Floor((float)(screenPos.Y + Main.screenHeight) / (float)(cellHeight * 16));
         for (int x = x_min; x <= x_max; ++x)
         {
            for (int y = y_min; y <= y_max; ++y)
            {
               int box_x = x;
               int box_y = y;

               if (unlockedCells.Contains(new Tuple<int, int>(x, y)))
               {
                  continue;
               }

               var pos = new Vector2(
                     (float)(baseCellCornerX + box_x * cellWidth), 
                     (float)(baseCellCornerY + box_y * cellHeight));
               pos = pos * 16.0f - Main.screenPosition;

               // Unfortunately we would want to draw the border 
               // after drawing liquids, but there isn't a hook to do so.
               // This results in seeing liquids if `old` == true
               bool old = false;
               if (old)
               {
                  Main.spriteBatch.Draw(
                        TextureAssets.MagicPixel.Value, 
                        new Rectangle(
                           (int)pos.X, (int)pos.Y, 
                           cellWidth * 16, cellHeight * 16), 
                        null, 
                        Color.Lerp(Color.Transparent, Color.Blue, 0.3f));
               }
               else
               {
                  // TODO This could look better
                  if (unlockedCells.Contains(new Tuple<int, int>(x - 1, y)))
                  {
                     Main.spriteBatch.Draw(
                           TextureAssets.MagicPixel.Value, 
                           new Rectangle(
                              (int)pos.X, (int)pos.Y, 
                              8, cellHeight * 16), 
                           null, 
                           Color.Lerp(Color.Transparent, Color.Blue, 0.3f));
                  }
                  if (unlockedCells.Contains(new Tuple<int, int>(x + 1, y)))
                  {
                     Main.spriteBatch.Draw(
                           TextureAssets.MagicPixel.Value, 
                           new Rectangle(
                              (int)pos.X + cellWidth * 16 - 8, (int)pos.Y, 
                              8, cellHeight * 16), 
                           null, 
                           Color.Lerp(Color.Transparent, Color.Blue, 0.3f));
                  }
                  if (unlockedCells.Contains(new Tuple<int, int>(x, y - 1)))
                  {
                     Main.spriteBatch.Draw(
                           TextureAssets.MagicPixel.Value, 
                           new Rectangle(
                              (int)pos.X, (int)pos.Y, 
                              cellWidth * 16, 8), 
                           null, 
                           Color.Lerp(Color.Transparent, Color.Blue, 0.3f));
                  }
                  if (unlockedCells.Contains(new Tuple<int, int>(x, y + 1)))
                  {
                     Main.spriteBatch.Draw(
                           TextureAssets.MagicPixel.Value, 
                           new Rectangle(
                              (int)pos.X, (int)pos.Y + cellHeight * 16 - 8,
                              cellWidth * 16, 8), 
                           null, 
                           Color.Lerp(Color.Transparent, Color.Blue, 0.3f));
                  }
               }
            }
         }
         Main.spriteBatch.End();
      }

      public override void PostDrawInterface(SpriteBatch batch)
      {
         if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<Items.Token>())
         {
            int cost = GetCost();
            Color platColor = Color.White;
            Color goldColor = Color.Gold;
            Color silverColor = Color.Gray;
            float lineDist = 30.0f;
            float linePos = lineDist;
            bool cantAfford = !Main.LocalPlayer.CanBuyItem(cost);
            if (cantAfford)
            {
               platColor = Color.Red;
               goldColor = Color.Red;
               silverColor = Color.Red;

            }
            int to_display;
            to_display = ((cost / 100) % 100);
            if (to_display > 0)
            {
               Utils.DrawBorderString(
                     batch,
                     to_display.ToString() + " Silver",
                     new Vector2((float)Main.mouseX, (float)Main.mouseY - linePos),
                     silverColor);
               linePos += lineDist;
            }

            to_display = ((cost / 100 / 100) % 100);
            if (to_display > 0)
            {
               Utils.DrawBorderString(
                     batch,
                     ((cost / 100 / 100) % 100).ToString() + " Gold",
                     new Vector2((float)Main.mouseX, (float)Main.mouseY - linePos),
                     goldColor);
               linePos += lineDist;
            }

            to_display = (cost / 100 / 100 / 100);
            if (to_display > 0)
            {
               Utils.DrawBorderString(
                     batch,
                     to_display.ToString() + " Platinum",
                     new Vector2((float)Main.mouseX, (float)Main.mouseY - linePos),
                     platColor);
               linePos += lineDist;
            }

            if (cantAfford)
            {
               Utils.DrawBorderString(
                     batch,
                     "You need:",
                     new Vector2((float)Main.mouseX, (float)Main.mouseY - linePos),
                     Color.Red);
            }
         }
      }

      public override void OnWorldLoad()
      {
         unlockedCells = new HashSet<Tuple<int, int>>();
         unlockedCells.Add(new Tuple<int, int>(0, 0));
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
      public override void SaveWorldData(TagCompound tag)
      {
         //var Logger = ModContent.GetInstance<Boxes>().Logger;
         //Logger.Info("Started saving!");
         var list = new List<Tuple<int, int>>();
         //Logger.Info("Made a list");
         foreach (var element in unlockedCells)
         {
            //Logger.InfoFormat("Saved {0}", element);
            list.Add(element);
         }
         //Logger.Info("Completed the list");
         tag.Add(WORLD_CELLS, list);
         //Logger.Info("Saved the list");
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

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
         IL.Terraria.Main.DrawInfernoRings += DrawBordersILEdit;
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

      public void DrawBorders()
      {
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

      // This doesn't work (for now atleast)
      public void PostDrawTilesNewProbably()
      {
         List<VertexPositionColor> verts_list = new List<VertexPositionColor>();
         List<int> indices_list = new List<int>();
         
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

               VertexPositionColor[] verts = new VertexPositionColor[4];

               verts[0].Position = new Vector3((float)pos.X, (float)pos.Y, (float)0);
               verts[1].Position = verts[0].Position;
               verts[1].Position.X += (float)cellWidth * 16.0f;

               verts[2].Position = verts[1].Position;
               verts[2].Position.Y += cellHeight * 16.0f;
               verts[3].Position = verts[0].Position;
               verts[3].Position.Y += cellHeight * 16.0f;

               verts[0].Color = new Color(1, 0, 0, 1);
               verts[1].Color = new Color(0, 1, 0, 1);
               verts[2].Color = new Color(0, 0, 1, 1);
               verts[3].Color = new Color(0, 1, 1, 1);

               int[] indices = new int[] { 1, 0, 2, 3, 2, 0 };
               foreach (var index in indices)
               {
                  indices_list.Add(index + verts_list.Count);
               }
               verts_list.AddRange(verts);
            }
         }
         device.DrawUserIndexedPrimitives<VertexPositionColor>(
            PrimitiveType.TriangleList,
            verts_list.ToArray(), 0, verts_list.Count,
            indices_list.ToArray(), 0, indices_list.Count);
      }

      public override void PostDrawInterface(SpriteBatch batch)
      {
         if (Main.dedServ) { return; }
         if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<Items.BoxSeller>())
         {
            int cost = getCost();
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

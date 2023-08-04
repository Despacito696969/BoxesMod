using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ID;
using System;
using Terraria;
using MonoMod.Cil;

namespace Boxes
{
	public class BoxesModPlayer : ModPlayer
	{
      private bool MoveIntoCell1D(
            int int_cell, 
            int base_cell_corner, 
            int cell_size, 
            ref float pos, 
            ref float velocity, 
            int size)
      {
         var center = (float)(base_cell_corner * 16);
         var cell = (float)(int_cell * 16);
         cell *= (float)cell_size;
         float gridPos = pos - center;
         float too_much = (gridPos + (float)size) - (cell + (float)(cell_size * 16));
         float too_little = cell - gridPos;
         bool result = false;
         const float EPS = 0.1f; 
         const float EPS2 = 0.05f; 
         if (too_much > -EPS2)
         {
            pos -= too_much + EPS;
            velocity = 0.0f;
            result = true;
         }
         else if (too_little > -EPS2)
         {
            pos += too_little + EPS;
            velocity = 0.0f;
            result = true;
         }
         return result;

      }
      private bool MoveIntoCell(
            int int_cell_x, 
            int int_cell_y, 
            BoxesSystem gridSystem, 
            ref Vector2 pos, 
            ref Vector2 velocity)
      {
         bool result = false;
         result = result || MoveIntoCell1D(
               int_cell_x, 
               gridSystem.baseCellCornerX, 
               gridSystem.cellWidth, 
               ref pos.X, 
               ref velocity.X, 
               Player.width);

         result = result || MoveIntoCell1D(
               int_cell_y, 
               gridSystem.baseCellCornerY, 
               gridSystem.cellHeight, 
               ref pos.Y,
               ref velocity.Y, 
               Player.height);

         return result;
      }

      public Tuple<Tuple<int, int>?, bool> findBoxToPushTo(BoxesSystem gridSystem, Vector2 pos)
      {
         var currentTile = gridSystem.getCell(pos);
         var currentTile2 = gridSystem.getCell(pos + new Vector2((float)Player.width, (float)Player.height));

         Tuple<int, int> goodCell = null;
         bool isInLockedBox = false;
         for (int x = (int)currentTile.Item1; x <= (int)currentTile2.Item1; ++x)
         {
            for (int y = (int)currentTile.Item2; y <= (int)currentTile2.Item2; ++y)
            {
               var checkedPos = new Tuple<int, int>(x, y);
               if (gridSystem.unlockedCells.Contains(checkedPos))
               {
                  goodCell = checkedPos;
               }
               else
               {
                  isInLockedBox = true;
               }
            }
         }
         return new Tuple<Tuple<int, int>, bool>(goodCell, isInLockedBox);
      }

      public Vector2 oldPosition;

      public override void PreUpdate()
      {
          oldPosition = Player.position;
      }

      public void BoxCollision()
      {
         // Previously this was just in PreUpdateMovement, 
         // but it had more issues than this workaround.
         Vector2 delta = Player.position - oldPosition;
         Player.position = oldPosition;

         var gridSystem = ModContent.GetInstance<BoxesSystem>();
         Tuple<int, int>? goodCell;
         bool isInLockedBox;

         {
            var result = findBoxToPushTo(gridSystem, Player.position);
            goodCell = result.Item1;
            isInLockedBox = result.Item2;
         }

         if (isInLockedBox)
         {
            if (goodCell == null)
            {
               if (ModContent.GetInstance<BoxesConfig>().trollThinkingOutOfTheBox)
               {
                  Player.AddBuff(BuffID.Burning, 1);
                  Player.AddBuff(BuffID.Suffocation, 1);
                  Player.AddBuff(BuffID.Cursed, 1);
                  Player.AddBuff(BuffID.OnFire, 1);
                  Player.AddBuff(BuffID.OnFire3, 1);
                  Player.AddBuff(BuffID.Frostburn, 1);
                  Player.AddBuff(BuffID.Frostburn2, 1);
                  Player.AddBuff(BuffID.ShadowFlame, 1);
                  Player.AddBuff(BuffID.Poisoned, 1);
                  Player.AddBuff(BuffID.Venom, 1);
               }
            }
            else
            {
               //MoveIntoCell(goodCell.Item1, goodCell.Item2, gridSystem, ref Player.position, ref Player.velocity);
               if (MoveIntoCell1D(
                     goodCell.Item2, 
                     gridSystem.baseCellCornerY, 
                     gridSystem.cellHeight, 
                     ref Player.position.Y, 
                     ref delta.Y, 
                     Player.height))
               {
                  Player.velocity.Y = 0.0f;
                  Player.gfxOffY = 0.0f;
               }
               if (findBoxToPushTo(gridSystem, Player.position).Item2)
               {
                  if (MoveIntoCell1D(
                        goodCell.Item1, 
                        gridSystem.baseCellCornerX, 
                        gridSystem.cellWidth, 
                        ref Player.position.X, 
                        ref delta.X, 
                        Player.width))
                  {
                     Player.velocity.X = 0.0f;
                  }
               }
               Player.position += delta;
            }
            return;
         }

         var prevBox = gridSystem.getCell(Player.Center);
         var copyPos = Player.position;

         copyPos.X += delta.X;

         if (findBoxToPushTo(gridSystem, copyPos).Item2)
         {
            var resultX = MoveIntoCell1D(
                  prevBox.Item1, 
                  gridSystem.baseCellCornerX, 
                  gridSystem.cellWidth, 
                  ref copyPos.X,
                  ref delta.X, 
                  Player.width);

            if (resultX)
            {
               Player.position.X = copyPos.X;
               Player.velocity.X = 0.0f;
            }
         }

         copyPos.Y += Player.velocity.Y;

         if (findBoxToPushTo(gridSystem, copyPos).Item2)
         {
            var resultY = MoveIntoCell1D(
                  prevBox.Item2, 
                  gridSystem.baseCellCornerY,
                  gridSystem.cellHeight,
                  ref copyPos.Y, 
                  ref delta.Y, 
                  Player.height);

            if (resultY)
            {
               Player.position.Y = copyPos.Y;
               Player.velocity.Y = 0.0f;
               Player.gfxOffY = 0.0f;
            }
         }
         Player.position += delta;
      }

      // This needs to be IL Editted because there is no way to update collision
      // before PlayerFrame and after actual collision
      public override void Load()
      {
         Terraria.IL_Player.BordersMovement += boxUpdateIL;
      }

      private void boxUpdateIL(ILContext il)
      {
         var c = new ILCursor(il);
         c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
         c.EmitDelegate<Action<Player>>((player) =>
         {
            player.GetModPlayer<BoxesModPlayer>().BoxCollision();
         });
      }
   }
}

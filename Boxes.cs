using System.IO;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Terraria;
using Microsoft.Xna.Framework;
using Terraria.Audio;

namespace Boxes
{
   enum Packet : byte
   {
      OnJoinSync,
      OnCreateBox,
      OnDestroyBox,
   }

   public class Boxes : Mod
   {
      public Boxes()
      {
      }

      public override void HandlePacket(BinaryReader reader, int whoAmI)
      {
         var packetType = (Packet)reader.ReadByte();
         if (Terraria.Main.netMode == NetmodeID.MultiplayerClient)
         {
            switch (packetType) 
            {
               case Packet.OnJoinSync:
               {
                  var cells = ModContent.GetInstance<BoxesSystem>().unlockedCells;
                  int length = reader.ReadInt32();
                  for (int i = 0; i < length; ++i)
                  {
                     int x = reader.ReadInt32();
                     int y = reader.ReadInt32();
                     cells.Add(new Tuple<int, int>(x, y));
                  }
               }
               break;

               case Packet.OnCreateBox:
               {
                  var cells = ModContent.GetInstance<BoxesSystem>().unlockedCells;
                  int x = reader.ReadInt32();
                  int y = reader.ReadInt32();
                  int px = reader.ReadInt32();
                  int py = reader.ReadInt32();
                  cells.Add(new Tuple<int, int>(x, y));
                  SoundEngine.PlaySound(SoundID.Item4, new Vector2((float)px, (float)py));
               }
               break;

               case Packet.OnDestroyBox:
               {
                  var cells = ModContent.GetInstance<BoxesSystem>().unlockedCells;
                  int x = reader.ReadInt32();
                  int y = reader.ReadInt32();
                  int px = reader.ReadInt32();
                  int py = reader.ReadInt32();
                  cells.Remove(new Tuple<int, int>(x, y));
                  SoundEngine.PlaySound(SoundID.Item14, new Vector2((float)px, (float)py));
               }
               break;
            }
         }
         if (Terraria.Main.netMode == NetmodeID.Server)
         {
            switch (packetType) 
            {
               case Packet.OnJoinSync:
               {
                  var resultPacket = GetPacket();
                  resultPacket.Write((byte)Packet.OnJoinSync);
                  var cells = ModContent.GetInstance<BoxesSystem>().unlockedCells;
                  resultPacket.Write((int)cells.Count);
                  foreach (var elem in cells)
                  {
                     resultPacket.Write((int)elem.Item1);
                     resultPacket.Write((int)elem.Item2);
                  }
                  resultPacket.Send(whoAmI);
               }
               break;

               case Packet.OnCreateBox:
               {
                  var boxesSystem = ModContent.GetInstance<BoxesSystem>();
                  var cells = boxesSystem.unlockedCells;
                  int x = reader.ReadInt32();
                  int y = reader.ReadInt32();
                  int px = reader.ReadInt32();
                  int py = reader.ReadInt32();
                  if (!boxesSystem.isBoxBuyable(x, y))
                  {
                     break;
                  }
                  cells.Add(new Tuple<int, int>(x, y));
                  var packet = GetPacket();
                  packet.Write((byte)Packet.OnCreateBox);
                  packet.Write((int)x);
                  packet.Write((int)y);
                  packet.Write((int)px);
                  packet.Write((int)py);
                  packet.Send();
               }
               break;

               case Packet.OnDestroyBox:
               {
                  var boxesSystem = ModContent.GetInstance<BoxesSystem>();
                  var cells = boxesSystem.unlockedCells;
                  int x = reader.ReadInt32();
                  int y = reader.ReadInt32();
                  int px = reader.ReadInt32();
                  int py = reader.ReadInt32();
                  if (!boxesSystem.hasBox(x, y))
                  { 
                     break; 
                  }
                  cells.Remove(new Tuple<int, int>(x, y));
                  var packet = GetPacket();
                  packet.Write((byte)Packet.OnDestroyBox);
                  packet.Write((int)x);
                  packet.Write((int)y);
                  packet.Write((int)px);
                  packet.Write((int)py);
                  packet.Send();
               }
               break;
            }
         }
      }
   }
}

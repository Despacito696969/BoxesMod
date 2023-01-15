using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Boxes
{
   public class BoxesConfig : ModConfig
   {
      public override ConfigScope Mode => ConfigScope.ServerSide;

      [DefaultValue(200)]
      [Range(2, 1<<20)]
      [Label("Box Width (in tiles)")]
      public int BoxWidth;
      [DefaultValue(100)]
      [Range(3, 1<<20)]
      [Label("Box Height (in tiles)")]
      public int BoxHeight;
      [DefaultValue(5000)]
      [Range(0, 1<<20)]
      [Label("Base Cost (in silver coins)")]
      public int costSilver;
      [DefaultValue(5000)]
      [Range(0, 1<<20)]
      [Label("Base Cost Increase (in silver coins)")]
      public int costIncreaseSilver;
      [DefaultValue(5)]
      [Range(1, 1<<20)]
      [Label("Boxes per Increase")]
      public int boxesPerIncrease;
      [Label("Troll those who think outside the box")]
      [Tooltip("Applies funny amount of debuffs to those who dare to use hoiks to get to areas they weren't supposed to")]
      [DefaultValue(true)]
      public bool trollThinkingOutOfTheBox;
   }
}

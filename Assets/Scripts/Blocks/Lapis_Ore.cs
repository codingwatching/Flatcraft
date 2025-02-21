﻿public class Lapis_Ore : Block
{
    public override string[] RandomTextures { get; } = {"lapis_ore", "lapis_ore_1"};

    public override float BreakTime { get; } = 6;

    public override Tool_Type ProperToolType { get; } = Tool_Type.Pickaxe;
    public override Tool_Level ProperToolLevel { get; } = Tool_Level.Iron;
    public override BlockSoundType BlockSoundType { get; } = BlockSoundType.Stone;
}
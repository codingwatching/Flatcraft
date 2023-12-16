﻿public class Log : Block
{
    public override bool Solid { get; set; } = false;

    public override float BreakTime { get; } = 3f;
    public override bool IsFlammable { get; } = true;

    public override Tool_Type ProperToolType { get; } = Tool_Type.Axe;
    public override BlockSoundType BlockSoundType { get; } = BlockSoundType.Wood;

    protected override string GetTexture()
    {
        bool leafTexture = GetData().GetTag("leaf_texture") == "true";
        if (leafTexture)
        {
            return "logged_leaves";
        }

        return base.GetTexture();
    }
}
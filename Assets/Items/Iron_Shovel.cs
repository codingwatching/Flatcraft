﻿public class Iron_Shovel : Tool
{
    public override string texture { get; set; } = "item_iron_shovel";
    public override Tool_Type tool_type { get; } = Tool_Type.Shovel;
    public override Tool_Level tool_level { get; } = Tool_Level.Iron;
    public override int maxDurabulity { get; } = 250;
}
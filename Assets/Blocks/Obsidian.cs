public class Obsidian : Block
{
    public static string default_texture = "block_obsidian";
    public override float breakTime { get; } = 250;

    public override Tool_Type propperToolType { get; } = Tool_Type.Pickaxe;
    public override Tool_Level propperToolLevel { get; } = Tool_Level.Diamond;
    public override Block_SoundType blockSoundType { get; } = Block_SoundType.Stone;
    public override void Tick()
    {
        print("tick");
        CheckPortalActivation();

        base.Tick();
    }

    public void CheckPortalActivation()
    {
        if ((location + new Location(0, 1)).GetMaterial() == Material.Fire)
        {
            TryActivatePortal();
        }
    }

    public void TryActivatePortal()
    {
        int y = location.y + 1;
        while (true)
        {
            Location loc = new Location(location.x, y, location.dimension);
            Material mat = loc.GetMaterial();

            if (mat == Material.Obsidian)
                break;
            if (y >= Chunk.Height)
                return;

            y++;
        }

        for (int buildY = location.y + 1; buildY < y; buildY++)
        {
            Location loc = new Location(location.x, buildY, location.dimension);

            loc.SetMaterial(Material.Portal_Frame).Tick();
        }
    }
}
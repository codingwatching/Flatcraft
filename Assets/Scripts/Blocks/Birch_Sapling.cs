using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Birch_Sapling : Block
{
    public override string texture { get; set; } = "block_birch_sapling";

    public override float averageRandomTickDuration { get; } = 20 * 60;
    public override bool solid { get; set; } = false;
    public override float breakTime { get; } = 0.01f;
    public override bool requiresGround { get; } = true;
    public override bool isFlammable { get; } = true;

    public override Block_SoundType blockSoundType { get; } = Block_SoundType.Grass;
    
    public override void RandomTick()
    {
        base.RandomTick();
        
        location.SetState(new BlockState(Material.Structure_Block, new BlockData("structure=Birch_Tree"))).Tick();
    }
    
    public override void Tick()
    {
        base.Tick();
        Material matBelow = (location + new Location(0, -1)).GetMaterial();

        if (matBelow != Material.Grass && matBelow != Material.Dirt)
        {
            Break();
        }
    }
}

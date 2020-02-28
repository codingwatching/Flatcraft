﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LibNoise;
using System.IO;
using System.Threading;
using Unity.Burst;

[BurstCompile]
public class Chunk : MonoBehaviour
{
    public static float AutosaveDuration = 5;
    public static int Width = 16, Height = 255;
    public static int RenderDistance = 4;
    public static int SpawnChunkDistance = 0;
    public static int MinimumUnloadTime = 20;
    public static int TickRate = 1;

    public GameObject blockPrefab;

    public ChunkPosition chunkPosition;
    public bool isSpawnChunk = false;
    public bool isTickedChunk = false;
    public bool isLoaded = false;
    public bool isLoading = false;
    public int age = 0;

    public Chunk rightChunk;
    public Chunk leftChunk;
    public Dictionary<Vector2Int, Block> blocks = new Dictionary<Vector2Int, Block>();
    public Dictionary<int, Biome> cachedBiome = new Dictionary<int, Biome>();
    public Dictionary<Location, int> cachedRandomSeeds = new Dictionary<Location, int>();

    [Header("Cave Generation Settings")]
    public static float caveFrequency = 5;
    public static float caveLacunarity = 0.6f;
    public static float cavePercistance = 2;
    public static int caveOctaves = 4;
    public static float caveHollowValue = 2.2f;

    [Header("Ore Generation Settings")]
    public static int ore_coal_height = 128;
    public static double ore_coal_chance = 0.008f;

    public static int ore_iron_height = 64;
    public static double ore_iron_chance = 0.005f;

    public static int ore_gold_height = 32;
    public static double ore_gold_chance = 0.0015f;

    public static int ore_lapis_height = 32;
    public static double ore_lapis_chance = 0.0015f;

    public static int ore_redstone_height = 16;
    public static double ore_redstone_chance = 0.0015f;

    public static int ore_diamond_height = 16;
    public static double ore_diamond_chance = 0.0015f;

    public static int lava_height = 10;
    public static int sea_level = 62;


    public static float mobSpawningChance = 0.01f;
    public static List<string> mobSpawns = new List<string> { "Chicken", "Sheep" };



    public Dictionary<Location, string> blockChanges = new Dictionary<Location, string>();

    private void Start()
    {
        if (WorldManager.instance.chunks.ContainsKey(chunkPosition))
        {
            Debug.LogWarning("A duplicate of Chunk [" + chunkPosition.chunkX + "] has been destroyed.");
            Destroy(gameObject);
            return;
        }

        isSpawnChunk = (chunkPosition.chunkX >= -SpawnChunkDistance && chunkPosition.chunkX <= SpawnChunkDistance);

        WorldManager.instance.chunks.Add(chunkPosition, this);

        StartCoroutine(SelfDestructionChecker());

        gameObject.name = "Chunk [" + chunkPosition.chunkX + "]";
        transform.position = new Vector3(chunkPosition.worldX, 0, 0);


        StartCoroutine(GenerateChunk());
    }

    private void OnDestroy()
    {
        if (WorldManager.instance.chunks.ContainsKey(chunkPosition) && (isLoaded || isLoading))
            WorldManager.instance.chunks.Remove(chunkPosition);
    }

    private void Update()
    {
    }

    public static Chunk GetChunk(ChunkPosition pos)
    {
        return GetChunk(pos, true);
    }

    public static Chunk GetChunk(ChunkPosition pos, bool loadIfNotFound)
    {
        Chunk chunk = null;

        WorldManager.instance.chunks.TryGetValue(pos, out chunk);
        if (loadIfNotFound && chunk == null && pos.chunkX != 0)
        {
            chunk = LoadChunk(pos);
        }

        return chunk;
    }

    public static ChunkPosition GetChunkPosFromWorldPosition(int worldPos, Dimension dimension)
    {
        int chunkX = 0;
        if (worldPos >= 0)
        {
            chunkX = (int)((float)worldPos / (float)Width);
        }
        else
        {
            chunkX = Mathf.CeilToInt(((float)worldPos + 1f) / (float)Width) - 1;
        }

        return new ChunkPosition(chunkX, dimension);
    }

    public static Chunk LoadChunk(ChunkPosition cPos)
    {
        GameObject newChunk = Instantiate(WorldManager.instance.chunkPrefab);

        newChunk.GetComponent<Chunk>().chunkPosition = cPos;

        return newChunk.GetComponent<Chunk>();
    }
    
    public void UnloadChunk()
    {
        if (isSpawnChunk)
            return;

        WorldManager.instance.chunks.Remove(chunkPosition);

        if (isLoading)
            WorldManager.instance.amountOfChunksLoading--;

        Destroy(gameObject);
    }

    IEnumerator TickAllBlocks()
    {
        //Tick Blocks
        Block[] blocks = transform.GetComponentsInChildren<Block>();

        foreach (Block block in blocks)
        {
            if (block == null || block.transform == null)
                continue;

            if (age == 0)
                block.GeneratingTick();

            block.Tick(false);
        }
        yield return new WaitForSecondsRealtime(0f);
    }

    IEnumerator AutosaveAllBlocks()
    {
        List<Block> blocks = new List<Block>(this.blocks.Values);

        if (blocks.Count > 0)
        {
            int blocksPerBatch = 20;
            float timePerBatch = 5f / ((float)blocks.Count / (float)blocksPerBatch);
            foreach (Block block in blocks)
            {
                yield return new WaitForSeconds(timePerBatch);
                if (block == null || !block.autosave)
                    continue;


                block.Tick(false);
                block.Autosave();
            }
        }

    }

    IEnumerator SelfDestructionChecker()
    {
        while (true)
        {
            float timePassed = 0f;
            while (!inRenderDistance())
            {
                yield return new WaitForSeconds(1f);
                timePassed += 1f;
                if (timePassed > MinimumUnloadTime)
                {
                    UnloadChunk();
                    yield break;
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator Tick()
    {
        while (true)
        {
            if (inRenderDistance())
            {
                if (rightChunk == null && inRenderDistance(new ChunkPosition(chunkPosition.chunkX + 1, chunkPosition.dimension)))
                {
                    rightChunk = GetChunk(new ChunkPosition(chunkPosition.chunkX + 1, chunkPosition.dimension));
                }
                if (leftChunk == null && inRenderDistance(new ChunkPosition(chunkPosition.chunkX - 1, chunkPosition.dimension)))
                {
                    leftChunk = GetChunk(new ChunkPosition(chunkPosition.chunkX - 1, chunkPosition.dimension));
                }
            }

            isTickedChunk = inRenderDistance(chunkPosition, RenderDistance - 1);

            //Update neighbor chunks
            if (isTickedChunk || age < 5)
            {
                TrySpawnMobs();
            }

            age++;
            yield return new WaitForSeconds(1 / TickRate);
        }
    }

    public void TrySpawnMobs()
    {
        System.Random r = new System.Random();

        if (r.NextDouble() < mobSpawningChance / TickRate && Entity.livingEntityCount < Entity.MaxLivingAmount)
        {
            int x = r.Next(0, Width) + chunkPosition.worldX;
            int y = getTopmostBlock(x, chunkPosition.dimension).location.y + 1;
            List<string> entities = mobSpawns;
            entities.AddRange(getBiome(x, chunkPosition.dimension).biomeSpecificEntitySpawns);
            string entityType = entities[r.Next(0, entities.Count)];

            Entity entity = Entity.Spawn(entityType);
            entity.location = new Location(x, y, chunkPosition.dimension);
        }
    }

    public bool inRenderDistance()
    {
        return inRenderDistance(chunkPosition);
    }

    public bool inRenderDistance(ChunkPosition cPos)
    {
        return inRenderDistance(cPos, RenderDistance);
    }

    public bool inRenderDistance(ChunkPosition cPos, int range)
    {
        if (cPos.chunkX == 0)
            return true;

        Location playerLocation;


        if (Player.localInstance == null)
            playerLocation = new Location(0, 0);
        else
            playerLocation = Player.localInstance.location;

        float distanceFromPlayer = Mathf.Abs((cPos.worldX + (Width/2)) - playerLocation.x);

        return distanceFromPlayer < range * Width;
    }

    public Dictionary<Location, Material> loadChunkTerrain()
    {
        cacheBiomes();
        cacheRandomSeeds();
        Dictionary<Location, Material> blocks = new Dictionary<Location, Material>();

        for (int y = 0; y <= Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Location loc = new Location(x + chunkPosition.worldX, y);
                Material mat = getTheoreticalTerrainBlock(loc);

                if (mat != Material.Air)
                {
                    blocks.Add(loc, mat);
                }
            }
        }

        return blocks;
    }

    public void cacheBiomes()
    {
        for (int x = 0; x < Width; x++)
        {
            getBiome(x + chunkPosition.worldX, chunkPosition.dimension);
        }
    }

    public void cacheRandomSeeds()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y <= Height; y++)
            {
                seedByLocation(new Location(x + chunkPosition.worldX, y));
            }
        }
    }

    IEnumerator GenerateChunk()
    {
        //Wait for scene to load
        yield return new WaitForSeconds(0.2f);

        patchNoise = new LibNoise.Generator.Perlin(0.6f, 0.8f, 0.8f, 2, WorldManager.world.seed, QualityMode.Low);
        lakeNoise = new LibNoise.Generator.Perlin(2, 0.8f, 5f, 2, WorldManager.world.seed, QualityMode.Low);
        caveNoise = new LibNoise.Generator.Perlin(caveFrequency, caveLacunarity, cavePercistance, caveOctaves, WorldManager.world.seed, QualityMode.High);

        isLoading = true;
        WorldManager.instance.amountOfChunksLoading++;
        

        Dictionary<Location, Material> terrainBlocks = null;
        Thread terrainThread = new Thread(() => { terrainBlocks = loadChunkTerrain(); });
        terrainThread.Start();
        
        while (terrainThread.IsAlive)
        {
            yield return new WaitForSeconds(0.5f);
        }

        int i = 0;
        foreach (KeyValuePair<Location, Material> entry in terrainBlocks)
        {
            setLocalBlock(entry.Key, entry.Value, "", false, false);
            i++;
            if (i % 10 == 1)
            {
                yield return new WaitForSeconds(0.05f);
            }
        }
        

        for (int y = 0; y <= Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                GenerateStructures(Location.locationByPosition(transform.position, chunkPosition.dimension) + new Location(x, y));
            }
            if (y < 80 && y % 4 == 0)
                yield return new WaitForSeconds(0.05f);
        }
        

        LoadAllEntities();
        
        StartCoroutine(Tick());
        StartCoroutine(TickAllBlocks());
        yield return new WaitForSecondsRealtime(1f);


        //Load block changes
        string path = WorldManager.world.getPath() + "\\region\\" + chunkPosition.dimension.ToString() + "\\" + chunkPosition.chunkX + "\\blocks";
        if (File.Exists(path))
        {
            foreach (string line in File.ReadAllLines(path))
            {
                Location loc = new Location(int.Parse(line.Split('*')[0].Split(',')[0]), int.Parse(line.Split('*')[0].Split(',')[1]));
                Material mat = (Material)System.Enum.Parse(typeof(Material), line.Split('*')[1]);
                string data = line.Split('*')[2];

                setLocalBlock(loc, mat, data, false, false);
            }
        }

        isLoading = false;
        isLoaded = true;
        WorldManager.instance.amountOfChunksLoading--;
        
        StartCoroutine(GenerateLight());
        StartCoroutine(SaveLoop());
        if (isSpawnChunk)
        {
            StartCoroutine(processLightLoop());
        }
    }

    IEnumerator GenerateLight()
    {
        //Fill sunlight source list
        int minXPos = chunkPosition.worldX;
        int maxXPos = chunkPosition.worldX + Width - 1;

        for (int x = minXPos; x <= maxXPos; x++)
        {
            yield return new WaitForSecondsRealtime(0.05f);
            Block.UpdateSunlightSourceAt(x, chunkPosition.dimension);
        }

        //Update Light Sources (not sunlight again)
        foreach (Block block in GetComponentsInChildren<Block>())
        {
            if (block.glowLevel > 0)
            {
                Block.UpdateLightAround(block.location);
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
    }

    IEnumerator processLightLoop()
    {
        while (true)
        {
            if (Block.oldLight.Count > 0)
            {

                List<Location> oldLight = new List<Location>(Block.oldLight);
                Block.oldLight.Clear();
                List<KeyValuePair<Block, int>> lightToRender = null;

                Thread lightThread = new Thread(() => { lightToRender = processDirtyLight(oldLight); });
                lightThread.Start();

                while (lightThread.IsAlive)
                {
                    yield return new WaitForSeconds(0.1f);
                }

                //Render
                foreach (KeyValuePair<Block, int> entry in new List<KeyValuePair<Block, int>>(lightToRender))
                {
                    if (entry.Key == null)
                        continue;

                    entry.Key.RenderBlockLight(entry.Value);
                }
            }

            yield return new WaitForSecondsRealtime(0.05f);
        }
    }

    public List<KeyValuePair<Block, int>> processDirtyLight(List<Location> oldLight)
    {
        List<KeyValuePair<Block, int>> lightToRender = new List<KeyValuePair<Block, int>>();

        if (oldLight.Count == 0)
            return lightToRender;

        //Process
        foreach (Location loc in oldLight)
        {
            Block block = Chunk.getBlock(loc);
            if (block == null)
                continue;

            lightToRender.Add(new KeyValuePair<Block, int>(block, Block.GetLightLevel(loc)));
        }

        return lightToRender;
    }

    private void LoadAllEntities()
    {
        string path = WorldManager.world.getPath() + "\\region\\" + chunkPosition.dimension + "\\" + chunkPosition.chunkX + "\\entities";

        if (!Directory.Exists(path))
            return;

        foreach (string entityPath in Directory.GetFiles(path))
        {
            string entityFile = entityPath.Split('\\')[entityPath.Split('\\').Length - 1];
            string entityType = entityFile.Split('.')[1];
            int entityId = int.Parse(entityFile.Split('.')[0]);

            Entity entity = Entity.Spawn(entityType);
            entity.id = entityId;
            //Make sure the newly created entity is in the chunk, to make loading work correctly (setting actual position happens inside Entity class)
            entity.transform.position = transform.position + new Vector3(1, 1);
        }
    }

    private void GenerateStructures(Location loc)
    {
        Block block = getBlock(loc);
        if (block == null)
            return;

        Material mat = block.GetMaterial();
        Biome biome = getBiome(loc.x, chunkPosition.dimension);
        System.Random r = new System.Random(Chunk.seedByLocation(loc));

        if (biome.name == "forest")
        {
            if (mat == Material.Grass && getBlock((loc + new Location(0, 1))) == null)
            {
                //Trees
                if (r.Next(0, 100) <= 10)
                {
                    Chunk.setBlock(loc + new Location(0, 1), Material.Structure_Block, "structure=Tree|save=false", false, false);
                }

                //Large Trees
                if (r.Next(0, 100) <= 1)
                {
                    Chunk.setBlock(loc + new Location(0, 1), Material.Structure_Block, "structure=Large_Tree|save=false", false, false);
                }

                //Vegetation
                if (r.Next(0, 100) <= 25)
                {
                    Material[] vegetationMaterials = new Material[] { Material.Tall_Grass, Material.Red_Flower };

                    Chunk.setBlock(loc + new Location(0, 1), vegetationMaterials[r.Next(0, vegetationMaterials.Length)], "", false, false);
                }
            }
        }
        else if (biome.name == "desert")
        {
            if (mat == Material.Sand && getBlock((loc + new Location(0, 1))) == null)
            {
                //Cactie
                if (r.Next(0, 100) <= 8)
                {
                    Chunk.setBlock(loc + new Location(0, 1), Material.Structure_Block, "structure=Cactus|save=false", false, false);
                }
            }
        }

        //Generate Ores
        if (mat == Material.Stone)
        {
            if (r.NextDouble() < Chunk.ore_diamond_chance && loc.y <= Chunk.ore_diamond_height)
            {
                Chunk.setBlock(loc, Material.Structure_Block, "structure=Ore_Diamond|save=false", false, false);
            }
            else if (r.NextDouble() < Chunk.ore_redstone_chance && loc.y <= Chunk.ore_redstone_height)
            {
                Chunk.setBlock(loc, Material.Structure_Block, "structure=Ore_Redstone|save=false", false, false);
            }
            else if (r.NextDouble() < Chunk.ore_lapis_chance && loc.y <= Chunk.ore_lapis_height)
            {
                Chunk.setBlock(loc, Material.Structure_Block, "structure=Ore_Lapis|save=false", false, false);
            }
            else if (r.NextDouble() < Chunk.ore_gold_chance && loc.y <= Chunk.ore_gold_height)
            {
                Chunk.setBlock(loc, Material.Structure_Block, "structure=Ore_Gold|save=false", false, false);
            }
            else if (r.NextDouble() < Chunk.ore_iron_chance && loc.y <= Chunk.ore_iron_height)
            {
                Chunk.setBlock(loc, Material.Structure_Block, "structure=Ore_Iron|save=false", false, false);
            }
            else if (r.NextDouble() < Chunk.ore_coal_chance && loc.y <= Chunk.ore_coal_height)
            {
                Chunk.setBlock(loc, Material.Structure_Block, "structure=Ore_Coal|save=false", false, false);
            }
        }
    }

    IEnumerator SaveLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(AutosaveDuration);

            CreateChunkPath();

            StartCoroutine(AutosaveAllBlocks());


            //Save Block Changes
            Thread worldThread = new Thread(SaveBlockChanges);
            worldThread.Start();

            while (worldThread.IsAlive)
            {
                yield return new WaitForSeconds(0.1f);
            }

            //Save Entities
            Entity[] entities = GetEntities();
            Thread entityThread = new Thread(() => { SaveEntities(entities); });
            entityThread.Start();

            while (entityThread.IsAlive)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }


    public void SaveEntities(Entity[] entities)
    {
        foreach (Entity e in entities)
        {
            try
            {
                e.Save();
            } catch (System.Exception ex)
            {
                Debug.LogWarning("Error in saving entity: " + ex.StackTrace);
            }
        }
    }

    public void CreateChunkPath()
    {
        string path = WorldManager.world.getPath() + "\\region\\" + chunkPosition.dimension + "\\" + chunkPosition.chunkX;
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Directory.CreateDirectory(path + "\\entities");
            File.Create(path + "\\blocks").Close();
        }
    }

    public void SaveBlockChanges()
    {
        //Save Blocks
        if (blockChanges.Count > 0)
        {
            lock (blockChanges)
            {
                Dictionary<Location, string> changes = new Dictionary<Location, string>(blockChanges);
                blockChanges.Clear();

                string path = WorldManager.world.getPath() + "\\region\\" + chunkPosition.dimension + "\\" + chunkPosition.chunkX;
                foreach (string line in File.ReadAllLines(path + "\\blocks"))
                {
                    Location lineLoc = new Location(int.Parse(line.Split('*')[0].Split(',')[0]), int.Parse(line.Split('*')[0].Split(',')[1]));
                    string lineData = line.Split('*')[1] + "*" + line.Split('*')[2];

                    if (!changes.ContainsKey(lineLoc))
                        changes.Add(lineLoc, lineData);
                }

                //Empty lines before writing
                File.WriteAllText(path + "\\blocks", "");

                TextWriter c = new StreamWriter(path + "\\blocks");

                foreach (KeyValuePair<Location, string> line in changes)
                {
                    c.WriteLine(line.Key.x + "," + line.Key.y + "*" + line.Value);
                }

                c.Close();
            }
        }
    }

    public bool isBlockLocal(Location loc)
    {
        bool local = (GetChunkPosFromWorldPosition(loc.x, loc.dimension).chunkX == chunkPosition.chunkX && loc.dimension == chunkPosition.dimension);
        
        if (loc.y < 0 || loc.y > Height || loc.dimension != chunkPosition.dimension)
            local = false;

        return local;
    }

    public static Block setBlock(Location loc, Material mat)
    {
        return setBlock(loc, mat, true);
    }

    public static Block setBlock(Location loc, Material mat, bool save)
    {
        return setBlock(loc, mat, "", save, true);
    }

    public static Block setBlock(Location loc, Material mat, string data, bool save, bool spreadTick)
    {
        Chunk chunk = GetChunk(GetChunkPosFromWorldPosition(loc.x, loc.dimension), false);

        if (chunk == null)
            return null;

        return chunk.setLocalBlock(loc, mat, data, save, spreadTick);
    }

    public Block setLocalBlock(Location loc, Material mat)
    {
        return setLocalBlock(loc, mat, true);
    }

    public Block setLocalBlock(Location loc, Material mat, bool save)
    {
        return setLocalBlock(loc, mat, "", save, true);
    }

    public Block setLocalBlock(Location loc, Material mat, string data, bool save, bool spreadTick)
    {
        Vector2Int pos = Vector2Int.RoundToInt(loc.getPosition());

        System.Type type = System.Type.GetType(mat.ToString());
        if (!type.IsSubclassOf(typeof(Block)))
            return null;

        if (!isBlockLocal(loc))
        {
            Debug.LogWarning("Tried setting local block outside of chunk (" + loc.x + ", " + loc.y + ") inside Chunk [" + chunkPosition.chunkX + ", " + chunkPosition.dimension.ToString() + "]");
            return null;
        }

        //remove old block
        if (getLocalBlock(loc) != null)
        {
            Destroy(getLocalBlock(loc).gameObject);
            blocks.Remove(pos);
        }
        
        if (save)
        {
            if (blockChanges.ContainsKey(loc))
                blockChanges.Remove(loc);
            blockChanges.Add(loc, mat.ToString() + "*" + data);
        }

        Block result = null;

        if (mat == Material.Air)
        {
            Block.SpreadTick(loc);
        }else
        {
            //Place new block
            GameObject block = null;

            block = Instantiate(blockPrefab);

            //Attach it to the object
            block.AddComponent(type);

            block.transform.parent = transform;
            block.transform.position = loc.getPosition();

            //Add the block to block list
            if(blocks.ContainsKey(pos))
                blocks[pos] = block.GetComponent<Block>();
            else
                blocks.Add(pos, block.GetComponent<Block>());

            block.GetComponent<Block>().data = Block.dataFromString(data);
            block.GetComponent<Block>().location = loc;
            block.GetComponent<Block>().Initialize();
            if (spreadTick)
                block.GetComponent<Block>().FirstTick();
            block.GetComponent<Block>().Tick(spreadTick);   ///TICKED BEFORE OTHER BLOCKS            

            result = block.GetComponent<Block>();
        }

        if (isLoaded)
        {
            Block.UpdateSunlightSourceAt(loc.x, Player.localInstance.location.dimension);
            Block.UpdateLightAround(loc);
        }
        return result;
    }

    public static Block getBlock(Location loc)
    {
        Chunk chunk = GetChunk(GetChunkPosFromWorldPosition(loc.x, loc.dimension), false);

        if (chunk == null)
        {
            return null;
        }

        Block block = chunk.getLocalBlock(loc);

        return block;
    }

    public Block getLocalBlock(Location loc)
    {
        if (!isBlockLocal(loc))
        {
            Debug.LogWarning("Tried getting local block outside of chunk (" + loc.x + ", " + loc.y + ") inside Chunk [" + chunkPosition.chunkX + ", " + chunkPosition.dimension.ToString() + "]");
            return null;
        }
        
        Block block = null;
        
        blocks.TryGetValue(Vector2Int.RoundToInt(loc.getPosition()), out block);
        
        return block;
    }
    
    LibNoise.Generator.Perlin caveNoise;
    LibNoise.Generator.Perlin patchNoise;
    LibNoise.Generator.Perlin lakeNoise;
    public Material getTheoreticalTerrainBlock(Location loc)
    {
        System.Random r = new System.Random(seedByLocation(loc));
        Material mat = Material.Air;

        List<Biome> biomes = getTwoMostProminantBiomes(loc.x);
        Biome biome = biomes[0];

        //-Terrain Generation-//
        float noiseValue = biome.blendNoiseValues(biomes[1], loc);

        //-Ground-//
        if (noiseValue > 0.1f)
        {
            if (biome.name == "desert" || biome.name == "lake")
            {
                mat = Material.Sand;
            }
            else if (biome.name == "forest")
            {
                mat = Material.Grass;
            }

            if (noiseValue > 0.5f)
            {
                mat = Material.Stone;
            }
        }

        //-Lakes-//
        if (mat == Material.Air && loc.y <= sea_level && biome.name == "lake")
        {
            mat = Material.Water;
        }

        //-Dirt & Gravel Patches-//
        if (mat == Material.Stone)
        {
            if (Mathf.Abs((float)caveNoise.GetValue((float)loc.x / 20, (float)loc.y / 20)) > 7.5f)
            {
                mat = Material.Dirt;
            }
            if (Mathf.Abs((float)caveNoise.GetValue((float)loc.x / 20 + 100, (float)loc.y / 20, 200)) > 7.5f)
            {
                mat = Material.Gravel;
            }
        }

        //-Sea-//
        if (mat == Material.Air && loc.y <= sea_level)
        {
            mat = Material.Water;
        }

        //-Caves-//
        if(noiseValue > 0.1f)
        {
            double caveValue =
                (caveNoise.GetValue((float)loc.x / 20, (float)loc.y / 20) + 4.0f) / 4f;
            if (caveValue > caveHollowValue)
            {
                mat = Material.Air;

        //-Lava Lakes-//
                if (loc.y <= lava_height)
                    mat = Material.Lava;
            }
        }

        //-Bedrock Generation-//
        if (loc.y <= 4)
        {
            //Fill layer 0 and then progressively less chance of bedrock further up
            if (loc.y == 0)
                mat = Material.Bedrock;
            else if (r.Next(0, (int)loc.y+2) <= 1)
                mat = Material.Bedrock;
        }

        return mat;
    }

    public Entity[] GetEntities()
    {
        List<Entity> entities = new List<Entity>();
        
        foreach (Entity e in Entity.entities)
        {
            if (e.location.x >= chunkPosition.worldX && 
                e.location.x <= chunkPosition.worldX + Width)
                entities.Add(e);
        }

        return entities.ToArray();
    }

    public static Block getTopmostBlock(int x, Dimension dimension)
    {
        Chunk chunk = GetChunk(GetChunkPosFromWorldPosition(x, dimension), false);
        if (chunk == null)
            return null;

        return chunk.getLocalTopmostBlock(x);
    }

    public Block getLocalTopmostBlock(int x)
    {
        for(int y = Height; y > 0; y--)
        {
            if(getLocalBlock(new Location(x, y, chunkPosition.dimension)) != null)
            {
                return getLocalBlock(new Location(x, y, chunkPosition.dimension));
            }
        }
        return null;
    }

    public static int seedByLocation(Location loc)
    {
        Chunk chunk = Chunk.GetChunk(Chunk.GetChunkPosFromWorldPosition(loc.x, loc.dimension), false);
        int seed = 0;

        if (chunk == null)
            return seed;

        chunk.cachedRandomSeeds.TryGetValue(loc, out seed);
        if (seed == 0) {
            seed = new System.Random((WorldManager.world.seed.ToString() + ", " + loc.x + ", " + loc.y + ", " + loc.dimension.ToString()).GetHashCode()).Next(int.MinValue, int.MaxValue);
            chunk.cachedRandomSeeds[loc] = seed;
        }

        return seed;
    }

    public static Biome getBiome(int pos, Dimension dimension)
    {
        Chunk chunk = Chunk.GetChunk(Chunk.GetChunkPosFromWorldPosition(pos, dimension), false);
        Biome biome = null;

        if (chunk == null)
            return null;

        if (chunk.cachedBiome.ContainsKey(pos))
        {
            biome = chunk.cachedBiome[pos];
        }
        else
        {
            biome = getTwoMostProminantBiomes(pos)[0];
            chunk.cachedBiome.Add(pos, biome);
        }

        return biome;
    }

    public static List<Biome> getTwoMostProminantBiomes(int pos)
    {
        List<Biome> biomes = new List<Biome>(WorldManager.instance.biomes);

        biomes.Sort((x, y) => x.getBiomeValueAt(pos).CompareTo(y.getBiomeValueAt(pos)));

        return biomes;
    }
}
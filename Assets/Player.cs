﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Player : HumanEntity
{
    //Entity Properties
    public override bool chunk_loading { get; } = true;
    public override float maxHealth { get; } = 20;
    public float maxHunger = 20;


    //Entity Data Tags
    [EntityDataTag(false)]
    public float hunger;
    [EntityDataTag(true)]
    public PlayerInventory inventory = new PlayerInventory();
    [EntityDataTag(true)]
    public Vector2 spawnPosition = new Vector2(0, 80);

    //Entity State
    [Space]
    public static Player localInstance;
    public GameObject crosshair;
    private float lastFrameScroll;

    public override void Start()
    {
        base.Start();

        localInstance = this;

        health = maxHealth;
        hunger = maxHunger;
        inventory = new PlayerInventory();

        Load();

        transform.position = ValidSpawn(spawnPosition);
    }

    public override void Update()
    {
        base.Update();

        float scroll = Input.mouseScrollDelta.y;
        //Check once every 5 frames
        if (scroll != 0 && (Time.frameCount % 5 == 0 || lastFrameScroll == 0))
        {
            inventory.selectedSlot += (scroll > 0) ? -1 : 1;

            if (inventory.selectedSlot > 8)
                inventory.selectedSlot = 0;
            if (inventory.selectedSlot < 0)
                inventory.selectedSlot = 8;
        }
        lastFrameScroll = scroll;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        performInput();
    }

    private void performInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && !inventory.open)
            inventory.Open();

        //Movement
        if (Input.GetKey(KeyCode.A))
        {
            Walk(-1);
        }
        if (Input.GetKey(KeyCode.D))
        {
            Walk(1);
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space))
        {
            Jump();
        }
        
        //Inventory Managment
        if (Input.GetKeyDown(KeyCode.Q))
            Drop();

        KeyCode[] numpadCodes = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5
                                , KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9};
        foreach(KeyCode keyCode in numpadCodes)
        {
            if (Input.GetKeyDown(keyCode))
                inventory.selectedSlot = System.Array.IndexOf<KeyCode>(numpadCodes, keyCode);
        }

        //Crosshair
        Crosshair();
    }

    private void Crosshair() { 
        //Crosshair
        if (WorldManager.instance.loadingProgress == 1)
        {
            Vector3Int mousePosition = Vector3Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            mousePosition.z = 0;
            Block block = Chunk.getBlock((Vector2Int)mousePosition);

            crosshair.transform.position = mousePosition;


            if (System.Type.GetType(inventory.getSelectedItem().material.ToString()).IsSubclassOf(typeof(Block)))
            {
                if (block == null || (block.GetMateral() == Material.Water || block.GetMateral() == Material.Lava))
                {
                    if (Input.GetMouseButtonDown(1))
                    {
                        if (inventory.getSelectedItem().material != Material.Air &&
                            (inventory.getSelectedItem().amount > 0))
                        {

                            Chunk.setBlock((Vector2Int)mousePosition, inventory.getSelectedItem().material);
                            inventory.setItem(inventory.selectedSlot,
        new ItemStack(inventory.getSelectedItem().material, inventory.getSelectedItem().amount - 1));
                        }
                    }
                }
                else
                {
                    Item sampleItem = (Item)System.Activator.CreateInstance(typeof(Item));

                    if (Input.GetMouseButtonDown(1))
                    {
                        sampleItem.Interact((Vector2Int)mousePosition, 1, true);
                    }
                    else if (Input.GetMouseButton(1))
                    {
                        sampleItem.Interact((Vector2Int)mousePosition, 1, false);
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        sampleItem.Interact((Vector2Int)mousePosition, 0, true);
                    }
                    else if (Input.GetMouseButton(0))
                    {
                        sampleItem.Interact((Vector2Int)mousePosition, 0, false);
                    }
                }
            }
            else if(System.Type.GetType(inventory.getSelectedItem().material.ToString()).IsSubclassOf(typeof(Item)))
            {
                Item item = (Item)System.Activator.CreateInstance(System.Type.GetType(inventory.getSelectedItem().material.ToString()));

                if (Input.GetMouseButtonDown(1))
                {
                    item.Interact((Vector2Int)mousePosition, 1, true);
                }
                else if (Input.GetMouseButton(1))
                {
                    item.Interact((Vector2Int)mousePosition, 1, false);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    item.Interact((Vector2Int)mousePosition, 0, true);
                }
                else if (Input.GetMouseButton(0))
                {
                    item.Interact((Vector2Int)mousePosition, 0, false);
                }
            }
        }
    }

    public void Drop()
    {
        ItemStack item = inventory.getSelectedItem().Clone();

        if (item.amount <= 0)
            return;

        item.amount = 1;
        inventory.getSelectedItem().amount --;

        item.Drop(Vector2Int.CeilToInt(transform.position + new Vector3(4, 0)));
    }

    public void DropAll()
    {
        int i = 0;
        foreach(ItemStack item in inventory.items)
        {
            if(item.material != Material.Air && item.amount > 0)
            {
                System.Random random = new System.Random((transform.position + "" + i).GetHashCode());
                Vector2 maxVelocity = new Vector2(2, 2);
                Vector2Int dropPosition = Vector2Int.FloorToInt((Vector2)transform.position + new Vector2(0, 2));

                item.Drop(dropPosition, 
                    new Vector2((float)random.NextDouble() * (maxVelocity.x - -maxVelocity.x) + -maxVelocity.x,
                    (float)random.NextDouble() * (maxVelocity.x - -maxVelocity.x) + -maxVelocity.x));

                inventory.items[i] = new ItemStack();

                i++;
            }
        }
    }

    public Vector2 ValidSpawn(Vector2 pos)
    {
        for (int i = Chunk.Height; i <= 0; i --)
        {
            if(Chunk.getBlock(new Vector2Int((int)pos.x, i)) != null)
            {
                return new Vector2(pos.x, i+2);
            }
        }
        return pos;
    }

    public override void Die()
    {
        DeathMenu.active = true;
        DropAll();
        health = 20;
        hunger = 20;
        Save();

        base.Die();
    }

    public override void Save()
    {
        string path = WorldManager.world.getPath() + "\\players\\player.dat";

        if (!File.Exists(path))
        {
            File.Create(path);
            return;
        }

        List<string> lines = new List<string>();

        lines = GetSaveStrings();

        File.WriteAllLines(path, lines);
    }

    public override string SavePath()
    {
        return WorldManager.world.getPath() + "\\players\\player.dat";
    }

    private Dictionary<string, string> dataFromString(string[] lines)
    {
        Dictionary<string, string> resultData = new Dictionary<string, string>();

        foreach (string line in lines)
        {
            if (line.Contains("="))
                resultData.Add(line.Split('=')[0], line.Split('=')[1]);
        }

        return resultData;
    }
}

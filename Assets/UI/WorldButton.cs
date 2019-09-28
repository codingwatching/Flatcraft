﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class WorldButton : MonoBehaviour
{
    public Text nameText;
    public Text descriptionText;

    public float lastClickTime;

    void Start()
    {
        World world = GetComponentInParent<SingleplayerMenu>().worlds[transform.GetSiblingIndex()];

        nameText.text = world.name;
        float fileSize = (world.getDiskSize() / 1000);
        float fileSizeRounded = (float)Mathf.Round(fileSize * 100f) / 100f;

        descriptionText.text = "Survival Mode (Version: ?, " + fileSizeRounded + "KB)";
    }

    public void Click()
    {
        GetComponentInParent<SingleplayerMenu>().selectedWorld = transform.GetSiblingIndex();

        if(Time.time - lastClickTime < 0.3f)
        {
            GetComponentInParent<SingleplayerMenu>().Play();
        }

        lastClickTime = Time.time;
    }
}

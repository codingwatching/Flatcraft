﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugBiomeWave : MonoBehaviour
{
    [Space]
    public Biome biome;

    [Space]
    public int previewWidth;
    public float previewUpdateFrequency;
    public float lineWidth;
    public int highestPixel;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(UpdateLoop());
    }

    IEnumerator UpdateLoop()
    {
        while (true)
        {
            Texture2D tex = new Texture2D(previewWidth, highestPixel);
            for (int x = 0; x < previewWidth; x++)
            {
                int noiseValue = (int)biome.getBiomeValueAt(x);

                for (int y = noiseValue; y >= noiseValue - lineWidth; y--)
                {
                    if(y < 0 || y > highestPixel)
                        continue;
                    
                    tex.SetPixel(x, y, Color.black);
                }
            }
            
            tex.Apply();
            
            Sprite sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), Vector2.zero);
            GetComponent<SpriteRenderer>().sprite = sprite;
            
            yield return new WaitForSeconds(previewUpdateFrequency);
        }
    }
}

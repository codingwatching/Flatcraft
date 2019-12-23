﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockLight : MonoBehaviour
{
    public int glowingLevel = 0;
    public float flickerLevel = 0;
    public float intensity = 1;
    private float flickersInASecond = 4;
    public Color color;
    public bool sunlight = false;

    private float flickerValue = 0;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Flicker());
    }

    // Update is called once per frame
    void Update()
    {
        /*
        GetComponent<Light2D>().lightType = Light2D.LightType.Point;
        GetComponent<Light2D>().pointLightInnerRadius = (glowingLevel / 10) + flickerValue;
        GetComponent<Light2D>().pointLightOuterRadius = glowingLevel;
        GetComponent<Light2D>().color = color;
        GetComponent<Light2D>().intensity = intensity;
        transform.GetChild(0).GetComponent<Light2D>().lightType = Light2D.LightType.Point;
        transform.GetChild(0).GetComponent<Light2D>().pointLightInnerRadius = (glowingLevel / 10) + flickerValue;
        transform.GetChild(0).GetComponent<Light2D>().pointLightOuterRadius = glowingLevel;
        transform.GetChild(0).GetComponent<Light2D>().color = color;
        if (sunlight)
            transform.GetChild(0).GetComponent<Light2D>().intensity = Sky.sunlightIntensity;
        else transform.GetChild(0).GetComponent<Light2D>().intensity = intensity;*/

    }

    IEnumerator Flicker()
    {
        while (true)
        {
            flickerValue = (flickerValue == 0) ? flickerLevel : 0;
            yield return new WaitForSecondsRealtime(1f / (float)flickersInASecond);
        }
    }
}

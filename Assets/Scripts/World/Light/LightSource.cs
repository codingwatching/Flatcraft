using System;
using UnityEngine;

public class LightSource : MonoBehaviour
{
    public LightValues lightValues;
    
    private Location _cachedLocation;

    private void Awake()
    {
        _cachedLocation = Location.LocationByPosition(transform.position);
    }

    public void UpdateLightWithinReach()
    {
        LightManager.UpdateLightInArea(
            GetLocation() + new Location(-LightManager.MaxLightLevel, -LightManager.MaxLightLevel), 
            GetLocation() + new Location(LightManager.MaxLightLevel, LightManager.MaxLightLevel));
    }

    public void UpdateLightValues(LightValues values, bool updateSurroundingLight)
    {
        lightValues = values;

        bool chunkLoaded = new ChunkPosition(GetLocation()).IsChunkLoaded();

        if (updateSurroundingLight && chunkLoaded)
            UpdateLightWithinReach();
    }

    public static LightSource Create(Transform parent)
    {
        GameObject obj = Instantiate(LightManager.Instance.lightSourcePrefab, parent);
        LightSource source = obj.GetComponent<LightSource>();
        obj.transform.localPosition = Vector3.zero;

        return source;
    }

    public virtual Location GetLocation()
    {
        return _cachedLocation;
    }
}
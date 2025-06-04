using System;
using System.Collections.Generic;
using ColoredShadows.Scripts;
using EasyButtons;
using UnityEngine;

public class SetQuadDebugMats : MonoBehaviour
{
    [SerializeField] private List<Material> debugQuadMaterials;
    [SerializeField] private List<CustomLight> customLights;

    private void Start()
    {
        SyncQuads();
    }

    [Button]
    private void SyncQuads()
    {
        for (int i = 0; i < debugQuadMaterials.Count; i++)
        {
            SetQuadShadowMapID(
                debugQuadMaterials[i],
                customLights[i].lightIndex,
                customLights[i].lightData.lightMode == CustomLightData.LightMode.Point
            );
        }

        void SetQuadShadowMapID(Material material, int id, bool isCubemap)
        {
            Debug.Log("_ShowMap" + id);
            for (int i = 0; i < customLights.Count; i++)
            {
                material.SetInt("_ShowMap" + i, 0);
            }
            material.SetInt("_ShowMap" + id, 1);
            material.SetInt("_CubeMap", isCubemap ? 1 : 0);
        }
    }
}

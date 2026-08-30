using System.Collections.Generic;
using UnityEngine;

namespace Northwild
{
    public sealed class NorthwildVegetationWind : MonoBehaviour
    {
        private WorldClimate climate;
        private Transform[] trees;
        private Quaternion[] restingRotations;
        private float[] phases;
        private float[] flexibility;

        public void Configure(WorldClimate worldClimate)
        {
            climate = worldClimate;
            List<Transform> treeList = new List<Transform>(transform.childCount);
            for (int i = 0; i < transform.childCount; i++)
                treeList.Add(transform.GetChild(i));

            trees = treeList.ToArray();
            restingRotations = new Quaternion[trees.Length];
            phases = new float[trees.Length];
            flexibility = new float[trees.Length];
            for (int i = 0; i < trees.Length; i++)
            {
                restingRotations[i] = trees[i].localRotation;
                Vector3 position = trees[i].position;
                phases[i] = Mathf.Repeat(position.x * 0.071f + position.z * 0.113f, 10f);
                bool birch = trees[i].name.Contains("Birch");
                flexibility[i] = birch ? 1.2f : Mathf.Lerp(0.58f, 0.9f, trees[i].localScale.y - 0.7f);
            }
        }

        private void LateUpdate()
        {
            if (climate == null || trees == null)
                return;

            float windStrength = Mathf.Clamp01(climate.WindMetresPerSecond / 16f);
            float gust = Mathf.Lerp(0.48f, 1f, Mathf.PerlinNoise(Time.time * 0.075f, 4.31f));
            Vector3 direction = climate.WindDirection;
            for (int i = 0; i < trees.Length; i++)
            {
                if (trees[i] == null)
                    continue;

                float fineMotion = Mathf.Sin(Time.time * (0.62f + flexibility[i] * 0.16f) + phases[i]);
                float bend = windStrength * gust * flexibility[i] * 1.35f + fineMotion * windStrength * 0.22f;
                Quaternion sway = Quaternion.Euler(direction.z * bend, 0f, -direction.x * bend);
                trees[i].localRotation = restingRotations[i] * sway;
            }
        }
    }

    public sealed class NorthwildWaterSurface : MonoBehaviour
    {
        private WorldClimate climate;
        private Material material;
        private Vector2 broadOffset;
        private Vector2 detailOffset;

        public void Configure(Renderer waterRenderer, WorldClimate worldClimate)
        {
            climate = worldClimate;
            material = waterRenderer == null ? null : waterRenderer.sharedMaterial;
        }

        private void Update()
        {
            if (climate == null || material == null)
                return;

            Vector2 direction = new Vector2(climate.WindDirection.x, climate.WindDirection.z);
            float wind = Mathf.Clamp(climate.WindMetresPerSecond, 0.5f, 18f);
            broadOffset += direction * (0.0014f + wind * 0.00024f) * Time.deltaTime;
            detailOffset += new Vector2(-direction.y, direction.x) * (0.0032f + wind * 0.00042f) * Time.deltaTime;
            if (material.HasProperty("_NormalMap"))
                material.SetTextureOffset("_NormalMap", broadOffset);
            if (material.HasProperty("_DetailMap"))
                material.SetTextureOffset("_DetailMap", detailOffset);

            Color target = climate.Weather == WeatherType.Clear
                ? new Color(0.035f, 0.13f, 0.18f, 0.84f)
                : climate.Weather == WeatherType.Snow
                    ? new Color(0.08f, 0.16f, 0.19f, 0.88f)
                    : new Color(0.035f, 0.085f, 0.11f, 0.9f);
            if (material.HasProperty("_BaseColor"))
            {
                Color current = material.GetColor("_BaseColor");
                material.SetColor("_BaseColor", Color.Lerp(current, target, Time.deltaTime * 0.35f));
            }
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", Mathf.Lerp(0.96f, 0.82f, Mathf.Clamp01(wind / 17f)));
        }
    }

    public sealed class NorthwildTerrainWeathering : MonoBehaviour
    {
        private TerrainData data;
        private WorldClimate climate;
        private float lakeSurfaceY;
        private float[,,] dryBlend;
        private float[,] snowMask;
        private int snowLayer;
        private float snowAmount;
        private float lastAppliedSnow = -1f;
        private float nextRefresh;

        public void Configure(Terrain source, WorldClimate worldClimate, float waterHeight)
        {
            if (source == null)
                return;

            data = source.terrainData;
            climate = worldClimate;
            lakeSurfaceY = waterHeight;
            snowLayer = data.terrainLayers.Length - 1;
            dryBlend = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            BuildSnowMask();
            snowAmount = climate != null && climate.Weather == WeatherType.Snow ? 0.46f : 0f;
            ApplySnow(snowAmount);
        }

        private void Update()
        {
            if (data == null || climate == null || Time.time < nextRefresh)
                return;

            nextRefresh = Time.time + 5f;
            float target = climate.Weather == WeatherType.Snow ? 0.68f : 0f;
            snowAmount = Mathf.MoveTowards(snowAmount, target, climate.Weather == WeatherType.Snow ? 0.12f : 0.075f);
            if (Mathf.Abs(snowAmount - lastAppliedSnow) >= 0.045f || snowAmount == target)
                ApplySnow(snowAmount);
        }

        private void BuildSnowMask()
        {
            int width = data.alphamapWidth;
            int height = data.alphamapHeight;
            snowMask = new float[height, width];
            for (int z = 0; z < height; z++)
            {
                float nz = z / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    float slope = data.GetSteepness(nx, nz);
                    float worldHeight = data.GetInterpolatedHeight(nx, nz);
                    float flatness = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(19f, 48f, slope));
                    float shoreClearance = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                        lakeSurfaceY + 0.25f, lakeSurfaceY + 2.4f, worldHeight));
                    float breakup = Mathf.Lerp(0.72f, 1f, Mathf.PerlinNoise(nx * 38f + 7f, nz * 38f + 31f));
                    snowMask[z, x] = flatness * shoreClearance * breakup;
                }
            }
        }

        private void ApplySnow(float amount)
        {
            if (dryBlend == null || snowMask == null || snowLayer < 0)
                return;

            float[,,] blend = (float[,,])dryBlend.Clone();
            int height = blend.GetLength(0);
            int width = blend.GetLength(1);
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float snow = Mathf.Clamp01(amount * snowMask[z, x]);
                    for (int layer = 0; layer < snowLayer; layer++)
                        blend[z, x, layer] *= 1f - snow;
                    blend[z, x, snowLayer] = snow;
                }
            }

            data.SetAlphamaps(0, 0, blend);
            lastAppliedSnow = amount;
        }
    }
}

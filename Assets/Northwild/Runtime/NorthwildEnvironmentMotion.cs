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
        private Mesh waterMesh;
        private Vector3[] restingVertices;
        private Vector3[] animatedVertices;
        private Vector2 broadOffset;
        private Vector2 detailOffset;
        private float nextWaveUpdate;

        public void Configure(
            Renderer waterRenderer,
            MeshFilter waterMeshFilter,
            WorldClimate worldClimate)
        {
            climate = worldClimate;
            material = waterRenderer == null ? null : waterRenderer.material;
            waterMesh = waterMeshFilter == null ? null : waterMeshFilter.mesh;
            if (waterMesh != null)
            {
                waterMesh.MarkDynamic();
                restingVertices = waterMesh.vertices;
                animatedVertices = (Vector3[])restingVertices.Clone();
            }
        }

        private void Update()
        {
            if (climate == null || material == null)
                return;

            Vector2 direction = new Vector2(climate.WindDirection.x, climate.WindDirection.z);
            float wind = Mathf.Clamp(climate.WindMetresPerSecond, 0.5f, 18f);
            broadOffset += direction * (0.0045f + wind * 0.00065f) * Time.deltaTime;
            detailOffset += new Vector2(-direction.y, direction.x) *
                (0.009f + wind * 0.0011f) * Time.deltaTime;
            if (material.HasProperty("_NormalMap"))
                material.SetTextureOffset("_NormalMap", broadOffset);
            if (material.HasProperty("_DetailMap"))
                material.SetTextureOffset("_DetailMap", detailOffset);

            if (waterMesh != null && Time.time >= nextWaveUpdate)
            {
                nextWaveUpdate = Time.time + 0.04f;
                AnimateWaterMesh(direction, wind);
            }

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
            if (material.HasProperty("_NormalScale"))
                material.SetFloat("_NormalScale", Mathf.Lerp(0.44f, 0.78f, Mathf.Clamp01(wind / 17f)));
        }

        private void AnimateWaterMesh(Vector2 direction, float wind)
        {
            if (restingVertices == null || animatedVertices == null)
                return;

            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;
            direction.Normalize();
            Vector2 crossWind = new Vector2(-direction.y, direction.x);
            float windStrength = Mathf.Clamp01(wind / 18f);
            float amplitude = Mathf.Lerp(0.035f, 0.19f, windStrength);
            float speed = 0.72f + wind * 0.055f;
            float time = Time.time;

            for (int i = 0; i < restingVertices.Length; i++)
            {
                Vector3 resting = restingVertices[i];
                float alongWind = resting.x * direction.x + resting.z * direction.y;
                float acrossWind = resting.x * crossWind.x + resting.z * crossWind.y;
                float broadWave = Mathf.Sin(alongWind * 0.052f + time * speed);
                float crossingWave = Mathf.Sin(acrossWind * 0.089f - time * (speed * 0.73f) + 1.7f);
                float shortWave = Mathf.Sin((alongWind + acrossWind) * 0.145f + time * (speed * 1.36f));
                animatedVertices[i] = new Vector3(
                    resting.x,
                    resting.y + broadWave * amplitude + crossingWave * amplitude * 0.38f +
                        shortWave * amplitude * 0.14f,
                    resting.z);
            }

            waterMesh.vertices = animatedVertices;
            waterMesh.RecalculateNormals();
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
        private float lastRefresh;
        private bool settlingNoticeShown;
        private bool coverNoticeShown;

        public float SnowCover01 { get { return snowAmount; } }

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
            snowAmount = climate != null && climate.Weather == WeatherType.Snow ? 0.08f : 0f;
            ApplySnow(snowAmount);
            lastRefresh = Time.time;
        }

        private void Update()
        {
            if (data == null || climate == null || Time.time < nextRefresh)
                return;

            float elapsed = Mathf.Max(0.1f, Time.time - lastRefresh);
            lastRefresh = Time.time;
            nextRefresh = Time.time + 1.5f;
            bool snowing = climate.Weather == WeatherType.Snow && climate.Precipitation > 0f;
            float target = snowing ? 1f : 0f;
            float changePerSecond = snowing
                ? Mathf.Lerp(0.055f, 0.095f, climate.Precipitation)
                : Mathf.Lerp(0.008f, 0.026f, Mathf.InverseLerp(-3f, 7f, climate.AmbientTemperatureC));
            snowAmount = Mathf.MoveTowards(snowAmount, target, changePerSecond * elapsed);
            if (Mathf.Abs(snowAmount - lastAppliedSnow) >= 0.025f || snowAmount == target)
                ApplySnow(snowAmount);

            if (snowing && !settlingNoticeShown && snowAmount >= 0.08f)
            {
                settlingNoticeShown = true;
                if (NorthwildGame.Instance != null)
                    NorthwildGame.Instance.Notify("Fresh snow is beginning to settle on the ground.");
            }
            if (snowing && !coverNoticeShown && snowAmount >= 0.48f)
            {
                coverNoticeShown = true;
                if (NorthwildGame.Instance != null)
                    NorthwildGame.Instance.Notify("The forest floor is developing continuous snow cover.");
            }
            if (!snowing && snowAmount <= 0.01f)
            {
                settlingNoticeShown = false;
                coverNoticeShown = false;
            }
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
                    float flatness = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(27f, 55f, slope));
                    float shoreClearance = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                        lakeSurfaceY + 0.08f, lakeSurfaceY + 0.7f, worldHeight));
                    float breakup = Mathf.Lerp(0.84f, 1f, Mathf.PerlinNoise(nx * 38f + 7f, nz * 38f + 31f));
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

using System;
using UnityEngine;

namespace Northwild
{
    public sealed class WorldGenerator : MonoBehaviour
    {
        private const float WorldSize = 1000f;
        private const float TerrainVerticalSize = 60f;
        private const float HeightmapBaseElevation = 715f;
        private const float LakeSurfaceElevation = 720.95f;
        private const float LakeSurfaceY = LakeSurfaceElevation - HeightmapBaseElevation;
        private const int RealHeightmapResolution = 1025;
        private const string RealHeightmapResource = "Heightmaps/femundsmarka_nedre_roasten";

        private Terrain terrain;
        private Transform generatedRoot;
        private Vector3 playerSpawn;

        public Vector3 PlayerSpawn { get { return playerSpawn; } }

        public void Generate()
        {
            generatedRoot = new GameObject("Generated Scandinavian Woodland").transform;
            generatedRoot.SetParent(transform);
            CreateTerrain();
            playerSpawn = FindDryGround(new Vector2(430f, 520f), 180f, 28f) + Vector3.up * 1.15f;
            CreateLake();
            CreateForest();
            CreateResources();
            CreateLandmarks();
        }

        public float HeightAt(Vector3 worldPosition)
        {
            if (terrain == null)
                return 0f;
            return terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
        }

        private void CreateTerrain()
        {
            float[,] heights = LoadRealHeightmap();
            bool usingRealHeightmap = heights != null;
            if (!usingRealHeightmap)
                heights = CreateFallbackHeightmap(513);

            TerrainData data = new TerrainData();
            data.name = usingRealHeightmap
                ? "Femundsmarka - Nedre Roasten DTM"
                : "Northwild Fallback Terrain";
            data.heightmapResolution = heights.GetLength(0);
            data.size = new Vector3(WorldSize, TerrainVerticalSize, WorldSize);
            data.SetHeights(0, 0, heights);
            CreateGroundLayer(data);

            GameObject terrainObject = Terrain.CreateTerrainGameObject(data);
            terrainObject.name = "Terrain - Femundsmarka 1 km DTM";
            terrainObject.transform.SetParent(generatedRoot);
            terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = WorldSize;
        }

        private static float[,] LoadRealHeightmap()
        {
            TextAsset encoded = Resources.Load<TextAsset>(RealHeightmapResource);
            int expectedBytes = RealHeightmapResolution * RealHeightmapResolution * 2;
            if (encoded == null || encoded.bytes == null || encoded.bytes.Length != expectedBytes)
            {
                Debug.LogWarning(
                    "Northwild could not load the Femundsmarka 16-bit heightmap. " +
                    "Using the procedural fallback terrain.");
                return null;
            }

            byte[] bytes = encoded.bytes;
            float[,] heights = new float[RealHeightmapResolution, RealHeightmapResolution];
            int offset = 0;
            for (int z = 0; z < RealHeightmapResolution; z++)
            {
                for (int x = 0; x < RealHeightmapResolution; x++)
                {
                    ushort encodedHeight = (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
                    heights[z, x] = encodedHeight / 65535f;
                    offset += 2;
                }
            }
            return heights;
        }

        private static float[,] CreateFallbackHeightmap(int resolution)
        {
            float[,] heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float worldX = x / (float)(resolution - 1) * WorldSize;
                    float worldZ = z / (float)(resolution - 1) * WorldSize;
                    float broad = Mathf.PerlinNoise(worldX * 0.0028f + 13f, worldZ * 0.0028f + 31f);
                    float detail = Mathf.PerlinNoise(worldX * 0.011f + 91f, worldZ * 0.011f + 7f);
                    float height = 0.105f + broad * 0.42f + detail * 0.065f;

                    float lakeDistance = Mathf.Sqrt(
                        Mathf.Pow((worldX - 680f) / 340f, 2f) +
                        Mathf.Pow((worldZ - 650f) / 430f, 2f));
                    if (lakeDistance < 1.18f)
                    {
                        float blend = Mathf.InverseLerp(1.18f, 0.82f, lakeDistance);
                        height = Mathf.Lerp(height, 0.045f, blend);
                    }

                    heights[z, x] = height;
                }
            }
            return heights;
        }

        private void CreateGroundLayer(TerrainData data)
        {
            Texture2D groundTexture = Resources.Load<Texture2D>("Textures/forest_ground_albedo");
            Texture2D groundNormal = Resources.Load<Texture2D>("Textures/forest_ground_normal");
            Texture2D groundMask = Resources.Load<Texture2D>("Textures/forest_ground_mask");

            if (groundTexture == null)
                groundTexture = CreateFallbackGroundTexture();

            TerrainLayer groundLayer = new TerrainLayer();
            groundLayer.name = "Mossy Forest Floor";
            groundLayer.diffuseTexture = groundTexture;
            groundLayer.normalMapTexture = groundNormal;
            groundLayer.maskMapTexture = groundMask;
            groundLayer.normalScale = 0.78f;
            groundLayer.tileSize = new Vector2(6.5f, 6.5f);
            groundLayer.metallic = 0f;
            groundLayer.smoothness = 0.08f;
            data.terrainLayers = new[] { groundLayer };

            data.alphamapResolution = 256;
            float[,,] blend = new float[data.alphamapHeight, data.alphamapWidth, 1];
            for (int z = 0; z < data.alphamapHeight; z++)
            {
                for (int x = 0; x < data.alphamapWidth; x++)
                    blend[z, x, 0] = 1f;
            }
            data.SetAlphamaps(0, 0, blend);
        }

        private static Texture2D CreateFallbackGroundTexture()
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGB24, true);
            texture.name = "Fallback Moss and Forest Floor";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;

            Color moss = new Color(0.18f, 0.25f, 0.12f);
            Color earth = new Color(0.25f, 0.19f, 0.11f);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = ((i + i / 4) % 3 == 0) ? earth : moss;
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private void CreateLake()
        {
            GameObject lake = NorthwildVisuals.Primitive(
                PrimitiveType.Cube,
                "Nedre Roasten Water Surface",
                generatedRoot,
                new Vector3(WorldSize * 0.5f, LakeSurfaceY - 0.12f, WorldSize * 0.5f),
                new Vector3(WorldSize, 0.24f, WorldSize),
                new Color(0.055f, 0.19f, 0.27f));
            lake.GetComponent<Renderer>().sharedMaterial = NorthwildVisuals.Material(
                new Color(0.055f, 0.19f, 0.27f), 0.88f);
            lake.GetComponent<BoxCollider>().isTrigger = true;
            lake.AddComponent<WaterSource>();
        }

        private void CreateForest()
        {
            Transform forest = new GameObject("Forest").transform;
            forest.SetParent(generatedRoot);
            System.Random random = new System.Random(7719);

            int created = 0;
            int attempts = 0;
            while (created < 520 && attempts < 30000)
            {
                attempts++;
                float x = 8f + (float)random.NextDouble() * (WorldSize - 16f);
                float z = 8f + (float)random.NextDouble() * (WorldSize - 16f);
                Vector3 position = new Vector3(x, 0f, z);
                position.y = HeightAt(position);
                if (!IsDryGround(position) || TerrainSteepness(position) > 31f)
                    continue;

                bool birch = random.NextDouble() < 0.27;
                float scale = 0.78f + (float)random.NextDouble() * 0.65f;
                CreateTree(forest, position, scale, birch);
                created++;
            }
        }

        private void CreateTree(Transform parent, Vector3 position, float scale, bool birch)
        {
            Transform root = new GameObject(birch ? "Birch" : "Norway Spruce").transform;
            root.SetParent(parent);
            root.position = position;
            root.localScale = Vector3.one * scale;

            Color trunkColour = birch ? new Color(0.78f, 0.78f, 0.7f) : new Color(0.25f, 0.16f, 0.09f);
            GameObject trunk = NorthwildVisuals.Primitive(
                PrimitiveType.Cylinder, "Trunk", root, new Vector3(0f, 2.6f, 0f),
                new Vector3(birch ? 0.22f : 0.32f, 2.6f, birch ? 0.22f : 0.32f), trunkColour);

            Color foliage = birch ? new Color(0.34f, 0.48f, 0.19f) : new Color(0.08f, 0.25f, 0.12f);
            if (birch)
            {
                GameObject crown = NorthwildVisuals.Primitive(
                    PrimitiveType.Sphere, "Crown", root, new Vector3(0f, 5.4f, 0f),
                    new Vector3(2.5f, 3.2f, 2.5f), foliage);
                NorthwildVisuals.RemoveCollider(crown);
            }
            else
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    float width = 3.8f - layer * 0.85f;
                    GameObject crown = NorthwildVisuals.Primitive(
                        PrimitiveType.Sphere, "Needles", root,
                        new Vector3(0f, 3.6f + layer * 1.25f, 0f),
                        new Vector3(width, 1.35f, width), foliage);
                    NorthwildVisuals.RemoveCollider(crown);
                }
            }
        }

        private void CreateResources()
        {
            Transform resources = new GameObject("Ground Resources").transform;
            resources.SetParent(generatedRoot);
            System.Random random = new System.Random(9817);

            SpawnGroup(resources, random, ItemId.Twig, 110);
            SpawnGroup(resources, random, ItemId.Stick, 78);
            SpawnGroup(resources, random, ItemId.Stone, 88);
            SpawnGroup(resources, random, ItemId.BirchBark, 48);
            SpawnGroup(resources, random, ItemId.DryGrass, 42);
            SpawnGroup(resources, random, ItemId.Log, 28);
            SpawnGroup(resources, random, ItemId.Cloudberry, 36);
        }

        private void SpawnGroup(Transform parent, System.Random random, ItemId item, int count)
        {
            int created = 0;
            int attempts = 0;
            while (created < count && attempts < count * 80)
            {
                attempts++;
                bool nearSpawn = created < Mathf.Min(8, count);
                float x;
                float z;
                if (nearSpawn)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float radius = 9f + (float)random.NextDouble() * 58f;
                    x = playerSpawn.x + Mathf.Cos(angle) * radius;
                    z = playerSpawn.z + Mathf.Sin(angle) * radius;
                }
                else
                {
                    x = 6f + (float)random.NextDouble() * (WorldSize - 12f);
                    z = 6f + (float)random.NextDouble() * (WorldSize - 12f);
                }

                if (x < 4f || x > WorldSize - 4f || z < 4f || z > WorldSize - 4f)
                    continue;

                Vector3 position = new Vector3(x, 0f, z);
                position.y = HeightAt(position);
                if (!IsDryGround(position) || TerrainSteepness(position) > 36f)
                    continue;

                position.y += 0.16f;
                CreateResource(parent, position, item);
                created++;
            }
        }

        private void CreateResource(Transform parent, Vector3 position, ItemId item)
        {
            PrimitiveType type = PrimitiveType.Sphere;
            Vector3 scale = Vector3.one * 0.32f;
            Color colour = new Color(0.38f, 0.24f, 0.12f);

            switch (item)
            {
                case ItemId.Stone:
                    scale = new Vector3(0.42f, 0.25f, 0.36f);
                    colour = new Color(0.38f, 0.4f, 0.42f);
                    break;
                case ItemId.Twig:
                    type = PrimitiveType.Cylinder;
                    scale = new Vector3(0.06f, 0.35f, 0.06f);
                    colour = new Color(0.32f, 0.19f, 0.08f);
                    break;
                case ItemId.Stick:
                    type = PrimitiveType.Cylinder;
                    scale = new Vector3(0.09f, 0.65f, 0.09f);
                    colour = new Color(0.3f, 0.17f, 0.07f);
                    break;
                case ItemId.Log:
                    type = PrimitiveType.Cylinder;
                    scale = new Vector3(0.22f, 0.75f, 0.22f);
                    colour = new Color(0.28f, 0.14f, 0.055f);
                    break;
                case ItemId.BirchBark:
                    type = PrimitiveType.Cube;
                    scale = new Vector3(0.42f, 0.04f, 0.3f);
                    colour = new Color(0.78f, 0.7f, 0.48f);
                    break;
                case ItemId.DryGrass:
                    scale = new Vector3(0.45f, 0.18f, 0.45f);
                    colour = new Color(0.62f, 0.52f, 0.18f);
                    break;
                case ItemId.Cloudberry:
                    scale = Vector3.one * 0.22f;
                    colour = new Color(0.95f, 0.48f, 0.08f);
                    break;
            }

            GameObject resource = NorthwildVisuals.Primitive(type, ItemCatalog.DisplayName(item), parent, position, scale, colour);
            resource.transform.rotation = Quaternion.Euler(18f, item.GetHashCode() * 29f, 72f);
            resource.AddComponent<Gatherable>().Initialise(item, 1);
        }

        private void CreateLandmarks()
        {
            Transform landmark = new GameObject("Ridge Marker").transform;
            landmark.SetParent(generatedRoot);
            Vector3 position = FindDryGround(new Vector2(145f, 785f), 240f, 34f);
            landmark.position = position;

            GameObject stone = NorthwildVisuals.Primitive(
                PrimitiveType.Cube, "Standing Stone", landmark, new Vector3(0f, 2.8f, 0f),
                new Vector3(1.8f, 5.6f, 1.3f), new Color(0.33f, 0.36f, 0.38f));
            stone.transform.localRotation = Quaternion.Euler(0f, 18f, -4f);
        }

        private bool IsDryGround(Vector3 position)
        {
            return position.y > LakeSurfaceY + 0.22f;
        }

        private float TerrainSteepness(Vector3 position)
        {
            if (terrain == null)
                return 0f;
            float x = Mathf.Clamp01(position.x / WorldSize);
            float z = Mathf.Clamp01(position.z / WorldSize);
            return terrain.terrainData.GetSteepness(x, z);
        }

        private Vector3 FindDryGround(Vector2 preferred, float searchRadius, float maximumSlope)
        {
            const int samples = 320;
            for (int i = 0; i < samples; i++)
            {
                float fraction = i / (float)(samples - 1);
                float radius = Mathf.Sqrt(fraction) * searchRadius;
                float angle = i * 2.39996323f;
                float x = Mathf.Clamp(preferred.x + Mathf.Cos(angle) * radius, 8f, WorldSize - 8f);
                float z = Mathf.Clamp(preferred.y + Mathf.Sin(angle) * radius, 8f, WorldSize - 8f);
                Vector3 candidate = new Vector3(x, 0f, z);
                candidate.y = HeightAt(candidate);
                if (IsDryGround(candidate) && TerrainSteepness(candidate) <= maximumSlope)
                    return candidate;
            }

            Vector3 fallback = new Vector3(40f, 0f, 40f);
            fallback.y = HeightAt(fallback);
            return fallback;
        }
    }
}

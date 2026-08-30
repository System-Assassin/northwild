using System;
using System.Collections.Generic;
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
        private NorthwildTerrainWeathering terrainWeathering;
        private Transform generatedRoot;
        private Vector3 playerSpawn;

        public Vector3 PlayerSpawn { get { return playerSpawn; } }
        public float SnowCover01
        {
            get { return terrainWeathering == null ? 0f : terrainWeathering.SnowCover01; }
        }

        public void Generate()
        {
            generatedRoot = new GameObject("Generated Scandinavian Woodland").transform;
            generatedRoot.SetParent(transform);
            CreateTerrain();
            playerSpawn = FindDryGround(new Vector2(430f, 520f), 180f, 28f) + Vector3.up * 1.15f;
            CreateLake();
            CreateForest();
            CreateUndergrowth();
            CreateResources();
            CreateForestDebris();
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
            terrain.treeDistance = 720f;
            terrain.treeBillboardDistance = 230f;
            if (NorthwildGame.Instance != null)
            {
                terrainWeathering = terrainObject.AddComponent<NorthwildTerrainWeathering>();
                terrainWeathering.Configure(
                    terrain,
                    NorthwildGame.Instance.Climate,
                    LakeSurfaceY);
            }
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
            Texture2D rockTexture = Resources.Load<Texture2D>("Textures/mossy_rock_albedo");
            Texture2D rockNormal = Resources.Load<Texture2D>("Textures/mossy_rock_normal");
            Texture2D rockMask = Resources.Load<Texture2D>("Textures/mossy_rock_mask");

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

            TerrainLayer rockLayer = new TerrainLayer();
            rockLayer.name = "Exposed Mossy Granite";
            rockLayer.diffuseTexture = rockTexture != null ? rockTexture : groundTexture;
            rockLayer.normalMapTexture = rockNormal;
            rockLayer.maskMapTexture = rockMask;
            rockLayer.normalScale = 0.9f;
            rockLayer.tileSize = new Vector2(4.2f, 4.2f);
            rockLayer.metallic = 0f;
            rockLayer.smoothness = 0.12f;

            TerrainLayer shorelineLayer = new TerrainLayer();
            shorelineLayer.name = "Damp Peat and Shore Gravel";
            shorelineLayer.diffuseTexture = CreateShorelineTexture();
            shorelineLayer.normalScale = 0.45f;
            shorelineLayer.tileSize = new Vector2(3.2f, 3.2f);
            shorelineLayer.metallic = 0f;
            shorelineLayer.smoothness = 0.18f;

            TerrainLayer snowLayer = new TerrainLayer();
            snowLayer.name = "Fresh Scandinavian Snow";
            snowLayer.diffuseTexture = CreateSnowTexture();
            snowLayer.normalScale = 0.3f;
            snowLayer.tileSize = new Vector2(4.8f, 4.8f);
            snowLayer.metallic = 0f;
            snowLayer.smoothness = 0.24f;
            data.terrainLayers = new[] { groundLayer, rockLayer, shorelineLayer, snowLayer };

            data.alphamapResolution = 512;
            float[,,] blend = new float[data.alphamapHeight, data.alphamapWidth, 4];
            for (int z = 0; z < data.alphamapHeight; z++)
            {
                for (int x = 0; x < data.alphamapWidth; x++)
                {
                    float normalX = x / (float)(data.alphamapWidth - 1);
                    float normalZ = z / (float)(data.alphamapHeight - 1);
                    float slope = data.GetSteepness(normalX, normalZ);
                    float noise = Mathf.PerlinNoise(normalX * 31f + 8f, normalZ * 31f + 19f);
                    float exposedRock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(24f, 43f, slope));
                    exposedRock = Mathf.Clamp01(exposedRock + Mathf.Max(0f, noise - 0.72f) * 0.24f);
                    float worldHeight = data.GetInterpolatedHeight(normalX, normalZ);
                    float wetShore = worldHeight <= LakeSurfaceY
                        ? 0f
                        : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                            LakeSurfaceY + 0.18f,
                            LakeSurfaceY + 2.8f,
                            worldHeight));
                    wetShore *= 1f - Mathf.Clamp01(slope / 34f);
                    wetShore *= Mathf.Lerp(0.78f, 1f, noise);
                    float remaining = 1f - wetShore;
                    blend[z, x, 0] = (1f - exposedRock) * remaining;
                    blend[z, x, 1] = exposedRock * remaining;
                    blend[z, x, 2] = wetShore;
                    blend[z, x, 3] = 0f;
                }
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

        private static Texture2D CreateShorelineTexture()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true);
            texture.name = "Procedural Damp Peat and Shore Gravel";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 6;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float peat = Mathf.PerlinNoise(x * 0.048f + 11f, y * 0.048f + 29f);
                    float gravel = Mathf.PerlinNoise(x * 0.22f + 43f, y * 0.22f + 7f);
                    Color darkPeat = new Color(0.105f, 0.09f, 0.062f);
                    Color stone = new Color(0.31f, 0.315f, 0.29f);
                    float stones = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.69f, 0.87f, gravel));
                    pixels[y * size + x] = Color.Lerp(
                        darkPeat * Mathf.Lerp(0.78f, 1.18f, peat),
                        stone,
                        stones * 0.58f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static Texture2D CreateSnowTexture()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true);
            texture.name = "Procedural Wind-Packed Snow";
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 6;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float broad = Mathf.PerlinNoise(x * 0.04f + 19f, y * 0.04f + 61f);
                    float crystals = Mathf.PerlinNoise(x * 0.27f + 3f, y * 0.27f + 41f);
                    float brightness = Mathf.Lerp(0.84f, 0.98f, broad * 0.72f + crystals * 0.28f);
                    pixels[y * size + x] = new Color(
                        brightness * 0.91f,
                        brightness * 0.95f,
                        brightness);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private void CreateLake()
        {
            GameObject lake = new GameObject("Nedre Roasten Water Surface");
            lake.transform.SetParent(generatedRoot, false);
            lake.transform.localPosition = new Vector3(
                WorldSize * 0.5f,
                LakeSurfaceY - 0.055f,
                WorldSize * 0.5f);
            MeshFilter waterMeshFilter = lake.AddComponent<MeshFilter>();
            waterMeshFilter.sharedMesh = CreateLakeMesh();
            MeshRenderer waterRenderer = lake.AddComponent<MeshRenderer>();
            waterRenderer.sharedMaterial = NorthwildVisuals.WaterMaterial();
            waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            waterRenderer.receiveShadows = true;
            waterRenderer.allowOcclusionWhenDynamic = false;
            BoxCollider waterTrigger = lake.AddComponent<BoxCollider>();
            waterTrigger.center = new Vector3(0f, -0.04f, 0f);
            waterTrigger.size = new Vector3(WorldSize, 0.14f, WorldSize);
            waterTrigger.isTrigger = true;
            lake.AddComponent<WaterSource>();
            if (NorthwildGame.Instance != null)
                lake.AddComponent<NorthwildWaterSurface>().Configure(
                    waterRenderer,
                    waterMeshFilter,
                    NorthwildGame.Instance.Climate);
        }

        private static Mesh CreateLakeMesh()
        {
            const int resolution = 64;
            int verticesPerSide = resolution + 1;
            Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[resolution * resolution * 6];

            for (int z = 0; z < verticesPerSide; z++)
            {
                float normalZ = z / (float)resolution;
                for (int x = 0; x < verticesPerSide; x++)
                {
                    float normalX = x / (float)resolution;
                    int index = z * verticesPerSide + x;
                    vertices[index] = new Vector3(
                        (normalX - 0.5f) * WorldSize,
                        0f,
                        (normalZ - 0.5f) * WorldSize);
                    normals[index] = Vector3.up;
                    uvs[index] = new Vector2(normalX, normalZ);
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int lowerLeft = z * verticesPerSide + x;
                    int upperLeft = lowerLeft + verticesPerSide;
                    triangles[triangleIndex++] = lowerLeft;
                    triangles[triangleIndex++] = upperLeft;
                    triangles[triangleIndex++] = lowerLeft + 1;
                    triangles[triangleIndex++] = lowerLeft + 1;
                    triangles[triangleIndex++] = upperLeft;
                    triangles[triangleIndex++] = upperLeft + 1;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "Animated Nedre Roasten Water Grid";
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(WorldSize, 1.5f, WorldSize));
            return mesh;
        }

        private void CreateForest()
        {
            Transform forest = new GameObject("Forest").transform;
            forest.SetParent(generatedRoot);
            System.Random random = new System.Random(7719);

            int created = 0;
            int attempts = 0;
            while (created < 780 && attempts < 50000)
            {
                attempts++;
                bool nearSpawn = created < 170;
                float x;
                float z;
                if (nearSpawn)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float radius = 18f + Mathf.Sqrt((float)random.NextDouble()) * 175f;
                    x = playerSpawn.x + Mathf.Cos(angle) * radius;
                    z = playerSpawn.z + Mathf.Sin(angle) * radius;
                }
                else
                {
                    x = 8f + (float)random.NextDouble() * (WorldSize - 16f);
                    z = 8f + (float)random.NextDouble() * (WorldSize - 16f);
                }

                if (x < 8f || x > WorldSize - 8f || z < 8f || z > WorldSize - 8f)
                    continue;
                float forestDensity = Mathf.PerlinNoise(x * 0.0065f + 18f, z * 0.0065f + 73f);
                if (!nearSpawn && forestDensity < 0.36f)
                    continue;

                Vector3 position = new Vector3(x, 0f, z);
                position.y = HeightAt(position);
                if (!IsDryGround(position) || TerrainSteepness(position) > 31f)
                    continue;

                bool birch = random.NextDouble() < Mathf.Lerp(0.18f, 0.38f, 1f - forestDensity);
                float scale = 0.76f + (float)random.NextDouble() * 0.58f;
                CreateTree(forest, position, scale, birch);
                created++;
            }

            if (NorthwildGame.Instance != null)
                forest.gameObject.AddComponent<NorthwildVegetationWind>().Configure(
                    NorthwildGame.Instance.Climate);
        }

        private void CreateTree(Transform parent, Vector3 position, float scale, bool birch)
        {
            Transform root = new GameObject(birch ? "Birch" : "Norway Spruce").transform;
            root.SetParent(parent);
            root.position = position;
            root.localScale = Vector3.one * scale;
            root.localRotation = Quaternion.Euler(0f, Mathf.Repeat(position.x * 17.3f + position.z * 9.7f, 360f), 0f);

            Color trunkColour = birch ? new Color(0.78f, 0.78f, 0.7f) : new Color(0.25f, 0.16f, 0.09f);
            GameObject trunk = NorthwildVisuals.Primitive(
                PrimitiveType.Cylinder, "Trunk", root, new Vector3(0f, birch ? 3.65f : 3.45f, 0f),
                new Vector3(birch ? 0.2f : 0.34f, birch ? 3.65f : 3.45f, birch ? 0.2f : 0.34f), trunkColour);
            trunk.transform.localRotation = Quaternion.Euler(0f, 0f, birch ? -1.8f : 0.6f);

            Color foliage = birch
                ? new Color(0.78f, 0.86f, 0.65f)
                : new Color(0.62f, 0.73f, 0.56f);
            NorthwildVisuals.CreateFoliageCrown(
                birch ? "Downy Birch Crown" : "Layered Norway Spruce Crown",
                root,
                Vector3.zero,
                birch ? new Vector3(1.08f, 1.08f, 1.08f) : Vector3.one,
                birch,
                foliage);
        }

        private void CreateUndergrowth()
        {
            Transform undergrowth = new GameObject("Batched Boreal Undergrowth").transform;
            undergrowth.SetParent(generatedRoot);
            System.Random random = new System.Random(48321);
            List<Vector3> positions = new List<Vector3>(240);
            List<float> scales = new List<float>(240);
            List<float> yaws = new List<float>(240);
            int created = 0;
            int attempts = 0;
            int patch = 0;

            while (created < 1550 && attempts < 65000)
            {
                attempts++;
                bool nearSpawn = created < 650;
                float x;
                float z;
                if (nearSpawn)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float radius = 10f + Mathf.Sqrt((float)random.NextDouble()) * 190f;
                    x = playerSpawn.x + Mathf.Cos(angle) * radius;
                    z = playerSpawn.z + Mathf.Sin(angle) * radius;
                }
                else
                {
                    x = 6f + (float)random.NextDouble() * (WorldSize - 12f);
                    z = 6f + (float)random.NextDouble() * (WorldSize - 12f);
                }

                if (x < 5f || x > WorldSize - 5f || z < 5f || z > WorldSize - 5f)
                    continue;
                float density = Mathf.PerlinNoise(x * 0.012f + 47f, z * 0.012f + 11f);
                if (!nearSpawn && density < 0.37f)
                    continue;

                Vector3 position = new Vector3(x, 0f, z);
                position.y = HeightAt(position) + 0.025f;
                if (!IsDryGround(position) || TerrainSteepness(position) > 34f)
                    continue;

                positions.Add(position);
                scales.Add(0.58f + (float)random.NextDouble() * 0.82f);
                yaws.Add((float)random.NextDouble() * 180f);
                created++;

                if (positions.Count >= 240)
                {
                    NorthwildVisuals.CreateUndergrowthPatch(
                        "Undergrowth Patch " + patch++, undergrowth, positions, scales, yaws);
                    positions = new List<Vector3>(240);
                    scales = new List<float>(240);
                    yaws = new List<float>(240);
                }
            }

            if (positions.Count > 0)
                NorthwildVisuals.CreateUndergrowthPatch(
                    "Undergrowth Patch " + patch, undergrowth, positions, scales, yaws);
        }

        private void CreateForestDebris()
        {
            Transform debris = new GameObject("Fallen Timber and Rock Clusters").transform;
            debris.SetParent(generatedRoot);
            System.Random random = new System.Random(16017);

            for (int i = 0; i < 42; i++)
            {
                bool nearSpawn = i < 18;
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = nearSpawn
                    ? 28f + (float)random.NextDouble() * 165f
                    : 80f + (float)random.NextDouble() * 390f;
                float x = nearSpawn
                    ? playerSpawn.x + Mathf.Cos(angle) * radius
                    : 35f + (float)random.NextDouble() * (WorldSize - 70f);
                float z = nearSpawn
                    ? playerSpawn.z + Mathf.Sin(angle) * radius
                    : 35f + (float)random.NextDouble() * (WorldSize - 70f);
                if (x < 8f || x > WorldSize - 8f || z < 8f || z > WorldSize - 8f)
                    continue;

                Vector3 position = new Vector3(x, 0f, z);
                position.y = HeightAt(position);
                if (!IsDryGround(position) || TerrainSteepness(position) > 28f)
                    continue;

                float logRadius = 0.17f + (float)random.NextDouble() * 0.13f;
                float logLength = 2.4f + (float)random.NextDouble() * 2.5f;
                position.y += logRadius * 0.72f;
                NorthwildVisuals.CreateLog(
                    "Fallen Weathered Spruce",
                    debris,
                    position,
                    Quaternion.Euler(
                        -5f + (float)random.NextDouble() * 10f,
                        (float)random.NextDouble() * 360f,
                        90f),
                    logRadius,
                    logLength,
                    false,
                    true);
            }

            for (int i = 0; i < 28; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = 25f + (float)random.NextDouble() * 230f;
                Vector3 position = new Vector3(
                    playerSpawn.x + Mathf.Cos(angle) * radius,
                    0f,
                    playerSpawn.z + Mathf.Sin(angle) * radius);
                if (position.x < 5f || position.x > WorldSize - 5f ||
                    position.z < 5f || position.z > WorldSize - 5f)
                    continue;
                position.y = HeightAt(position) + 0.2f;
                if (!IsDryGround(position) || TerrainSteepness(position) > 30f)
                    continue;

                GameObject rock = NorthwildVisuals.Primitive(
                    PrimitiveType.Sphere,
                    "Mossy Glacial Stone",
                    debris,
                    position,
                    new Vector3(
                        0.5f + (float)random.NextDouble() * 0.8f,
                        0.28f + (float)random.NextDouble() * 0.42f,
                        0.48f + (float)random.NextDouble() * 0.76f),
                    new Color(0.55f, 0.58f, 0.54f));
                rock.transform.localRotation = Quaternion.Euler(
                    (float)random.NextDouble() * 25f,
                    (float)random.NextDouble() * 360f,
                    (float)random.NextDouble() * 18f);
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
            GameObject resource = new GameObject(ItemCatalog.DisplayName(item));
            resource.transform.SetParent(parent, false);
            resource.transform.localPosition = position;
            resource.transform.localRotation = Quaternion.Euler(
                item == ItemId.Stone ? 8f : 0f,
                Mathf.Repeat(position.x * 23f + position.z * 41f, 360f),
                item == ItemId.Stone ? 5f : 0f);
            float pickupRadius = 0.38f;
            switch (item)
            {
                case ItemId.Stone:
                    GameObject stone = NorthwildVisuals.Primitive(
                        PrimitiveType.Sphere,
                        "Mossy Gatherable Stone",
                        resource.transform,
                        Vector3.zero,
                        new Vector3(0.42f, 0.25f, 0.36f),
                        new Color(0.57f, 0.59f, 0.55f));
                    NorthwildVisuals.RemoveCollider(stone);
                    pickupRadius = 0.44f;
                    break;
                case ItemId.Twig:
                    NorthwildVisuals.CreateLog(
                        "Dry Twig",
                        resource.transform,
                        Vector3.zero,
                        Quaternion.Euler(6f, 0f, 88f),
                        0.035f,
                        0.72f,
                        false,
                        false);
                    break;
                case ItemId.Stick:
                    NorthwildVisuals.CreateLog(
                        "Dry Stick",
                        resource.transform,
                        Vector3.zero,
                        Quaternion.Euler(5f, 0f, 90f),
                        0.065f,
                        1.25f,
                        false,
                        false);
                    pickupRadius = 0.68f;
                    break;
                case ItemId.Log:
                    NorthwildVisuals.CreateLog(
                        "Split Firewood Log",
                        resource.transform,
                        Vector3.zero,
                        Quaternion.Euler(4f, 0f, 90f),
                        0.18f,
                        1.35f,
                        false,
                        false);
                    pickupRadius = 0.78f;
                    break;
                case ItemId.BirchBark:
                    for (int strip = 0; strip < 3; strip++)
                    {
                        GameObject bark = NorthwildVisuals.Primitive(
                            PrimitiveType.Cube,
                            "Birch Bark Strip",
                            resource.transform,
                            new Vector3((strip - 1) * 0.09f, strip * 0.025f, (strip % 2) * 0.06f),
                            new Vector3(0.34f, 0.018f, 0.19f),
                            new Color(0.88f, 0.86f, 0.74f));
                        bark.transform.localRotation = Quaternion.Euler(strip * 8f, strip * 27f, strip * 6f - 5f);
                        NorthwildVisuals.RemoveCollider(bark);
                    }
                    break;
                case ItemId.DryGrass:
                    for (int blade = 0; blade < 9; blade++)
                    {
                        float angle = blade / 9f * Mathf.PI * 2f;
                        GameObject grass = NorthwildVisuals.Primitive(
                            PrimitiveType.Cylinder,
                            "Dry Grass Stem",
                            resource.transform,
                            new Vector3(Mathf.Cos(angle) * 0.13f, 0.18f, Mathf.Sin(angle) * 0.13f),
                            new Vector3(0.012f, 0.2f + (blade % 3) * 0.035f, 0.012f),
                            new Color(0.56f, 0.43f, 0.16f));
                        grass.transform.localRotation = Quaternion.Euler(
                            Mathf.Cos(angle) * 16f, 0f, Mathf.Sin(angle) * 16f);
                        NorthwildVisuals.RemoveCollider(grass);
                    }
                    break;
                case ItemId.Cloudberry:
                    NorthwildVisuals.CreateUndergrowthPatch(
                        "Cloudberry Leaves",
                        resource.transform,
                        new[] { Vector3.zero },
                        new[] { 0.42f },
                        new[] { 0f });
                    for (int berry = 0; berry < 4; berry++)
                    {
                        float angle = berry / 4f * Mathf.PI * 2f;
                        GameObject fruit = NorthwildVisuals.Primitive(
                            PrimitiveType.Sphere,
                            "Ripe Cloudberry",
                            resource.transform,
                            new Vector3(Mathf.Cos(angle) * 0.15f, 0.28f + (berry % 2) * 0.07f, Mathf.Sin(angle) * 0.15f),
                            Vector3.one * 0.085f,
                            new Color(0.96f, 0.39f, 0.035f));
                        NorthwildVisuals.RemoveCollider(fruit);
                    }
                    break;
            }

            SphereCollider pickup = resource.AddComponent<SphereCollider>();
            pickup.radius = pickupRadius;
            pickup.center = new Vector3(0f, 0.12f, 0f);
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Northwild
{
    public static class NorthwildVisuals
    {
        private enum SurfaceType
        {
            Plain,
            PineBark,
            BirchBark,
            Foliage,
            Stone,
            CutWood,
            Cloth
        }

        private static Texture2D birchBarkTexture;
        private static Texture2D foliageTexture;
        private static Texture2D cutWoodTexture;
        private static Texture2D clothTexture;
        private static Mesh spruceCrownMesh;
        private static Mesh birchCrownMesh;
        private static readonly Dictionary<string, Material> sharedPrimitiveMaterials =
            new Dictionary<string, Material>();
        private static readonly Dictionary<string, Material> sharedVegetationMaterials =
            new Dictionary<string, Material>();

        public static Material Material(Color colour, float smoothness = 0.15f)
        {
            return CreateMaterial(colour, smoothness, SurfaceType.Plain);
        }

        private static Material CreateMaterial(Color colour, float smoothness, SurfaceType surface)
        {
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            material.color = colour;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            ApplySurfaceTextures(material, surface);
            if (shader.name.StartsWith("HDRP/"))
                HDMaterial.ValidateMaterial(material);
            return material;
        }

        public static Material EmissiveMaterial(Color baseColour, Color emissionColour, float intensity)
        {
            Material material = Material(baseColour, 0.08f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColour * Mathf.Max(0f, intensity));
            }
            if (material.HasProperty("_EmissiveColor"))
            {
                material.SetColor("_EmissiveColor", emissionColour.linear * Mathf.Max(0f, intensity));
                HDMaterial.ValidateMaterial(material);
            }
            return material;
        }

        public static GameObject Primitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color colour)
        {
            GameObject created = GameObject.CreatePrimitive(type);
            created.name = name;
            created.transform.SetParent(parent, false);
            created.transform.localPosition = localPosition;
            created.transform.localScale = localScale;
            Renderer renderer = created.GetComponent<Renderer>();
            if (renderer != null)
            {
                SurfaceType surface = SurfaceFor(name, colour);
                Color tint = SurfaceTint(name, colour, surface);
                float smoothness = SurfaceSmoothness(name);
                renderer.sharedMaterial = SharedPrimitiveMaterial(tint, smoothness, surface);
            }
            return created;
        }

        private static Material SharedPrimitiveMaterial(Color colour, float smoothness, SurfaceType surface)
        {
            string key = surface + ":" + ColorUtility.ToHtmlStringRGBA(colour) + ":" +
                Mathf.RoundToInt(smoothness * 1000f);
            Material material;
            if (sharedPrimitiveMaterials.TryGetValue(key, out material) && material != null)
                return material;

            material = CreateMaterial(colour, smoothness, surface);
            material.name = "Northwild Shared " + key;
            sharedPrimitiveMaterials[key] = material;
            return material;
        }

        private static Color SurfaceTint(string objectName, Color colour, SurfaceType surface)
        {
            if (surface == SurfaceType.Foliage || surface == SurfaceType.Plain ||
                surface == SurfaceType.CutWood || surface == SurfaceType.Cloth)
                return colour;
            if (surface == SurfaceType.BirchBark)
                return new Color(0.94f, 0.94f, 0.9f);
            if (surface == SurfaceType.PineBark && objectName.ToLowerInvariant().Contains("charred"))
                return new Color(0.28f, 0.2f, 0.17f);

            float brightest = Mathf.Max(0.01f, Mathf.Max(colour.r, Mathf.Max(colour.g, colour.b)));
            Color hue = new Color(colour.r / brightest, colour.g / brightest, colour.b / brightest);
            return surface == SurfaceType.Stone
                ? Color.Lerp(Color.white, hue, 0.12f)
                : Color.Lerp(Color.white, hue, 0.2f);
        }

        private static SurfaceType SurfaceFor(string objectName, Color colour)
        {
            string lower = objectName.ToLowerInvariant();
            if (lower.Contains("stone"))
                return SurfaceType.Stone;
            if (lower.Contains("cut wood") || lower.Contains("split face"))
                return SurfaceType.CutWood;
            if (lower.Contains("parka") || lower.Contains("trouser") || lower.Contains("hood") ||
                lower.Contains("backpack") || lower.Contains("glove") || lower.Contains("boot"))
                return SurfaceType.Cloth;
            if (lower.Contains("birch bark") || (lower.Contains("trunk") && colour.r > 0.6f))
                return SurfaceType.BirchBark;
            if (lower.Contains("trunk") || lower.Contains("twig") || lower.Contains("stick") ||
                lower.Contains("log") || lower.Contains("post") || lower.Contains("pole") ||
                lower.Contains("firewood"))
                return SurfaceType.PineBark;
            if (lower.Contains("crown") || lower.Contains("needle") || lower.Contains("bough") ||
                lower.Contains("bed"))
                return SurfaceType.Foliage;
            return SurfaceType.Plain;
        }

        private static float SurfaceSmoothness(string objectName)
        {
            string lower = objectName.ToLowerInvariant();
            if (lower.Contains("stone"))
                return 0.16f;
            if (lower.Contains("cut wood") || lower.Contains("split face"))
                return 0.07f;
            if (lower.Contains("parka") || lower.Contains("trouser") || lower.Contains("hood") ||
                lower.Contains("backpack") || lower.Contains("glove") || lower.Contains("boot"))
                return 0.04f;
            if (lower.Contains("trunk") || lower.Contains("bark") || lower.Contains("wood") ||
                lower.Contains("twig") || lower.Contains("stick") || lower.Contains("log"))
                return 0.06f;
            if (lower.Contains("crown") || lower.Contains("needle") || lower.Contains("bough"))
                return 0.12f;
            return 0.15f;
        }

        private static void ApplySurfaceTextures(Material material, SurfaceType surface)
        {
            Texture2D albedo = null;
            Texture2D normal = null;
            Texture2D mask = null;
            Vector2 tiling = Vector2.one;

            switch (surface)
            {
                case SurfaceType.PineBark:
                    albedo = Resources.Load<Texture2D>("Textures/pine_bark_albedo");
                    normal = Resources.Load<Texture2D>("Textures/pine_bark_normal");
                    mask = Resources.Load<Texture2D>("Textures/pine_bark_mask");
                    tiling = new Vector2(1.4f, 2.8f);
                    break;
                case SurfaceType.BirchBark:
                    albedo = BirchBarkTexture();
                    tiling = new Vector2(1.25f, 2.5f);
                    break;
                case SurfaceType.Foliage:
                    albedo = FoliageTexture();
                    tiling = new Vector2(2.2f, 2.2f);
                    break;
                case SurfaceType.Stone:
                    albedo = Resources.Load<Texture2D>("Textures/mossy_rock_albedo");
                    normal = Resources.Load<Texture2D>("Textures/mossy_rock_normal");
                    mask = Resources.Load<Texture2D>("Textures/mossy_rock_mask");
                    tiling = new Vector2(1.35f, 1.35f);
                    break;
                case SurfaceType.CutWood:
                    albedo = CutWoodTexture();
                    tiling = new Vector2(1.25f, 1.25f);
                    break;
                case SurfaceType.Cloth:
                    albedo = ClothTexture();
                    tiling = new Vector2(4f, 4f);
                    break;
            }

            SetTexture(material, "_BaseColorMap", "_MainTex", albedo, tiling);
            SetTexture(material, "_NormalMap", "_BumpMap", normal, tiling);
            SetTexture(material, "_MaskMap", "_MetallicGlossMap", mask, tiling);
            if (normal != null && material.HasProperty("_NormalScale"))
                material.SetFloat("_NormalScale", surface == SurfaceType.Stone ? 0.72f : 0.9f);
        }

        private static void SetTexture(
            Material material,
            string hdrpProperty,
            string fallbackProperty,
            Texture texture,
            Vector2 tiling)
        {
            if (texture == null)
                return;

            string property = material.HasProperty(hdrpProperty) ? hdrpProperty : fallbackProperty;
            if (!material.HasProperty(property))
                return;
            material.SetTexture(property, texture);
            material.SetTextureScale(property, tiling);
        }

        private static Texture2D BirchBarkTexture()
        {
            if (birchBarkTexture != null)
                return birchBarkTexture;

            const int width = 128;
            const int height = 256;
            birchBarkTexture = new Texture2D(width, height, TextureFormat.RGB24, true);
            birchBarkTexture.name = "Procedural Scandinavian Birch Bark";
            birchBarkTexture.wrapMode = TextureWrapMode.Repeat;
            birchBarkTexture.filterMode = FilterMode.Trilinear;
            birchBarkTexture.anisoLevel = 4;

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.075f, y * 0.032f);
                    float pore = Mathf.PerlinNoise(x * 0.19f + 37f, y * 0.24f + 91f);
                    Color colour = Color.Lerp(new Color(0.72f, 0.73f, 0.68f), Color.white, grain);
                    bool darkDash = pore > 0.72f && (y % 29) < 4 && (x % 31) > 5;
                    if (darkDash)
                        colour *= 0.2f;
                    pixels[y * width + x] = colour;
                }
            }

            birchBarkTexture.SetPixels(pixels);
            birchBarkTexture.Apply(true, true);
            return birchBarkTexture;
        }

        private static Texture2D FoliageTexture()
        {
            if (foliageTexture != null)
                return foliageTexture;

            const int size = 128;
            foliageTexture = new Texture2D(size, size, TextureFormat.RGB24, true);
            foliageTexture.name = "Procedural Conifer Foliage Detail";
            foliageTexture.wrapMode = TextureWrapMode.Repeat;
            foliageTexture.filterMode = FilterMode.Trilinear;
            foliageTexture.anisoLevel = 4;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float broad = Mathf.PerlinNoise(x * 0.055f + 12f, y * 0.055f + 28f);
                    float needles = Mathf.Abs(Mathf.Sin((x * 0.63f + y * 0.24f) + broad * 5f));
                    float value = Mathf.Lerp(0.48f, 1f, broad * 0.65f + needles * 0.35f);
                    pixels[y * size + x] = new Color(value * 0.88f, value, value * 0.82f);
                }
            }

            foliageTexture.SetPixels(pixels);
            foliageTexture.Apply(true, true);
            return foliageTexture;
        }

        private static Texture2D CutWoodTexture()
        {
            if (cutWoodTexture != null)
                return cutWoodTexture;

            const int size = 128;
            cutWoodTexture = new Texture2D(size, size, TextureFormat.RGB24, true);
            cutWoodTexture.name = "Procedural Cut Wood Rings";
            cutWoodTexture.wrapMode = TextureWrapMode.Repeat;
            cutWoodTexture.filterMode = FilterMode.Trilinear;
            cutWoodTexture.anisoLevel = 4;

            Color[] pixels = new Color[size * size];
            Vector2 centre = Vector2.one * (size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 delta = new Vector2(x, y) - centre;
                    float radius = delta.magnitude;
                    float wobble = Mathf.PerlinNoise(x * 0.055f + 9f, y * 0.055f + 21f) * 4.5f;
                    float ring = Mathf.Pow(Mathf.Abs(Mathf.Sin((radius + wobble) * 0.34f)), 5f);
                    float ray = Mathf.Abs(Mathf.Sin(Mathf.Atan2(delta.y, delta.x) * 11f)) * 0.08f;
                    float tone = Mathf.Clamp01(0.78f - ring * 0.24f - ray);
                    pixels[y * size + x] = new Color(tone, tone * 0.72f, tone * 0.43f);
                }
            }

            cutWoodTexture.SetPixels(pixels);
            cutWoodTexture.Apply(true, true);
            return cutWoodTexture;
        }

        private static Texture2D ClothTexture()
        {
            if (clothTexture != null)
                return clothTexture;

            const int size = 128;
            clothTexture = new Texture2D(size, size, TextureFormat.RGB24, true);
            clothTexture.name = "Procedural Woven Outdoor Fabric";
            clothTexture.wrapMode = TextureWrapMode.Repeat;
            clothTexture.filterMode = FilterMode.Trilinear;
            clothTexture.anisoLevel = 4;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float weave = ((x + y) & 3) == 0 ? 0.78f : 0.96f;
                    float noise = Mathf.Lerp(0.92f, 1.04f, Mathf.PerlinNoise(x * 0.18f, y * 0.18f));
                    float value = weave * noise;
                    pixels[y * size + x] = new Color(value, value, value);
                }
            }

            clothTexture.SetPixels(pixels);
            clothTexture.Apply(true, true);
            return clothTexture;
        }

        public static Material VegetationMaterial(string resourcePath, Color tint, float cutoff = 0.34f)
        {
            string key = resourcePath + ":" + ColorUtility.ToHtmlStringRGBA(tint) + ":" +
                Mathf.RoundToInt(cutoff * 100f);
            Material material;
            if (sharedVegetationMaterials.TryGetValue(key, out material) && material != null)
                return material;

            material = CreateMaterial(tint, 0.05f, SurfaceType.Plain);
            material.name = "Northwild Vegetation " + key;
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                texture = FoliageTexture();
            SetTexture(material, "_BaseColorMap", "_MainTex", texture, Vector2.one);

            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.EnableKeyword("_ALPHATEST_ON");
            if (material.HasProperty("_AlphaCutoffEnable"))
                material.SetFloat("_AlphaCutoffEnable", 1f);
            if (material.HasProperty("_AlphaCutoff"))
                material.SetFloat("_AlphaCutoff", cutoff);
            if (material.HasProperty("_Cutoff"))
                material.SetFloat("_Cutoff", cutoff);
            if (material.HasProperty("_AlphaCutoffShadow"))
                material.SetFloat("_AlphaCutoffShadow", cutoff + 0.04f);
            if (material.HasProperty("_DoubleSidedEnable"))
                material.SetFloat("_DoubleSidedEnable", 1f);
            if (material.HasProperty("_CullMode"))
                material.SetFloat("_CullMode", (float)CullMode.Off);
            if (material.HasProperty("_CullModeForward"))
                material.SetFloat("_CullModeForward", (float)CullMode.Off);
            material.doubleSidedGI = true;
            material.enableInstancing = true;
            if (material.shader != null && material.shader.name.StartsWith("HDRP/"))
                HDMaterial.ValidateMaterial(material);
            sharedVegetationMaterials[key] = material;
            return material;
        }

        public static GameObject CreateFoliageCrown(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            bool birch,
            Color tint)
        {
            Mesh mesh = birch ? BirchCrownMesh() : SpruceCrownMesh();
            Material material = VegetationMaterial(
                birch ? "Vegetation/birch_branch" : "Vegetation/spruce_bough",
                tint,
                birch ? 0.3f : 0.36f);
            return MeshObject(name, parent, localPosition, Quaternion.identity, localScale, mesh, material);
        }

        public static GameObject CreateLog(
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            float radius,
            float length,
            bool charred,
            bool keepCollider)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = localRotation;

            Color bark = charred
                ? new Color(0.17f, 0.11f, 0.085f)
                : new Color(0.34f, 0.19f, 0.085f);
            GameObject body = Primitive(
                PrimitiveType.Cylinder,
                charred ? "Charred Firewood" : "Pine Bark Log",
                root.transform,
                Vector3.zero,
                new Vector3(radius, length * 0.5f, radius),
                bark);
            if (!keepCollider)
                RemoveCollider(body);

            Color cut = charred
                ? new Color(0.19f, 0.105f, 0.055f)
                : new Color(0.72f, 0.49f, 0.25f);
            for (int end = -1; end <= 1; end += 2)
            {
                GameObject cap = Primitive(
                    PrimitiveType.Cylinder,
                    "Cut Wood End",
                    root.transform,
                    new Vector3(0f, end * length * 0.501f, 0f),
                    new Vector3(radius * 0.94f, 0.008f, radius * 0.94f),
                    cut);
                RemoveCollider(cap);
            }
            return root;
        }

        public static GameObject CreateUndergrowthPatch(
            string name,
            Transform parent,
            IList<Vector3> positions,
            IList<float> scales,
            IList<float> yaws)
        {
            List<Vector3> vertices = new List<Vector3>(positions.Count * 8);
            List<Vector2> uvs = new List<Vector2>(positions.Count * 8);
            List<int> triangles = new List<int>(positions.Count * 12);
            for (int i = 0; i < positions.Count; i++)
            {
                float scale = i < scales.Count ? scales[i] : 1f;
                float yaw = i < yaws.Count ? yaws[i] : 0f;
                float width = 1.18f * scale;
                float height = 1.05f * scale;
                Vector3 centre = positions[i] + Vector3.up * (height * 0.5f - 0.03f);
                AddCrossedCard(vertices, uvs, triangles, centre, width, height, yaw, 2);
            }

            Mesh mesh = new Mesh();
            mesh.name = name + " Mesh";
            if (vertices.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);

            Material material = VegetationMaterial(
                "Vegetation/boreal_undergrowth",
                new Color(0.82f, 0.9f, 0.74f),
                0.32f);
            return MeshObject(name, parent, Vector3.zero, Quaternion.identity, Vector3.one, mesh, material);
        }

        private static GameObject MeshObject(
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Mesh mesh,
            Material material)
        {
            GameObject created = new GameObject(name);
            created.transform.SetParent(parent, false);
            created.transform.localPosition = localPosition;
            created.transform.localRotation = localRotation;
            created.transform.localScale = localScale;
            created.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = created.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.allowOcclusionWhenDynamic = true;
            return created;
        }

        private static Mesh SpruceCrownMesh()
        {
            if (spruceCrownMesh != null)
                return spruceCrownMesh;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            float[] heights = { 1.45f, 2.65f, 3.85f, 5.0f, 6.05f, 6.95f };
            float[] widths = { 5.4f, 5f, 4.35f, 3.7f, 2.9f, 1.9f };
            for (int i = 0; i < heights.Length; i++)
            {
                float cardHeight = Mathf.Lerp(2.4f, 1.55f, i / (float)(heights.Length - 1));
                AddCrossedCard(
                    vertices,
                    uvs,
                    triangles,
                    new Vector3(0f, heights[i], 0f),
                    widths[i],
                    cardHeight,
                    i * 17f,
                    3);
            }

            spruceCrownMesh = BuildCardMesh("Instanced Norway Spruce Crown", vertices, uvs, triangles);
            return spruceCrownMesh;
        }

        private static Mesh BirchCrownMesh()
        {
            if (birchCrownMesh != null)
                return birchCrownMesh;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();
            Vector3[] centres =
            {
                new Vector3(-0.75f, 4.9f, 0.15f),
                new Vector3(0.8f, 5.15f, -0.2f),
                new Vector3(-0.15f, 6.0f, 0.45f),
                new Vector3(0.25f, 6.75f, -0.15f),
                new Vector3(-0.95f, 5.75f, -0.35f),
                new Vector3(1.0f, 6.05f, 0.3f),
                new Vector3(0f, 7.45f, 0f)
            };
            for (int i = 0; i < centres.Length; i++)
            {
                float width = i == centres.Length - 1 ? 2.35f : 3.05f;
                float height = i == centres.Length - 1 ? 1.9f : 2.35f;
                AddCrossedCard(vertices, uvs, triangles, centres[i], width, height, i * 31f, 2);
            }

            birchCrownMesh = BuildCardMesh("Instanced Downy Birch Crown", vertices, uvs, triangles);
            return birchCrownMesh;
        }

        private static Mesh BuildCardMesh(
            string name,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static void AddCrossedCard(
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 centre,
            float width,
            float height,
            float yaw,
            int planes)
        {
            Vector3 bottomCentre = centre - Vector3.up * (height * 0.5f);
            for (int plane = 0; plane < planes; plane++)
            {
                float angle = (yaw + plane * (180f / planes)) * Mathf.Deg2Rad;
                Vector3 right = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (width * 0.5f);
                int start = vertices.Count;
                vertices.Add(bottomCentre - right);
                vertices.Add(bottomCentre + right);
                vertices.Add(bottomCentre + right + Vector3.up * height);
                vertices.Add(bottomCentre - right + Vector3.up * height);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
        }

        public static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }
        }
    }
}

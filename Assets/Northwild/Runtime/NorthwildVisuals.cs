using System.Collections.Generic;
using UnityEngine;
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
            Stone
        }

        private static Texture2D birchBarkTexture;
        private static Texture2D foliageTexture;
        private static readonly Dictionary<string, Material> sharedPrimitiveMaterials =
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
            if (surface == SurfaceType.Foliage || surface == SurfaceType.Plain)
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

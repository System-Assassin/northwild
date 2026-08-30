using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NorthwildTextureImporter
{
    private const string TextureFolder = "Assets/Northwild/Resources";
    private static bool queued;

    static NorthwildTextureImporter()
    {
        QueueImportSetup();
    }

    [MenuItem("Northwild/HDRP/Reimport PBR Textures")]
    public static void ConfigureTextures()
    {
        queued = false;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            bool normalMap = path.Contains("_normal.");
            bool maskMap = path.Contains("_mask.");
            bool vegetation = path.Contains("/Vegetation/");
            TextureImporterType expectedType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool expectedSrgb = !normalMap && !maskMap;
            TextureWrapMode expectedWrap = vegetation ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            bool changed = importer.textureType != expectedType ||
                           importer.sRGBTexture != expectedSrgb ||
                           importer.wrapMode != expectedWrap ||
                           importer.filterMode != FilterMode.Trilinear ||
                           !importer.mipmapEnabled || importer.maxTextureSize != 2048 ||
                           importer.anisoLevel != (normalMap ? 8 : 4) ||
                           importer.textureCompression != TextureImporterCompression.CompressedHQ ||
                           (vegetation && (!importer.alphaIsTransparency ||
                                           !importer.mipMapsPreserveCoverage));

            importer.textureType = expectedType;
            importer.sRGBTexture = expectedSrgb;
            importer.wrapMode = expectedWrap;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.anisoLevel = normalMap ? 8 : 4;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (vegetation)
            {
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = 0.44f;
            }
            if (changed)
                importer.SaveAndReimport();
        }

        Debug.Log("Northwild textures configured: PBR surfaces plus alpha-clipped HDRP vegetation.");
    }

    private static void QueueImportSetup()
    {
        if (queued)
            return;
        queued = true;
        EditorApplication.delayCall += ConfigureWhenReady;
    }

    private static void ConfigureWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            queued = false;
            QueueImportSetup();
            return;
        }
        ConfigureTextures();
    }
}

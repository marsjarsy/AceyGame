using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteTools
{

    public static readonly string NORMAL_MAP_SUFFIX = "_n";
    public static readonly string SECONDARY_TEXTURE_NAME_NORMAL = "_NormalMap";

    // -----------------------------------------------------------
    
    [MenuItem("Tools/Add secondary texture to sprite")]
    public static void AddSecondaryTextureToSprite()
    {

        // filter selection to assets only
        Object[] spriteAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

        // process all assets
        foreach (Object item in spriteAssets)
        {

            // path to selected sprite asset
            string spriteAssetPath = AssetDatabase.GetAssetPath(item);


            // find normal texture in subfolders
            string normalTextureName = Path.GetFileNameWithoutExtension(spriteAssetPath) + NORMAL_MAP_SUFFIX;
            string normalTextureType = " t:Texture2D";
            string[] searchFolders = AssetDatabase.GetSubFolders(Path.GetDirectoryName(spriteAssetPath));
            string[] normalGUIDs = AssetDatabase.FindAssets(normalTextureName + normalTextureType, searchFolders);

            // if no normal, then log warrning
            if (normalGUIDs == null || normalGUIDs.Length == 0)
            {
                Debug.LogWarning("Not found " + normalTextureName + " in subfolders");
                continue;
            }


            // get texture importer for current sprite asset
            TextureImporter importer = AssetImporter.GetAtPath(spriteAssetPath) as TextureImporter;

            // create secondary texture entries array
            SecondarySpriteTexture[] secondarySpriteTextures = new SecondarySpriteTexture[] {
                new SecondarySpriteTexture {
                    name = SECONDARY_TEXTURE_NAME_NORMAL,
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(normalGUIDs[0]))
                }
            };
            // set new array to importer
            importer.secondarySpriteTextures = secondarySpriteTextures;


            // save importer
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
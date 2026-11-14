using UnityEditor;

// Assets/Resources/Art altina atilan her gorsel otomatik Sprite olarak ice aktarilir (ilk iceri aktarmada).
// Sanatci Unity ayarina dokunmadan PNG'yi klasore birakir, oyun bulur.
//   background.png, boss.png  -> pivot ortada
//   diger dosyalar (binalar)  -> pivot ALT-ORTA (bina tabani sahnedeki zemin noktasina oturur)
public class ArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
        if (!assetImporter.importSettingsMissing) return;   // sanatci sonradan elle degistirirse ezme

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;

        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool building = file != "background" && file != "boss";
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)(building ? UnityEngine.SpriteAlignment.BottomCenter : UnityEngine.SpriteAlignment.Center);
        ti.SetTextureSettings(s);
    }
}

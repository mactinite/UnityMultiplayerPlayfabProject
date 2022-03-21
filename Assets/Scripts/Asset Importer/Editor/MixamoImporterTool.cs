using UnityEngine;
using UnityEditor;
using System.IO;

public class CustomImportAnimation : AssetPostprocessor
{
    void OnPreprocessAnimation()
    {
        if (assetPath.Contains("Animations"))
        {
            ModelImporter modelImporter = assetImporter as ModelImporter;
            var animations = modelImporter.clipAnimations;

            for (int i = 0; i < animations.Length; i++)
            {
                // only process poorly named mixamo animations
                if (animations[i].name.Contains("mixamo.com"))
                {
                    animations[i].loopTime = true;
                    animations[i].loopPose = true;

                    animations[i].keepOriginalOrientation = true;
                    // use the filename (without extension) for animations.
                    animations[i].name = Path.GetFileName(assetPath).Split('.')[0];
                }
            }

            modelImporter.clipAnimations = animations;
            Debug.Log("Found and processed mixamo FBX animations.");
        }
    }
}
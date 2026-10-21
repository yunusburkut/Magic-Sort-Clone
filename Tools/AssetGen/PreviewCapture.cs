// Editor-only preview: copy into Assets/Editor of a throwaway project copy and run
//   Unity -batchmode -projectPath <copy> -executeMethod PreviewCapture.Run -quit -- <out.png>
// Places a few tubes with water stacks in SampleScene and renders the main camera to a PNG.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PreviewCapture
{
    private static readonly Color[] Colors =
    {
        new Color(1f, 0.25f, 0.35f), new Color(0.2f, 0.55f, 1f), new Color(0.25f, 0.9f, 0.4f),
        new Color(1f, 0.85f, 0.15f), new Color(0.7f, 0.35f, 1f), new Color(1f, 0.55f, 0.1f),
    };

    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        var outPath = args[args.Length - 1];

        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var tubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HeronCaseRepo/Prefabs/TubePrefab.prefab");
        var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HeronCaseRepo/Prefabs/WaterPrefab.prefab");

        int[][] stacks = { new[] { 0, 1, 1 }, new[] { 2, 3, 0 }, new[] { 4, 4, 4 }, new[] { 5, 2 }, new int[0] };
        for (var t = 0; t < stacks.Length; t++)
        {
            var tube = (GameObject)PrefabUtility.InstantiatePrefab(tubePrefab);
            tube.transform.position = new Vector3((t - (stacks.Length - 1) * 0.5f) * 2.6f, 0f, 0f);
            foreach (var sr in tube.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name == "Outline" || sr.name == "Line") sr.color = Color.clear;
                if (sr.name == "Cloak") sr.gameObject.SetActive(false);
            }

            var container = tube.transform.Find("WaterRect");
            AddStack(waterPrefab, container, stacks[t]);
        }

        var cam = Camera.main;
        cam.orthographicSize = 5.5f;
        var rt = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Debug.Log("[PreviewCapture] wrote " + outPath);
    }

    // Mirrors TubeWaterSlots: merge equal neighbours, slot height 1, overlap 0.2, surface on top only.
    private static void AddStack(GameObject waterPrefab, Transform container, int[] stack)
    {
        const float slot = 1f, overlap = 0.2f;
        var i = 0;
        while (i < stack.Length)
        {
            var count = 1;
            while (i + count < stack.Length && stack[i + count] == stack[i]) count++;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab, container);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = Colors[stack[i]];
            sr.size = new Vector2(sr.size.x, count * slot + overlap);
            sr.sortingOrder = i;
            go.transform.localPosition = new Vector3(0f, (i + count * 0.5f) * slot + overlap * 0.5f, 0f);
            var mpb = new MaterialPropertyBlock();
            mpb.SetFloat("_RevealAmount", 1f);
            mpb.SetFloat("_Surface", i + count == stack.Length ? 1f : 0f);
            sr.SetPropertyBlock(mpb);
            i += count;
        }
    }
}

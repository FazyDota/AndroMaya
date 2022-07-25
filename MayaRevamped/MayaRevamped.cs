using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class MayaRevamped : Mod
{
    public static Network_Player localPlayer = null;

    public static Vector3[] originalMayaVertices;
    public static Texture2D originalMayaDiffuseTexture;
    public static Texture2D originalMayaNormalTexture;

    public static AssetBundle mayaBundle;
    public static Texture2D loadedMayaDiffuseTexture;
    public static Texture2D loadedMayaNormalTexture;
    public static Texture2D loadedMayaNormalTexture2;

    public static Vector3[] newMayaVertices;

    public static byte[] loadedVerticesBytes;
    public static Vector3[] loadedMayaVertices;
    public static byte[] coreVerticesBytes;
    public static Vector3[] loadedCoreVertices;
    public static byte[] noChangesVerticesBytes;
    public static Vector3[] loadedNoChangesVertices;

    public static int mode = 0;
    public static bool initialized = false;

    public IEnumerator Start()
    {
        AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("mayabundle"));
        yield return request;
        mayaBundle = request.assetBundle;

        GameObject moreChangesMaya = mayaBundle.LoadAsset<GameObject>("mayaNew");

        
        loadedVerticesBytes = GetEmbeddedFileBytes("betterMayaVertices2.json");
        Debug.Log("betterMayaVertices2");
        coreVerticesBytes = GetEmbeddedFileBytes("coreVertices.json");
        Debug.Log("coreVertices");
        noChangesVerticesBytes = GetEmbeddedFileBytes("blenderNoChangesVertices.json");
        Debug.Log("blenderNoChangesVertices");

        MemoryStream verticesStream = new MemoryStream(coreVerticesBytes);
        using (StreamReader verticesReader = new StreamReader(verticesStream, Encoding.UTF8))
        {
            JsonSerializer serializer = new JsonSerializer();
            loadedCoreVertices = (Vector3[])serializer.Deserialize(verticesReader, typeof(Vector3[]));
            Debug.Log("loadedCoreVertices");
        }

        verticesStream = new MemoryStream(noChangesVerticesBytes);
        using (StreamReader verticesReader = new StreamReader(verticesStream, Encoding.UTF8))
        {
            JsonSerializer serializer = new JsonSerializer();
            loadedNoChangesVertices = (Vector3[])serializer.Deserialize(verticesReader, typeof(Vector3[]));
            Debug.Log("loadedNoChangesVertices");
        }

        newMayaVertices = moreChangesMaya.GetComponent<MeshFilter>().sharedMesh.vertices;
        Debug.Log("newMayaVertices");
        newMayaVertices = updateThreeSetsOfVertices(loadedNoChangesVertices, newMayaVertices, loadedCoreVertices);
        Debug.Log("newMayaVertices processed");

        loadedMayaDiffuseTexture = mayaBundle.LoadAsset<Texture2D>("betterTextureV2");
        Debug.Log("betterTextureV2");
        loadedMayaNormalTexture = mayaBundle.LoadAsset<Texture2D>("updatedNormalMapV3");
        Debug.Log("updatedNormalMapV3");
        loadedMayaNormalTexture2 = mayaBundle.LoadAsset<Texture2D>("updatedNormalMapV4");
        Debug.Log("updatedNormalMapV4");

        Debug.Log("Mod MayaRevamped has been loaded! What a wonderful day.");
    }

    public static void loadPlayerRelated()
    {
        if (initialized) return;
        localPlayer = RAPI.GetLocalPlayer();

        MemoryStream verticesStream = new MemoryStream(loadedVerticesBytes);
        using (StreamReader verticesReader = new StreamReader(verticesStream, Encoding.UTF8))
        {
            JsonSerializer serializer = new JsonSerializer();
            loadedMayaVertices = (Vector3[])serializer.Deserialize(verticesReader, typeof(Vector3[]));
        }
        
        Texture currentDiffuseTexture = localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].GetTexture("_Diffuse");
        originalMayaDiffuseTexture = (Texture2D)currentDiffuseTexture;
        Texture currentNormalTexture = localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].GetTexture("_Normal");
        originalMayaNormalTexture = (Texture2D)currentNormalTexture;

        originalMayaVertices = localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices;
        initialized = true;
    }

    [ConsoleCommand(name: "switchMayaVisual", docs: "Swaps back and forth between different Maya models and textures.")]
    public static string SwitchMayaVisual(string[] args)
    {
        if (!initialized) loadPlayerRelated();
        switchMaya();
        return $"Command switchMayaVisual finished.";
    }

    public static void switchMaya()
    {
        Raft_Network raftNetwork = ComponentManager<Raft_Network>.Value;
        if (localPlayer is null)
        {
            localPlayer = RAPI.GetLocalPlayer();
            
        }

        Debug.Log($"Current voice type: {raftNetwork.GetLocalPlayer().currentModel.voiceType}");
        Debug.Log($"Current remote users: {raftNetwork.remoteUsers}");

        if (mode == 0)
        {
            localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = originalMayaVertices;
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", originalMayaDiffuseTexture);
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", originalMayaNormalTexture);
            Debug.Log("0: DEFAULT");
            mode = 1;
            return; 
        }

        if (mode == 1)
        {
            localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = loadedMayaVertices;
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", loadedMayaDiffuseTexture);
            Debug.Log("1: IMPROVED VERTICES AND DIFFUSE");
            mode = 2;
            return;
        }
        if (mode == 2)
        {
            localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = loadedMayaVertices;
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", loadedMayaDiffuseTexture);
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", loadedMayaNormalTexture);
            Debug.Log("2: IMPROVED VERTICES AND DIFFUSE, NORMALMAP");
            mode = 3;
            return;
        }
        if (mode == 3)
        {
            localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = newMayaVertices;
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", loadedMayaDiffuseTexture);
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", loadedMayaNormalTexture2);
            Debug.Log("3: IMPROVED VERTICES B AND DIFFUSE, NORMALMAP B");
            mode = 0;
            return;
        }
        return;
    }

    public void OnModUnload()
    {
        mode = 0;
        switchMaya();
        mayaBundle.Unload(true);
        Debug.Log("Mod MayaRevamped has been unloaded!");
    }

    public static void switchOutfitAndSaveTexture()
    {
        localPlayer.currentModel.ApplyOutfit(6);
        Texture currentTexture = localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].GetTexture("_Diffuse");
        originalMayaDiffuseTexture = (Texture2D)currentTexture;
        var textureBytes = originalMayaDiffuseTexture.EncodeToPNG();
        File.WriteAllBytes("./" + "outfit6" + ".png", textureBytes);
        Debug.Log("Saved outfit6.");

    }

    static Vector3[] updateThreeSetsOfVertices(Vector3[] blenderNoChangesVertices, Vector3[] blenderChangedVertices, Vector3[] originalVertices)
    {
        Debug.Log("Updating vertex set.");
        Vector3[] result = new Vector3[originalVertices.Length];
        originalVertices.CopyTo(result, 0);

        for (int i = 0; i < originalVertices.Length; i++)
        {
            if (i > 6824)
            {
                continue;
            }
            if (blenderNoChangesVertices[i] != blenderChangedVertices[i])
            {
                result[i] = blenderChangedVertices[i];
                Debug.Log($"Replacing vertex {i}.");
            }
        }
        Debug.Log($"noChangesLen: {blenderNoChangesVertices.Length}, changedLen: {blenderChangedVertices.Length}, originalLen: {originalVertices.Length}");
        return result;
    }
}
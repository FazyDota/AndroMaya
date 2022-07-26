using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class AndroMaya : Mod
{
    public static Network_Player localPlayer = null;

    // Original resources
    public static Vector3[] originalMayaVertices;
    public static Texture2D originalMayaDiffuseTexture;
    public static Texture2D originalMayaNormalTexture;

    // Loaded resources
    public static AssetBundle androMayaBundle;
    public static Texture2D androMayaDiffuseTexture;
    public static Texture2D androMayaNormalTexture;
    public static byte[] androMayaVerticeBytes;
    public static Vector3[] androMayaVertices;

    public static bool defaultResourcesInitialized = false;

    public IEnumerator Start()
    {
        // Get external resources - vertices from JSON, updated normal map texture and updated diffuse texture
        AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("mayabundle"));
        yield return request;
        androMayaBundle = request.assetBundle;

        androMayaVerticeBytes = GetEmbeddedFileBytes("AndroMayaVertices.json");

        MemoryStream verticesStream = new MemoryStream(androMayaVerticeBytes);
        using (StreamReader verticesReader = new StreamReader(verticesStream, Encoding.UTF8))
        {
            JsonSerializer serializer = new JsonSerializer();
            androMayaVertices = (Vector3[])serializer.Deserialize(verticesReader, typeof(Vector3[]));
        }

        androMayaNormalTexture = androMayaBundle.LoadAsset<Texture2D>("updatedNormalMapV4");
        androMayaDiffuseTexture = androMayaBundle.LoadAsset<Texture2D>("betterTextureV2");

        Debug.Log($"Mod AndroMaya {version} has been loaded! What a wonderful day.");
    }

    public override void WorldEvent_WorldLoaded()
    {
        if (RAPI.IsCurrentSceneGame())
        {
            setAndroMaya();
        }
    }

    public static void loadPlayerRelated()
    {
        if (defaultResourcesInitialized) return;
        localPlayer = RAPI.GetLocalPlayer();
        
        Texture currentDiffuseTexture = localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].GetTexture("_Diffuse");
        originalMayaDiffuseTexture = (Texture2D)currentDiffuseTexture;
        Texture currentNormalTexture = localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].GetTexture("_Normal");
        originalMayaNormalTexture = (Texture2D)currentNormalTexture;

        originalMayaVertices = localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices;
        defaultResourcesInitialized = true;
    }

    public static void setAndroMaya()
    {
        if (!defaultResourcesInitialized) loadPlayerRelated();
        localPlayer = RAPI.GetLocalPlayer();
        VoiceType currentVoiceType = localPlayer.currentModel.voiceType;
        if (currentVoiceType == VoiceType.Maya)
        {
            Debug.Log("VoiceType is Maya.");
            localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = androMayaVertices;
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", androMayaDiffuseTexture);
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", androMayaNormalTexture);
            Debug.Log("Swapped vertices, diffuse and normal texture to AndroMaya successfully.");
        }
        else
        {
            Debug.Log("VoiceType is not Maya.");
        }
    }

    public static int switchCommandMode = 1;

    [ConsoleCommand(name: "switchMayaVisual", docs: "Swaps back and forth between different Maya models and textures.")]
    public static string SwitchMayaVisual(string[] args)
    {
        if (!defaultResourcesInitialized) loadPlayerRelated();
        Raft_Network raftNetwork = ComponentManager<Raft_Network>.Value;
        if (localPlayer is null)
        {
            localPlayer = RAPI.GetLocalPlayer();

        }
        var currentVoiceType = localPlayer.currentModel.voiceType;

        if (currentVoiceType == VoiceType.Maya)
        {
            if (switchCommandMode == 0)
            {
                localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = originalMayaVertices;
                localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", originalMayaDiffuseTexture);
                localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", originalMayaNormalTexture);

                switchCommandMode = 1;
                return $"Swapping back to default Maya vertices and textures.";
            }

            if (switchCommandMode == 1)
            {
                localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = androMayaVertices;
                localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", androMayaDiffuseTexture);
                localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", androMayaNormalTexture);

                switchCommandMode = 0;
                return $"Swapping to edited Maya vertices and textures.";
            }
        }
        return $"Not using Maya character.";
    }
}
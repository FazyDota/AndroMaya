using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class MayaRevamped : Mod
{
    public static Network_Player localPlayer = null;

    // Original resources
    public static Vector3[] originalMayaVertices;
    public static Texture2D originalMayaDiffuseTexture;
    public static Texture2D originalMayaNormalTexture;

    public static AssetBundle mayaBundle;
    public static Texture2D loadedMayaDiffuseTexture;
    public static Texture2D loadedMayaNormalTexture;
    public static byte[] loadedAndroMayaVerticeBytes;
    public static Vector3[] loadedAndroMayaVertices;

    public static int mode = 0;
    public static bool initialized = false;

    public IEnumerator Start()
    {
        // Get external resources - vertices from JSON, updated normal map texture and updated diffuse texture
        AssetBundleCreateRequest request = AssetBundle.LoadFromMemoryAsync(GetEmbeddedFileBytes("mayabundle"));
        yield return request;
        mayaBundle = request.assetBundle;

        loadedAndroMayaVerticeBytes = GetEmbeddedFileBytes("AndroMayaVertices.json");

        MemoryStream verticesStream = new MemoryStream(loadedAndroMayaVerticeBytes);
        using (StreamReader verticesReader = new StreamReader(verticesStream, Encoding.UTF8))
        {
            JsonSerializer serializer = new JsonSerializer();
            loadedAndroMayaVertices = (Vector3[])serializer.Deserialize(verticesReader, typeof(Vector3[]));
            Debug.Log("loadedAndroMayaVertices");
        }

        loadedMayaNormalTexture = mayaBundle.LoadAsset<Texture2D>("updatedNormalMapV4");
        loadedMayaDiffuseTexture = mayaBundle.LoadAsset<Texture2D>("betterTextureV2");

        Debug.Log("Mod MayaRevamped has been loaded! What a wonderful day.");
    }

    public override void WorldEvent_WorldLoaded()
    {
        if (RAPI.IsCurrentSceneGame())
        {
            loadPlayerRelated();
            mode = 1;
            //switchMaya();
        }
    }

    public static void loadPlayerRelated()
    {
        if (initialized) return;
        localPlayer = RAPI.GetLocalPlayer();
        
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
        var currentVoiceType = raftNetwork.GetLocalPlayer().currentModel.voiceType;
        Debug.Log($"Current voice type: {currentVoiceType}");
        Debug.Log($"Current voice type: {currentVoiceType.ToString()}");
        Debug.Log($"Current voice type: {((int)currentVoiceType)}");
        Debug.Log($"Current remote users: {raftNetwork.remoteUsers}");

        foreach (var user in raftNetwork.remoteUsers)
        {
            Debug.Log(user.Key, user.Value);
            Debug.Log(user.ToString());
        }

        Network_Player currentNetworkPlayer = raftNetwork.GetLocalPlayer();

        if ((int)currentVoiceType == 1)
        {
            Debug.Log($"YES, MAYA CHARACTER IN USE!");
            if (mode == 0)
            {
                localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = originalMayaVertices;
                localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", originalMayaDiffuseTexture);
                localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", originalMayaNormalTexture);

                currentNetworkPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = originalMayaVertices;
                currentNetworkPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", originalMayaDiffuseTexture);
                currentNetworkPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", originalMayaNormalTexture);

                Debug.Log("0: DEFAULT");
                mode = 1;
                return;
            }

            if (mode == 1)
            {
                localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = loadedAndroMayaVertices;
                localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", loadedMayaDiffuseTexture);
                localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", loadedMayaNormalTexture);

                currentNetworkPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = loadedAndroMayaVertices;
                currentNetworkPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", loadedMayaDiffuseTexture);
                currentNetworkPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", loadedMayaNormalTexture);

                Debug.Log("3: IMPROVED VERTICES B AND DIFFUSE, NORMALMAP B");
                mode = 0;
                return;
            }
        }
        else
        {
            Debug.Log($"NO, OTHER CHARACTER IN USE!");
        }
        return;
    }

    public static void setAndroMaya()
    {
        if (!initialized) loadPlayerRelated();
        localPlayer = RAPI.GetLocalPlayer();
        VoiceType currentVoiceType = localPlayer.currentModel.voiceType;
        if ((int)currentVoiceType == 1)
        {
            Debug.Log("VoiceType is Maya.");
            localPlayer.currentModel.fullBodyMesh.sharedMesh.vertices = loadedAndroMayaVertices;
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Diffuse", loadedMayaDiffuseTexture);
            localPlayer.currentModel.fullBodyMesh.sharedMaterials[0].SetTexture("_Normal", loadedMayaNormalTexture);
            Debug.Log("Swapped vertices, diffuse and normal texture to AndroMaya successfully.");
        }
        else
        {
            Debug.Log("VoiceType is not Maya.");
        }
    }

    [HarmonyPatch(typeof(CharacterModelModifications), "Start")]
    public class HarmonyPatch_IgnoreCollisionOnAlt
    {
        [HarmonyPostfix]
        static void SwapModelIfMaya()
        {
            setAndroMaya();
        }
    }
}
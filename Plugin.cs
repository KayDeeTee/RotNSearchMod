using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Shared;
using Shared.RiftInput;
using Shared.TrackData;
using Shared.TrackSelection;
using Shared.UGC.Placeholder;
using TMPro;
using UnityEngine;

namespace SearchMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class SearchModPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static string searchString = "";
    public static Canvas canvas;

    public static TextMeshProUGUI TextObj;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;

        Logger.LogInfo(String.Format("BuildVer: {0}", BuildInfoHelper.Instance.BuildId));
        string[] versions = ["1.7.0", "1.7.1"];
        if (!versions.Contains(BuildInfoHelper.Instance.BuildId.Split('-')[0]))
        {
            Logger.LogInfo("Mod built for a previous version of the game, wait for an update or update this yourself.");
            return;
        }


        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Harmony.CreateAndPatchAll(typeof(SearchModPlugin));
    }

    [HarmonyPatch(typeof(CustomTracksSelectionSceneController), "Awake")]
    [HarmonyPrefix]
    public static bool CustomTrackAwake(CustomTracksSelectionSceneController __instance)
    {
        canvas = __instance._mainCanvas;

        RectTransform screen = (RectTransform)canvas.transform.Find("ScreenContainer");

        GameObject cont = new GameObject("OSD", typeof(RectTransform));
        cont.transform.SetParent(screen, false);

        RectTransform cont_transform = (RectTransform)cont.transform;
        cont_transform.anchorMin = new Vector2(0, 0);
        cont_transform.anchorMax = new Vector2(1, 1);
        cont_transform.offsetMax = new Vector2(0, 0);
        cont_transform.offsetMin = new Vector2(0, 0);
        cont_transform.sizeDelta = new Vector2(0, 0);

        //create text
        GameObject text = new GameObject("OSDText", typeof(RectTransform), typeof(CanvasRenderer));
        text.transform.SetParent(cont.transform, false);
        TextObj = text.gameObject.AddComponent<TextMeshProUGUI>();
        TextObj.font = __instance._sortingOrderText.font;
        TextObj.fontSize = 32;

        RectTransform text_transform = (RectTransform)text.transform;
        text_transform.anchorMin = new Vector2(0.45f, 0.45f);
        text_transform.anchorMax = new Vector2(0.95f, 0.95f);
        text_transform.offsetMax = new Vector2(0, 0);
        text_transform.offsetMin = new Vector2(0, 0);
        text_transform.sizeDelta = new Vector2(0, 0);

        TextObj.enableWordWrapping = true;
        TextObj.overflowMode = TextOverflowModes.Overflow;
        TextObj.outlineColor = Color.black;
        TextObj.outlineWidth = 0.5f;
        TextObj.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.5f);

        TextObj.text = searchString;
        TextObj.color = Color.gray;

        if (searchString == "")
        {
            TextObj.text = " Press '/' to toggle search";
        }

        return true;
    }

    public static void ListAllChildren(Transform transform, string path)
    {
        Logger.LogInfo(path + transform.name);
        foreach (Transform child in transform)
        {
            if (child == transform) continue;
            ListAllChildren(child, path + transform.name + "/");
        }
    }

    [HarmonyPatch(typeof(CustomTracksSelectionSceneController), "Update")]
    [HarmonyPrefix]
    public static bool CustomTrackUpdate(CustomTracksSelectionSceneController __instance)
    {
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            __instance.InputDisabled = !__instance.InputDisabled;

            if (__instance.InputDisabled)
            {
                TextObj.color = Color.white;
            }
            else
            {
                TextObj.color = Color.gray;
            }
        }
        if (!__instance.InputDisabled) return true;
        bool refilter = false;
        foreach (char c in Input.inputString)
        {
            if (c == '/') continue;
            if (c == '\b')
            {
                if (searchString.Length != 0)
                {
                    searchString = searchString.Substring(0, searchString.Length - 1);
                    refilter = true;
                }
            }
            else if (c == '\n' || c == '\r')
            {
            }
            else
            {
                searchString += c;
                refilter = true;

            }
        }
        if (refilter)
        {
            TextObj.text = searchString;

            if (searchString == "")
            {
                TextObj.text = " Press '/' to toggle search";
            }

            string levelId = "";
            if (__instance._displayedTrackMetaDatas.Length != 0)
            {
                levelId = __instance._displayedTrackMetaDatas[__instance._selectedTrackIndex].LevelId;
            }
             

            __instance.FillInTracksToDisplayForCurrentDifficulty();
            __instance.SortTrackMetadata();

            if (levelId != "")
            {
                __instance._selectedTrackIndex = __instance.GetTrackMetadataIndexFromLevelId(levelId);
                if (__instance._selectedTrackIndex < 0)
                {
                    __instance._selectedTrackIndex = 0;
                }
            }


            if ((bool)__instance._trackSelectionOptionGroup)
            {
                __instance._trackSelectionOptionGroup.RefreshTrackData(__instance._displayedTrackMetaDatas, __instance._selectedTrackIndex, __instance._selectedDifficulty);
            }

            __instance.FillInTrackDetails();
        }
        return true;
    }

    

    [HarmonyPatch(typeof(CustomTracksSelectionSceneController), "FillInTracksToDisplayForCurrentDifficulty")]
    [HarmonyPrefix]
    public static bool FillInTracksToDisplayForCurrentDifficulty(CustomTracksSelectionSceneController __instance)
    {
        List<ITrackMetadata> list = new List<ITrackMetadata>();

        for (int i = 0; i < __instance._customTrackMetadatas.Count; i++)
        {
            if (__instance._customTrackMetadatas[i].GetDifficulty(__instance._selectedDifficulty) == null) continue;
            bool match = false;
            match |= __instance._customTrackMetadatas[i].StageCreatorName.ToLower().Contains(searchString.ToLower());
            match |= __instance._customTrackMetadatas[i].TrackName.ToLower().Contains(searchString.ToLower());

            if (!match) continue;


            list.Add(__instance._customTrackMetadatas[i]);
        }

        __instance._displayedTrackMetaDatas = list.ToArray();


        return false;
    }
}

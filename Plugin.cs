using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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


    public class SearchFilter
    {
        public SearchFilter(string param_type, string param_data)
        {
            switch (param_type)
            {
                case "": parameter = PARAMETER.TRACK_OR_AUTHOR_NAME; break;
                case "t": parameter = PARAMETER.TRACK_NAME; break;
                case "c": parameter = PARAMETER.AUTHOR_NAME; break;
                case "s": parameter = PARAMETER.SUBTITLE; break;
                case "a": parameter = PARAMETER.ARTIST_NAME; break;
                case "b": parameter = PARAMETER.BPM; break;
            }

            compString = param_data;

            if (parameter == PARAMETER.BPM)
            {
                int mask = 0;
                foreach (char c in compString)
                {
                    if (c == '=') mask |= 1;
                    if (c == '>') mask |= 2;
                    if (c == '<') mask |= 4;
                }
                compMode = (COMP_MODE)mask;
                if (compMode == COMP_MODE.CONTAINS) compMode = COMP_MODE.EQUALS;

                float.TryParse(new string(compString.Where(c => char.IsDigit(c)).ToArray()), out compFloat);
            }
        }
        public enum PARAMETER
        {
            TRACK_OR_AUTHOR_NAME,
            TRACK_NAME,
            AUTHOR_NAME,
            SUBTITLE,
            ARTIST_NAME,
            BPM,
            LENGTH,
            CATEGORY,
        }

        public enum COMP_MODE
        {
            CONTAINS,
            EQUALS = 1,
            GREATER_THAN = 2,
            GREATER_THAN_EQUAL = 3,
            LESS_THAN = 4,            
            LESS_THAN_EQUAL = 5,
        }

        public PARAMETER parameter = PARAMETER.TRACK_OR_AUTHOR_NAME;
        public COMP_MODE compMode = COMP_MODE.CONTAINS;
        public string compString = "";
        public float compFloat = 0.0f;

        public bool CheckMatch(ITrackMetadata track)
        {

            Logger.LogInfo(String.Format("{0}, {1}", parameter, compString));
            switch (parameter)
            {
                case PARAMETER.TRACK_OR_AUTHOR_NAME:
                    return track.StageCreatorName.ToLower().Contains(compString.ToLower()) || track.TrackName.ToLower().Contains(compString.ToLower());
                case PARAMETER.TRACK_NAME:
                    return track.TrackName.ToLower().Contains(compString.ToLower());
                case PARAMETER.AUTHOR_NAME:
                    return track.StageCreatorName.ToLower().Contains(compString.ToLower());
                case PARAMETER.SUBTITLE:
                    return track.TrackSubtitle.ToLower().Contains(compString.ToLower());
                case PARAMETER.ARTIST_NAME:
                    return track.ArtistName.ToLower().Contains(compString.ToLower());
                case PARAMETER.CATEGORY:
                    if (compFloat == 0) return track.Category == TrackCategory.UgcLocal;
                    return track.Category == TrackCategory.UgcRemote;
                case PARAMETER.BPM:
                    return CompValue(track.BeatsPerMinute);
                case PARAMETER.LENGTH:
                    return false;
                    //return CompValue(track.TrackLength);
            }
            return false;
        }

        public bool CompValue(float? v) {
            if (!v.HasValue) return false;
            switch (compMode)
            {
                case COMP_MODE.EQUALS: return v.Value == compFloat;
                case COMP_MODE.GREATER_THAN: return v.Value > compFloat;
                case COMP_MODE.GREATER_THAN_EQUAL: return v.Value >= compFloat;
                case COMP_MODE.LESS_THAN: return v.Value < compFloat;
                case COMP_MODE.LESS_THAN_EQUAL: return v.Value <= compFloat;
            }
            return false;            
        }

    }


    [HarmonyPatch(typeof(CustomTracksSelectionSceneController), "FillInTracksToDisplayForCurrentDifficulty")]
    [HarmonyPrefix]
    public static bool FillInTracksToDisplayForCurrentDifficulty(CustomTracksSelectionSceneController __instance)
    {
        List<ITrackMetadata> list = new List<ITrackMetadata>();

        string current_word = "";
        string current_section = "";

        string param_type = "";
        string param_data = "";

        List<SearchFilter> filters = new List<SearchFilter>();

        foreach (char c in searchString)
        {
            current_section += c;
            if (c == ' ')
            {
                current_word = "";
            }
            else if (c == ':')
            {
                param_type = current_word;
                current_section = "";
                current_word = "";
            }
            else if (c == ';')
            {
                param_data = current_section;
                current_section = "";
                current_word = "";
                //Logger.LogInfo(String.Format("{0}, {1}", param_type, param_data));
                filters.Add(new SearchFilter(param_type, param_data.Substring(0, param_data.Length - 1)));
            }
            else
            {
                current_word += c;
            }
        }
        //don't require a trailing ;
        if (current_section != "") filters.Add(new SearchFilter(param_type, current_section));

        Logger.LogInfo(String.Format("Filters: {0}", filters.Count));

        for (int i = 0; i < __instance._customTrackMetadatas.Count; i++)
        {
            if (__instance._customTrackMetadatas[i].GetDifficulty(__instance._selectedDifficulty) == null) continue;

            bool match = true;
            foreach (SearchFilter filter in filters)
            {
                match &= filter.CheckMatch(__instance._customTrackMetadatas[i]);
                if (!match) break;
            }

            if( match ) list.Add(__instance._customTrackMetadatas[i]);

            //match |= __instance._customTrackMetadatas[i].StageCreatorName.ToLower().Contains(searchString.ToLower());
            //match |= __instance._customTrackMetadatas[i].TrackName.ToLower().Contains(searchString.ToLower());

            //if (!match) continue;            
        }

        __instance._displayedTrackMetaDatas = list.ToArray();


        return false;
    }
}

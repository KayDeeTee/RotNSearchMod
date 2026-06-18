using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using RiftOfTheNecroManager;
using Shared.PlayerData;
using Shared.TrackData;
using Shared.TrackSelection;
using TMPro;
using UnityEngine;

namespace SearchMod;

[BepInPlugin("rotn.katie.utils.searchmod", MyPluginInfo.PLUGIN_NAME, "1.0.1")]
public class SearchModPlugin : RiftPlugin
{
    public static string searchString = "";
    public static Canvas canvas;

    public static TextMeshProUGUI TextObj;

    public static KeyCode ToggleKey => (KeyCode)SearchMod.Config.Keybinds.ToggleSearchBar;

    public static bool searchActive = false;

    [HarmonyPatch(typeof(CustomTracksSelectionSceneController))]
    public static class CustomTracksSelectionPatch
    {
        [HarmonyPatch("Awake")]
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

            searchString = "";

            TextObj.text = searchString;
            TextObj.color = Color.gray;

            if (searchString == "")
            {
                TextObj.text = String.Format(" Press '{0}' to toggle search", ToggleKey);
            }

            searchActive = false;

            return true;
        }

        public static void ListAllChildren(Transform transform, string path)
        {
            Log.Info(path + transform.name);
            foreach (Transform child in transform)
            {
                if (child == transform) continue;
                ListAllChildren(child, path + transform.name + "/");
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool CustomTrackUpdate(CustomTracksSelectionSceneController __instance)
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                if (!__instance.InputDisabled)
                {
                    searchActive = true;
                    __instance.InputDisabled = true;

                    TextObj.color = Color.white;
                }
                else if (searchActive)
                {
                    searchActive = false;
                    __instance.InputDisabled = false;

                    TextObj.color = Color.gray;
                }
            }

            if (!searchActive) return true;
            bool refilter = false;
            foreach (char c in Input.inputString)
            {
                if (c == ((char)ToggleKey)) continue;
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
                    TextObj.text = string.Format(" Press '{0}' to toggle search", ToggleKey);
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


        [HarmonyPatch("FillInTracksToDisplayForCurrentDifficulty")]
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
                    filters.Add(new SearchFilter(param_type, param_data.Substring(0, param_data.Length - 1)));
                }
                else
                {
                    current_word += c;
                }
            }
            //don't require a trailing ;
            if (current_section != "") filters.Add(new SearchFilter(param_type, current_section));

            for (int i = 0; i < __instance._customTrackMetadatas.Count; i++)
            {
                bool match = true;
                foreach (SearchFilter filter in filters)
                {
                    match &= filter.CheckMatch(__instance._customTrackMetadatas[i], __instance);
                    if (!match) break;
                }

                if (match) list.Add(__instance._customTrackMetadatas[i]);
            }

            __instance._displayedTrackMetaDatas = list.ToArray();

            switch (__instance._filterOption)
            {
                case TrackFilterOption.UnplayedSongs:
                    __instance._displayedTrackMetaDatas = __instance._displayedTrackMetaDatas.Where((ITrackMetadata d) => d.GetDifficulty(__instance._selectedDifficulty) != null && PlayerDataUtil.GetAttemptCountForDifficulty(d.LevelId, __instance._selectedDifficulty) <= 0).ToArray();
                    break;
                case TrackFilterOption.Favorites:
                    __instance._displayedTrackMetaDatas = __instance._displayedTrackMetaDatas.Where((ITrackMetadata d) => PlayerDataUtil.IsLevelFavorite(d.LevelId)).ToArray();
                    break;
                case TrackFilterOption.CurrentDifficultyAvailable:
                    __instance._displayedTrackMetaDatas = __instance._displayedTrackMetaDatas.Where((ITrackMetadata d) => d.GetDifficulty(__instance._selectedDifficulty) != null).ToArray();
                    break;
                case TrackFilterOption.None:
                case TrackFilterOption.DLC:
                    break;
            }

            return false;
        }
    }
}

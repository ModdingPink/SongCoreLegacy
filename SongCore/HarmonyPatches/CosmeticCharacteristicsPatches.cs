using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HMUI;
using IPA.Utilities;
using SongCore.Data;
using SongCore.Utilities;
using UnityEngine;
using static IPA.Logging.Logger;

namespace SongCore.HarmonyPatches
{
    internal class CosmeticCharacteristicsPatches
    {


        [HarmonyPatch(typeof(BeatLineManager))]
        [HarmonyPatch(nameof(BeatLineManager.HandleNoteWasSpawned))]
        internal class BeatLineManager_HandleNoteWasSpawned
        {
            private static bool Prefix()
            {
                var beatmap = BS_Utils.Plugin.LevelData.GameplayCoreSceneSetupData.difficultyBeatmap;
                if (beatmap == null)
                    return true;
                var beatmapData = Collections.RetrieveDifficultyData(beatmap);
                if (beatmapData == null)
                    return true;
                if (beatmapData._showRotationNoteSpawnLines == null)
                    return true;
                return beatmapData._showRotationNoteSpawnLines.Value;
            }
        }


        [HarmonyPatch(typeof(BeatmapEnvironmentHelper))]
        [HarmonyPatch(nameof(BeatmapEnvironmentHelper.GetEnvironmentInfo))]
        internal class StandardLevelScenesTransitionSetupDataSO_Init_Backport
        {
            static CustomLevelLoader loader;

            private static bool Prefix(IDifficultyBeatmap difficultyBeatmap, ref EnvironmentInfoSO __result)
            {
                if(loader == null)
                    loader = Resources.FindObjectsOfTypeAll<CustomLevelLoader>().FirstOrDefault();

                var diffBeatmapLevel = difficultyBeatmap.level;
                var level = diffBeatmapLevel is CustomBeatmapLevel ? diffBeatmapLevel as CustomPreviewBeatmapLevel : null;
                if (level == null)
                    return true;

                var songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(level));
                var diffData = Collections.RetrieveDifficultyData(difficultyBeatmap);

                if (songData == null || diffData == null)
                    return true;
                if (diffData._environmentNameIdx == null)
                    return true;

                string? envName = songData._environmentNames.ElementAtOrDefault(diffData._environmentNameIdx.Value);
                if (envName == null) return true;

                bool rotations = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.containsRotationEvents;

                __result = loader.LoadEnvironmentInfo(envName, rotations);
                return false;
            }
        }




        [HarmonyPatch(typeof(GameplayCoreInstaller))]
        [HarmonyPatch(nameof(GameplayCoreInstaller.InstallBindings))]
        internal class GameplayCoreInstaller_InstallBindingsPatch
        {
            private static ExtraSongData.DifficultyData? diffData = null;
            private static int numberOfColors = -1;
            private static void Prefix(GameplayCoreInstaller __instance)
            {
                GameplayCoreSceneSetupData sceneSetupData = __instance.GetField<GameplayCoreSceneSetupData, GameplayCoreInstaller>("_sceneSetupData");
                var diffBeatmapLevel = sceneSetupData.difficultyBeatmap.level;
                var level = diffBeatmapLevel is CustomBeatmapLevel ? diffBeatmapLevel as CustomPreviewBeatmapLevel : null;
                if (level == null)
                {
                    diffData = null;
                    return;
                }
                diffData = Collections.RetrieveDifficultyData(sceneSetupData.difficultyBeatmap);
                if (diffData == null)
                    return;
                if (diffData._oneSaber == null)
                    return;

                numberOfColors = sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.numberOfColors;
                sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.SetField("_numberOfColors", diffData._oneSaber.Value == true ? 1 : 2);

            }
            private static void Postfix(GameplayCoreInstaller __instance) {
                if (diffData == null)
                    return;
                if (diffData._oneSaber == null)
                    return;
                GameplayCoreSceneSetupData sceneSetupData = __instance.GetField<GameplayCoreSceneSetupData, GameplayCoreInstaller>("_sceneSetupData");
                sceneSetupData.difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.SetField("_numberOfColors", numberOfColors);

            }

        }


        [HarmonyPatch(typeof(BeatmapCharacteristicSegmentedControlController))]
        [HarmonyPatch(nameof(BeatmapCharacteristicSegmentedControlController.SetData), MethodType.Normal)]
        internal class CosmeticCharacteristicsPatch
        {
            //      public static OverrideClasses.CustomLevel previouslySelectedSong = null;
            private static void Postfix(IReadOnlyList<IDifficultyBeatmapSet> difficultyBeatmapSets, BeatmapCharacteristicSO selectedBeatmapCharacteristic, ref List<BeatmapCharacteristicSO> ____beatmapCharacteristics, ref IconSegmentedControl ____segmentedControl)
            {
                if(!Plugin.Configuration.DisplayCustomCharacteristics) return;
                var diffBeatmapLevel = difficultyBeatmapSets.FirstOrDefault().difficultyBeatmaps.FirstOrDefault().level;
                var level = diffBeatmapLevel is CustomBeatmapLevel ? diffBeatmapLevel as CustomPreviewBeatmapLevel : null;

                if(level == null) return;

                var songData = Collections.RetrieveExtraSongData(Hashing.GetCustomLevelHash(level));
                if(songData == null) return;

                if (songData._characteristicDetails.Length > 0)
                {
                    var dataItems = ____segmentedControl.GetField<IconSegmentedControl.DataItem[], IconSegmentedControl>("_dataItems");
                    List<IconSegmentedControl.DataItem> newDataItems = new List<IconSegmentedControl.DataItem>();

                    int i = 0;
                    int cell = 0;
                    foreach (var item in dataItems)
                    {
                        var characteristic = ____beatmapCharacteristics[i];
                        string serializedName = characteristic.serializedName;
                        ExtraSongData.CharacteristicDetails? detail = songData._characteristicDetails.Where(x => x._beatmapCharacteristicName == serializedName).FirstOrDefault();

                        if(detail != null)
                        {
                            Sprite sprite = Utilities.Utils.LoadSpriteFromFile(Path.Combine(level.customLevelPath, detail._characteristicIconFilePath)) ?? characteristic.icon;
                            string label = detail._characteristicLabel ?? Polyglot.Localization.Get(characteristic.descriptionLocalizationKey);
                            newDataItems.Add(new IconSegmentedControl.DataItem(sprite, label)); //<---------- THIS 
                        }
                        else
                        {
                            newDataItems.Add(item);
                        }

                        if (characteristic == selectedBeatmapCharacteristic)
                        {
                            cell = i;
                        }
                        i++;
                    }
                    ____segmentedControl.SetData(newDataItems.ToArray());
                    ____segmentedControl.SelectCellWithNumber(cell);
                }
            }
        }

    }
}

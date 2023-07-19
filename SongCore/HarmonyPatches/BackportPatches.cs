using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using SongCore.Utilities;
using UnityEngine;
using static AlphabetScrollInfo;

namespace SongCore.HarmonyPatches
{
    internal class BackportPatches
    {
        [HarmonyPatch(typeof(StandardLevelInfoSaveData))]
        [HarmonyPatch(nameof(StandardLevelInfoSaveData.DeserializeFromJSONString))]
        internal class StandardLevelInfoSaveData_DeserializeFromJSONString
        {
            private static bool Prefix(ref string stringData, ref StandardLevelInfoSaveData __result)
            {
                StandardLevelInfoSaveData.VersionCheck versionCheck = null;
                try
                {
                    versionCheck = JsonUtility.FromJson<StandardLevelInfoSaveData.VersionCheck>(stringData);
                }
                catch
                {
                }
                if (versionCheck == null)
                {
                    __result = null;
                    return false;
                }
                if (versionCheck.version.StartsWith("2."))
                {
                    try
                    {
                        __result = JsonUtility.FromJson<StandardLevelInfoSaveData>(stringData);
                        return false;
                    }
                    catch
                    {
                        __result = null;
                        return false;
                    }
                }
                StandardLevelInfoSaveData standardLevelInfoSaveData = null;
                if (versionCheck.version == "1.0.0")
                {
                    StandardLevelInfoSaveData_V100 standardLevelInfoSaveData_V = null;
                    try
                    {
                        standardLevelInfoSaveData_V = JsonUtility.FromJson<StandardLevelInfoSaveData_V100>(stringData);
                    }
                    catch
                    {
                        standardLevelInfoSaveData_V = null;
                    }
                    if (standardLevelInfoSaveData_V != null)
                    {
                        StandardLevelInfoSaveData.DifficultyBeatmapSet[] array = new StandardLevelInfoSaveData.DifficultyBeatmapSet[1];
                        StandardLevelInfoSaveData.DifficultyBeatmap[] array2 = new StandardLevelInfoSaveData.DifficultyBeatmap[standardLevelInfoSaveData_V.difficultyBeatmaps.Length];
                        for (int i = 0; i < array2.Length; i++)
                        {
                            array2[i] = new StandardLevelInfoSaveData.DifficultyBeatmap(standardLevelInfoSaveData_V.difficultyBeatmaps[i].difficulty, standardLevelInfoSaveData_V.difficultyBeatmaps[i].difficultyRank, standardLevelInfoSaveData_V.difficultyBeatmaps[i].beatmapFilename, standardLevelInfoSaveData_V.difficultyBeatmaps[i].noteJumpMovementSpeed, (float) standardLevelInfoSaveData_V.difficultyBeatmaps[i].noteJumpStartBeatOffset);
                        }
                        array[0] = new StandardLevelInfoSaveData.DifficultyBeatmapSet("Standard", array2);
                        standardLevelInfoSaveData = new StandardLevelInfoSaveData(standardLevelInfoSaveData_V.songName, standardLevelInfoSaveData_V.songSubName, standardLevelInfoSaveData_V.songAuthorName, standardLevelInfoSaveData_V.levelAuthorName, standardLevelInfoSaveData_V.beatsPerMinute, standardLevelInfoSaveData_V.songTimeOffset, standardLevelInfoSaveData_V.shuffle, standardLevelInfoSaveData_V.shufflePeriod, standardLevelInfoSaveData_V.previewStartTime, standardLevelInfoSaveData_V.previewDuration, standardLevelInfoSaveData_V.songFilename, standardLevelInfoSaveData_V.coverImageFilename, standardLevelInfoSaveData_V.environmentName, null, array);
                    }
                }
                __result = standardLevelInfoSaveData;
                return false;
            }
        }

        [HarmonyPatch(typeof(BeatmapEnvironmentHelper))]
        [HarmonyPatch(nameof(BeatmapEnvironmentHelper.GetEnvironmentInfo))]
        internal class StandardLevelScenesTransitionSetupDataSO_Init_Backport
        {
            static CustomLevelLoader loader;

            private static bool Prefix(IDifficultyBeatmap difficultyBeatmap, ref EnvironmentInfoSO __result)
            {
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

                bool rotationInfo = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.containsRotationEvents;
                if (loader == null)
                    loader = Resources.FindObjectsOfTypeAll<CustomLevelLoader>().FirstOrDefault();
                string? envName = songData._environmentNames.ElementAtOrDefault(diffData._environmentNameIdx.Value);
                if (envName == null)
                    return true;
                __result = loader.LoadEnvironmentInfo(envName, rotationInfo);
                return false;
            }
        }



    }
}

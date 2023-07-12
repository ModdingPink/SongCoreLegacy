using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongCore.Utilities;
using UnityEngine;
using UnityEngine.UI;
using static BloomPrePassBackgroundColorsGradientFromColorSchemeColors;

namespace SongCore.Data
{
    public class SongData
    {
        public string RawSongData;
        public StandardLevelInfoSaveData SaveData;

        public SongData(string rawSongData, StandardLevelInfoSaveData saveData)
        {
            RawSongData = rawSongData;
            SaveData = saveData;
        }
    }

    [Serializable]
    public class ExtraSongData
    {
        public Contributor[] contributors; //convert legacy mappers/lighters fields into contributors
        public string _customEnvironmentName;
        public string _customEnvironmentHash;
        public DifficultyData[] _difficulties;
        public string _defaultCharacteristic = null;

        public ColorScheme[] _colorSchemes; //beatmap 2.1.0, community decided to song-core ify colour stuff
        public string[] _environmentNames; //these have underscores but the actual format doesnt, I genuinely dont know what to go by so I went consistent with songcore

        [Serializable]
        public class Contributor
        {
            public string _role;
            public string _name;
            public string _iconPath;

            [NonSerialized]
            public Sprite? icon = null;
        }

        [Serializable]
        public class DifficultyData
        {
            public string _beatmapCharacteristicName;
            public BeatmapDifficulty _difficulty;
            public string _difficultyLabel;
            public RequirementData additionalDifficultyData;
            public MapColor? _colorLeft;
            public MapColor? _colorRight;
            public MapColor? _envColorLeft;
            public MapColor? _envColorRight;
            public MapColor? _envColorWhite;
            public MapColor? _envColorLeftBoost;
            public MapColor? _envColorRightBoost;
            public MapColor? _envColorWhiteBoost;
            public MapColor? _obstacleColor;
            public int? _beatmapColorSchemeIdx;
            public int? _environmentNameIdx;
        }

        [Serializable]
        public class ColorScheme //stuck to the same naming convention as the json itself
        {
            public bool useOverride;
            public string colorSchemeId;
            public MapColor? saberAColor;
            public MapColor? saberBColor;
            public MapColor? environmentColor0;
            public MapColor? environmentColor1;
            public MapColor? obstaclesColor;
            public MapColor? environmentColor0Boost;
            public MapColor? environmentColor1Boost;
            //Not officially within the default scheme, added for consistency
            public MapColor? environmentColorW;
            public MapColor? environmentColorWBoost;
        }


        [Serializable]
        public class RequirementData
        {
            public string[] _requirements;
            public string[] _suggestions;
            public string[] _warnings;
            public string[] _information;
        }

        [Serializable]
        public class MapColor
        {
            public float r;
            public float g;
            public float b;
            public float a;


            public MapColor(float r, float g, float b, float a = 1f)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;
            }
        }

        public ExtraSongData()
        {
        }

        [JsonConstructor]
        public ExtraSongData(string levelID, Contributor[] contributors, string customEnvironmentName, string customEnvironmentHash, DifficultyData[] difficulties)
        {
            this.contributors = contributors;
            _customEnvironmentName = customEnvironmentName;
            _customEnvironmentHash = customEnvironmentHash;
            _difficulties = difficulties;
        }

        internal ExtraSongData(string rawSongData, string songPath)
        {
            try
            {
                JObject info = JObject.Parse(rawSongData);
                List<Contributor> levelContributors = new List<Contributor>();
                //Check if song uses legacy value for full song One Saber mode
                if (info.TryGetValue("_customData", out var data))
                {
                    JObject infoData = (JObject) data;
                    if (infoData.TryGetValue("_contributors", out var contributors))
                    {
                        levelContributors.AddRange(contributors.ToObject<Contributor[]>());
                    }

                    if (infoData.TryGetValue("_customEnvironment", out var customEnvironment))
                    {
                        _customEnvironmentName = (string) customEnvironment;
                    }

                    if (infoData.TryGetValue("_customEnvironmentHash", out var envHash))
                    {
                        _customEnvironmentHash = (string) envHash;
                    }

                    if (infoData.TryGetValue("_defaultCharacteristic", out var defaultChar))
                    {
                        _defaultCharacteristic = (string) defaultChar;
                    }
                }

                contributors = levelContributors.ToArray();

                var envNames = new List<string>();
                if (info.TryGetValue("_environmentNames", out var environmentNames))
                {
                    envNames.AddRange(((JArray) environmentNames).Select(c => (string) c));
                }
                _environmentNames = envNames.ToArray();


                List<ColorScheme> colorSchemeList = new List<ColorScheme>();
                if (info.TryGetValue("_colorSchemes", out var colorSchemes)) //I DO NOT TRUST THAT PEOPLE DO THIS PROPERLY
                {
                    JArray colorSchemeListData = (JArray) colorSchemes;
                    foreach (var colorSchemeItem in colorSchemeListData)
                    {
                        JObject colorSchemeItemData = (JObject) colorSchemeItem;
                        bool _useOverride = false;
                        string _colorSchemeId = "SongCoreDefaultID";
                        MapColor? _saberAColor = null;
                        MapColor? _saberBColor = null;
                        MapColor? _environmentColor0 = null;
                        MapColor? _environmentColor1 = null;
                        MapColor? _obstaclesColor = null;
                        MapColor? _environmentColor0Boost = null;
                        MapColor? _environmentColor1Boost = null;
                        MapColor? _environmentColorW = null;
                        MapColor? _environmentColorWBoost = null;
                        if (colorSchemeItemData.TryGetValue("useOverride", out var useOverrideVal)) 
                        {
                            _useOverride = (bool) useOverrideVal;
                        }

                        if (colorSchemeItemData.TryGetValue("colorScheme", out var colorScheme))
                        {
                            JObject colorSchemeData = (JObject) colorScheme;

                            if (colorSchemeData.TryGetValue("colorSchemeId", out var colorSchemeIdVal))
                            {
                                _colorSchemeId = (string) colorSchemeIdVal;
                            }

                            if (colorSchemeData.TryGetValue("saberAColor", out var colorLeft))
                            {
                                if (colorLeft.Children().Count() >= 3)
                                {
                                    _saberAColor = new MapColor(
                                        (float) (colorLeft["r"] ?? 0),
                                        (float) (colorLeft["g"] ?? 0),
                                        (float) (colorLeft["b"] ?? 0),
                                        (float) (colorLeft["a"] ?? 1));
                                }
                            }

                            if (colorSchemeData.TryGetValue("saberBColor", out var colorRight))
                            {
                                if (colorRight.Children().Count() >= 3)
                                {
                                    _saberBColor = new MapColor(
                                        (float) (colorRight["r"] ?? 0),
                                        (float) (colorRight["g"] ?? 0),
                                        (float) (colorRight["b"] ?? 0),
                                        (float) (colorRight["a"] ?? 1));
                                }
                            }

                            if (colorSchemeData.TryGetValue("environmentColor0", out var envColorLeft))
                            {
                                if (envColorLeft.Children().Count() >= 3)
                                {
                                    _environmentColor0 = new MapColor(
                                        (float) (envColorLeft["r"] ?? 0),
                                        (float) (envColorLeft["g"] ?? 0),
                                        (float) (envColorLeft["b"] ?? 0),
                                        (float) (envColorLeft["a"] ?? 1));
                                }
                            }

                            if (colorSchemeData.TryGetValue("environmentColor1", out var envColorRight))
                            {
                                if (envColorRight.Children().Count() >= 3)
                                {
                                    _environmentColor1 = new MapColor(
                                        (float) (envColorRight["r"] ?? 0),
                                        (float) (envColorRight["g"] ?? 0),
                                        (float) (envColorRight["b"] ?? 0),
                                        (float) (envColorRight["a"] ?? 1));
                                }
                            }

                            if (colorSchemeData.TryGetValue("environmentColorW", out var envColorWhite))
                            {
                                if (envColorWhite.Children().Count() >= 3)
                                {
                                    _environmentColorW = new MapColor(
                                        (float) (envColorWhite["r"] ?? 0),
                                        (float) (envColorWhite["g"] ?? 0),
                                        (float) (envColorWhite["b"] ?? 0),
                                        (float) (envColorWhite["a"] ?? 1));
                                }
                            }

                            if (colorSchemeData.TryGetValue("environmentColor0Boost", out var envColorLeftBoost))
                            {
                                if (envColorLeftBoost.Children().Count() >= 3)
                                {
                                    _environmentColor0Boost = new MapColor(
                                        (float) (envColorLeftBoost["r"] ?? 0),
                                        (float) (envColorLeftBoost["g"] ?? 0),
                                        (float) (envColorLeftBoost["b"] ?? 0),
                                        (float) (envColorLeftBoost["a"] ?? 1));
                                }
                            }

                            if (colorSchemeData.TryGetValue("environmentColor1Boost", out var envColorRightBoost))
                            {
                                if (envColorRightBoost.Children().Count() >= 3)
                                {
                                    _environmentColor1Boost = new MapColor(
                                        (float) (envColorRightBoost["r"] ?? 0),
                                        (float) (envColorRightBoost["g"] ?? 0),
                                        (float) (envColorRightBoost["b"] ?? 0),
                                        (float) (envColorRightBoost["a"] ?? 1));
                                }
                            }

                            if (colorSchemeData.TryGetValue("environmentColorWBoost", out var envColorWhiteBoost))
                            {
                                if (envColorWhiteBoost.Children().Count() >= 3)
                                {
                                    _environmentColorWBoost = new MapColor(
                                        (float) (envColorWhiteBoost["r"] ?? 0),
                                        (float) (envColorWhiteBoost["g"] ?? 0),
                                        (float) (envColorWhiteBoost["b"] ?? 0),
                                        (float) (envColorWhiteBoost["a"] ?? 1));
                                }
                            }

                            if (colorSchemeData.TryGetValue("obstaclesColor", out var obColor))
                            {
                                if (obColor.Children().Count() == 3)
                                {
                                    _obstaclesColor = new MapColor(
                                        (float) (obColor["r"] ?? 0),
                                        (float) (obColor["g"] ?? 0),
                                        (float) (obColor["b"] ?? 0),
                                        (float) (obColor["a"] ?? 1));
                                }
                            }
                        }

                        colorSchemeList.Add(new ColorScheme
                        {
                            useOverride = _useOverride,
                            saberAColor = _saberAColor,
                            saberBColor = _saberBColor,
                            environmentColor0 = _environmentColor0,
                            environmentColor1 = _environmentColor1,
                            obstaclesColor = _obstaclesColor,
                            environmentColor0Boost = _environmentColor0Boost,
                            environmentColor1Boost = _environmentColor1Boost,
                            environmentColorW = _environmentColorW,
                            environmentColorWBoost = _environmentColorWBoost
                        });

                    }
                }

                _colorSchemes = colorSchemeList.ToArray();


                var diffData = new List<DifficultyData>();
                var diffSets = (JArray) info["_difficultyBeatmapSets"];
                foreach (var diffSet in diffSets)
                {
                    var setCharacteristic = (string) diffSet["_beatmapCharacteristicName"];
                    JArray diffBeatmaps = (JArray) diffSet["_difficultyBeatmaps"];
                    foreach (JObject diffBeatmap in diffBeatmaps)
                    {
                        var diffRequirements = new List<string>();
                        var diffSuggestions = new List<string>();
                        var diffWarnings = new List<string>();
                        var diffInfo = new List<string>();
                        var diffLabel = "";
                        MapColor? diffLeft = null;
                        MapColor? diffRight = null;
                        MapColor? diffEnvLeft = null;
                        MapColor? diffEnvRight = null;
                        MapColor? diffEnvWhite = null;
                        MapColor? diffEnvLeftBoost = null;
                        MapColor? diffEnvRightBoost = null;
                        MapColor? diffEnvWhiteBoost = null;
                        MapColor? diffObstacle = null;
                        int? beatmapColorSchemeIdx = null;
                        int? environmentNameIdx = null;

                        var diffDifficulty = Utils.ToEnum((string) diffBeatmap["_difficulty"], BeatmapDifficulty.Normal);

                        if (diffBeatmap.TryGetValue("_beatmapColorSchemeIdx", out var beatmapColorSchemeIdxVal))
                        {
                            beatmapColorSchemeIdx = (int) beatmapColorSchemeIdxVal;
                        }

                        if (diffBeatmap.TryGetValue("_environmentNameIdx", out var environmentNameIdxVal))
                        {
                            environmentNameIdx = (int) environmentNameIdxVal;
                        }

                        
                        bool useSongCoreColours = true;
                        
                        if (beatmapColorSchemeIdx != null)
                        {
                            var colorScheme = _colorSchemes.ElementAtOrDefault(beatmapColorSchemeIdx.Value);
                            if (colorScheme != null)
                            {
                                if (colorScheme.useOverride)
                                {
                                    useSongCoreColours = false;
                                    diffLeft = colorScheme.saberAColor;
                                    diffRight = colorScheme.saberBColor;
                                    diffEnvLeft = colorScheme.environmentColor0;
                                    diffEnvRight = colorScheme.environmentColor1;
                                    diffEnvWhite = colorScheme.environmentColorW;
                                    diffEnvLeftBoost = colorScheme.environmentColor0Boost;
                                    diffEnvRightBoost = colorScheme.environmentColor1Boost;
                                    diffEnvWhiteBoost = colorScheme.environmentColorWBoost;
                                    diffObstacle = colorScheme.obstaclesColor;
                                }
                            }
                        }

                        if (diffBeatmap.TryGetValue("_customData", out var customData))
                        {
                            JObject beatmapData = (JObject) customData;
                            if (beatmapData.TryGetValue("_difficultyLabel", out var difficultyLabel))
                            {
                                diffLabel = (string) difficultyLabel;
                            }

                            if (useSongCoreColours)
                            {
                                //Get difficulty json fields
                                if (beatmapData.TryGetValue("_colorLeft", out var colorLeft))
                                {
                                    if (colorLeft.Children().Count() >= 3)
                                    {
                                        JObject leftC = (JObject) colorLeft;

                                        diffLeft = new MapColor(
                                            (float) (colorLeft["r"] ?? 0),
                                            (float) (colorLeft["g"] ?? 0),
                                            (float) (colorLeft["b"] ?? 0),
                                            (float) (colorLeft["a"] ?? 1));
                                    }
                                }

                                if (beatmapData.TryGetValue("_colorRight", out var colorRight))
                                {
                                    if (colorRight.Children().Count() >= 3)
                                    {
                                        diffRight = new MapColor(
                                            (float) (colorRight["r"] ?? 0),
                                            (float) (colorRight["g"] ?? 0),
                                            (float) (colorRight["b"] ?? 0),
                                            (float) (colorRight["a"] ?? 1));
                                    }
                                }

                                if (beatmapData.TryGetValue("_envColorLeft", out var envColorLeft))
                                {
                                    if (envColorLeft.Children().Count() >= 3)
                                    {
                                        diffEnvLeft = new MapColor(
                                            (float) (envColorLeft["r"] ?? 0),
                                            (float) (envColorLeft["g"] ?? 0),
                                            (float) (envColorLeft["b"] ?? 0),
                                            (float) (envColorLeft["a"] ?? 1));
                                    }
                                }

                                if (beatmapData.TryGetValue("_envColorRight", out var envColorRight))
                                {
                                    if (envColorRight.Children().Count() >= 3)
                                    {
                                        diffEnvRight = new MapColor(
                                            (float) (envColorRight["r"] ?? 0),
                                            (float) (envColorRight["g"] ?? 0),
                                            (float) (envColorRight["b"] ?? 0),
                                            (float) (envColorRight["a"] ?? 1));
                                    }
                                }

                                if (beatmapData.TryGetValue("_envColorWhite", out var envColorWhite))
                                {
                                    if (envColorWhite.Children().Count() >= 3)
                                    {
                                        diffEnvWhite = new MapColor(
                                            (float) (envColorWhite["r"] ?? 0),
                                            (float) (envColorWhite["g"] ?? 0),
                                            (float) (envColorWhite["b"] ?? 0),
                                            (float) (envColorWhite["a"] ?? 1));
                                    }
                                }

                                if (beatmapData.TryGetValue("_envColorLeftBoost", out var envColorLeftBoost))
                                {
                                    if (envColorLeftBoost.Children().Count() >= 3)
                                    {
                                        diffEnvLeftBoost = new MapColor(
                                            (float) (envColorLeftBoost["r"] ?? 0),
                                            (float) (envColorLeftBoost["g"] ?? 0),
                                            (float) (envColorLeftBoost["b"] ?? 0),
                                            (float) (envColorLeftBoost["a"] ?? 1));
                                    }
                                }

                                if (beatmapData.TryGetValue("_envColorRightBoost", out var envColorRightBoost))
                                {
                                    if (envColorRightBoost.Children().Count() >= 3)
                                    {
                                        diffEnvRightBoost = new MapColor(
                                            (float) (envColorRightBoost["r"] ?? 0),
                                            (float) (envColorRightBoost["g"] ?? 0),
                                            (float) (envColorRightBoost["b"] ?? 0),
                                            (float) (envColorRightBoost["a"] ?? 1));
                                    }
                                }

                                if (beatmapData.TryGetValue("_envColorWhiteBoost", out var envColorWhiteBoost))
                                {
                                    if (envColorWhiteBoost.Children().Count() >= 3)
                                    {
                                        diffEnvWhiteBoost = new MapColor(
                                            (float) (envColorWhiteBoost["r"] ?? 0),
                                            (float) (envColorWhiteBoost["g"] ?? 0),
                                            (float) (envColorWhiteBoost["b"] ?? 0),
                                            (float) (envColorWhiteBoost["a"] ?? 1));
                                    }
                                }

                                if (beatmapData.TryGetValue("_obstacleColor", out var obColor))
                                {
                                    if (obColor.Children().Count() == 3)
                                    {
                                        diffObstacle = new MapColor(
                                            (float) (obColor["r"] ?? 0),
                                            (float) (obColor["g"] ?? 0),
                                            (float) (obColor["b"] ?? 0),
                                            (float) (obColor["a"] ?? 1));
                                    }
                                }
                            }

                            if (beatmapData.TryGetValue("_warnings", out var warnings))
                            {
                                diffWarnings.AddRange(((JArray) warnings).Select(c => (string) c));
                            }

                            if (beatmapData.TryGetValue("_information", out var information))
                            {
                                diffInfo.AddRange(((JArray) information).Select(c => (string) c));
                            }

                            if (beatmapData.TryGetValue("_suggestions", out var suggestions))
                            {
                                diffSuggestions.AddRange(((JArray) suggestions).Select(c => (string) c));
                            }

                            if (beatmapData.TryGetValue("_requirements", out var requirements))
                            {
                                diffRequirements.AddRange(((JArray) requirements).Select(c => (string) c));
                            }
                        }

                        RequirementData diffReqData = new RequirementData
                        {
                            _requirements = diffRequirements.ToArray(),
                            _suggestions = diffSuggestions.ToArray(),
                            _information = diffInfo.ToArray(),
                            _warnings = diffWarnings.ToArray()
                        };

                        diffData.Add(new DifficultyData
                        {
                            _beatmapCharacteristicName = setCharacteristic,
                            _difficulty = diffDifficulty,
                            _difficultyLabel = diffLabel,
                            additionalDifficultyData = diffReqData,
                            _colorLeft = diffLeft,
                            _colorRight = diffRight,
                            _envColorLeft = diffEnvLeft,
                            _envColorRight = diffEnvRight,
                            _envColorWhite = diffEnvWhite,
                            _envColorLeftBoost = diffEnvLeftBoost,
                            _envColorRightBoost = diffEnvRightBoost,
                            _envColorWhiteBoost = diffEnvWhiteBoost,
                            _obstacleColor = diffObstacle,
                            _beatmapColorSchemeIdx = beatmapColorSchemeIdx,
                            _environmentNameIdx = environmentNameIdx
                        });
                    }
                }

                _difficulties = diffData.ToArray();
            }
            catch (Exception ex)
            {
                Logging.Logger.Error($"Error in Level {songPath}:");
                Logging.Logger.Error(ex);
            }
        }
    }
}
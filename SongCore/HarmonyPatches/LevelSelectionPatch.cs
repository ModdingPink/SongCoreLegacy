using System.Globalization;
using System.Linq;
using HarmonyLib;

namespace SongCore.HarmonyPatches
{
    [HarmonyPatch(typeof(LevelListTableCell))]
    [HarmonyPatch(nameof(LevelListTableCell.SetDataFromLevelAsync), MethodType.Normal)]
    internal class LevelListTableCellSetDataFromLevel
    {
        private static void Postfix(LevelListTableCell __instance, BeatmapLevel level)
        {
            // Rounding BPM display for all maps, including official ones
            __instance._songBpmText.text = System.Math.Round(level.beatsPerMinute).ToString(CultureInfo.InvariantCulture);

            var authors = level.allMappers.Concat(level.allLighters).Join();
            if (!string.IsNullOrWhiteSpace(authors))
            {
                __instance._songAuthorText.richText = true;
                //Get PinkCore'd

                string mapperColor = Plugin.Configuration.GreenMapperColor ? "89ff89" : "ff69b4";

                __instance._songAuthorText.text = $"<size=80%>{level.songAuthorName}</size> <size=90%>[<color=#{mapperColor}>{authors.Replace(@"<", "<\u200B").Replace(@">", ">\u200B")}</color>]</size>";
                __instance._songAuthorText.overflowMode = TMPro.TextOverflowModes.Page;
            }
        }
    }
}

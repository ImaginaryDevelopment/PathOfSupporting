using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CHelpers;
using static PathOfSupporting.Configuration.Pad.Extensions;
using static PathOfSupporting.TreeParsing.Gems;

namespace SampleConsumer.Parsing
{
    public static class TreeParsing
    {
        public static void GetSkillGems()
        {
            var gemJsonPath = GetGemPath();
            var sgResult = getSkillGems(Sgjp);
            if(sgResult.GetOkOrNull()?.toList() is var sgs)
            {
                Console.WriteLine("Found " + sgs.Count + " skill gem(s)");
                foreach(var g in sgs)
                {
                    Console.WriteLine("  " + g.Name + " - " + g.Level);
                }

            } else
            {
                Console.Error.WriteLine(sgResult.ErrorValue.Item1);
            }

        }
        public static void GetSkillGem()
        {
            var sgResult = getSkillGem(Sgjp, "Vaal Arc");
            if (sgResult.IsOk && sgResult.ResultValue is var g)
            {
                Console.WriteLine("Found " + g.Name + " - " + g.Level);
            }
        }
        public static void GetGemReqLevels()
        {
            var result = getGemReqLevels(Sgjp, new[] { "Vaal Arc", "Essence Drain" });
            if (result.IsOk && result.ResultValue is var gs)
            {
                foreach (var g in gs)
                    Console.WriteLine("Gem:" + g.ProvidedName + (g.LevelRequirement != null ? " req level" + g.LevelRequirement.Value : ""));
            }
        }

            // expecting to be running from PathOfSupporting/SampleConsumer/bin/debug/
            // target is ../../../PoS/
            // aka PathOfSupporting/PoS/
        internal static string GetGemPath() => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))), "PoS");
        internal static SkillGemJsonPath Sgjp => new SkillGemJsonPath(GetGemPath(), null);
    }
}

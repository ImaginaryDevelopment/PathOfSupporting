using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static CHelpers;
using static PathOfSupporting.Configuration.Pad.Extensions;
using static PathOfSupporting.TreeParsing.Gems;

namespace SampleConsumer.Parsing
{
    public static class TreeParsing
    {
        public static void GetSkillGems()
        {
            var gemJsonPath = GetGemPath();
            var sgResult = PathOfSupporting.TreeParsing.Gems.getSkillGems(new PathOfSupporting.TreeParsing.Gems.SkillGemJsonPath(gemJsonPath, null));
            if(sgResult.IsOk && sgResult.ResultValue?.ToList() is var sgs)
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
            var sgResult = PathOfSupporting.TreeParsing.Gems.getSkillGem(Sgjp, "Vaal Arc");
            if (sgResult.IsOk && sgResult.ResultValue is var g)
            {
                Console.WriteLine("Found " + g.Name + " - " + g.Level);
            }
        }

            // expecting to be running from PathOfSupporting/SampleConsumer/bin/debug
            // target is ../../../PoS
        internal static string GetGemPath() => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))), "PoS");
        internal static SkillGemJsonPath Sgjp => new SkillGemJsonPath(GetGemPath(), null);
    }
}

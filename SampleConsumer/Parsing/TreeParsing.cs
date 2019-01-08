using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CHelpers;
using static PathOfSupporting.Configuration.Pad.Extensions;
using static PathOfSupporting.TreeParsing;
using static PathOfSupporting.TreeParsing.Gems;

namespace SampleConsumer.Parsing
{
    public static class TreeParsing
    {
        internal static string GetResourcePath() => Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))), "PoS");
        internal static JsonResourcePath Rp => new JsonResourcePath(GetResourcePath(), null);

        // things we can read out of the Path of Exile official Gems.json
        public static class Gems
        {

            public static void GetSkillGems()
            {
                var sgResult = getSkillGems(Rp);
                // have to check isOk since C#'s pattern matching to var will accept nulls
                if (sgResult.IsOk && sgResult.GetOkOrNull()?.toList() is var sgs)
                {
                    Console.WriteLine("Found " + sgs.Count + " skill gem(s)");
                    foreach (var g in sgs)
                    {
                        Console.WriteLine("  " + g.Name + " - " + g.Level);
                    }

                }
                else
                {
                    Console.Error.WriteLine(sgResult.ErrorValue.Item1);
                }

            }
            public static void GetSkillGem()
            {
                var sgResult = getSkillGem(Rp, "Vaal Arc");
                if (sgResult.IsOk && sgResult.ResultValue is var g)
                {
                    Console.WriteLine("Found " + g.Name + " - " + g.Level);
                }
            }
            public static void GetGemReqLevels()
            {
                var result = getGemReqLevels(Rp, new[] { "Vaal Arc", "Essence Drain" });
                if (result.IsOk && result.ResultValue is var gs)
                {
                    foreach (var g in gs)
                        Console.WriteLine("Gem:" + g.ProvidedName + (g.LevelRequirement != null ? " req level" + g.LevelRequirement.Value : ""));
                }
            }

            // expecting to be running from PathOfSupporting/SampleConsumer/bin/debug/
            // target is ../../../PoS/
            // aka PathOfSupporting/PoS/
        }
        // things we can read out of the Path of Exile official Passives.json
        public static class Passives
        {
            public static void DecodeUrl()
            {
                var decodeResult = PassiveJsParsing.decodePassives(Rp, "https://www.pathofexile.com/fullscreen-passive-skill-tree/AAAABAMBAAceDXwOSBBREQ8RLxFQEZYTbRQJFLAVfhcvF1QYahslHNwdFB3ZJogo-isKK5osnCy_Ow09X0NUSVFKn0uuTC1Ms1JTUrJT1FXWVkhXyVgHXfJfKmTnZp5ncWnYa9ttGXRVdoJ4L3zlfqGApIIegseESIhCibyMdo2CjxqPRo_6knSVLpeVl_SYrZuhnwGg5qcIpyuo6qyYtUi3MLcxuJO6Drv8wcXG98gM0B_Q0NHk2CTZW9t634rfsONW6QLrY-vu7Bjsiu_r8NX0cffB99f56PrS");
                // have to check ok, or type out the type of result
                if (decodeResult.IsOk && decodeResult.GetOkOrNull() is var tree)
                {
                    Console.WriteLine(tree.Class);
                    foreach (var n in tree.Nodes)
                    {
                        Console.WriteLine(n.sd);
                    }

                }
                else if (decodeResult.GetErrOrDefault() is var err){
                    Console.Error.WriteLine(err.Item1);

                }

            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static CHelpers;
using static PathOfSupporting.Configuration.Pad.Extensions;

namespace SampleConsumer.Parsing
{
    public static class TreeParsing
    {
        public static void GetSkillGems()
        {
            var gemJsonPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))), "PoS");
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
    }
}

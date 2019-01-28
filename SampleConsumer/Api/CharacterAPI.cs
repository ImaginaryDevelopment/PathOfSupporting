using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CHelpers;

using PathOfSupporting.Api.Characters;

using static PathOfSupporting.Schema;


namespace SampleConsumer.Api
{
    public static class CharacterAPI
    {
        public static async Task GetCharacterPassives()
        {
            var an = "DevelopersDevelopersDevelopers";
            var cn = "HazeMe";
            var result = await Fetch.fetchPassiveTree(Apis.FetchArguments<PassiveSkillsArguments>.NewValues(new PassiveSkillsArguments(an, character: cn))).ToTask();
            if (result?.Value != null)
            {
                var response = result.Value;
                Console.WriteLine(an + ":" + cn);
                Console.WriteLine("Jewel Names:");
                foreach (var item in response.Items)
                    Console.WriteLine("  " + item.Name);

            }


        }
    }
}

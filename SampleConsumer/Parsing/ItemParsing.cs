using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;

using CHelpers;

using static PathOfSupporting.Configuration.Pad.Extensions;

namespace SampleConsumer
{
    public static class ItemParsing
    {
        // use the extension method
        public static void ParseItem()
        {
            var values = PathOfSupporting.ItemParsing.Resistances.getValues(@"
                +16% to Cold and Lightning Resistances
                Adds 4 to 9 Physical Damage to Attacks
                +10 to all Attributes
                +63 to maximum Mana
                17% increased Rarity of Items found
                +43% to Fire Resistance
                +27% to Lightning Resistance"
            );
            values.ProcessResult(x => x.Dump(), (msg, err) => (msg, err).Dump());
        }

        // example with no inputs
        // less extension methods used
        public static void ParseItem2()
        {
            var text = @"
                +16% to Cold and Lightning Resistances
                Adds 4 to 9 Physical Damage to Attacks
                +10 to all Attributes
                +63 to maximum Mana
                17% increased Rarity of Items found
                +43% to Fire Resistance
                +27% to Lightning Resistance" ;
            ParseItem(text);
        }

        public static void ParseItem(string itemText)
        {
            var values = PathOfSupporting.ItemParsing.Resistances.getValues(itemText);
            if (values.IsOk && values.GetOkOrNull() is var itemLines)
            {
                itemLines.Dump();
            }
            else if (values.GetErrOrDefault() is var e)
            {
                var errorDisplay = new { Msg = e.Item1, Ex = e.Item2 };
                errorDisplay.Dump();
            }
            else
                values.Dump();
        }

    }
}

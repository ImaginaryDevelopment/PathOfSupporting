using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CHelpers;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using PathOfSupporting.NinjaAPI;

namespace SampleConsumer
{
    public static class NinjaAPI
    {
        public static void FetchCurrency()
        {
            var ninjaResponse = Fetch.fetchCurrency(FetchArguments.NewLeague("Betrayal")).ToTask().Result?.Value;
            if (ninjaResponse != null)
            {
                Console.WriteLine("NinjaResponse:" + ninjaResponse);
            }
            else
            {
                Console.Error.WriteLine("Unable to fetch Ninja Currency Info");
            }
        }

        // show directly accessing the implementation for getting error details
        public static async Task FetchDebug()
        {
            var resultOrError = await Impl.fetchCurrency("").ToTask();
            if (resultOrError .IsOk)
            {
                var result = resultOrError.ResultValue;
                var ninja = result.Item1;
                var rawResponse = result.Item2;
                Console.WriteLine("Ninja Result:" + ninja);
            }
            else
            {
                Console.Error.WriteLine("Ninja Error: " + ; resultOrError.ErrorValue)
            }
        }
    }
}

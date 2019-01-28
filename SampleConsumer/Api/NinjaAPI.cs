using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CHelpers;
using PathOfSupporting.Api.Ninja;

namespace SampleConsumer
{
    public static class NinjaAPI
    {
        static readonly FetchArguments fa = FetchArguments.NewLeague("Betrayal");
        public static void FetchCurrency()
        {
            var ninjaResponse = Fetch.fetchCurrency(fa).ToTask().Result?.Value;
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
            var resultOrError = await Impl.fetchCurrency(Impl.getTargeting(fa)).ToTask();
            if (resultOrError.IsOk)
            {
                var (ninja, rawResponse) = resultOrError.ResultValue;
                Console.WriteLine("Ninja Result:" + ninja);
            }
            else
            {
                var (errorMsg, errorInfo) = resultOrError.ErrorValue;
                Console.Error.WriteLine(errorMsg);
                if (errorInfo.Value != null)
                    Console.Error.WriteLine("Ninja Error: " + errorInfo.Value);
            }
        }
    }
}

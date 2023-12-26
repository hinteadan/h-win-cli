using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using System;
using System.Threading.Tasks;

namespace H.Win.CLI.Commands
{
    internal class DebugCommand : CommandBase
    {
        public override async Task<OperationResult> Run()
        {
            Log("Debugging...");
            using (new TimeMeasurement(x => Log($"DONE Debugging in {x}")))
            {
                await Task.Delay(TimeSpan.FromSeconds(.5));


                //TODO: Do stuff here


            }

            return OperationResult.Win();
        }
    }
}

using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using WindowsFirewallHelper;

namespace H.Win.CLI.Commands
{
    internal class DebugCommand : CommandBase
    {
        IFirewall firewall;
        public override void ReferDependencies(ImADependencyProvider dependencyProvider)
        {
            base.ReferDependencies(dependencyProvider);
            this.firewall = dependencyProvider.Get<IFirewall>();
        }

        public override async Task<OperationResult> Run()
        {
            Log("Debugging...");
            using (new TimeMeasurement(x => Log($"DONE Debugging in {x}")))
            {
                await Task.Delay(TimeSpan.Zero);



                //IFirewallRule[] rules
                //    = firewall
                //    .Rules
                //    .ToArray()
                //    ;
            }

            return OperationResult.Win();
        }
    }
}

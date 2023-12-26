using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using H.Necessaire.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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

                DirectoryInfo folder = new DirectoryInfo("StaticData");
                FileInfo[] dataFiles =  folder.GetFiles("*.json");

                string[] rawJsons = dataFiles.Select(x => File.ReadAllText(x.FullName)).ToArray();

                RawDataFileEntry[] dataEntries = rawJsons.SelectMany(x => x.JsonToObject<RawDataFileEntry[]>()).ToArray();

                var ips
                    = dataEntries
                    .Select(x => x.IPAddress)
                    .GroupBy(x => x.Substring(0, x.LastIndexOf(".")) + ".*")
                    .Select(x => new { IPGroup = x.Key, IPs = x.Select(x => x).Distinct().Order().ToArray(), Count = x.Count() })
                    .OrderByDescending(x => x.IPs.Length)
                    .ThenBy(x => x.IPGroup)
                    .ToArray()
                    ;



                //IFirewallRule[] rules
                //    = firewall
                //    .Rules
                //    .ToArray()
                //    ;
            }

            return OperationResult.Win();
        }

        [DataContract]
        class RawDataFileEntry
        {
            [DataMember(Name = "newrelic.IP")] public string IPAddress { get; set; }
        }
    }
}

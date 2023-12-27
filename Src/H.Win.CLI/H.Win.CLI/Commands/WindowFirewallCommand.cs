using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using H.Win.CLI.BLL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WindowsFirewallHelper;

namespace H.Win.CLI.Commands
{
    [ID("windows-firewall")]
    internal class WindowFirewallCommand : CommandBase
    {
        #region Construct
        static readonly string[] usageSyntaxes = new string[]
        {
            "windows-firewall all-blocked-ips out=\"currently-blocked-ips.txt\"",
            "windows-firewall block-ips in=\"malicious-ip-ranges.txt\" rule-name=\"Blocked Malicious IPs\" overwrite-rule=true",
        };
        protected override string[] GetUsageSyntaxes() => usageSyntaxes;

        IFirewall firewall;
        public override void ReferDependencies(ImADependencyProvider dependencyProvider)
        {
            base.ReferDependencies(dependencyProvider);
            this.firewall = dependencyProvider.Get<IFirewall>();
        }
        #endregion

        public override async Task<OperationResult> Run()
        {
            Note[] args = (await GetArguments())?.Jump(1);

            if (args?.FirstOrDefault().ID.Is("all-blocked-ips") == true)
                return await AggregateAndPrintAllCurrentlyBlockedIPsInWindowsFirewall(args?.Jump(1));

            if (args?.FirstOrDefault().ID.Is("block-ips") == true)
                return await BlockIPs(args?.Jump(1));

            PrintUsageSyntax();

            return OperationResult.Win();
        }

        private async Task<OperationResult> BlockIPs(Note[] args)
        {
            OperationResult result = OperationResult.Fail("Not yet started");

            await
                new Func<Task>(() =>
                {
                    string inPath = args?.Get("in");
                    if (inPath.IsEmpty())
                    {
                        result = OperationResult.Fail("Input file path with blocked IPs is required via the \"in\" args");
                        return Task.CompletedTask;
                    }

                    FileInfo inFile = new FileInfo(inPath);
                    if(!inFile.Exists)
                    {
                        result = OperationResult.Fail($"Input file path with blocked IPs doesn't exist: {inFile.FullName}");
                        return Task.CompletedTask;
                    }



                    FirewallAddressInfo[] blockedAddresses
                        = firewall
                        .Rules
                        .Where(x => x.HasBlockingAddresses())
                        .Where(rule => rule.IsEnable && rule.Direction.In(FirewallDirection.Inbound))
                        .SelectMany(rule => rule.AggregateBlockedAddressInfos() ?? Array.Empty<FirewallAddressInfo>())
                        .AggregateBulk()
                        ?.OrderBy(x => x.ID)
                        .ToArray()
                        ;

                    result = OperationResult.Win();

                    return Task.CompletedTask;
                })
                .TryOrFailWithGrace(
                    onFail: ex => result = OperationResult.Fail(ex, $"Error occurred while trying to block IPs. Message: {ex.Message}")
                );

            return result;
        }

        private async Task<OperationResult> AggregateAndPrintAllCurrentlyBlockedIPsInWindowsFirewall(Note[] notes)
        {
            OperationResult result = OperationResult.Fail("Not yet started");

            await
                new Func<Task>(() =>
                {
                    FirewallAddressInfo[] blockedAddresses
                        = firewall
                        .Rules
                        .Where(x => x.HasBlockingAddresses())
                        .Where(rule => rule.IsEnable && rule.Direction.In(FirewallDirection.Inbound))
                        .SelectMany(rule => rule.AggregateBlockedAddressInfos() ?? Array.Empty<FirewallAddressInfo>())
                        .AggregateBulk()
                        ?.OrderBy(x => x.ID)
                        .ToArray()
                        ;

                    result = OperationResult.Win();

                    return Task.CompletedTask;
                })
                .TryOrFailWithGrace(
                    onFail: ex => result = OperationResult.Fail(ex, $"Error occurred while trying to Aggregate And Print All Currently Blocked IPs In Windows Firewall. Message: {ex.Message}")
                );

            return result;
        }
    }
}

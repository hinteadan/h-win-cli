using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using H.Win.CLI.BLL;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace H.Win.CLI.Commands
{
    [ID("windows-firewall")]
    internal class WindowFirewallCommand : CommandBase
    {
        #region Construct
        static readonly string[] usageSyntaxes = new string[]
        {
            "windows-firewall all-blocked-ips out=\"currently-blocked-ips.txt\"",
            "windows-firewall block-ips in=\"malicious-ip-ranges.txt\" rule-name=\"Block Malicious IPs\" overwrite-rule=true",
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
                    string ruleName = args?.Get("rule-name");
                    if (ruleName.IsEmpty())
                    {
                        result = OperationResult.Fail("Firewall Rule Name is required via the \"rule-name\" arg");
                        return Task.CompletedTask;
                    }

                    string inPath = args?.Get("in");
                    if (inPath.IsEmpty())
                    {
                        result = OperationResult.Fail("Input file path with blocked IPs is required via the \"in\" arg");
                        return Task.CompletedTask;
                    }

                    FileInfo inFile = new FileInfo(inPath);
                    if (!inFile.Exists)
                    {
                        result = OperationResult.Fail($"Input file path with blocked IPs doesn't exist: {inFile.FullName}");
                        return Task.CompletedTask;
                    }

                    IAddress[] addresses = File.ReadAllLines(inFile.FullName).ParseAsFirewallAddresses();

                    bool canOverwrite = args?.Get("overwrite-rule")?.ParseToBoolOrFallbackTo(false).Value ?? false;
                    IFirewallRule existingRule = firewall.Rules?.SingleOrDefault(x => x.Name?.Is(ruleName) == true || x.FriendlyName?.Is(ruleName) == true);

                    if (existingRule != null && !canOverwrite)
                    {
                        result = OperationResult.Fail($"Rule \"{ruleName}\" already exists. If you're willing to overwrite specifiy it via the \"overwrite-rule=true\" arg");
                        return Task.CompletedTask;
                    }

                    if (existingRule != null)
                    {
                        firewall.Rules.Remove(existingRule);
                    }

                    IFirewallRule rule = CreateBlockRule(ruleName, addresses);

                    firewall.Rules.Add(rule);

                    result = OperationResult.Win();

                    return Task.CompletedTask;
                })
                .TryOrFailWithGrace(
                    onFail: ex => result = OperationResult.Fail(ex, $"Error occurred while trying to block IPs. Message: {ex.Message}")
                );

            return result;
        }

        private IFirewallRule CreateBlockRule(string ruleName, IAddress[] addresses)
        {
            IFirewallRule rule = firewall.CreatePortRule(FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public, ruleName, FirewallAction.Block, 0, FirewallProtocol.TCP);
            rule.Action = FirewallAction.Block;
            rule.ApplicationName = null;
            rule.Direction = FirewallDirection.Inbound;
            rule.IsEnable = true;
            rule.LocalAddresses = SingleIP.Any.AsArray();
            rule.LocalPorts = NetworkAddressExtensions.AllPortsExceptWebBrowsing;
            //rule.LocalPortType = FirewallPortType.Specific;
            rule.Name = ruleName;
            rule.Protocol = FirewallProtocol.TCP;
            rule.RemoteAddresses = addresses;
            rule.RemotePorts = Array.Empty<ushort>();
            //rule.Scope = FirewallScope.Specific;
            rule.ServiceName = null;
            return rule;
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

                    var rule = blockedAddresses.First().Rules.First();

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

using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using System;
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

            PrintUsageSyntax();

            return OperationResult.Win();
        }

        private async Task<OperationResult> AggregateAndPrintAllCurrentlyBlockedIPsInWindowsFirewall(Note[] notes)
        {
            OperationResult result = OperationResult.Fail("Not yet started");

            await
                new Func<Task>(async () =>
                {
                    IFirewallRule[] rulesWithBlockingIPs
                        = firewall
                        .Rules
                        .Where(RuleHasBlockingIPs)
                        .Where(rule => rule.IsEnable && rule.Direction.In(FirewallDirection.Inbound))
                        .ToNoNullsArray();

                    result = OperationResult.Win();
                })
                .TryOrFailWithGrace(
                    onFail: ex => result = OperationResult.Fail(ex, $"Error occurred while trying to Aggregate And Print All Currently Blocked IPs In Windows Firewall. Message: {ex.Message}")
                );

            return result;
        }

        private bool RuleHasBlockingIPs(IFirewallRule rule)
        {
            if (rule == null)
                return false;

            if (rule.Action.NotIn(FirewallAction.Block))
                return false;

            bool hasLocalIPs = rule.LocalAddresses?.Any() == true;
            bool hasRemoteIPs = rule.RemoteAddresses?.Any() == true;

            if (!hasLocalIPs && !hasRemoteIPs)
                return false;

            return true;
        }


        class BlockedAddressInfo : IDentityType<IAddress>
        {
            public IAddress ID { get; set; }
            public Type AddressType => ID?.GetType();
            public string AddressTypeName => AddressType?.Name;
            public ushort[] LocalPorts { get; set; }
            public ushort[] RemotePorts { get; set; }
            public IPType IPType { get; set; }
            public IFirewallRule[] Rules { get; set; }
            public string[] RuleFriendlyNames { get; set; }
            public string[] RuleSystemNames { get; set; }
            public string[] ApplicationNames { get; set; }
            public string[] ServiceNames { get; set; }
            public FirewallProfiles[] Profiles { get; set; }
            public FirewallProtocol[] Protocols { get; set; }
            public FirewallScope[] Scopes { get; set; }
        }

        enum IPType
        {
            Local = 0,
            Remote = 17,
        }
    }
}

using H.Necessaire;
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFirewallHelper;

namespace H.Win.CLI.BLL
{
    public static class WindowsFirewallExtentions
    {
        public static FirewallAddressInfo[] AggregateBlockedAddressInfos(this IFirewallRule firewallRule)
        {
            if (firewallRule.HasBlockingAddresses() != true)
                return null;

            List<FirewallAddressInfo> result = new List<FirewallAddressInfo>();

            if (firewallRule.LocalAddresses?.Any() == true)
            {
                result.AddRange(
                    firewallRule
                    .LocalAddresses
                    .Select(address => BuildBlockedAddressInfo(firewallRule, address, WindowsFirewallAddressType.Local))
                );
            }

            if (firewallRule.RemoteAddresses?.Any() == true)
            {
                result.AddRange(
                    firewallRule
                    .LocalAddresses
                    .Select(address => BuildBlockedAddressInfo(firewallRule, address, WindowsFirewallAddressType.Remote))
                );
            }

            return result.ToNoNullsArray();
        }



        public static FirewallAddressInfo[] AggregateBulk(this IEnumerable<FirewallAddressInfo> allBulkBlockedAddressInfos)
        {
            if (allBulkBlockedAddressInfos?.Any() != true)
                return null;

            return
                allBulkBlockedAddressInfos
                .GroupBy(x => x.ID)
                .Select(BuildBlockedAddressInfo)
                .ToNoNullsArray();
        }

        public static bool HasBlockingAddresses(this IFirewallRule rule)
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

        private static FirewallAddressInfo BuildBlockedAddressInfo(IEnumerable<FirewallAddressInfo> group)
        {
            if (group?.Any() != true)
                return null;

            IAddress address = group.FirstOrDefault()?.Address;

            if (address == null)
                return null;

            return new FirewallAddressInfo
            {
                Address = address,
                LocalPorts = group.SelectMany(x => x.LocalPorts ?? Array.Empty<ushort>()).Distinct().ToNoNullsArray(),
                RemotePorts = group.SelectMany(x => x.RemotePorts ?? Array.Empty<ushort>()).Distinct().ToNoNullsArray(),
                IPType = group.First().IPType,
                Rules = group.SelectMany(x => x.Rules ?? Array.Empty<IFirewallRule>()).Distinct().ToNoNullsArray(),
                RuleFriendlyNames = group.SelectMany(x => x.RuleFriendlyNames ?? Array.Empty<string>()).Distinct().ToNoNullsArray(),
                RuleSystemNames = group.SelectMany(x => x.RuleSystemNames ?? Array.Empty<string>()).Distinct().ToNoNullsArray(),
                Applications = group.SelectMany(x => x.Applications ?? Array.Empty<string>()).Distinct().ToNoNullsArray(),
                Profiles = group.SelectMany(x => x.Profiles ?? Array.Empty<FirewallProfiles>()).Distinct().ToNoNullsArray(),
                Protocols = group.SelectMany(x => x.Protocols ?? Array.Empty<FirewallProtocol>()).Distinct().ToNoNullsArray(),
                Scopes = group.SelectMany(x => x.Scopes ?? Array.Empty<FirewallScope>()).Distinct().ToNoNullsArray(),
                ServiceNames = group.SelectMany(x => x.ServiceNames ?? Array.Empty<string>()).Distinct().ToNoNullsArray(),
                Directions = group.SelectMany(x => x.Directions ?? Array.Empty<FirewallDirection>()).Distinct().ToNoNullsArray(),
                IsDisabled = group.All(x => x.IsDisabled),
                IsFullyEnabled = group.All(x => x.IsFullyEnabled),
                Actions = group.SelectMany(x => x.Actions ?? Array.Empty<FirewallAction>()).Distinct().ToNoNullsArray(),
                LocalPortTypes = group.SelectMany(x => x.LocalPortTypes ?? Array.Empty<FirewallPortType>()).Distinct().ToNoNullsArray(),
            };
        }

        private static FirewallAddressInfo BuildBlockedAddressInfo(IFirewallRule firewallRule, IAddress address, WindowsFirewallAddressType addressType)
        {
            return new FirewallAddressInfo
            {
                Address = address,
                LocalPorts = firewallRule.LocalPorts,
                RemotePorts = firewallRule.RemotePorts,
                IPType = addressType,
                Rules = firewallRule.AsArray(),
                RuleFriendlyNames = firewallRule.FriendlyName.AsArray(),
                RuleSystemNames = firewallRule.Name.AsArray(),
                Applications = firewallRule.ApplicationName.AsArray(),
                Profiles = Enum.GetValues(typeof(FirewallProfiles)).Cast<int>().Where(f => (f & (int)firewallRule.Profiles) == f).ToArray().Select(x => (FirewallProfiles)x).ToArray(),
                Protocols = firewallRule.Protocol.AsArray(),
                Scopes = firewallRule.Scope.AsArray(),
                ServiceNames = firewallRule.ServiceName.AsArray(),
                Directions = firewallRule.Direction.AsArray(),
                IsDisabled = !firewallRule.IsEnable,
                IsFullyEnabled = firewallRule.IsEnable,
                Actions = firewallRule.Action.AsArray(),
                LocalPortTypes = firewallRule.LocalPortType.AsArray(),
            };
        }
    }
}

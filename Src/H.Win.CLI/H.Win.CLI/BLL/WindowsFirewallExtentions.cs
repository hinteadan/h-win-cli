using H.Necessaire;
using System.Collections.Generic;
using System;
using System.Linq;
using WindowsFirewallHelper;

namespace H.Win.CLI.BLL
{
    public static class WindowsFirewallExtentions
    {
        public static FirewallBlockedAddressInfo[] AggregateBlockedAddressInfos(this IFirewallRule firewallRule)
        {
            if (firewallRule.HasBlockingAddresses() != true)
                return null;

            List<FirewallBlockedAddressInfo> result = new List<FirewallBlockedAddressInfo>();

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

        

        public static FirewallBlockedAddressInfo[] AggregateBulk(this IEnumerable<FirewallBlockedAddressInfo> allBulkBlockedAddressInfos)
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

        private static FirewallBlockedAddressInfo BuildBlockedAddressInfo(IEnumerable<FirewallBlockedAddressInfo> group)
        {
            if (group?.Any() != true)
                return null;

            IAddress address = group.FirstOrDefault()?.Address;

            if (address == null)
                return null;

            return new FirewallBlockedAddressInfo
            {
                Address = address,
                LocalPorts = group.SelectMany(x => x.LocalPorts ?? Array.Empty<ushort>()).Distinct().ToArrayNullIfEmpty(),
                RemotePorts = group.SelectMany(x => x.RemotePorts ?? Array.Empty<ushort>()).Distinct().ToArrayNullIfEmpty(),
                IPType = group.First().IPType,
                Rules = group.SelectMany(x => x.Rules ?? Array.Empty<IFirewallRule>()).Distinct().ToArrayNullIfEmpty(),
                RuleFriendlyNames = group.SelectMany(x => x.RuleFriendlyNames ?? Array.Empty<string>()).Distinct().ToArrayNullIfEmpty(),
                RuleSystemNames = group.SelectMany(x => x.RuleSystemNames ?? Array.Empty<string>()).Distinct().ToArrayNullIfEmpty(),
                Applications = group.SelectMany(x => x.Applications ?? Array.Empty<string>()).Distinct().ToArrayNullIfEmpty(),
                Profiles = group.SelectMany(x => x.Profiles ?? Array.Empty<FirewallProfiles>()).Distinct().ToArrayNullIfEmpty(),
                Protocols = group.SelectMany(x => x.Protocols ?? Array.Empty<FirewallProtocol>()).Distinct().ToArrayNullIfEmpty(),
                Scopes = group.SelectMany(x => x.Scopes ?? Array.Empty<FirewallScope>()).Distinct().ToArrayNullIfEmpty(),
                ServiceNames = group.SelectMany(x => x.ServiceNames ?? Array.Empty<string>()).Distinct().ToArrayNullIfEmpty(),
            };
        }

        private static FirewallBlockedAddressInfo BuildBlockedAddressInfo(IFirewallRule firewallRule, IAddress address, WindowsFirewallAddressType addressType)
        {
            return new FirewallBlockedAddressInfo
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
            };
        }
    }
}

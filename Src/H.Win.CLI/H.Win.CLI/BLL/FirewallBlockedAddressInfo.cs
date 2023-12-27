using H.Necessaire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsFirewallHelper;

namespace H.Win.CLI.BLL
{
    public class FirewallBlockedAddressInfo : IStringIdentity
    {
        public string ID => Address?.ToString();

        public IAddress Address { get; set; }
        public Type AddressType => Address?.GetType();
        public string AddressTypeName => AddressType?.Name;

        public ushort[] LocalPorts { get; set; }
        public ushort[] RemotePorts { get; set; }
        public WindowsFirewallAddressType IPType { get; set; }
        public IFirewallRule[] Rules { get; set; }
        public string[] RuleFriendlyNames { get; set; }
        public string[] RuleSystemNames { get; set; }
        public string[] Applications { get; set; }
        public string[] ServiceNames { get; set; }
        public FirewallProfiles[] Profiles { get; set; }
        public FirewallProtocol[] Protocols { get; set; }
        public FirewallScope[] Scopes { get; set; }
    }
}

using H.Necessaire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using WindowsFirewallHelper;
using WindowsFirewallHelper.Addresses;

namespace H.Win.CLI.BLL
{
    public static class NetworkAddressExtensions
    {
        public const string RangeSeparator = "-";
        static readonly Lazy<ushort[]> allPortsExceptWebBrowsing = new Lazy<ushort[]>(GenerateAllPortsExceptWebBrowsing());
        private static ushort[] GenerateAllPortsExceptWebBrowsing()
        {
            return
                Enumerable
                .Range(0, ushort.MaxValue)
                .Except(new int[] { 80, 443 })
                .Select(x => (ushort)x)
                .ToArray()
                ;
        }
        public static ushort[] AllPortsExceptWebBrowsing => allPortsExceptWebBrowsing.Value;

        public static string ToRange(this string ipv4)
        {
            if (ipv4.IsEmpty())
                return null;

            if (!IPAddress.TryParse(ipv4, out IPAddress parsedAddress))
            {
                return null;
            }

            if (parsedAddress.AddressFamily.NotIn(AddressFamily.InterNetwork))//NOT IPv4
            {
                return null;
            }

            string root = ipv4.Substring(0, ipv4.LastIndexOf("."));

            return $"{root}.0{RangeSeparator}{root}.255";
        }

        public static IAddress ParseAsFirewallAddress(this string addressValue)
        {
            if (addressValue.IsEmpty())
                return null;

            string[] parts
                = addressValue
                .Split(RangeSeparator.AsArray(), 2, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.NullIfEmpty()?.Trim())
                .ToNoNullsArray();
            ;

            if (parts?.Any() != true)
                return null;

            if (parts.Length == 1)
            {
                if (!IPAddress.TryParse(parts.Single(), out IPAddress address))
                    return null;
                return new SingleIP(address);
            }

            if (!IPAddress.TryParse(parts.First(), out IPAddress from) || !IPAddress.TryParse(parts.Last(), out IPAddress to))
                return null;
            return new IPRange(from, to);
        }
        public static IAddress[] ParseAsFirewallAddresses(this IEnumerable<string> addressesValues)
        {
            if (addressesValues?.Any() != true)
                return null;

            return
                addressesValues
                .Select(x => x.ParseAsFirewallAddress())
                .ToNoNullsArray()
                ;
        }
    }
}

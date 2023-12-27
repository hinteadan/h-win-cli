using H.Necessaire;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

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

            if(!IPAddress.TryParse(ipv4, out IPAddress parsedAddress))
            {
                return null;
            }

            if(parsedAddress.AddressFamily.NotIn(AddressFamily.InterNetwork))//NOT IPv4
            {
                return null;
            }

            string root = ipv4.Substring(0, ipv4.LastIndexOf("."));

            return $"{root}.0{RangeSeparator}{root}.255";
        }

        //public static string[]
    }
}

using H.Necessaire;
using System.Linq;

namespace H.Win.CLI.Model
{
    public class DetailedIP
    {
        public static readonly string CsvHeader = $"{nameof(IP)},{nameof(CountryCode)},{nameof(CountryName)},{nameof(ISP)}";

        public string IP { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string ISP { get; set; }


        public string ToCsvLine()
            => (IP.IsEmpty()) ? null : $"{IP.Replace(",", " ")},{CountryCode?.Replace(",", " ")},{CountryName?.Replace(",", " ")},{ISP?.Replace(",", " ")}";

        public static DetailedIP FromCsvLine(string csvLine)
        {
            string[] parts = csvLine.Split(",");
            if (!parts.Any())
                return null;

            return new DetailedIP
            {
                IP = parts[0].Trim().NullIfEmpty(),
                CountryCode = parts.Length > 1 ? parts[1].Trim().NullIfEmpty() : null,
                CountryName = parts.Length > 2 ? parts[2].Trim().NullIfEmpty() : null,
                ISP = parts.Length > 3 ? parts[3].Trim().NullIfEmpty() : null,
            };
        }
    }
}

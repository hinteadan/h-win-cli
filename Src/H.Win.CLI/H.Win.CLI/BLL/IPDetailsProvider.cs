using H.Necessaire;
using H.Necessaire.Serialization;
using H.Win.CLI.Model;
using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace H.Win.CLI.BLL
{
    internal class IPDetailsProvider
    {
        static readonly HttpClient http = new HttpClient();
        static readonly string baseUrl = "https://api.iplocation.net/?ip=";

        internal async Task<DetailedIP> DetailIP(string ipToDetail)
        {
            DetailedIP result = null;

            await
                new Func<Task>(async () =>
                {
                    string rawDetails = await http.GetStringAsync($"{baseUrl}{ipToDetail}");

                    RawIPDetails rawIPDetails = rawDetails.JsonToObject<RawIPDetails>();

                    if (rawIPDetails == null)
                        return;

                    result = new DetailedIP
                    {
                        IP = ipToDetail,
                        CountryCode = rawIPDetails.CountryCodeAsISO2,
                        CountryName = rawIPDetails.CountryName,
                        ISP = rawIPDetails.ISP,
                    };


                })
                .TryOrFailWithGrace(
                    onFail: ex =>
                    {
                        result = null;
                    }
                );

            return result;
        }

        [DataContract]
        class RawIPDetails
        {
            [DataMember(Name = "ip")] public string IP { get; set; }
            [DataMember(Name = "ip_number")] public string IPNumber { get; set; }
            [DataMember(Name = "ip_version")] public int IPVersion { get; set; }
            [DataMember(Name = "country_name")] public string CountryName { get; set; }
            [DataMember(Name = "country_code2")] public string CountryCodeAsISO2 { get; set; }
            [DataMember(Name = "isp")] public string ISP { get; set; }
            [DataMember(Name = "response_code")] public string ResponseCode { get; set; }
            [DataMember(Name = "response_message")] public string ResponseMessage { get; set; }
        }
    }
}

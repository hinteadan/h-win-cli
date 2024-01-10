using H.Win.CLI.Model;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace H.Win.CLI.BLL
{
    internal class IPDetailsProvider
    {
        static readonly HttpClient http = new HttpClient();

        internal Task<DetailedIP> Detail(string ipToDetail)
        {
            throw new NotImplementedException();
        }
    }
}

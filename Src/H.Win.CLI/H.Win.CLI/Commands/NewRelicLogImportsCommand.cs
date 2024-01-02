using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using H.Necessaire.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace H.Win.CLI.Commands
{
    [ID("newrelic-log-imports")]
    internal class NewRelicLogImportsCommand : CommandBase
    {
        static readonly string[] usageSyntaxes = new string[]
        {
            "newrelic-log-imports malicious-ips out=\"StaticData\"",
        };
        protected override string[] GetUsageSyntaxes() => usageSyntaxes;

        string newRelicApiBaseUrl = "https://api.eu.newrelic.com";
        string newRelicAccountID;
        string newRelicUserApiKey;
        string newRelicMaliciousLogsNRQL;
        public override void ReferDependencies(ImADependencyProvider dependencyProvider)
        {
            base.ReferDependencies(dependencyProvider);

            RuntimeConfig runtimeConfig = dependencyProvider.GetRuntimeConfig();
            ConfigNode newRelicConfig = runtimeConfig?.Get("NewRelic");
            newRelicAccountID = newRelicConfig?.Get("AccountID")?.ToString();
            newRelicUserApiKey = newRelicConfig?.Get("UserApiKey")?.ToString();
            newRelicMaliciousLogsNRQL = newRelicConfig?.Get("NRQL")?.Get("MaliciousLogs")?.ToString();
        }

        public override async Task<OperationResult> Run()
        {
            Note[] args = (await GetArguments())?.Jump(1);

            if (args?.FirstOrDefault().ID.Is("malicious-ips") == true)
                return await AggregateAndExportLatestMaliciousIPs(args?.Jump(1));

            PrintUsageSyntax();

            return OperationResult.Win();
        }

        private async Task<OperationResult> AggregateAndExportLatestMaliciousIPs(Note[] args)
        {
            OperationResult result = OperationResult.Fail("Not yet started");

            await
                new Func<Task>(async () =>
                {
                    string outFolderPath = args?.Get("out");
                    if (outFolderPath.IsEmpty())
                    {
                        result = OperationResult.Fail($"Output folder wasn't specified via 'out' arg, ");
                        return;
                    }

                    DirectoryInfo folder = new DirectoryInfo(outFolderPath);
                    if (!folder.Exists)
                        folder.Create();

                    OperationResult<RawLogsQueryResponse> newRelicLogsResult = await FetchLatestMaliciousIPsLogsFromNewRelic();

                })
                .TryOrFailWithGrace(
                    onFail: ex => result = OperationResult.Fail(ex, $"Error occurred while trying to Aggregate And Export Latest Malicious IPs from NewRelic. Message: {ex.Message}")
                );

            return result;
        }

        private async Task<OperationResult<RawLogsQueryResponse>> FetchLatestMaliciousIPsLogsFromNewRelic()
        {
            OperationResult<RawLogsQueryResponse> result = OperationResult.Fail("Not yet started").WithoutPayload<RawLogsQueryResponse>();

            await
                new Func<Task>(async () =>
                {
                    if (newRelicAccountID.IsEmpty())
                    {
                        result = OperationResult.Fail("New Relic AccountID from config is empty").WithoutPayload<RawLogsQueryResponse>();
                        return;
                    }

                    if (newRelicUserApiKey.IsEmpty())
                    {
                        result = OperationResult.Fail("New Relic User API Key from config is empty").WithoutPayload<RawLogsQueryResponse>();
                        return;
                    }

                    if (newRelicMaliciousLogsNRQL.IsEmpty())
                    {
                        result = OperationResult.Fail("New Relic Malicious Logs NRQL from config is empty").WithoutPayload<RawLogsQueryResponse>();
                        return;
                    }

                    string requestBodyString = @$"{{
  actor {{
    account(id: {newRelicAccountID}) {{
      nrql(
        query: ""{newRelicMaliciousLogsNRQL}""
      ) {{
        results
      }}
    }}
  }}
}}
";

                    using (HttpClient http = BuildNewHttpClient())
                    using (StringContent requestCotent = new StringContent(requestBodyString, Encoding.UTF8, "application/json"))
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{newRelicApiBaseUrl}/graphql").And(x => {
                        x.Headers.Add("API-Key", newRelicUserApiKey);
                        x.Content = requestCotent; 
                    }))
                    using (HttpResponseMessage response = (await http.SendAsync(request, HttpCompletionOption.ResponseContentRead)))
                    {
                        response.EnsureSuccessStatusCode();
                        string responseJsonString = await response.Content.ReadAsStringAsync();
                        result = responseJsonString.TryJsonToObject<RawLogsQueryResponse>();
                    }

                })
                .TryOrFailWithGrace(
                    onFail: ex => result = OperationResult.Fail(ex, $"Error occurred while trying to Fetch Latest Malicious IPs Logs From NewRelic. Message: {ex.Message}").WithoutPayload<RawLogsQueryResponse>()
                );

            return result;
        }

        static SocketsHttpHandler BuildNewStandardSocketsHttpHandler()
        {
            return
                new SocketsHttpHandler()
                {
                    // The maximum idle time for a connection in the pool. When there is no request in
                    // the provided delay, the connection is released.
                    // Default value in .NET 6: 1 minute
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),

                    // This property defines maximal connection lifetime in the pool regardless
                    // of whether the connection is idle or active. The connection is reestablished
                    // periodically to reflect the DNS or other network changes.
                    // ⚠️ Default value in .NET 6: never
                    //    Set a timeout to reflect the DNS or other network changes
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                };
        }

        static HttpClient BuildNewHttpClient()
        {
            return
                new HttpClient
                (
                    handler: BuildNewStandardSocketsHttpHandler(),
                    disposeHandler: true
                );
        }




        #region Private Serialization Helping Classes
        [DataContract]
        class RawLogsQueryResponse
        {
            [DataMember(Name = "data")] public RawLogsQueryData Data { get; set; }
        }

        [DataContract]
        class RawLogsQueryData
        {
            [DataMember(Name = "actor")] public RawLogsQueryActor Actor { get; set; }
        }

        [DataContract]
        class RawLogsQueryActor
        {
            [DataMember(Name = "account")] public RawLogsQueryAccount Account { get; set; }
        }

        [DataContract]
        class RawLogsQueryAccount
        {
            [DataMember(Name = "nrql")] public RawLogsQueryNRQL NRQL { get; set; }
        }

        [DataContract]
        class RawLogsQueryNRQL
        {
            [DataMember(Name = "results")] public RawLogsQueryResult[] Results { get; set; }
        }

        [DataContract]
        class RawLogsQueryResult
        {
            [DataMember(Name = "newrelic.IP")] public string IPAddress { get; set; }
            [DataMember(Name = "timestamp")] public long UnixTimestamp { get; set; }
        }


        [DataContract]
        class RawDataFileEntry
        {
            [DataMember(Name = "newrelic.IP")] public string IPAddress { get; set; }
        }
        #endregion
    }
}

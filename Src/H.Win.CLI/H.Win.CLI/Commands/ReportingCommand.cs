using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using H.Necessaire.Serialization;
using H.Win.CLI.BLL;
using H.Win.CLI.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace H.Win.CLI.Commands
{
    [ID("report")]
    internal class ReportingCommand : CommandBase
    {
        static readonly string[] usageSyntaxes = new string[]
        {
            "report malicious-ips src=\"FolderWithNewRelicIPLogs\"",
            "report malicious-ips src=\"FolderWithNewRelicIPLogs\" out=\"malicious-ips.txt\"",
            "report ips-add-details src=\"malicious-ips.txt\" out=\"malicious-ips-with-details.csv\"",
            "report ips-add-details src=\"malicious-ips.txt\" out=\"malicious-ips-with-details.csv\" force-refresh",
        };
        protected override string[] GetUsageSyntaxes() => usageSyntaxes;

        IPDetailsProvider ipDetailsProvider;
        public override void ReferDependencies(ImADependencyProvider dependencyProvider)
        {
            base.ReferDependencies(dependencyProvider);
            ipDetailsProvider = dependencyProvider.Get<IPDetailsProvider>();
        }

        public override async Task<OperationResult> Run()
        {
            Note[] args = (await GetArguments())?.Jump(1);

            if (args?.FirstOrDefault().ID.Is("malicious-ips") == true)
                return await AggregateAndPrintMaliciousIPsFromNewRelicJsonExports(args?.Jump(1));

            if (args?.FirstOrDefault().ID.Is("ips-add-details") == true)
                return await DecorateIPsWithDetails(args?.Jump(1));

            PrintUsageSyntax();

            return OperationResult.Win();
        }

        private async Task<OperationResult> DecorateIPsWithDetails(Note[] args)
        {
            OperationResult result = OperationResult.Fail("Not yet started");

            await
                new Func<Task>(async () =>
                {
                    string inPath = args?.Get("src");
                    if (inPath.IsEmpty())
                    {
                        result = OperationResult.Fail("Input file path with IPs is required via the \"src\" arg");
                        return;
                    }
                    string outPath = args?.Get("out");
                    if (outPath.IsEmpty())
                    {
                        result = OperationResult.Fail("Output file path with detailed IPs is required via the \"out\" arg");
                        return;
                    }
                    FileInfo inFile = new FileInfo(inPath);
                    if (!inFile.Exists)
                    {
                        result = OperationResult.Fail($"Input file path with IPs doesn't exist: {inFile.FullName}");
                        return;
                    }
                    FileInfo outFile = new FileInfo(outPath);

                    string[] ips = File.ReadAllLines(inFile.FullName).Where(x => !x.IsEmpty()).ToArray();
                    List<DetailedIP> detailedIPs = new List<DetailedIP>();

                    bool isForcedRefresh = (args?.Any(x => x.ID == "force-refresh")) == true;
                    if (!isForcedRefresh && outFile.Exists)
                        detailedIPs.AddRange(ParseExistingDetailedIPs(outFile) ?? Enumerable.Empty<DetailedIP>());

                    string[] ipsToDetail = ips;
                    if (detailedIPs?.Any() == true)
                    {
                        string[] existingIPs = detailedIPs.Select(x => x.IP).ToArray();
                        ipsToDetail = ipsToDetail.Except(existingIPs).ToArray();
                    }

                    foreach (string ipToDetail in ipsToDetail)
                    {
                        DetailedIP detailedIP = await ipDetailsProvider.DetailIP(ipToDetail);
                        await Console.Out.WriteAsync(".");
                        if (detailedIP != null)
                            detailedIPs.Add(detailedIP);
                    }
                    await Console.Out.WriteLineAsync();

                    await File.WriteAllLinesAsync(
                        outFile.FullName,
                        DetailedIP.CsvHeader.AsArray().Concat(
                            detailedIPs.OrderBy(x => x.IP).Select(x => x.ToCsvLine())
                        )
                        .Where(x => !x.IsEmpty())
                        .ToArray()
                    );

                    result = OperationResult.Win();
                })
                .TryOrFailWithGrace(
                    onFail: ex =>
                    {
                        result = OperationResult.Fail(ex, $"Error occurred while trying to DecorateIPsWithDetails. Message: {ex.Message}");
                    }
                );

            return result;
        }
        private async Task<OperationResult> AggregateAndPrintMaliciousIPsFromNewRelicJsonExports(Note[] args)
        {
            OperationResult result = OperationResult.Fail("Not yet started");

            await
                new Func<Task>(async () =>
                {
                    DirectoryInfo folder = new DirectoryInfo(args?.Get("src"));
                    if (!folder.Exists)
                    {
                        result = OperationResult.Fail($"Source folder {folder.FullName} doesn't exist");
                        return;
                    }
                    FileInfo[] dataFiles = folder.GetFiles("*.json");
                    if (dataFiles?.Any() != true)
                    {
                        result = OperationResult.Fail($"Source folder {folder.FullName} doesn't contain any JSON files");
                        return;
                    }

                    FileParseResult[] parseResults = new FileParseResult[dataFiles.Length];
                    int index = -1;
                    foreach (FileInfo file in dataFiles)
                    {
                        index++;
                        await Console.Out.WriteAsync(".");
                        string rawJson = await file.OpenRead().ReadAsStringAsync(isStreamLeftOpen: false);
                        await Console.Out.WriteAsync(".");
                        OperationResult<RawDataFileEntry[]> parseResult = rawJson.TryJsonToObject<RawDataFileEntry[]>();
                        await Console.Out.WriteAsync(".");
                        parseResults[index]
                            = new FileParseResult
                            {
                                File = file,
                                ParseResult = parseResult,
                            };
                    }

                    FileParseResult[] parseFailures = parseResults.Where(x => !x.ParseResult.IsSuccessful).ToArrayNullIfEmpty();
                    if (parseFailures?.Any() == true)
                    {
                        string[] failedFiles = parseFailures.Select(x => x.File.Name).ToArray();
                        result = parseFailures.Select(x => x.ParseResult).Merge(globalReasonIfNecesarry: $"The following files couldn't be parsed: {string.Join(", ", failedFiles)}. Details below.");
                        return;
                    }

                    string[] ips
                        = parseResults
                        .SelectMany(x => x.ParseResult.Payload.Select(j => j.IPAddress))
                        .Where(x => !x.IsEmpty())
                        .Distinct()
                        .Order()
                        .ToArray()
                        ;

                    await Console.Out.WriteLineAsync();
                    await Console.Out.WriteLineAsync(string.Join(Environment.NewLine, ips));

                    string outPath = args?.Get("out");
                    if (outPath.IsEmpty())
                    {
                        result = OperationResult.Win();
                        return;
                    }

                    FileInfo outFile = new FileInfo(outPath);
                    await File.WriteAllLinesAsync(outFile.FullName, ips);

                    result = OperationResult.Win();
                })
                .TryOrFailWithGrace(
                    onFail: ex => result = OperationResult.Fail(ex, $"Error occurred while trying to AggregateAndPrintMaliciousIPsFromNewRelicJsonExports. Message: {ex.Message}")
                );

            return result;
        }


        private DetailedIP[] ParseExistingDetailedIPs(FileInfo detailedIPsFile)
        {
            string[] csvLines = File.ReadAllLines(detailedIPsFile.FullName).Jump(1).Where(x => !x.IsEmpty()).ToArray();
            DetailedIP[] result = csvLines.Select(ParseDetailedIPCsvLine).ToNoNullsArray();
            return result;
        }

        private DetailedIP ParseDetailedIPCsvLine(string csvLine)
        {
            if (csvLine.IsEmpty())
                return null;

            DetailedIP result = null;

            new Action(() =>
            {
                result = DetailedIP.FromCsvLine(csvLine);
            })
            .TryOrFailWithGrace(onFail: ex => result = null);

            return result;
        }




        class FileParseResult
        {
            public FileInfo File { get; set; }
            public OperationResult<RawDataFileEntry[]> ParseResult { get; set; }
        }

        [DataContract]
        class RawDataFileEntry
        {
            [DataMember(Name = "newrelic.IP")] public string IPAddress { get; set; }
        }
    }
}

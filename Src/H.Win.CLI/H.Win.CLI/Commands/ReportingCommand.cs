using H.Necessaire;
using H.Necessaire.Runtime.CLI.Commands;
using H.Necessaire.Serialization;
using H.Win.CLI.BLL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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
        };
        protected override string[] GetUsageSyntaxes() => usageSyntaxes;

        public override async Task<OperationResult> Run()
        {
            Note[] args = (await GetArguments())?.Jump(1);

            if (args?.FirstOrDefault().ID.Is("malicious-ips") == true)
                return await AggregateAndPrintMaliciousIPsFromNewRelicJsonExports(args?.Jump(1));

            PrintUsageSyntax();

            return OperationResult.Win();
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

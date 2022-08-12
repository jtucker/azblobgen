using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;

namespace azblobgen.Commands
{
    public class CreateBlobCommand : AsyncCommand<CreateBlobCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandOption("--file-size <FILESIZE>")]
            [Description("The size of the file to create in Megabytes")]
            [DefaultValue(100L)]
            public long FileSize { get; set; }

            [CommandOption("-n|--name")]
            [Description("Optional. Name of the blob to create. Default will generate a random file name.")]
            public string? Name { get; set; } = Path.GetRandomFileName();

#nullable disable
            [CommandOption("--blob-url")]
            public string BlobServiceUrl { get; set; }

            [CommandOption("--blob-container-name")]
            public string BlobContainerName { get; set; }
#nullable enable
        }

        public async override Task<int> ExecuteAsync(CommandContext context, Settings settings) =>
            await AnsiConsole
               .Status()
               .StartAsync($"Creating dummy blob in storage account: {settings.BlobServiceUrl}",
                async ctx =>
                {
                    AnsiConsole.MarkupLine($"[blue]Connecting to blob storage {settings.BlobServiceUrl}[/]");
                    var azureCredential = new DefaultAzureCredential();
                    var blobServiceClient = new BlobServiceClient(new Uri(settings.BlobServiceUrl!), azureCredential);
                    var blobContainerClient = blobServiceClient.GetBlobContainerClient(settings.BlobContainerName);

                    AnsiConsole.MarkupLine($"[blue]Creating blob container [italic]{settings.BlobContainerName}[/] if it doesn't exist[/]");
                    _ = await blobContainerClient.CreateIfNotExistsAsync();

                    var blobClient = blobContainerClient.GetAppendBlobClient(settings.Name);
                    _ = await blobClient.CreateIfNotExistsAsync();

                    ctx.Status("[green bold]Starting the upload process..[/]");
                    ctx.Spinner(Spinner.Known.Arrow3);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    for (long i = 0; i < settings.FileSize; i++)
                    {
                        using var memoryStream = new MemoryStream();
                        var randomBytes = new byte[1024 * 1024];
                        new Random().NextBytes(randomBytes);
                        await memoryStream.WriteAsync(randomBytes);
                        await memoryStream.FlushAsync();
                        memoryStream.Position = 0;
                        ctx.Status($"[yellow]{stopWatch.Elapsed}: Uploading block #{i + 1}[/]");
                        await blobClient.AppendBlockAsync(memoryStream);
                    }

                    stopWatch.Stop();

                    AnsiConsole.MarkupLine(
                        $"[green bold]Upload took {0:00}:{1:00}:{2:00}[/]",
                        stopWatch.Elapsed.Hours,
                        stopWatch.Elapsed.Minutes,
                        stopWatch.Elapsed.Seconds);
                    return 0;
                });

        public override ValidationResult Validate(CommandContext context, Settings settings)
            => string.IsNullOrEmpty(settings.BlobServiceUrl) ||
               !Uri.TryCreate(settings.BlobServiceUrl, UriKind.Absolute, out var _)
                ? ValidationResult.Error($"The Blob Service URL was not valid {settings.BlobServiceUrl}")
                : ValidationResult.Success();
    }
}

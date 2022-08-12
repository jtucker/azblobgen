using azblobgen.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<CreateBlobCommand>("blob")
        .WithDescription("Create a blob file in a storage container of a provided size.");
    config.SetExceptionHandler(ex =>
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.Default);
    });
});

return app.Run(args);
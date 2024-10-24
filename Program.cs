using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Console = Spectre.Console.AnsiConsole;
using System.Net;
using System.Net.Quic;
using System.Diagnostics;
using Newtonsoft.Json;
using nl2sql.Services;

namespace nl2sql
{
    internal class Program
    {
        static OpenAIService openAIService = null;

        static async Task Main(string[] args)
        {
            Console.WriteLine("");

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true);

            var config = configuration.Build();

            const string search = "1.\tAsk AI Assistant";
            const string exit = "2.\tExit this Application";

            while (true)
            {
                var selectedOption = AnsiConsole.Prompt(
                      new SelectionPrompt<string>()
                          .Title("Select an option to continue")
                          .PageSize(10)
                          .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                          .AddChoices(new[] {
                            search, exit
                          }));

                
                switch (selectedOption)
                {
                    case search:
                        PerformSearch(config);
                        break;
                    case exit:
                        return;
                }
            }
        }

        private static OpenAIService initOpenAIService(IConfiguration config)
        {
            string endpoint = config["OpenAIEndpoint"];
            string key = config["OpenAIKey"];
            string embeddingDeployment = config["OpenAIEmbeddingDeployment"];
            string completionsDeployment = config["OpenAIcompletionsDeployment"];
            string maxToken = config["OpenAIMaxToken"];

            return new OpenAIService(endpoint, key, embeddingDeployment, completionsDeployment, maxToken);
        }

        private static void PerformSearch(IConfiguration config)
        {
            Dictionary<string, float[]> dictEmbeddings = new Dictionary<string, float[]>();

            string chatCompletion = string.Empty;

            string userQuery = Console.Prompt(
                new TextPrompt<string>("Type your question and hit enter when ready.")
                    .PromptStyle("teal")
            );


            AnsiConsole.Status()
               .Start("Processing...", ctx =>
               {
                   ctx.Spinner(Spinner.Known.Star);
                   ctx.SpinnerStyle(Style.Parse("green"));

                   if (openAIService == null)
                   {
                       ctx.Status("Connecting to Open AI Service..");
                       openAIService = initOpenAIService(config);
                   }


                   ctx.Status($"Processing user prompt to generate Completion using OpenAI Service..");

                   (string completion, int promptTokens, int completionTokens) = openAIService.GetChatCompletionAsync(userQuery).GetAwaiter().GetResult();
                   chatCompletion = completion;


               });

            Console.WriteLine("");
            Console.Write(new Rule($"[silver]AI Assistant Response[/]") { Justification = Justify.Center });
            AnsiConsole.MarkupLine(chatCompletion);
            Console.WriteLine("");
            Console.WriteLine("");
            Console.Write(new Rule($"[yellow]****[/]") { Justification = Justify.Center });
            Console.WriteLine("");
        }
    }
}



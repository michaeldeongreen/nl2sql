using Azure.AI.OpenAI;
using Azure.Core;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nl2sql.Services
{
    /// <summary>
    /// Service to access Azure OpenAI.
    /// </summary>
    public class OpenAIService
    {

        private readonly string _openAIEmbeddingDeployment = string.Empty;
        private readonly string _openAICompletionDeployment = string.Empty;
        private readonly int _openAIMaxTokens = default;

        private readonly OpenAIClient? _openAIClient;

        //System prompts to send with user prompts to instruct the model for chat session
        private readonly string _systemPromptRecipeAssistant = @"
        You are an intelligent for Contoso. 
        You are designed to convert English natural language to Transact-SQL.
        The query will run on a database who schema is represented in this string:
        Don't use joins for this schema and if all columns are required give the (*) notation.

        CREATE TABLE customer (
          customer_id INT PRIMARY KEY,
          first_name NVARCHAR(50),
          last_name NVARCHAR(50),
          email NVARCHAR(100),
          phone NVARCHAR(20),
          address NVARCHAR(255),
          city NVARCHAR(50),
          state NVARCHAR(50),
          zip_code NVARCHAR(10),
          created_at DATETIME DEFAULT GETDATE()
        );

        Instructions:
        - If you're unsure of an answer, you can say ""I don't know"" or ""I'm not sure"" and recommend users search themselves.        
        - Your response  should be complete. 
        - Format the content so that it can be printed to the Command Line ";

        public OpenAIService(string endpoint, string key, string embeddingsDeployment, string CompletionDeployment, string maxTokens)
        {

            _openAIEmbeddingDeployment = embeddingsDeployment;
            _openAICompletionDeployment = CompletionDeployment;
            _openAIMaxTokens = int.TryParse(maxTokens, out _openAIMaxTokens) ? _openAIMaxTokens : 8191;


            OpenAIClientOptions clientOptions = new OpenAIClientOptions()
            {
                Retry =
            {
                Delay = TimeSpan.FromSeconds(2),
                MaxRetries = 10,
                Mode = RetryMode.Exponential
            }
            };

            try
            {

                //Use this as endpoint in configuration to use non-Azure Open AI endpoint and OpenAI model names
                if (endpoint.Contains("api.openai.com"))
                    _openAIClient = new OpenAIClient(key, clientOptions);
                else
                    _openAIClient = new(new Uri(endpoint), new AzureKeyCredential(key), clientOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenAIService Constructor failure: {ex.Message}");
            }
        }

        public async Task<float[]?> GetEmbeddingsAsync(dynamic data)
        {
            try
            {
                EmbeddingsOptions embeddingsOptions = new()
                {
                    DeploymentName = _openAIEmbeddingDeployment,
                    Input = { data },
                };
                var response = await _openAIClient.GetEmbeddingsAsync(embeddingsOptions);

                Embeddings embeddings = response.Value;

                float[] embedding = embeddings.Data[0].Embedding.ToArray();

                return embedding;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetEmbeddingsAsync Exception: {ex.Message}");
                return null;
            }
        }

        public async Task<(string response, int promptTokens, int responseTokens)> GetChatCompletionAsync(string userPrompt)
        {

            try
            {

                var systemMessage = new ChatRequestSystemMessage(_systemPromptRecipeAssistant);
                var userMessage = new ChatRequestUserMessage(userPrompt);


                ChatCompletionsOptions options = new()
                {
                    DeploymentName = _openAICompletionDeployment,
                    Messages =
                {
                    systemMessage,
                    userMessage
                },
                    MaxTokens = _openAIMaxTokens,
                    Temperature = 0.5f, //0.3f,
                    NucleusSamplingFactor = 0.95f,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                Azure.Response<ChatCompletions> completionsResponse = await _openAIClient.GetChatCompletionsAsync(options);

                ChatCompletions completions = completionsResponse.Value;

                return (
                    response: completions.Choices[0].Message.Content,
                    promptTokens: completions.Usage.PromptTokens,
                    responseTokens: completions.Usage.CompletionTokens
                );

            }
            catch (Exception ex)
            {

                string message = $"OpenAIService.GetChatCompletionAsync(): {ex.Message}";
                Console.WriteLine(message);
                throw;

            }
        }

    }
}

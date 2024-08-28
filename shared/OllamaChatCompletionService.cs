using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Net.Http.Headers;
using System.Text.Json;
using sk_customllm.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using Azure;
using Codeblaze.SemanticKernel.Connectors.Ollama.ChatCompletion;
using System.IO;

namespace CopilotChat.Shared
{
    public class OllamaChatCompletionService : IChatCompletionService
    {
        private string _modelId;
        private string _baseUrl;
        private HttpClient _http;
        private ILoggerFactory _loggerFactory;

        public OllamaChatCompletionService(string modelId, string baseUrl, HttpClient http, ILoggerFactory loggerFactory)
        {
            this._modelId = modelId;
            this._baseUrl = baseUrl;
            this._http = http;
            this._loggerFactory = loggerFactory;
        }

        public IReadOnlyDictionary<string, object?> Attributes => throw new NotImplementedException();

        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"{this._baseUrl}/v1/chat/completions"))
                {

                    // iterate though chatHistory and generate a json document based on the Root class
                    var root = new ChatRequest();
                    for (int i = 0; i < chatHistory.Count; i++)
                    {
                        var message = chatHistory[i];
                        var msg = new ChatMessage();
                        msg.role = message.Role.ToString().ToLower();
                        msg.content = message.Content;
                        root.messages.Add(msg);
                    }

                    // validate if ModelName is not empty and add it to the root object
                    if (!string.IsNullOrEmpty(this._modelId))
                    {
                        root.model = this._modelId;
                    }

                    // generate the json string from the root object
                    var jsonString = JsonSerializer.Serialize(root);
                    request.Content = new StringContent(jsonString);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    var httpResponse = await httpClient.SendAsync(request);

                    // get the response content
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();

                    // deserialize the response content into a ChatResponse object
                    var chatResponse = JsonSerializer.Deserialize<ChatResponse>(responseContent);

                    // add httpResponse content to chatHistory
                    chatHistory.AddAssistantMessage(chatResponse.choices[0].message.content);
                }
            }

            return chatHistory;
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            using (var httpClient = new HttpClient())
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"{this._baseUrl}/v1/chat/completions"))
                {

                    // iterate though chatHistory and generate a json document based on the Root class
                    var root = new ChatRequest();
                    for (int i = 0; i < chatHistory.Count; i++)
                    {
                        var message = chatHistory[i];
                        var msg = new ChatMessage();
                        msg.role = message.Role.ToString().ToLower();
                        msg.content = message.Content;
                        root.messages.Add(msg);
                    }

                    // validate if ModelName is not empty and add it to the root object
                    if (!string.IsNullOrEmpty(this._modelId))
                    {
                        root.model = this._modelId;
                    }

                    // generate the json string from the root object
                    var jsonString = JsonSerializer.Serialize(root);
                    request.Content = new StringContent(jsonString);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                    var httpResponse = await httpClient.SendAsync(request);

                    // get the response content
                    //var responseContent = await httpResponse.Content.ReadAsStringAsync();




                    using var stream = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    using var reader = new StreamReader(stream);

                    var done = false;

                    while (!done)
                    {
                        string jsonResponse = await reader.ReadLineAsync();

                        // deserialize the response content into a ChatResponse object
                        var chatResponse = JsonSerializer.Deserialize<ChatResponse>(jsonResponse);

                        // add httpResponse content to chatHistory
                        chatHistory.AddAssistantMessage(chatResponse.choices[0].message.content);
                        done = true;

                        yield return new StreamingChatMessageContent(AuthorRole.Assistant, chatResponse.choices[0].message.content);
                    }
                }
            }
        }

    }
}

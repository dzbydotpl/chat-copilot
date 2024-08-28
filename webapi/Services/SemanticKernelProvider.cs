// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using Codeblaze.SemanticKernel.Connectors.Ollama;
using CopilotChat.Shared;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Extension methods for registering Semantic Kernel related services.
/// </summary>
public sealed class SemanticKernelProvider
{
    private readonly Kernel _kernel;

    public SemanticKernelProvider(IServiceProvider serviceProvider, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        this._kernel = InitializeCompletionKernel(serviceProvider, configuration, httpClientFactory);
    }

    /// <summary>
    /// Produce semantic-kernel with only completion services for chat.
    /// </summary>
    public Kernel GetCompletionKernel() => this._kernel.Clone();

    private static Kernel InitializeCompletionKernel(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        var builder = Kernel.CreateBuilder();

        builder.Services.AddLogging();

        var memoryOptions = serviceProvider.GetRequiredService<IOptions<KernelMemoryConfig>>().Value;

        switch (memoryOptions.TextGeneratorType)
        {
            case string x when x.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase):
            case string y when y.Equals("AzureOpenAIText", StringComparison.OrdinalIgnoreCase):
                var azureAIOptions = memoryOptions.GetServiceConfig<AzureOpenAIConfig>(configuration, "AzureOpenAIText");
#pragma warning disable CA2000 // No need to dispose of HttpClient instances from IHttpClientFactory
                builder.AddAzureOpenAIChatCompletion(
                    azureAIOptions.Deployment,
                    azureAIOptions.Endpoint,
                    azureAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
                break;

            case string x when x.Equals("OpenAI", StringComparison.OrdinalIgnoreCase):
                var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");
                builder.AddOpenAIChatCompletion(
                    openAIOptions.TextModel,
                    openAIOptions.APIKey,
                    httpClient: httpClientFactory.CreateClient());
#pragma warning restore CA2000
                break;

            default:
                //var openAIOptions = memoryOptions.GetServiceConfig<OpenAIConfig>(configuration, "OpenAI");

                Func<IServiceProvider, object?, OllamaChatCompletionService> factory = (serviceProvider, _) =>
            new(modelId: "llama3.1:8b",
                baseUrl: "http://localhost:11434",
                http: httpClientFactory.CreateClient(),
                loggerFactory: serviceProvider.GetService<ILoggerFactory>());

                builder.Services.AddKeyedSingleton<IChatCompletionService>(null, factory);
                //builder.Services.AddKeyedSingleton<ITextGenerationService>(null, (Func<IServiceProvider, object, ITextGenerationService>)factory);
                break;
        }

        return builder.Build();
    }
}

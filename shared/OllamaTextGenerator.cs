// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Azure.Identity;
using Codeblaze.SemanticKernel.Connectors.Ollama;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI.AzureOpenAI.Internals;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel;

namespace Microsoft.KernelMemory.AI.AzureOpenAI;

[Experimental("KMEXP01")]
public sealed class OllamaTextGenerator : ITextGenerator
{
    private readonly ITextTokenizer _textTokenizer;
    private readonly OllamaTextGenerationService _client;
    private readonly ILogger<OllamaTextGenerator> _log;
    private readonly bool _useTextCompletionProtocol;
    private readonly string _deployment;

    public OllamaTextGenerator(
        ITextTokenizer? textTokenizer = null,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
    {
        this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<OllamaTextGenerator>();

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4Tokenizer));
            textTokenizer = new GPT4Tokenizer();
        }

        this._textTokenizer = textTokenizer;

        OpenAIClientOptions options = new()
        {
            RetryPolicy = new RetryPolicy(maxRetries: Math.Max(0, 3)),
            Diagnostics =
            {
                IsTelemetryEnabled = Telemetry.IsTelemetryEnabled,
                ApplicationId = Telemetry.HttpUserAgent,
            }
        };

        if (httpClient is not null)
        {
            options.Transport = new HttpClientTransport(httpClient);
        }

        this._client = new OllamaTextGenerationService("llama", "http://localhost:11435", httpClient, loggerFactory);
    }

    /// <inheritdoc/>
    public int MaxTokenTotal { get; }

    /// <inheritdoc/>
    public int CountTokens(string text)
    {
        return this._textTokenizer.CountTokens(text);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetTokens(string text)
    {
        return this._textTokenizer.GetTokens(text);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        //if (this._useTextCompletionProtocol)
        {
            this._log.LogTrace("Sending text generation request, deployment '{0}'", this._deployment);

            var openaiOptions = new CompletionsOptions
            {
                DeploymentName = this._deployment,
                MaxTokens = options.MaxTokens,
                Temperature = (float)options.Temperature,
                NucleusSamplingFactor = (float)options.NucleusSampling,
                FrequencyPenalty = (float)options.FrequencyPenalty,
                PresencePenalty = (float)options.PresencePenalty,
                ChoicesPerPrompt = 1,
            };

            if (options.StopSequences is { Count: > 0 })
            {
                foreach (var s in options.StopSequences) { openaiOptions.StopSequences.Add(s); }
            }

            if (options.TokenSelectionBiases is { Count: > 0 })
            {
                foreach (var (token, bias) in options.TokenSelectionBiases) { openaiOptions.TokenSelectionBiases.Add(token, (int)bias); }
            }

            var response = await this._client.GetTextContentsAsync(prompt, cancellationToken: cancellationToken);
            foreach (TextContent? completions in response)
            {
                {
                    yield return completions.Text;
                }
            }
        }
        //else
        //{
        //    this._log.LogTrace("Sending chat message generation request, deployment '{0}'", this._deployment);

        //    var openaiOptions = new ChatCompletionsOptions
        //    {
        //        DeploymentName = this._deployment,
        //        MaxTokens = options.MaxTokens,
        //        Temperature = (float)options.Temperature,
        //        NucleusSamplingFactor = (float)options.NucleusSampling,
        //        FrequencyPenalty = (float)options.FrequencyPenalty,
        //        PresencePenalty = (float)options.PresencePenalty,
        //        // ChoiceCount = 1,
        //    };

        //    if (options.StopSequences is { Count: > 0 })
        //    {
        //        foreach (var s in options.StopSequences) { openaiOptions.StopSequences.Add(s); }
        //    }

        //    if (options.TokenSelectionBiases is { Count: > 0 })
        //    {
        //        foreach (var (token, bias) in options.TokenSelectionBiases) { openaiOptions.TokenSelectionBiases.Add(token, (int)bias); }
        //    }

        //    openaiOptions.Messages.Add(new ChatRequestSystemMessage(prompt));

        //    var response = await this._client.GetStreamingTextContentsAsync(prompt, cancellationToken: cancellationToken);
        //    foreach (var update in response.EnumerateValues().WithCancellation(cancellationToken).ConfigureAwait(false))
        //    {
        //        yield return update.ContentUpdate;
        //    }
        //}
    }
}

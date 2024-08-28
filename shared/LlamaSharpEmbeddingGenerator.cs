// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Codeblaze.SemanticKernel.Connectors.Ollama;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.AzureOpenAI;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

namespace CopilotChat.Shared;
public class LlamaSharpEmbeddingGenerator : ITextEmbeddingGenerator, ITextEmbeddingBatchGenerator
{
#pragma warning disable KMEXP00 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly ITextTokenizer _textTokenizer;
#pragma warning restore KMEXP00 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly OllamaTextEmbeddingGeneration _client;
    private readonly ILogger<LlamaSharpEmbeddingGenerator> _log;
    private readonly string _deployment;

    public LlamaSharpEmbeddingGenerator(
#pragma warning disable KMEXP00 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ITextTokenizer? textTokenizer = null,
#pragma warning restore KMEXP00 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null)
    {
        //this._log = (loggerFactory ?? DefaultLogger.Factory).CreateLogger<LlamaSharpEmbeddingGenerator>();

        if (textTokenizer == null)
        {
            this._log.LogWarning(
                "Tokenizer not specified, will use {0}. The token count might be incorrect, causing unexpected errors",
                nameof(GPT4Tokenizer));
            textTokenizer = new GPT4Tokenizer();
        }

        //this._textTokenizer = textTokenizer;
        //this._deployment = config.Deployment;

        //this.MaxTokens = config.MaxTokenTotal;
        //this.MaxBatchSize = config.MaxEmbeddingBatchSize;
       


        this._client = new OllamaTextEmbeddingGeneration("gemma2:latest", "http://localhost:11434", new HttpClient(), loggerFactory);
    }

    /// <inheritdoc/>
    public int MaxTokens { get; } = 4096;

    /// <inheritdoc/>
    public int MaxBatchSize { get; }

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
    public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        //this._log.LogTrace("Generating embedding, deployment '{0}'", this._deployment);
        return this._client.GenerateEmbeddingAsync(text, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Embedding[]> GenerateEmbeddingBatchAsync(IEnumerable<string> textList, CancellationToken cancellationToken = default)
    {
        var list = textList.ToList();
        //this._log.LogTrace("Generating embeddings, deployment '{0}', batch size '{1}'", this._deployment, list.Count);
        IList<ReadOnlyMemory<float>> embeddings = await this._client.GenerateEmbeddingsAsync(list, cancellationToken: cancellationToken).ConfigureAwait(false);
        return embeddings.Select(e => new Embedding(e)).ToArray();
    }
}

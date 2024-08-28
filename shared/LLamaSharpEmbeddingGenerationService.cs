using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace Codeblaze.SemanticKernel.Connectors.Ollama;

#pragma warning disable SKEXP0001
public class OllamaTextEmbeddingGeneration(string modelId, string baseUrl, HttpClient http, ILoggerFactory? loggerFactory)
    : OllamaBase<OllamaTextEmbeddingGeneration>(modelId, baseUrl, http, loggerFactory),
        ITextEmbeddingGenerationService
#pragma warning restore SKEXP0001
{
    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, Kernel? kernel = null,
        CancellationToken cancellationToken = new())
    {
        var result = new List<ReadOnlyMemory<float>>(data.Count);

        foreach (var text in data)
        {
            var request = new
            {
                model = Attributes["model_id"],
                prompt = text
            };

            var response = await Http.PostAsJsonAsync($"{Attributes["base_url"]}/v1/embeddings", request, cancellationToken).ConfigureAwait(false);

            ValidateOllamaResponse(response);

            var json = JsonSerializer.Deserialize<JsonNode>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

            var embedding = new ReadOnlyMemory<float>(json!["embedding"]?.AsArray().GetValues<float>().ToArray());

            result.Add(embedding);
        }

        return result;
    }
}

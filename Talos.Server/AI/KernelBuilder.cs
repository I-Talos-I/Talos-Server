using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Talos.Server.AI;

public static class KernelBuilder
{
    public static Kernel Create(IConfiguration config)
    {
        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();

        builder.AddOpenAIChatCompletion(
            modelId: config["AI:Model"]!,
            apiKey: config["AI:ApiKey"]!
        );

        return builder.Build();
    }
}
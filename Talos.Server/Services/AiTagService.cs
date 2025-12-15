using Microsoft.SemanticKernel;
using Talos.Server.AI;

namespace Talos.Server.Services;

public class AiTagService
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _function;

    public AiTagService(IConfiguration config)
    {
        _kernel = KernelBuilder.Create(config);

        var prompt = File.ReadAllText("AI/Prompts/GenerateTags.txt");

        _function = _kernel.CreateFunctionFromPrompt(
            prompt,
            functionName: "GenerateTags"
        );
    }

    public async Task<List<string>> GenerateTagsAsync(string content)
    {
        try
        {
            var result = await _kernel.InvokeAsync(
                _function,
                new KernelArguments
                {
                    ["input"] = content
                }
            );

            return result
                .GetValue<string>()!
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
        }
        catch (Exception ex)
        {
            // Log simple (puedes usar ILogger si quieres)
            Console.WriteLine($"[AI TAG ERROR] {ex.Message}");

            // Fallback seguro
            return new List<string> { "general" };
        }
    }
}
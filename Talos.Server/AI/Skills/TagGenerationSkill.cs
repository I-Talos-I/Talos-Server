using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Talos.Server.AI.Skills;

public class TagGenerationSkill
{
    [KernelFunction]
    [Description("Generate tags for a post")]
    public string GenerateTags(
        [Description("Post content")] string content
    )
    {
        return content;
    }
}
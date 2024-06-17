﻿namespace Ollama;

/// <summary>
/// This example shows how you can use Streaming with Kernel.
/// </summary>
/// <param name="output"></param>
public class Ollama_Connectors_KernelStreaming(ITestOutputHelper output) : BaseTest(output)
{
    [Fact]
    public async Task RunAsync()
    {
        Kernel kernel = Kernel.CreateBuilder()
            .AddOllamaChatCompletion(
                model: TestConfiguration.Ollama.ModelId,
                endpoint: TestConfiguration.Ollama.Endpoint,
                serviceId: "OllamaChat")
            .Build();

        var funnyParagraphFunction = kernel.CreateFunctionFromPrompt("Write a funny paragraph about streaming", new OllamaPromptExecutionSettings()
        {
            MaxTokens = 100,
            Temperature = 0.4,
            TopP = 1
        });

        var roleDisplayed = false;

        Console.WriteLine("\n===  Prompt Function - Streaming ===\n");

        string fullContent = string.Empty;

        // Streaming can be of any type depending on the underlying service the function is using.
        await foreach (var update in kernel.InvokeStreamingAsync<StreamingChatMessageContent>(funnyParagraphFunction))
        {
            // You will be always able to know the type of the update by checking the Type property.
            if (!roleDisplayed && update.Role.HasValue)
            {
                Console.WriteLine($"Role: {update.Role}");
                fullContent += $"Role: {update.Role}\n";
                roleDisplayed = true;
            }

            if (update.Content is { Length: > 0 })
            {
                fullContent += update.Content;
                Console.Write(update.Content);
            }
        }

        Console.WriteLine("\n------  Streamed Content ------\n");
        Console.WriteLine(fullContent);
    }
}

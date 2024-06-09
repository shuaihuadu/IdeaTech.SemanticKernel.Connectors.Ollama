using System.Text.Json;

namespace SemanticKernel.Connectors.UnitTests.Ollama;

public sealed class OllamaTextGenerationTests : IDisposable
{
    private readonly HttpMessageHandlerStub _messageHandlerStub;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;

    public OllamaTextGenerationTests()
    {
        this._messageHandlerStub = new HttpMessageHandlerStub();
        this._messageHandlerStub.ResponseToReturn.Content = new StringContent(OllamaTestHelper.GetTestResponse("text_generation_test_response.json"));

        this._httpClient = new HttpClient(this._messageHandlerStub, false)
        {
            BaseAddress = TestConstants.FakeUri
        };
        this._mockLoggerFactory = new Mock<ILoggerFactory>();
    }

    #region Constructors

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConstructorWithUriStringWorksCorrectly(bool includeLoggerFactory)
    {
        OllamaTextGenerationService ollamaTextGenerationService = includeLoggerFactory
            ? new OllamaTextGenerationService(TestConstants.FakeModel, TestConstants.FakeUriString, loggerFactory: this._mockLoggerFactory.Object)
            : new OllamaTextGenerationService(TestConstants.FakeModel, TestConstants.FakeUriString);

        Assert.NotNull(ollamaTextGenerationService);
        Assert.Equal(TestConstants.FakeModel, ollamaTextGenerationService.Attributes["ModelId"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConstructorWithUriWorksCorrectly(bool includeLoggerFactory)
    {
        OllamaTextGenerationService ollamaTextGenerationService = includeLoggerFactory
            ? new OllamaTextGenerationService(TestConstants.FakeModel, TestConstants.FakeUri, loggerFactory: this._mockLoggerFactory.Object)
            : new OllamaTextGenerationService(TestConstants.FakeModel, TestConstants.FakeUri);

        Assert.NotNull(ollamaTextGenerationService);
        Assert.Equal(TestConstants.FakeModel, ollamaTextGenerationService.Attributes["ModelId"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConstructorWithHttpClientWorksCorrectly(bool includeLoggerFactory)
    {
        OllamaTextGenerationService ollamaTextGenerationService = includeLoggerFactory
            ? new OllamaTextGenerationService(TestConstants.FakeModel, TestConstants.FakeHttpClient, loggerFactory: this._mockLoggerFactory.Object)
            : new OllamaTextGenerationService(TestConstants.FakeModel, TestConstants.FakeHttpClient);

        Assert.NotNull(ollamaTextGenerationService);
        Assert.Equal(TestConstants.FakeModel, ollamaTextGenerationService.Attributes["ModelId"]);
    }

    #endregion

    [Fact]
    public async Task GetTextContentsWorksCorrectlyAsync()
    {
        OllamaTextGenerationService ollamaTextGenerationService = new(TestConstants.FakeModel, this._httpClient);

        IReadOnlyList<TextContent> textContents = await ollamaTextGenerationService.GetTextContentsAsync("Prompt");

        Assert.True(textContents.Count > 0);
        Assert.Equal("This is a test generation response", textContents[0].Text);
    }

    [Fact]
    public async Task GetTextContentsHandlesSettingCorrectlyAsync()
    {
        OllamaTextGenerationService ollamaTextGenerationService = new(TestConstants.FakeModel, this._httpClient);

        OllamaPromptExecutionSettings executionSettings = new()
        {
            MaxTokens = 100,
            Temperature = 0.5,
            TopP = 0.2,
            TopK = 100,
            FrequencyPenalty = 1.2,
            PresencePenalty = 1.4,
            Seed = 110,
            KeepAlive = 500,
            SystemPrompt = "You are an AI Assistant",
            Stop = ["stop_sequence"],
            Format = "json"
        };

        this._messageHandlerStub.ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(OllamaTestHelper.GetTestResponse("text_generation_test_response.json"))
        };

        IReadOnlyList<TextContent> textContents = await ollamaTextGenerationService.GetTextContentsAsync("Prompt", executionSettings);

        byte[]? requestContent = this._messageHandlerStub.RequestContent;

        Assert.NotNull(requestContent);

        JsonElement content = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(requestContent));

        Assert.Equal(TestConstants.FakeModel, content.GetProperty("model").GetString());
        Assert.Equal("Prompt", content.GetProperty("prompt").GetString());
        Assert.Equal("You are an AI Assistant", content.GetProperty("system").GetString());
        Assert.Equal(500, content.GetProperty("keep_alive").GetInt32());
        Assert.Equal("json", content.GetProperty("format").GetString());

        Assert.Equal(100, content.GetProperty("options").GetProperty("num_ctx").GetInt32());
        Assert.Equal(0.5, content.GetProperty("options").GetProperty("temperature").GetDouble());
        Assert.Equal(0.2, content.GetProperty("options").GetProperty("top_p").GetDouble());
        Assert.Equal(100, content.GetProperty("options").GetProperty("top_k").GetInt32());
        Assert.Equal(1.4, content.GetProperty("options").GetProperty("presence_penalty").GetDouble());
        Assert.Equal(1.2, content.GetProperty("options").GetProperty("frequency_penalty").GetDouble());
        Assert.Equal(110, content.GetProperty("options").GetProperty("seed").GetInt32());
        Assert.Equal("stop_sequence", content.GetProperty("options").GetProperty("stop")[0].GetString());
    }


    [Fact]
    public async Task ShouldHandleMetadataAsync()
    {
        OllamaTextGenerationService ollamaTextGenerationService = new(TestConstants.FakeModel, this._httpClient);

        IReadOnlyList<TextContent> textContents = await ollamaTextGenerationService.GetTextContentsAsync("Prompt");

        Assert.NotNull(textContents);
        Assert.NotEmpty(textContents);

        TextContent content = textContents.SingleOrDefault()!;

        Assert.NotNull(content);
        Assert.Equal("llama3", content.ModelId);
        Assert.IsType<OllamaTextGenerationMetadata>(content.Metadata);

        OllamaTextGenerationMetadata? metadata = content.Metadata as OllamaTextGenerationMetadata;

        Assert.Equal("This is a test generation response", content.Text);
        Assert.True(metadata!.Context!.Length > 0);
        Assert.Equal(4285976012, metadata.TotalDuration);
        Assert.Equal(819378, metadata.LoadDuration);
        Assert.Equal(10, metadata.PromptEvalCount);
        Assert.Equal(200559000, metadata.PromptEvalDuration);
        Assert.Equal(26, metadata.EvalCount);
        Assert.Equal(4042076000, metadata.EvalDuration);
        Assert.Equal("stop", metadata.DoneReason);

        Assert.True(metadata.Done);

        DateTimeOffset.TryParse("2024-06-09T02:24:37.6058572+00:00", out DateTimeOffset date);
        Assert.True(metadata.CreatedAt == date);
    }

    [Fact]
    public async Task GetStreamingTextContentsWorksCorrectlyAsync()
    {
        OllamaTextGenerationService ollamaTextGenerationService = new(TestConstants.FakeModel, this._httpClient);

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(OllamaTestHelper.GetTestResponse("text_generation_test_stream_response.txt")));

        this._messageHandlerStub.ResponseToReturn = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        };

        StringBuilder contentBuilder = new();

        await foreach (var chunk in ollamaTextGenerationService.GetStreamingTextContentsAsync("Prompt"))
        {
            contentBuilder.Append(chunk.Text);

            Assert.Equal("llama3", chunk.ModelId);
            Assert.IsType<OllamaTextGenerationMetadata>(chunk.Metadata);

            OllamaTextGenerationMetadata? metadata = chunk.Metadata as OllamaTextGenerationMetadata;

            if (metadata!.Done.HasValue && metadata.Done.Value)
            {
                Assert.Equal(string.Empty, chunk.Text);
                Assert.True(metadata!.Context!.Length > 0);
                Assert.Equal(6078554632, metadata.TotalDuration);
                Assert.Equal(1124087488, metadata.LoadDuration);
                Assert.Equal(11, metadata.PromptEvalCount);
                Assert.Equal(480050000, metadata.PromptEvalDuration);
                Assert.Equal(27, metadata.EvalCount);
                Assert.Equal(4431666000, metadata.EvalDuration);
                Assert.Equal("stop", metadata.DoneReason);

                DateTimeOffset.TryParse("2024-06-09T06:56:37.8054647+00:00", out DateTimeOffset date);
                Assert.True(metadata.CreatedAt == date);
            }
        }

        Assert.Equal("Hello there! It's nice to meet you. Is there something I can help you with, or would you like to chat?", contentBuilder.ToString());
    }

    public void Dispose()
    {
        this._messageHandlerStub.Dispose();
        this._httpClient.Dispose();
    }
}
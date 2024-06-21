﻿// Copyright (c) IdeaTech. All rights reserved.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public abstract class BaseTest
{
    /// <summary>
    /// Flag to force usage of OpenAI configuration if both <see cref="TestConfiguration.OpenAI"/>
    /// and <see cref="TestConfiguration.AzureOpenAI"/> are defined.
    /// If 'false', Azure takes precedence.
    /// </summary>
    protected virtual bool ForceOpenAI { get; } = false;

    protected ITestOutputHelper Output { get; }

    protected ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// This property makes the samples Console friendly. Allowing them to be copied and pasted into a Console app, with minimal changes.
    /// </summary>
    public BaseTest Console => this;

    protected Kernel CreateKernelWithChatCompletion()
    {
        var builder = Kernel.CreateBuilder();

        builder.AddOllamaChatCompletion(TestConfiguration.Ollama.ModelId, TestConfiguration.Ollama.Endpoint);

        return builder.Build();
    }

    protected BaseTest(ITestOutputHelper output)
    {
        this.Output = output;
        this.LoggerFactory = new XunitLogger(output);

        IConfigurationRoot configRoot = new ConfigurationBuilder()
            .AddJsonFile(@"D:\appsettings\test_configuration.json", true)
            .Build();

        TestConfiguration.Initialize(configRoot);
    }

    /// <summary>
    /// This method can be substituted by Console.WriteLine when used in Console apps.
    /// </summary>
    /// <param name="target">Target object to write</param>
    public void WriteLine(object? target = null)
        => this.Output.WriteLine(target ?? string.Empty);

    /// <summary>
    /// This method can be substituted by Console.WriteLine when used in Console apps.
    /// </summary>
    /// <param name="format">Format string</param>
    /// <param name="args">Arguments</param>
    public void WriteLine(string? format, params object?[] args)
        => this.Output.WriteLine(format ?? string.Empty, args);

    /// <summary>
    /// This method can be substituted by Console.WriteLine when used in Console apps.
    /// </summary>
    public void WriteLine(string? message)
        => this.Output.WriteLine(message);

    /// <summary>
    /// Current interface ITestOutputHelper does not have a Write method. This extension method adds it to make it analogous to Console.Write when used in Console apps.
    /// </summary>
    /// <param name="target">Target object to write</param>
    public void Write(object? target = null)
    {
        this.Output.WriteLine(target ?? string.Empty);
    }

    protected sealed class LoggingHandler(HttpMessageHandler innerHandler, ITestOutputHelper output) : DelegatingHandler(innerHandler)
    {
        private readonly ITestOutputHelper _output = output;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Log the request details
            if (request.Content is not null)
            {
                var content = await request.Content.ReadAsStringAsync(cancellationToken);
                this._output.WriteLine(content);
            }

            // Call the next handler in the pipeline
            var response = await base.SendAsync(request, cancellationToken);

            if (response.Content is not null)
            {
                // Log the response details
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                this._output.WriteLine(responseContent);
            }

            // Log the response details
            this._output.WriteLine("");

            return response;
        }
    }
}

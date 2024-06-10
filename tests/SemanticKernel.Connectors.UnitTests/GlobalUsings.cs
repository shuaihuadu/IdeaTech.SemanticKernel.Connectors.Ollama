﻿global using IdeaTech.SemanticKernel.Connectors.Ollama;
global using Microsoft.Extensions.Logging;
global using Microsoft.SemanticKernel;
global using Microsoft.SemanticKernel.ChatCompletion;
global using Microsoft.SemanticKernel.Embeddings;
global using Microsoft.SemanticKernel.TextGeneration;
global using Moq;
global using Ollama.Core.Models;
global using SemanticKernel.Connectors.UnitTests.Models;
global using System;
global using System.Collections;
global using System.Diagnostics.CodeAnalysis;
global using System.Net;
global using System.Text;
global using System.Text.Json;
global using Xunit;
global using OllamaHttpOperationException = Ollama.Core.HttpOperationException;
global using TestConstants = SemanticKernel.Connectors.UnitTests.OllamaTestHelper.Constants;

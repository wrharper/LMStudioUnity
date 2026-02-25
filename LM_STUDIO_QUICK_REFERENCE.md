# LM Studio Quick Reference

## Installation (5 minutes)

1. Download LM Studio from [lmstudio.ai](https://lmstudio.ai)
2. Install and launch
3. Search for and download a model (e.g., "neural-chat")
4. Go to Developer > Click "Start Server"
5. Default: `http://localhost:1234`

## Code Setup

### Basic Configuration

```csharp
// In your LLMClient setup
llmClient.remote = true;
llmClient.host = "localhost";
llmClient.port = 1234;
```

### Test Connection

```csharp
LLMUnity.LMStudioSetup.TestLMStudioConnection();
```

## Common Tasks

### Generate Text

```csharp
var response = await llmClient.Completion("Your prompt here");
Debug.Log(response);
```

### Stream Response

```csharp
await llmClient.Completion(
    "Your prompt",
    callback: (chunk) => Debug.Log(chunk)
);
```

### Get Embeddings

```csharp
var embedding = await llmClient.Embeddings("Text to embed");
```

### Tokenize Text

```csharp
var tokens = await llmClient.Tokenize("Your text");
```

## Parameters

| Parameter | Default | Min | Max | Purpose |
|-----------|---------|-----|-----|---------|
| `temperature` | 0.2 | 0.0 | 2.0 | Higher = more creative |
| `topP` | 0.9 | 0.0 | 1.0 | Nucleus sampling |
| `topK` | 40 | 1 | 100 | Top-K sampling |
| `numPredict` | -1 | -1 | N/A | Max tokens to generate |
| `seed` | 0 | 0 | MAX | Reproducibility |

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Server is not alive" | Start server in LM Studio Developer tab |
| No response | Check model is loaded in LM Studio |
| Slow responses | Enable GPU in LM Studio settings |
| Connection refused | Check firewall and port 1234 |
| Out of memory | Use smaller/quantized model |

## API Endpoints (Direct Usage)

```bash
# Check if server is running
curl http://localhost:1234/v1/models

# Get text completion
curl http://localhost:1234/v1/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "local-model",
    "prompt": "Hello world",
    "max_tokens": 100
  }'

# Get embeddings
curl http://localhost:1234/v1/embeddings \
  -H "Content-Type: application/json" \
  -d '{
    "model": "text-embedding-model",
    "input": "Your text here"
  }'
```

## What's Different from Local LLM

| Feature | Local LLM | LM Studio |
|---------|-----------|-----------|
| Model Loading | Programmatic | GUI one-click |
| Server Management | Built-in | Separate app |
| GPU Config | Code parameters | GUI settings |
| Grammar Support | Yes | No |
| LoRA Adapters | Yes (API) | Manual only |
| Latency | Minimal | Network overhead |
| Setup Complexity | Complex | Simple |
| Multi-model Switching | Programmatic | One-click |

## Models Recommended

- **General Chat:** neural-chat, mistral, llama2
- **Code:** codellama, deepseek-coder
- **Embeddings:** nomic-embed-text, bge-small
- **Creative:** openhermes, dolphin-mixtral

## Additional Resources

- [LM Studio Documentation](https://lmstudio.ai/docs)
- [Full Setup Guide](LM_STUDIO_SETUP.md)
- [Migration Guide](MIGRATION_GUIDE.md)
- [Example: Chat Bot Sample](Samples~/LMStudioChatBot/README.md)

## Useful Commands

```csharp
// Get server info
LLMUnity.LMStudioSetup.GetServerInfo("localhost", 1234);

// Configure multiple clients
var client1 = GetComponent<LLMClient>();
var client2 = GetComponent<LLMClient>();
LLMUnity.LMStudioSetup.ConfigureForLMStudio(client1);
LLMUnity.LMStudioSetup.ConfigureForLMStudio(client2);
```

## Local Network Access

To access from another PC:

```csharp
client.host = "192.168.1.100";  // Your PC's IP
client.port = 1234;
```

Note: Must configure LM Studio to listen on all interfaces.

---

**Quick Start:** [LM Studio Setup Guide](LM_STUDIO_SETUP.md)

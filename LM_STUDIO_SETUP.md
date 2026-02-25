# LLMUnity with LM Studio Setup Guide

## Overview

This conversion allows LLMUnity to use **LM Studio** as its backend instead of the local Llama implementation. LM Studio is a standalone application that provides an OpenAI-compatible API for running LLMs locally.

## Key Changes

1. **Removed local server management** - LM Studio runs as a separate application
2. **Replaced native C++ calls** - Now uses HTTP API calls instead
3. **Maintained API compatibility** - Existing code continues to work with minimal changes

## Prerequisites

1. **LM Studio** - Download from [lmstudio.ai](https://lmstudio.ai)
2. **Downloaded Model** - In LM Studio, select and download your desired model
3. **LM Studio Server Running** - LM Studio must be running with the server listening

## Setup Instructions

### Step 1: Install and Configure LM Studio

1. Download and install LM Studio from [lmstudio.ai](https://lmstudio.ai)
2. Launch LM Studio
3. In the left sidebar, click "Developer" (code icon)
4. Click "Start Server" to start the API server
5. By default, it runs on `http://localhost:1234`

### Step 2: Update Your LLMClient Configuration

Replace local LLM configuration with remote LM Studio configuration:

```csharp
// Old way (with local LLM):
public LLM llm;  // This is no longer needed

// New way (with LM Studio):
public LLMClient client;

void Start()
{
    // Configure to use remote LM Studio
    client.remote = true;
    client.host = "localhost";
    client.port = 1234;  // Default LM Studio port
    // client.APIKey = "";  // Leave empty unless LM Studio is configured with auth
}
```

### Step 3: Update Inspector Settings

For any **LLMClient** component in your scene:

1. Check **"Remote"** toggle in the inspector
2. Set **Host** to `localhost` (or your server IP)
3. Set **Port** to `1234` (or your configured LM Studio port)
4. Leave **API Key** empty unless you've configured authentication in LM Studio

### Step 4: Migrate Your Existing Code

If you had local LLM setup code:

```csharp
// Before (Local LLM):
[SerializeField] private LLM llm;
[SerializeField] private LLMClient llmClient;

private void Start()
{
    // Local LLM handled by LLM component
}

// After (LM Studio):
[SerializeField] private LLMClient llmClient;

private void Start()
{
    // Configure for remote LM Studio
    llmClient.remote = true;
    llmClient.host = "localhost";
    llmClient.port = 1234;
}
```

## Usage Examples

### Basic Completion

```csharp
public class ChatBot : MonoBehaviour
{
    private LLMClient llmClient;

    public async void GenerateResponse(string prompt)
    {
        string response = await llmClient.Completion(prompt);
        Debug.Log("Response: " + response);
    }
}
```

### Streaming Completion

```csharp
public async void StreamResponse(string prompt)
{
    await llmClient.Completion(
        prompt,
        callback: (chunk) => Debug.Log("Chunk: " + chunk),
        completionCallback: () => Debug.Log("Complete")
    );
}
```

### Getting Embeddings

Embeddings require a specific embedding model loaded in LM Studio:

```csharp
public async void GetTextEmbedding(string text)
{
    // Note: This requires an embedding model to be loaded in LM Studio
    var embeddings = await llmClient.Embeddings(text);
    Debug.Log("Embedding dimensions: " + embeddings.Count);
}
```

### Tokenization

```csharp
public async void TokenizeText(string text)
{
    // Note: This is an estimation since LM Studio doesn't expose tokenizer
    var tokens = await llmClient.Tokenize(text);
    Debug.Log("Estimated token count: " + tokens.Count);
}
```

## API Compatibility Notes

### Fully Supported Parameters

- `temperature` - Controls randomness
- `top_k` - Limits to k most likely tokens
- `top_p` - Nucleus sampling threshold
- `min_p` - Minimum probability threshold
- `max_tokens` / `n_predict` - Maximum generation length
- `seed` - For reproducibility
- `presence_penalty` - Penalizes repeated tokens
- `frequency_penalty` - Penalizes token frequency
- `mirostat` - Mirostat sampling mode (0, 1, or 2)
- `mirostat_tau` - Mirostat target entropy
- `mirostat_eta` - Mirostat learning rate

### Limitations

⚠️ **Note: The following features are not available when using LM Studio:**

1. **Grammar Constraints** - LM Studio doesn't expose grammar validation via API
2. ✅ **LORA Adapters** - **Now supported!** See [LM_STUDIO_LORA_GUIDE.md](./LM_STUDIO_LORA_GUIDE.md) for automatic LORA merging
3. **Manual Tokenization** - Returns estimates only
4. **Detokenization** - Not available via API
5. **Slot Management** - Not applicable to LM Studio's architecture
6. **Request Cancellation** - Would require closing HTTP connection

### LORA Adapter Support

LLMUnity now includes **automatic LORA merging** for seamless LORA adapter support with LM Studio:

- **Automatic merging** of LORA adapters into base models
- **Intelligent caching** - merged models are reused, not recreated
- **Multi-adapter support** - combine multiple LORA adapters with custom weights
- **Transparent integration** - LORA configuration works the same as with local backend

**See [LM_STUDIO_LORA_GUIDE.md](./LM_STUDIO_LORA_GUIDE.md) for complete LORA setup and usage instructions.**

## Network Configuration

### Local Network Access

To access LM Studio from another machine on your network:

```csharp
client.host = "192.168.1.100"; // Your machine's IP
client.port = 1234;
```

### Remote Access

To exposeLM Studio to the internet (⚠️ **Not recommended without authentication**):

1. In LM Studio settings, configure binding to `0.0.0.0`
2. Use your public IP in the client
3. Consider using a reverse proxy/firewall for security

## Troubleshooting

### "Server is not alive" Error

**Solution:**
1. Ensure LM Studio is running
2. Verify the server is started (click "Start Server" in Developer tab)
3. Check host and port match your LM Studio configuration

### Model Not Loading

**Solution:**
1. In LM Studio, select the model in the main interface
2. It will load automatically
3. The API will use the loaded model

### Slow Responses

**Reasons:**
- GPU acceleration not enabled in LM Studio
- Model too large for your hardware
- Network latency if using remote server

**Solutions:**
1. In LM Studio settings, enable GPU acceleration
2. Use a smaller quantized model
3. Check network connection

### Streaming Not Working

**Solution:**
- Ensure you're passing a callback function
- Check that LM Studio is accessible from your application
- Verify no firewall is blocking the connection

## Performance Optimization

### For Better Performance

1. **GPU Acceleration** - Enable in LM Studio settings
2. **Quantization** - Use Q4 or Q5 quantized models if available
3. **Context Size** - Keep reasonable for your hardware
4. **Batch Processing** - Use consistent batch sizes

### For Better Quality

1. **Use Larger Models** - 7B+ models for better quality
2. **Lower Temperature** - Use 0.1-0.5 for consistency
3. **Longer Context** - Increase context size for more context
4. **Adjust Top-P** - Use 0.9-0.95 for natural responses

## Migration Checklist

- [ ] LM Studio installed and running
- [ ] Model selected and loaded in LM Studio
- [ ] Server started (Developer > Start Server)
- [ ] LLMClient inspector settings updated (`remote=true`)
- [ ] Host and Port configured correctly
- [ ] Test connection with a simple prompt
- [ ] Remove any remaining local LLM component references
- [ ] Update documentation/comments in your code
- [ ] Test all LLM functionality in your application

## Additional Resources

- **LM Studio Documentation** - https://lmstudio.ai/docs
- **OpenAI API Documentation** - https://platform.openai.com/docs/api-reference
- **LLMUnity Repository** - Check for additional examples

## Support

If you encounter issues:

1. Check that LM Studio server is running
2. Verify network connectivity
3. Check Unity console for detailed error messages
4. Review LM Studio logs in the Developer tab
5. Ensure you're using a compatible model

## Reverting to Local LLM

If you need to revert to local LLM mode:

1. Re-add an `LLM` component to your scene
2. Configure the model path and parameters
3. Set `LLMClient.remote = false`
4. Assign the LLM component to the LLMClient

---

**Last Updated:** 2026-02-24

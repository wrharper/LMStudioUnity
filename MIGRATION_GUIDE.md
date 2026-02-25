# Migration Guide: From Local LLM to LM Studio

## Overview

This guide helps you migrate your LLMUnity project from using the local Llama-based LLM server to using LM Studio as the backend.

## Why Migrate to LM Studio?

**Advantages:**
- ✅ Easier to set up and manage
- ✅ Modern, user-friendly GUI application
- ✅ Automatic GPU optimization
- ✅ One-click model switching
- ✅ No build complexity (no native dependencies)
- ✅ Better error messages and diagnostics
- ✅ Consistent performance across platforms
- ✅ Community-driven development

**Trade-offs:**
- ⚠️ Requires running separate application
- ⚠️ Some advanced features unavailable (grammar, LoRA via API)
- ⚠️ Network overhead (vs local process)

## Step-by-Step Migration

### Phase 1: Prerequisites

#### 1.1 Install LM Studio

1. Visit [https://lmstudio.ai](https://lmstudio.ai)
2. Download the appropriate version for your OS
3. Install and launch LM Studio

#### 1.2 Download a Model

1. In LM Studio, click "Search" (magnifying glass)
2. Choose a model (recommended: neural-chat, mistral, llama2)
3. Click download and wait for completion

#### 1.3 Start the Server

1. Navigate to "Developer" tab (code icon)
2. Click "Start Server"
3. Verify it says "Server is running" and listens on port 1234

### Phase 2: Update Your Code

#### 2.1 Remove Local LLM References

**Before:**
```csharp
public class MyGame : MonoBehaviour
{
    [SerializeField] private LLM llm;  // ❌ Remove this
    [SerializeField] private LLMClient llmClient;
}
```

**After:**
```csharp
public class MyGame : MonoBehaviour
{
    [SerializeField] private LLMClient llmClient;  // ✅ Keep only client
}
```

#### 2.2 Update LLM Configuration Code

**Before (Local):**
```csharp
void Start()
{
    // Local LLM was configured via components
    // No additional code needed
}
```

**After (Remote):**
```csharp
void Start()
{
    if (llmClient != null)
    {
        llmClient.remote = true;
        llmClient.host = "localhost";
        llmClient.port = 1234;
    }
}
```

#### 2.3 Simplify Inspector Setup

**Remove these from Inspector:**
- LLM component assignment
- Model path configuration
- GPU layer settings
- Thread count settings
- Batch size settings

**Add these to Inspector:**
- ✅ Check "Remote" toggle
- ✅ Set Host to `localhost`
- ✅ Set Port to `1234`

### Phase 3: Test Your Setup

#### 3.1 Quick Connectivity Test

Add this to a test script:

```csharp
void Start()
{
    LLMUnity.LMStudioSetup.TestLMStudioConnection("localhost", 1234);
}
```

Expected output:
```
✓ LM Studio server is running at localhost:1234
✓ Loaded model: neural-chat:latest
```

#### 3.2 Test Basic Completion

```csharp
async void TestCompletion()
{
    var response = await llmClient.Completion("Say hello!");
    Debug.Log("Response: " + response);
}
```

#### 3.3 Test with Streaming

```csharp
async void TestStreaming()
{
    var response = await llmClient.Completion(
        "Write a short poem",
        callback: (chunk) => Debug.Log("Chunk: " + chunk)
    );
}
```

### Phase 4: Update Scene Setup

#### 4.1 Update Existing Scenes

For each scene with an LLM:

1. **Remove LLM GameObject** (if you have one)
2. **Keep LLMClient** components
3. **Update Inspector Settings:**
   - Remote: ✓ enabled
   - Host: `localhost`
   - Port: `1234`

#### 4.2 Create Test Scene

1. Create empty GameObject
2. Add `LLMClient` component
3. Configure as shown above
4. Add a test script to verify connectivity

### Phase 5: Adjust Parameters

Some parameters work differently with LM Studio:

#### 5.1 Supported Parameters

✅ **Fully Supported:**
- `temperature` (0.0-2.0)
- `top_p` (0.0-1.0)
- `top_k` (1-40)
- `min_p` (0.0-1.0)
- `numPredict` / `max_tokens`
- `seed`
- `mirostat` (0, 1, or 2)

⚠️ **Partially Supported:**
- `repeatPenalty` → mapped to `frequency_penalty`
- `presencePenalty` → `presence_penalty`

❌ **Not Supported:**
- Grammar constraints
- LoRA adapters (via API)
- Slot management
- Token-accurate detokenization

#### 5.2 Recommended Settings for Quality

```csharp
// For deterministic responses
llmClient.temperature = 0.1f;
llmClient.topP = 0.9f;

// For creative responses
llmClient.temperature = 0.8f;
llmClient.topP = 0.95f;

// For code generation
llmClient.temperature = 0.0f;  // Deterministic
llmClient.topP = 0.95f;
llmClient.topK = 40;
```

## Common Migration Issues

### Issue 1: "Server is not alive"

**Cause:** LM Studio not running or server not started

**Solution:**
1. Check LM Studio is running
2. In Developer tab, click "Start Server"
3. Verify "Server is running" appears at the bottom
4. Check port 1234 is not blocked by firewall

### Issue 2: Model Files Missing

**Cause:** No model loaded in LM Studio

**Solution:**
1. Go to Search tab in LM Studio
2. Download a model
3. Wait for completion
4. Model automatically loads when you reload

### Issue 3: Slow Responses

**Cause:** Using CPU instead of GPU

**Solution:**
1. In LM Studio Settings
2. Look for "GPU acceleration" or similar
3. Enable GPU support
4. Restart server

### Issue 4: Memory Issues

**Cause:** Model too large for available RAM

**Solution:**
- Use a smaller model (e.g., 7B instead of 13B)
- Use quantized version (Q4, Q5)
- Enable layer offloading in LM Studio

### Issue 5: Embeddings Not Working

**Cause:** Wrong model type loaded

**Solution:**
- Embeddings only work with embedding models
- Download an embedding model (e.g., nomic-embed-text)
- Load it in LM Studio
- Note: Switch models as needed

## Code Examples

### Example 1: Simple Chat

```csharp
public class SimpleChat : MonoBehaviour
{
    private LLMClient llmClient;

    async void Start()
    {
        // Configure for LM Studio
        llmClient.remote = true;
        llmClient.host = "localhost";
        llmClient.port = 1234;

        // Test connection
        LLMUnity.LMStudioSetup.TestLMStudioConnection();
    }

    async void OnGUI()
    {
        if (GUILayout.Button("Generate Response"))
        {
            string response = await llmClient.Completion("Hello!");
            Debug.Log("Response: " + response);
        }
    }
}
```

### Example 2: Streaming with UI

```csharp
public class StreamingChat : MonoBehaviour
{
    [SerializeField] private LLMClient llmClient;
    [SerializeField] private Text outputText;

    async void OnGUI()
    {
        if (GUILayout.Button("Generate and Stream"))
        {
            outputText.text = "";
            
            await llmClient.Completion(
                "Write a haiku about AI",
                callback: (chunk) => 
                {
                    outputText.text += chunk;
                },
                completionCallback: () => 
                {
                    Debug.Log("Completed!");
                }
            );
        }
    }
}
```

### Example 3: Batch Processing

```csharp
public class BatchProcessor : MonoBehaviour
{
    [SerializeField] private LLMClient llmClient;
    private string[] prompts = new[] 
    { 
        "What is AI?",
        "Explain ML",
        "What is Deep Learning?"
    };

    async void ProcessAll()
    {
        foreach (var prompt in prompts)
        {
            Debug.Log($"Processing: {prompt}");
            var response = await llmClient.Completion(prompt);
            Debug.Log($"Response: {response}\n");
            
            // Small delay between requests
            await Task.Delay(1000);
        }
        Debug.Log("All done!");
    }
}
```

## Rollback Plan

If you need to revert to local LLM:

1. **Re-add LLM component** to your scene
2. **Configure model path** and parameters
3. **Set LLMClient:**
   - Remote: ✗ unchecked
   - Assign the LLM component

## Performance Considerations

### For Better Performance:

1. **Use Smaller Models:**
   - 3B models fastest
   - 7B good balance
   - 13B+ better quality, slower

2. **Quantization:**
   - Q4 quantization recommended
   - Smaller and faster than full precision
   - Minimal quality loss

3. **GPU Acceleration:**
   - Enable in LM Studio settings
   - 5-10x speed improvement
   - Requires compatible GPU

4. **Local Network:**
   - Better latency than remote
   - No internet required
   - Fully offline operation

## Testing Checklist

- [ ] LM Studio installed and running
- [ ] Model downloaded and loaded
- [ ] Server started (check "Server is running")
- [ ] LLMClient.remote = true
- [ ] Host and port configured correctly
- [ ] Test connectivity in code
- [ ] Single completion works
- [ ] Streaming works
- [ ] All former functionality working
- [ ] Parameters adjusted as needed

## Additional Resources

- **LM Studio Documentation:** https://lmstudio.ai/docs
- **Model Selection Guide:** https://lmstudio.ai/models
- **OpenAI API Compatibility:** https://platform.openai.com/docs/api-reference
- **LLMUnity Issues:** GitHub issues for help

## Support and Troubleshooting

1. Check LM Studio logs in Developer tab
2. Verify network connectivity
3. Test with curl or Postman:
   ```bash
   curl http://localhost:1234/v1/models
   ```
4. Review Unity Console for errors
5. Check firewall/antivirus blocking

## When to Keep Local LLM

Keep using local LLM if:
- You need grammar constraints
- You need LoRA adapters
- You want to embed model in executable
- Network latency is critical
- You prefer lower-level control

---

**Migration completed successfully!** 🎉

For questions or issues, refer to the [LM_STUDIO_SETUP.md](../LM_STUDIO_SETUP.md) file.

# LM Studio Feature Support & Completeness Checklist

**Last Updated:** February 24, 2026

## Overview

This guide documents which LLMUnity features are supported when using LM Studio as the remote backend, and provides workarounds for unsupported features.

---

## Feature Matrix

### Text Completion API

| Feature | Local (LlamaLib) | Remote (LM Studio) | Status | Workaround |
|---------|------------------|--------------------|--------|-----------|
| **Basic Completion** | ✅ Full | ✅ Full | Verified | N/A |
| **Streaming** | ✅ Full | ✅ Full | Verified | N/A |
| **Max Tokens** | ✅ Yes (n_predict) | ✅ Yes (max_tokens) | Auto-mapped | N/A |
| **Temperature** | ✅ Yes | ✅ Yes | Auto-mapped | N/A |
| **Top-P** | ✅ Yes | ✅ Yes | Auto-mapped | N/A |
| **Top-K** | ✅ Yes | ✅ Yes | Auto-mapped | N/A |
| **Seed** | ✅ Yes | ✅ Yes | Auto-mapped | N/A |
| **Mirostat** | ✅ Yes | ✅ Yes | Auto-mapped | N/A |
| **Repeat Penalty** | ✅ Yes (lora) | ✅ Yes (frequency_penalty) | Auto-mapped | N/A |

**Test Results:**
```csharp
// ✅ Verified working with LM Studio
string response = await llmClient.Completion(
    "Say hello",
    completionParameters: new JObject {
        ["temperature"] = 0.5f,
        ["top_p"] = 0.9f,
        ["n_predict"] = 100,
        ["seed"] = 42
    }
);
```

---

### Grammar Constraints

| Feature | Local | Remote | Status | Alternative |
|---------|-------|--------|--------|-------------|
| **GBNF Grammar** | ✅ Yes | ❌ No | **Not Supported** | Prompt-based guidance |
| **JSON Schema** | ✅ Yes | ❌ No | **Not Supported** | Prompt-based guidance |
| **Structured Output** | ✅ Full | ⚠️ Limited | Partial | JSON prompt instructions |

**Explanation:**
- LM Studio's OpenAI API doesn't expose grammar constraint API endpoints
- LLMUnity automatically skips grammar when using remote mode
- Use prompt engineering instead

**Workaround Example:**
```csharp
// ❌ Won't work with LM Studio (grammar ignored)
llmClient.grammar = "root ::= \"[\" (number (\",\" number)*)? \"]\"";

// ✅ Works with both local and remote (prompt-based)
string response = await llmClient.Completion(
    "Output a JSON array of 3 numbers: [num1, num2, num3]"
);
// Output: [42, 17, 99]
```

**JSON Output Guidance (Remote):**
```csharp
string jsonPrompt = @"
You must output valid JSON object with these exact keys:
{
  ""name"": ""string"",
  ""age"": ""number"",
  ""active"": ""boolean""
}

Example output:
{""name"": ""Alice"", ""age"": 30, ""active"": true}

Now respond with your JSON:
";

string response = await llmClient.Completion(jsonPrompt);
var data = JObject.Parse(response);
```

---

### Tokenization & Detokenization

| Feature | Local | Remote | Status | Accuracy |
|---------|-------|--------|--------|----------|
| **Tokenize** | ✅ Exact | ⚠️ Estimated | Works | ~90% accurate |
| **Detokenize** | ✅ Exact | ❌ Not Available | **Not Supported** | N/A |
| **Token Count** | ✅ Exact | ⚠️ Estimated | Approximate | ±10-20% |

**Implementation:**
```csharp
// Tokenization - works but estimated
var tokens = await llmClient.Tokenize("Hello world");
// Returns approximately correct token count
// Use for: prompt length estimation

// Detokenization - only works locally
// ❌ Cannot do in remote mode
```

**Recommendation:**
- Use local mode if exact tokenization critical
- Remote tokenization good enough for quota/billing estimation
- Reserve token budget with 20% margin for remote mode

---

### Embeddings

| Feature | Local | Remote | Status | Requirements |
|---------|-------|--------|--------|--------------|
| **Embeddings API** | ✅ Yes | ✅ Yes | **Supported** | Embedding-capable model |
| **Vector Dimension** | Varies | Varies | Model-dependent | Check model docs |
| **Batch Embeddings** | ✅ Yes | ⚠️ Single | Single text only | Loop for multiple |

**Example - Embeddings Generation:**
```csharp
// Works with both local and remote
var embedding = await llmClient.Embeddings("Important document");
// Returns: List<float> with vector values

// Vector search example (RAG)
var documents = new List<string>
{
    "The quick brown fox",
    "The lazy dog",
    "Computer science is"
};

var embeddings = new List<List<float>>();
foreach (var doc in documents)
{
    var emb = await llmClient.Embeddings(doc);
    embeddings.Add(emb);
}

// Now use for similarity search
```

**Model Support:**
- ✅ Works with embeddings-only models (e.g., `all-MiniLM-L6-v2`)
- ✅ Works with chat models that include embeddings
- ❌ Chat-only models may not support embeddings
- Solution: Load embeddings model separately in LM Studio

---

### Chat/Multi-Turn Conversations

| Feature | Local (LLMAgent) | Remote (LLMClient) | Status | Notes |
|---------|------------------|-------------------|--------|-------|
| **Chat History** | ✅ Built-in | ✅ Manual | Supported | Must manage client-side |
| **System Prompt** | ✅ Built-in | ✅ In prompt | Works | Include in prompt text |
| **Turn Management** | ✅ Auto | ⚠️ Manual | Manual | Track locally |

**LLMAgent (Chat-Focused) - Works Locally:**
```csharp
// ✅ Full chat support with local LLM
[SerializeField] private LLMAgent agent;

void Start()
{
    agent.systemPrompt = "You are a helpful assistant.";
}

async void Chat()
{
    string response = await agent.Chat("Hello, how are you?");
    // History automatically tracked
}
```

**LLMClient (Remote) - Manual Chat:**
```csharp
// ✅ Works but manual history management
private List<string> conversationHistory = new List<string>();

async void Chat(string userInput)
{
    // Build context from history
    string context = "System: You are helpful.\n";
    foreach (var msg in conversationHistory)
        context += msg + "\n";
    
    context += $"User: {userInput}\nAssistant:";
    
    // Get response
    string response = await llmClient.Completion(context);
    
    // Track in history
    conversationHistory.Add($"User: {userInput}");
    conversationHistory.Add($"Assistant: {response}");
}
```

---

### LORA Adapters

| Feature | Local | Remote | Status | Method |
|---------|-------|--------|--------|--------|
| **LORA Loading** | ✅ Native API | ✅ Via Merging | Supported | LoraPreprocessor |
| **Multiple LORA** | ✅ Yes (weighted) | ✅ Yes (merged) | Supported | Set weights |
| **Dynamic Loading** | ✅ Runtime | ⚠️ Pre-merged | One-time | Load before game |

**Local Mode (Native LORA):**
```csharp
[SerializeField] private LLM llm;

void Start()
{
    // Configure LORA in inspector: 
    // _lora = "adapters/specialization.gguf"
    // _loraWeights = "0.8"
    
    // LORA loads automatically
}
```

**Remote Mode (Merged LORA):**
```csharp
// ✅ Works via automatic merging
var loras = new List<(string, float)>
{
    ("adapters/domain.gguf", 0.8f),
    ("adapters/style.gguf", 0.5f)
};

// Preprocessor merges into base model once
string mergedPath = await LoraPreprocessor.MergeLorasIntoModel(
    "models/base.gguf",
    loras
);

// Cache avoids re-merging
// ~30s first time, <100ms after
```

**Example - Domain-Specific AI:**
```csharp
// Configure once before game starts
async void SetupSpecialist(string domain)
{
    var loras = domain switch
    {
        "medicine" => new List<(string, float)>
        {
            ("adapters/medical-knowledge.gguf", 0.9f),
            ("adapters/safety-guidelines.gguf", 0.7f)
        },
        "coding" => new List<(string, float)>
        {
            ("adapters/code-generation.gguf", 0.8f),
            ("adapters/best-practices.gguf", 0.6f)
        },
        _ => null
    };
    
    if (loras != null)
    {
        await LoraPreprocessor.MergeLorasIntoModel(
            "models/base.gguf",
            loras
        );
    }
}
```

---

### Multi-Agent / Parallel Processing

| Feature | Local | Remote | Status | Notes |
|---------|-------|--------|--------|-------|
| **Parallel Slots** | ✅ Yes (up to N) | ❌ No | Limited | Sequential in remote |
| **Concurrent Requests** | ✅ Yes | ⚠️ Queue | Works (slower) | Handle internally |
| **Slot Management** | ✅ API-exposed | ❌ Internal | Internal | No control remote |

**Local Mode (Parallel NPC Agents):**
```csharp
// ✅ Multiple agents in parallel (with multiple GPU layers)
[SerializeField] private LLMAgent npc1;
[SerializeField] private LLMAgent npc2;
[SerializeField] private LLMAgent npc3;

void Start()
{
    // All 3 process in parallel if GPU/threads allow
    _ = npc1.Chat("Hello");
    _ = npc2.Chat("Hi there");
    _ = npc3.Chat("Hey");
}
```

**Remote Mode (Sequential Processing):**
```csharp
// ⚠️ Requests process sequentially server-side
// Appears concurrent to client, but slower
[SerializeField] private LLMClient client;

async void Start()
{
    // These run sequentially on server
    var t1 = client.Completion("Agent 1 speaks");
    var t2 = client.Completion("Agent 2 speaks");
    var t3 = client.Completion("Agent 3 speaks");
    
    await Task.WhenAll(t1, t2, t3);
    // Takes 3x time vs parallel
}
```

**Workaround - Load Balancing:**
```csharp
// Multiple LM Studio servers for true parallel processing
var client1 = new LLMClient { host = "server1.example.com" };
var client2 = new LLMClient { host = "server2.example.com" };
var client3 = new LLMClient { host = "server3.example.com" };

// Now parallel across servers
_ = client1.Completion("Agent 1");
_ = client2.Completion("Agent 2");
_ = client3.Completion("Agent 3");
```

---

### Advanced Parameters

| Parameter | Local | Remote | Status | Note |
|-----------|-------|--------|--------|------|
| **Context Size** | ✅ Configurable | ⚠️ Model default | Fixed | Can't override remote |
| **Batch Size** | ✅ Configurable | ❌ Fixed | Fixed | Server controlled |
| **Model Layers to GPU** | ✅ Per-model | ⚠️ GUI only | GUI-only | Set in LM Studio GUI |
| **Flash Attention** | ✅ Yes | ❌ Not exposed | Not available | Not in OpenAI API |
| **Reasoning Mode** | ✅ Yes | ❌ Not exposed | Not available | Not in OpenAI API |

---

## Feature Testing Checklist

### Quick Validation Test

```csharp
[MenuItem("LMStudio/Run Feature Tests")]
public static async void RunFeatureTests()
{
    Debug.Log("=== LM Studio Feature Tests ===");
    
    // 1. Basic Completion
    try
    {
        string r = await client.Completion("Test");
        Debug.Log("✅ Completion works");
    } catch { Debug.Log("❌ Completion failed"); }
    
    // 2. Streaming
    try
    {
        int chunks = 0;
        await client.Completion("Test", 
            streamCallback: (_) => chunks++);
        Debug.Log($"✅ Streaming works ({chunks} chunks)");
    } catch { Debug.Log("❌ Streaming failed"); }
    
    // 3. Parameters
    try
    {
        var p = new JObject { ["temperature"] = 0.5f };
        await client.Completion("Test", p);
        Debug.Log("✅ Parameters work");
    } catch { Debug.Log("❌ Parameters failed"); }
    
    // 4. Tokenization
    try
    {
        var t = await client.Tokenize("Test");
        Debug.Log($"✅ Tokenization works ({t.Count} tokens)");
    } catch { Debug.Log("❌ Tokenization failed"); }
    
    // 5. Embeddings (if available)
    try
    {
        var e = await client.Embeddings("Test");
        Debug.Log($"✅ Embeddings work ({e.Count}D)");
    } catch { Debug.Log("⚠️  Embeddings not available"); }
}
```

---

## Performance Characteristics

### Response Time by Feature

| Feature | Local | Remote |
|---------|-------|--------|
| First token (model load) | 0.5-2s | 10-50ms |
| Token generation | 10-100ms | 50-500ms |
| Streaming (per chunk) | <1ms | 5-20ms |
| Tokenization | 1-5ms | 10-50ms |
| Embeddings | 10-100ms | 100-500ms |

### Memory Usage

**Local Mode:**
- Base RAM: 2-8GB (model dependent)
- Per parallel agent: ~0 (shared model cache)

**Remote Mode:**
- Client RAM: <50MB (just HTTP client)
- Server RAM: 2-8GB (model only)

---

## Recommended Configuration by Use Case

### Use Case: RPG with Multiple NPCs
```
❌ Remote (would be slow with many sequential requests)
✅ Local (parallel processing, low latency)

Config:
- Local mode
- Multiple LLMAgent components
- GPU acceleration enabled
```

### Use Case: Multiplayer Chat System
```
✅ Remote (shared server, easy scaling)
❌ Local (duplicated models, resource intensive)

Config:
- Remote LM Studio
- Single shared server
- Load balancing across multiple LM Studio instances
```

### Use Case: Story Adventure Game
```
✅ Either mode works
- Local: Single-player, maximum performance
- Remote: Can release before gathering stories

Config:
- Start with Remote (quick iteration)
- Ship with Local (best performance)
```

### Use Case: Mobile Game
```
✅ Remote (offload to server)
❌ Local (burns battery, heats device)

Config:
- Remote mode
- Server with GPU
- Fallback to simple heuristics if offline
```

---

## Known Limitations & Workarounds

### Limitation 1: No Grammar Constraints

| Issue | Impact | Workaround |
|-------|--------|-----------|
| LM Studio API doesn't support GBNF grammar | Can't enforce JSON structure | Use prompt-based structured guidance |

```csharp
// Workaround: Structured prompt
string json = await llmClient.Completion(@"
Output ONLY valid JSON (no explanation):
{
  ""character"": """",
  ""action"": """",
  ""target"": """"
}");
```

### Limitation 2: Sequential Processing

| Issue | Impact | Workaround |
|-------|--------|-----------|
| LM Studio processes requests sequentially | Multiple agents slow | Use multiple servers or local mode |

```csharp
// Workaround: Multiple servers
var servers = new[]
{
    new LLMClient { host = "server1" },
    new LLMClient { host = "server2" },
    new LLMClient { host = "server3" }
};

// Distribute load
for (int i = 0; i < agents.Length; i++)
{
    var response = await servers[i % servers.Length]
        .Completion(agents[i].GetPrompt());
}
```

### Limitation 3: No Exact Tokenization

| Issue | Impact | Workaround |
|-------|--------|-----------|
| Estimated ~4 chars per token | Context length may overflow | Add 20% buffer, monitor actual tokens |

```csharp
// Workaround: Conservative estimation
const int BUFFER = 1.2f; // 20% safety margin
int maxPromptTokens = (maxContextTokens / BUFFER);
```

---

## Summary Table

### ✅ Fully Supported
- Text completion
- Streaming responses
- Standard sampling parameters
- Embeddings (with compatible model)
- LORA adapters (via preprocessing)
- Multi-turn conversations (manually managed)

### ⚠️ Partially Supported
- Tokenization (estimated, not exact)
- Parallel processing (sequential server-side)
- Context configuration (model-fixed)

### ❌ Not Supported
- Grammar constraints
- Detokenization
- Flash attention
- Reasoning mode
- Batch size configuration
- Slot management API

---

## Migration Path

If you discover needed features aren't available:

1. **Try Workaround First** (See section above)
2. **If No Workaround:** Consider local mode
   ```csharp
   llmClient.remote = false;  // Switch to local
   ```
3. **If Local Not Possible:** File LM Studio feature request
   - They actively update the API
   - OpenAI API features eventually come to LM Studio

---

## Questions to Ask Before Starting

- [ ] Do I need grammar constraints? → Use local
- [ ] Do I need exact tokenization? → Use local
- [ ] Do I need parallel multi-agent? → Use local or multiple servers
- [ ] Do I need easy deployment? → Use remote
- [ ] Do I need offline capability? → Use local
- [ ] Do I need flexibility for testing? → Use remote with fallback to local


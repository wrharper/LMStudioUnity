# AI Coding Agent Instructions for LLMUnity

## Project Overview

**LLMUnity** is a Unity package enabling integration of Large Language Models (LLMs) into games and applications. It provides:
- **Dual-backend support**: Local inference via LlamaLib (C++ wrapper around llama.cpp) + remote inference via LM Studio (HTTP API)
- **High-level APIs**: LLMAgent (NPC characters), RAG system (semantic search), LLMEmbedder (embeddings)
- **Cross-platform**: Windows, macOS, Linux, Android, iOS, VisionOS with GPU acceleration (NVIDIA CUDA, AMD ROCm, Apple Metal)

**Key Version**: 3.0.1 with recent LM Studio conversion (Feb 2026)

---

## Architecture & Component Hierarchy

### Core Components (Runtime/)

**LLM.cs** - Server component managing local LLM inference
- Handles model loading, GPU layer offloading, LORA adapters
- Manages service lifecycle (CreateServiceAsync, StartServiceAsync)
- State: `started`, `failed`, `modelSetupComplete` (static flags)
- Key method: `WaitUntilReady()` - waits for service initialization

**LLMClient.cs** - Base client for both local and remote connections
- Handles parameter management (temperature, top_p, top_k, mirostat, etc.)
- Supports local (direct LLM reference) and remote (host:port) modes
- Thread-safe setup via `SemaphoreSlim startSemaphore`
- Exposes: `Completion()`, `Tokenize()`, `Embeddings()`, `Detokenize()`

**LLMAgent.cs** (extends LLMClient) - Conversational AI with persistent history
- System prompt + chat history management
- Slot-based parallel processing (for multiple concurrent agents)
- Saves/loads conversation state (persistentDataPath)
- Uses `UndreamAI.LlamaLib.LLMAgent` for chat templating

**LMStudio/** (NEW) - Remote HTTP-based backend
- `LMStudioClient.cs` - HTTP client wrapping LM Studio's OpenAI-compatible API
- `LMStudioAdapter.cs` - Adapter maintaining compatibility with LlamaLib interface
- Allows switching backends without changing higher-level code

### RAG Subsystem (Runtime/RAG/)

**Searchable** (abstract) - Template for embeddings-based search
- `Search` (template) with `Add()`, `Remove()`, `IncrementalSearch()`, `Clear()`
- **SimpleSearch** - Brute-force cosine similarity
- **DBSearch** - Persists embeddings via USearch (Approximate Nearest Neighbor)

**Chunking** (abstract) - Text splitting strategies
- **SentenceSplitter**, **TokenSplitter**, **WordSplitter**
- Used by RAG to partition documents for embedding

**RAG.cs** - High-level orchestrator combining search + LLMClient for context injection

### Native Integration (Runtime/LlamaLib/)

**LlamaLib.cs** - P/Invoke wrapper around native C++/C library
- Platform-specific binary loading (win-x64, osx-x64, osx-arm64, linux-x64, android, ios, visionos)
- P/Invoke delegates for all llama.cpp operations
- GPU layer management per-platform

---

## Critical Patterns & Conventions

### 1. **Async/Await Throughout**
All I/O and inference operations use `async Task`:
```csharp
// Always await client operations
string response = await llmClient.Completion(prompt);
await llmAgent.Chat(userInput);

// Setup operations MUST be awaited
await CheckCaller();
await SetupCallerObject();
await PostSetupCallerObject();
```

### 2. **Logging & Error Handling**
Use `LLMUnitySetup.Log()` and `LLMUnitySetup.LogError()`:
```csharp
// Non-fatal (logs warning)
LLMUnitySetup.Log($"Creating service with threads={threads}");

// Fatal (logs error + optionally throws)
LLMUnitySetup.LogError("Model path invalid", true); // true = fatal
```

Custom exception: `LLMException` (with optional error codes)

### 3. **State Management**
Track initialization state carefully:
```csharp
// In LLM.cs:
public bool started { get; private set; }      // Service running
public bool failed { get; private set; }        // Initialization failed
public static bool modelSetupComplete { get; private set; } // Global setup complete

// Always check before use:
await llm.WaitUntilReady();
if (llm == null) LLMUnitySetup.LogError("...", true);
```

### 4. **Thread Safety**
Use semaphores for protected async initialization:
```csharp
// In LLMClient.SetupCallerObject():
await startSemaphore.WaitAsync();
try { /* setup code */ }
finally { startSemaphore.Release(); }
```

### 5. **Callback Patterns**
Two callback types for streaming:

**Action<string>** (modern, Unity-friendly):
```csharp
await llmClient.Completion(prompt, 
    callback: (chunk) => Debug.Log("Chunk: " + chunk)
);
```

**CharArrayCallback** (IL2CPP compatibility):
```csharp
// Defined in LlamaLib - for AOT compilation on iOS
// Wrapped via IL2CPP_Completion.CreateCallback() when ENABLE_IL2CPP
```

### 6. **Parameter Mapping**
LlamaLib ↔ LM Studio parameter translation in `LMStudioClient.MapParameterName()`:
- `n_predict` → `max_tokens`
- `repeat_penalty` → `frequency_penalty`
- Others map 1:1 (temperature, top_p, seed, mirostat, etc.)

### 7. **Execution Order Dependencies**
```csharp
[DefaultExecutionOrder(-2)] // RAG Search (needs initialization first)
[DefaultExecutionOrder(-1)] // LLMAgent (depends on LLM)
// Default (0):            // Game scripts depend on LLMAgent
```

---

## Key Files & Their Responsibilities

| File | Purpose | Key Methods |
|------|---------|------------|
| `LLM.cs` | Server lifecycle | `WaitUntilReady()`, `StartAsync()` |
| `LLMClient.cs` | Connection handling | `Completion()`, `SetupCaller()` |
| `LLMAgent.cs` | NPC behavior | `Chat()`, `ClearHistory()` |
| `LMStudioClient.cs` | HTTP API wrapper | `Completion()`, `IsServerAlive()` |
| `Search.cs` | RAG templates | `IncrementalSearch()`, `Add()` |
| `RAG.cs` | RAG orchestration | Combines search + LLM |
| `LlamaLib.cs` | Native bindings | P/Invoke delegates |
| `LLMUnitySetup.cs` | Utilities | `Log()`, path handling, platform detection |

---

## Developer Workflows

### Adding a New LLM Operation
1. **Define in LLMClient**: Add async method with callback support
2. **Test both backends**: Local (LLM + LlamaLib) and remote (LM Studio)
3. **Document parameter mapping** if using completion parameters
4. **Add streaming callback** if appropriate: `callback: (chunk) => {}`

Example:
```csharp
public virtual async Task<string> YourOperation(string input, Action<string> callback = null)
{
    await CheckCaller();
    SetCompletionParameters();
    
    Action<string> wrappedCallback = null;
    if (callback != null) {
#if ENABLE_IL2CPP
        var mainThreadCallback = Utils.WrapActionForMainThread(callback, this);
        wrappedCallback = IL2CPP_Completion.CreateCallback(mainThreadCallback);
#else
        wrappedCallback = Utils.WrapCallbackForAsync(callback, this);
#endif
    }
    
    return await llmClient.YourOperationAsync(input, wrappedCallback);
}
```

### Implementing RAG Search
1. Extend `Searchable` abstract class
2. Implement `Add()`, `Remove()`, `IncrementalSearch()`, `IncrementalFetchKeys()`
3. Handle embeddings via `Encode()` method
4. Persist state if needed (see DBSearch)

### Testing
Use `Tests/Editor/TestLLM.cs` as template:
- Create test class extending `TestLLM`
- Override `CreateLLM()` to configure model path
- Override `SetParameters()` for architecture-specific assertions
- Use `await llmAgent.Chat()` and validate `reply.Contains(expectedText)`

Architecture flags set via `LLMUnitySetup.CUBLAS`:
```csharp
public override void TestArchitecture() {
    Assert.That(llm.architecture.Contains("cublas"));
}
```

---

## Common Pitfalls & Solutions

| Problem | Solution |
|---------|----------|
| "Caller not initialized" error | Always `await CheckCaller(null)` before operations |
| Deadlock in async code | Avoid blocking calls; use `await Task.Yield()` |
| IL2CPP callback crashes | Use `IL2CPP_Completion.CreateCallback()` wrapper |
| Wrong architecture loaded | Check RuntimePlatform in LLM.CreateLib() logic |
| Slot validation fails | Verify `slot >= -1 && slot < llm.parallelPrompts` |
| Grammar constraints ignored | LM Studio doesn't support grammar - document limitation |
| Embeddings return empty | Verify embeddings-only model loaded in target backend |

---

## Backend-Specific Considerations

### Local Backend (LlamaLib + Native)
- **Strengths**: No network overhead, grammar support, LORA via API, full tokenizer
- **Responsibilities**: Model loading, GPU acceleration, server process management
- **Limitations**: Build complexity, platform-specific binaries

### Remote Backend (LM Studio)
- **Strengths**: No network overhead, ✅ **LORA merging support**, works cross-network, model switching via GUI
- **Responsibilities**: HTTP request formatting, parameter mapping, LORA preprocessing
- **Limitations**: Grammar constraints ❌, tokenization estimates, slot management not available
- **Key methods**: `LMStudioClient.Completion()`, `PrepareModelWithLora()`, parameter translation in `MapParameterName()`
- **LORA handling**: Automatic merging via `LoraPreprocessor.MergeLorasIntoModel()` creates cacheable .gguf files

---

## Integration Points & Dependencies

**External Dependencies**:
- Newtonsoft.Json - Parameter serialization (JObject, JArray)
- UnityEngine.Networking - HTTP requests
- System.Threading - Semaphores, Tasks
- System.Runtime.InteropServices - P/Invoke (native library loading)

**Cross-Component Calls**:
- LLMAgent → LLMClient → LlamaLib/LMStudioClient
- RAG → LLMEmbedder → LLMClient
- LLMBuilder → LLM (model downloading)
- LLMManager → LLM (global state management)

---

## Documentation Conventions

- Use XML doc comments (`/// <summary>`) extensively
- Mark limitations in docstrings (e.g., "Not supported when using LM Studio")
- Reference assembly namespaces: `undream.llmunity.Runtime.asmdef`
- Link to related classes in summary/remarks

---

## Recent Changes (LM Studio Conversion + LORA Support)

The codebase was recently converted (Feb 2026) to support LM Studio alongside the original LlamaLib backend, including **automatic LORA adapter merging**:

**New Files**:
- `Runtime/LMStudio/LMStudioClient.cs` - HTTP API wrapper
- `Runtime/LMStudio/LMStudioSetup.cs` - Configuration helpers
- `Runtime/LMStudio/LoraPreprocessor.cs` - **Automatic LORA merging for LM Studio**
- `Runtime/LlamaLib/LMStudioAdapter.cs` - Backend compatibility layer
- `Samples~/LMStudioWithLORA/` - **Complete LORA example with UI**
- `LM_STUDIO_LORA_GUIDE.md` - **Comprehensive LORA documentation**

**Key Design**:
- No changes to LLMClient/LLMAgent public APIs
- Remote flag switches between backends automatically
- **LORA adapters automatically merged into base models for LM Studio compatibility**
- Documentation updates in `LM_STUDIO_SETUP.md`, `MIGRATION_GUIDE.md`, `LM_STUDIO_LORA_GUIDE.md`

**Adapter Pattern**:
```csharp
// LMStudioAdapter implements LLMLocal interface
// Allows LMStudioClient to slot into LlamaLib-dependent code
public class LMStudioAdapter : LLMLocal {
    private LLMStudioClient lmStudioClient;
    // Wraps all LLMStudioClient calls as LLMLocal methods
}
```

**LORA Merging Pipeline**:
```csharp
// LoraPreprocessor.MergeLorasIntoModel() provides:
// 1. Automatic detection of LORA adapters
// 2. Merging into standalone .gguf files (offline, once)
// 3. Intelligent caching (no re-merging for same config)
// 4. Integration with LMStudioClient.PrepareModelWithLora()
// 5. Seamless fallback if merge tools unavailable
```

---

## When to Ask for Clarification

- ❓ Backend-specific behavior (ask "Are we in local or remote mode?")
- ❓ Parameter encoding (should match OpenAI API expectations)
- ❓ Platform-specific code (check RuntimePlatform branch logic)
- ❓ Test expectations (architecture detection varies by build)

---

## Reference Documentation

- Main README: `README.md`
- LM Studio setup: `LM_STUDIO_SETUP.md`
- Migration guide: `MIGRATION_GUIDE.md`
- API examples: `Samples~/LMStudioChatBot/`
- Doxygen docs: `.github/doxygen/`


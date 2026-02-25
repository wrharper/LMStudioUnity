# Local vs Remote LM Setup - Decision Guide

**Last Updated:** February 24, 2026

## Quick Decision Tree

```
Do you have a GPU and want maximum performance?
├─ YES → Use LOCAL (LlamaLib) for inference in-process
│         Pros: No network latency, full feature set, GPU optimized
│         Cons: Requires native dependencies, build complexity
│
└─ NO or MAYBE → Use REMOTE (LM Studio) for simplicity
                  Pros: Easy setup, no build complexity, flexible deployment
                  Cons: Network overhead, separate application required
```

## Detailed Comparison Matrix

| Feature | Local (LlamaLib) | Remote (LM Studio) |
|---------|------------------|------------------|
| **Setup Complexity** | High (native libs) | Low (just download app) |
| **Build Size** | Large (+platform binaries) | Small |
| **Dependencies** | Native C++ library | HTTP client only |
| **Network Latency** | None (in-process) | ~10-50ms |
| **GPU Support** | NVIDIA, AMD, Apple Metal | Yes (via LM Studio GUI) |
| **Model Switching** | Programmatic | Via LM Studio GUI |
| **Grammar Constraints** | ✅ Full support | ❌ Not available |
| **LORA Adapters** | ✅ Native API | ✅ Via merging |
| **Tokenization** | ✅ Exact | ⚠️ Estimated |
| **Detokenization** | ✅ Exact | ⚠️ Not available |
| **Parallel Processing** | ✅ Multi-slot | ⚠️ Single sequence |
| **Embeddings** | ✅ Full support | ✅ Model-dependent |
| **Cross-Platform** | Win/Mac/Linux/iOS/Android | Win/Mac/Linux (server) |

## Use Case Matrix

### Use LOCAL (LlamaLib) When...

#### 1. **Shipping a Game/App**
- Need production-grade performance
- Inference must be fast and low-latency
- Player experience matters
- Don't want external service dependency

```
Game + LLM in one build
├─ Windows → .exe (includes native libs)
├─ Mac → .app (includes native libs)
├─ iOS → .ipa (includes native libs)
└─ Android → .apk (includes native libs)
```

#### 2. **Using Advanced Features**
- Need grammar constraints for structured output
- Token-exact tokenization critical
- Parallel multi-agent conversations
- Custom LORA loading at runtime

#### 3. **Performance-Critical Applications**
- <100ms latency requirement
- Real-time conversational AI
- Mobile game with battery/network constraints
- Offline-first requirement

#### 4. **Deployment Flexibility**
- Single executable needed
- No separate service management
- Cold-start speed important
- Containerized/cloud deployment

**Local Example:**
```csharp
// Single-player game with AI companion
[SerializeField] private LLM llm;  // Local instance
[SerializeField] private LLMAgent npc;

void Start()
{
    // LLM loads in-process, fully under your control
    npc.llm = llm;
}
```

### Use REMOTE (LM Studio) When...

#### 1. **During Development/Testing**
- Rapidly testing different models
- Iterating on prompts
- Need to switch models without rebuild
- Want easy model management UI

#### 2. **Creating Prototypes**
- Proof of concept projects
- Quick demos
- Educational projects
- Research experiments

#### 3. **Server-Based Applications**
- Web backend with LLM capability
- Multiple clients connecting to one LLM
- Distributed system with shared LLM service
- Flexible load balancing

#### 4. **Build Simplicity Required**
- Minimal binary size important
- No native dependency build configuration
- Cross-platform without platform-specific binaries
- Simplified CI/CD pipeline

**Remote Example:**
```csharp
// Multiplayer game - all players share one LM Studio instance
[SerializeField] private LLMClient client;

void Start()
{
    // Configure to connect to LM Studio server
    client.remote = true;
    client.host = "server.example.com";
    client.port = 1234;
    
    // All clients use shared server
}
```

#### 5. **Mobile with External Server**
- Mobile app connects to remote LLM
- Offload inference to desktop/cloud
- Reduce mobile battery/heat

## Architecture Diagrams

### Local Architecture (Recommended for Games)
```
┌─ Player's Game ─────────────────────┐
│                                     │
│  ┌─ LLMClient                    │
│  │  ↓                             │
│  │ LLM (Local Service)           │
│  │  ↓                             │
│  │ LlamaLib.dll                  │
│  │  ↓                             │  
│  │ llama.cpp (In-Process)        │
│  │  ↓                             │
│  │ GPU (None/NVIDIA/AMD/Metal)   │
│  │                               │
└─────────────────────────────────────┘

Latency: <1ms
Performance: Full GPU utilization
Features: All (grammar, LORA, multi-slot)
```

### Remote Architecture (For Flexibility)
```
┌─ Game Instance 1 ──────┐
│ LLMClient              │
│ (remote=true)          │
└───────────┬────────────┘
            │ HTTP
            │ (10-50ms)
            ↓
┌─────────────────────────────────────┐
│ LM Studio Server                    │
│ (Separate Application)              │
│                                     │
│ OpenAI-Compatible API               │
│ ↓                                   │
│ llama.cpp                           │
│ ↓                                   │
│ GPU (NVIDIA/AMD/Metal)              │
│                                     │
└─────────────────────────────────────┘
            ↑ HTTP
            │
┌─ Game Instance 2 ──────┐
│ LLMClient              │
│ (remote=true)          │
└────────────────────────┘

Latency: 10-50ms per request
Performance: Shared GPU utilization
Features: Basic (no grammar, LORA merged)
```

## Performance Characteristics

### Local (LlamaLib)
```
First-time setup:  ~500ms-2s (model loading)
Single request:    5-50ms (depends on model size + GPU)
Parallel agents:   Yes (multiple slots)
Memory per query:  ~base model size (shared)
Startup time:      Fast (model preloaded)
```

### Remote (LM Studio)
```
First-time setup:  ~100ms (server discovery)
Single request:    50-500ms (includes network overhead)
Parallel agents:   Single sequence at a time
Memory usage:      Server-side only
Startup time:      Instant (no model loading)
Network latency:   10-50ms each way
```

## Cost Analysis

### Local (LlamaLib)
```
Development Time:  High (setup native libs)
Build Complexity:  High (per-platform binaries)
Deployment Size:   Large (base model included)
Runtime Speed:     Fastest
Feature Coverage:  100%
Cost:              Free (open source)
```

### Remote (LM Studio)
```
Development Time:  Low (just HTTP)
Build Complexity:  Low (standard dependencies)
Deployment Size:   Small (model on server)
Runtime Speed:     Good (with network latency)
Feature Coverage:  80-90%
Cost:              Free (open source) + server costs
```

## Migration Path

### Starting with Remote (Safe Choice)
Recommended for new projects:
```
1. Start with LM Studio (remote=true)
   - Fast development
   - Easy testing
   - Focus on game logic first
   
2. If performance needed, migrate to Local:
   - Change: remote=false, assign LLM
   - Code change: ~5 lines
   - Rebuild with native dependencies
```

### Starting with Local (Performance First)
Recommended for performance-critical projects:
```
1. Start with LlamaLib (remote=false)
   - Maximum performance
   - All features available
   - Optimized for production
   
2. If flexibility needed:
   - Can add remote fallback
   - Hybrid mode possible
   - Graceful degradation
```

## Configuration Examples

### Development Setup (Remote LM Studio)
```csharp
// Quick start - just add this to Start()
LMStudioSetup.ConfigureForLMStudio(llmClient);
await LMStudioSetup.ValidateConnection(llmClient);

// Or manually:
llmClient.remote = true;
llmClient.host = "localhost";
llmClient.port = 1234;
```

### Production Setup (Local LlamaLib)
```csharp
[SerializeField] private LLM llm;
[SerializeField] private LLMAgent npc;

void Start()
{
    // Local mode - full control
    // LLM automatically configures all settings
    npc.llm = llm;
    
    // Wait for model to load
    await llm.WaitUntilReady();
}
```

### Hybrid Setup (Auto-Fallback)
```csharp
async Task InitializeLLM()
{
    // Try local first
    if (llm != null && llm.llmService != null)
    {
        client.remote = false;
        await client.SetupCaller();
        return;
    }
    
    // Fallback to remote
    client.remote = true;
    client.host = "localhost";
    client.port = 1234;
    await client.SetupCaller();
}
```

## Troubleshooting Decision Making

### "I want the simplest possible setup"
→ **Use Remote (LM Studio)**
- Download LM Studio app
- Click "Start Server"
- Set `remote=true` in code
- Done in 5 minutes

### "I want best performance"
→ **Use Local (LlamaLib)**
- Configure native library dependencies
- Setup model in project
- Set `remote=false` in inspector
- Trade setup time for speed

### "I need both grammar AND easy development"
→ **Not possible** (grammar only in local mode)
Must choose one:
- Use local mode + native setup
- Use remote mode + implement grammar as prompt guidance

### "I need max flexibility"
→ **Use Local (LlamaLib)**
All features available:
- Grammar constraints
- Token-exact I/O
- Multi-slot processing
- Custom LORA chains

### "I need to ship a small binary"
→ **Use Remote (LM Studio)**
- Game binary: ~10-50MB
- LM Studio runs separately on player's machine
- No large model files in binary

## Recommended Setups by Project Type

| Project Type | Recommendation | Reasoning |
|--------------|-----------------|-----------|
| **Game with AI NPC** | Local | Best for immersive, low-latency experience |
| **Chat Bot App** | Remote (Dev) → Local (Prod) | Flexible development, optimized release |
| **Multiplayer Game** | Remote | Shared server-side LLM for all players |
| **Mobile App** | Remote | Offload to server, reduce mobile load |
| **Research/Learning** | Remote | Quick iteration, easy model switching |
| **Web Service** | Remote | Natural fit for HTTP interface |
| **Offline Game** | Local | Must work without server |
| **Performance Game** | Local | Low-latency requirement |

## Next Steps

1. **Choose your mode** using this guide
2. **Read the setup guide:**
   - Local: [README.md](README.md) - "Setup" section
   - Remote: [LM_STUDIO_SETUP.md](LM_STUDIO_SETUP.md)
3. **Configure your LLMClient** or **LLM component**
4. **Start building!**

## FAQ

**Q: Can I switch between local and remote?**  
A: Yes! Just change the `remote` property. Both use the same LLMClient API.

**Q: Does remote mode need the internet?**  
A: No, it just needs network connectivity to LM Studio (can be localhost or local network).

**Q: What if my network is slow?**  
A: Consider local mode or move LM Studio to a faster network connection.

**Q: Can I use both local and remote in the same project?**  
A: Yes! Have one LLMClient for local, another for remote.

**Q: Which is actually faster?**  
A: Local is always faster (no network latency), but remote might feel faster during development since you don't rebuild.

**Q: Should I use grammar constraints?**  
A: Yes if possible - makes LLM output reliable. Only available in local mode.

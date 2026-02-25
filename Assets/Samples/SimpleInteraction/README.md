# Simple Interaction Sample - Local & Remote Configuration

This sample demonstrates basic LLM interaction with support for both **local (LlamaLib)** and **remote (LM Studio)** backends.

## Quick Start (2 Minutes)

### Option A: Remote Mode (Easiest - Recommended for Learning)

1. **Download LM Studio**
   - Go to [lmstudio.ai](https://lmstudio.ai)
   - Install the application

2. **Start LM Studio Server**
   - Open LM Studio
   - Go to Developer tab (code icon)
   - Click "Start Server"
   - Download a model if needed (e.g., Mistral, Llama 2)

3. **Run the Sample**
   - Open `Samples~/SimpleInteraction/Scene.unity`
   - Press Play
   - The scene automatically configures for LM Studio (localhost:1234)
   - Type in the input field and watch the response stream

### Option B: Local Mode (Best Performance - More Setup)

1. **Configure Local LLM**
   - Add LLM GameObject to scene (see instructions below)
   - Set model path in inspector
   - Configure GPU layers if desired

2. **Assign to Sample**
   - Drag the LLM GameObject to LLMAgent's "LLM" field
   - Change `useRemote` to `false` in code

3. **Run the Sample**
   - Press Play
   - Responses will be faster (no network latency)

---

## Architecture

### Remote Mode (Default)
```
Unity Game (SimpleInteraction.cs)
    ↓
LLMAgent (uses remote LLMClient)
    ↓
LMStudioClient (HTTP)
    ↓
LM Studio Server (localhost:1234)
    ↓
Inference Engine (GPU/CPU)

Setup Time: ~5 minutes
Latency: 10-50ms
```

### Local Mode
```
Unity Game (SimpleInteraction.cs)
    ↓
LLMAgent (uses local LLM)
    ↓
LLM Service
    ↓
LlamaLib (Native C++)
    ↓
Inference Engine (GPU/CPU)

Setup Time: ~15 minutes
Latency: <1ms
```

---

## Scene Setup

### Inspector Configuration

The scene comes pre-configured, but here's what you'll see:

#### For Remote Mode (LM Studio)
```
SimpleInteraction (Script)
├─ Use Remote: true
├─ LLM Agent
│  └─ LLM Client (configured for localhost:1234)
└─ UI Elements
   ├─ Input Field
   ├─ Response Text
   └─ Submit Button
```

#### For Local Mode
```
SimpleInteraction (Script)
├─ Use Remote: false
├─ LLM Agent
│  └─ LLM (local component with model)
└─ UI Elements
   ├─ Input Field
   ├─ Response Text
   └─ Submit Button
```

---

## Code Walkthrough

### SimpleInteraction.cs

```csharp
public class SimpleInteraction : MonoBehaviour
{
    [SerializeField] private LLMAgent llmAgent;  // AI character
    [SerializeField] private InputField userInput;  // User types here
    [SerializeField] private Text responseText;  // AI response here
    [SerializeField] private bool useRemote = true;  // Toggle mode
    
    private void Start()
    {
        if (useRemote)
        {
            // Configure for LM Studio (remote)
            LMStudioSetup.ConfigureForLMStudio(
                llmAgent.llmClient,
                host: "localhost",
                port: 1234
            );
        }
        else
        {
            // Use local LLM (already configured in inspector)
            // LLM component handles everything
        }
    }
    
    public async void Chat(string userMessage)
    {
        // Get AI response
        string response = await llmAgent.Chat(
            userMessage, 
            addToHistory: true,
            streamCallback: (chunk) => {
                responseText.text += chunk;  // Stream to UI
            }
        );
    }
}
```

### Key Methods

#### Remote Setup
```csharp
// Automatic configuration for LM Studio
LMStudioSetup.ConfigureForLMStudio(
    llmAgent.llmClient,
    host: "localhost",      // LM Studio address
    port: 1234              // Default LM Studio port
);
```

#### Local Setup
```csharp
// Local LLM just works - must be assigned in inspector
// Configure in inspector:
// - LLM component with model path
// - GPU layers setting
// - Thread count
```

---

## Configuration Guide

### Running Locally (Same Machine)

**What to do:**
1. Start LM Studio
2. Click "Start Server"
3. Run the sample
4. No additional configuration needed

**Default config (already set):**
```csharp
host = "localhost"
port = 1234
```

### Running on Different Machine (Local Network)

**Find your server's IP:**
```powershell
# On your LM Studio machine:
ipconfig
# Look for IPv4 Address like 192.168.1.10
```

**Update the code:**
```csharp
// In SimpleInteraction.cs Start() method:
LMStudioSetup.ConfigureForLMStudio(
    llmAgent.llmClient,
    host: "192.168.1.10",  // Replace with your IP
    port: 1234
);
```

### Switching Between Local and Remote

**In Code:**
```csharp
[SerializeField] private bool useRemote = true;

void Start()
{
    if (useRemote)
    {
        // Configure for LM Studio
        LMStudioSetup.ConfigureForLMStudio(llmAgent.llmClient);
    }
    // else: use local LLM (from inspector)
}
```

**In Inspector:**
1. Select SimpleInteraction GameObject
2. Toggle `Use Remote` checkbox
3. If false: assign LLM component to LLMAgent

---

## Feature Support

| Feature | Remote | Local | Recommendation |
|---------|--------|-------|-----------------|
| **Latency** | 10-50ms | <1ms | Local if critical |
| **Setup Time** | 5 min | 15 min | Remote for learning |
| **GPU Support** | ✅ | ✅ | Either works |
| **Parallel Agents** | ⚠️ Sequential | ✅ Parallel | Local for multiple NPCs |
| **Streaming** | ✅ | ✅ | Either works |
| **Grammar** | ❌ | ✅ | Local only |

---

## Troubleshooting

### Remote Mode Issues

#### "Connection refused"
```
ERROR: Cannot connect to {host}:{port}

Solutions:
1. Is LM Studio running?
   → Open LM Studio application
   
2. Did you click "Start Server"?
   → Go to Developer tab > click "Start Server"
   
3. Is the port correct?
   → Should be 1234 (default)
   → Check Developer tab settings
   
4. Different machine?
   → Verify IP address (ipconfig)
   → Check network connectivity (ping)
```

#### "No model loaded"
```
WARNING: No model loaded in LM Studio

Solution:
1. In LM Studio, click Search tab
2. Download a model (e.g., Mistral-7B)
3. Wait for download to complete
4. Select the model
5. Try again
```

#### "Response is very slow"
```
SLOW PERFORMANCE: Response takes 100+ seconds per token

Causes:
1. Using large FP32 model (try quantized Q4_K_M)
2. GPU not being used (check LM Studio GPU tab)
3. Model very large (7B+ models slow on CPU)

Solutions:
- Use a smaller model (3B-7B quantized)
- Enable GPU in LM Studio settings
- Run LM Studio on more powerful machine
```

### Local Mode Issues

#### "Model fails to load"
```
ERROR: Model not found or corrupted

Solutions:
1. Check model path is correct
2. Download model from Hugging Face
3. Use .gguf format (not SafeTensors)
4. Verify file isn't corrupted (check file size)
```

#### "Out of memory"
```
ERROR: CUDA out of memory

Solutions:
1. Reduce GPU layers in inspector
2. Use smaller quantized model
3. Upgrade GPU memory
4. Use CPU only (slower but works)
```

---

## Next Steps

### Learn About Features
- [Feature Completeness Guide](../../FEATURE_COMPLETENESS_GUIDE.md) - What works where
- [Local vs Remote Decision Guide](../../LOCAL_VS_REMOTE_DECISION_GUIDE.md) - Choosing the right mode

### Advanced Topics
- [Chat Bot Sample](../LMStudioChatBot/) - Better conversation UI
- [RAG Sample](../RAG/) - Knowledge base integration
- [Multiple Characters](../MultipleCharacters/) - Multi-agent scenarios

### Deployment
- [Deployment Guide](../../DEPLOYMENT_GUIDE.md) - Ship your game with LM
- [LM Studio Setup](../../LM_STUDIO_SETUP.md) - Full setup reference

---

## Quick Reference

### API Usage

```csharp
// Chat with AI (includes history)
string response = await llmAgent.Chat("Hello!");

// Raw completion (no history)
string response = await llmClient.Completion("Generate a story:");

// With custom parameters
var parameters = new JObject {
    ["temperature"] = 0.7f,
    ["top_p"] = 0.9f,
    ["n_predict"] = 200
};
string response = await llmClient.Completion(prompt, parameters);

// With streaming
await llmClient.Completion(prompt, 
    streamCallback: (chunk) => Debug.Log(chunk)
);
```

### Configuration

```csharp
// Remote (LM Studio)
LMStudioSetup.ConfigureForLMStudio(client);

// Test connection
await LMStudioSetup.ValidateConnection(client);

// Get server info
await LMStudioSetup.GetServerInfo();

// Local (LlamaLib)
// Configure in inspector or via LLM component
```

---

## Files in This Sample

```
SimpleInteraction/
├─ README.md                    ← You are here
├─ Scene.unity                  ← Unity scene
├─ SimpleInteraction.cs         ← Main script
└─ SimpleInteraction.cs.meta
```

---

## Video Tutorial

(Coming soon - Check GitHub Discussions)

---

## Need Help?

- **LM Studio Issues?** → Check [LM_STUDIO_SETUP.md](../../LM_STUDIO_SETUP.md)
- **API Questions?** → See [FEATURE_COMPLETENESS_GUIDE.md](../../FEATURE_COMPLETENESS_GUIDE.md)
- **Choose Local or Remote?** → [LOCAL_VS_REMOTE_DECISION_GUIDE.md](../../LOCAL_VS_REMOTE_DECISION_GUIDE.md)
- **Deploying?** → [DEPLOYMENT_GUIDE.md](../../DEPLOYMENT_GUIDE.md)


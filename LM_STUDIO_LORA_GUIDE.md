# LORA Adapter Support with LM Studio

## Overview

LM Studio's HTTP API doesn't natively support LORA adapter loading like the local llama.cpp backend does. However, **LLMUnity 3.0.1 now includes automatic LORA merging** that seamlessly integrates LORA adapters with LM Studio through the `LoraPreprocessor` utility.

## How It Works

```
┌─────────────────────┐
│   Base Model        │
│   (mistral-7b)      │
└──────────┬──────────┘
           │
           ├──────────────────────┐
           │                      │
           v                      v
    ┌──────────────────┐   ┌─────────────────────┐
    │  LORA Adapter 1  │   │  LORA Adapter 2     │
    │  (chemistry)     │   │  (math)             │
    │   (weight=0.8)   │   │  (weight=0.9)       │
    └──────────────────┘   └─────────────────────┘
           │                      │
           └──────────┬───────────┘
                      │
                      v
        ┌───────────────────────────────┐
        │  LoraPreprocessor.Merge()     │
        │  (Creates merged GGUF file)   │
        └──────────────┬────────────────┘
                       │
                       v
        ┌──────────────────────────────┐
        │ Merged Model                 │
        │ mistral-7b_merged_hash.gguf  │
        │ (Cached for reuse)           │
        └──────────────┬───────────────┘
                       │
                       v
        ┌──────────────────────────────┐
        │  LM Studio                   │
        │  Loads & runs merged model   │
        └──────────────────────────────┘
```

## Installation & Setup

### Prerequisites

To use LORA merging with LM Studio, you'll need:

1. **llama.cpp built with LORA support** - The `quantize` binary or Python scripts
2. **Model path accessible** - Base model and LORA files must exist
3. **Sufficient disk space** - Merged models are cached; a 7B model = ~4GB disk space

### Option A: Using llama.cpp Quantize Binary (Recommended)

1. Clone and build llama.cpp:
   ```bash
   git clone https://github.com/ggerganov/llama.cpp.git
   cd llama.cpp
   mkdir -p build && cd build
   cmake ..
   cmake --build . --config Release
   ```

2. Copy the `quantize` binary to a location in your PATH or to:
   ```
   Assets/StreamingAssets/llama.cpp/build/bin/
   ```

3. The preprocessor will automatically detect and use it

### Option B: Using Python Scripts

If you prefer Python-based LORA merging:

1. Install llama.cpp Python package:
   ```bash
   pip install llama-cpp-python
   ```

2. Use the conversion scripts:
   ```bash
   python scripts/convert-lora-to-gguf.py base.gguf lora.gguf output.gguf
   ```

## Usage

### Automatic LORA Merging with LLMClient

When using `LLMClient` with LORA adapters configured in the inspector:

```csharp
public class MyNPC : MonoBehaviour {
    public LLMClient llmClient;
    
    private async void Start() {
        // LLMClient automatically merges LORA adapters
        // when using LM Studio (remote mode)
        await llmClient.SetupCallerObject();
        
        string response = await llmClient.Completion("Hello!");
        Debug.Log(response);
    }
}
```

**What happens internally:**
1. LLMClient detects LORA adapters are configured
2. Calls `LoraPreprocessor.MergeLorasIntoModel()`
3. Merged model is cached (not recreated each time)
4. Merged model path is passed to LM Studio
5. LM Studio loads and uses the merged model

### Manual LORA Merging

For advanced usage, you can call the preprocessor directly:

```csharp
using LLMUnity;

public class LoraManager : MonoBehaviour {
    
    public async void MergeLorasForLMStudio() {
        string baseModel = "Models/mistral-7b.gguf";
        var loras = new List<(string, float)>
        {
            ("Adapters/chemistry.gguf", 0.8f),
            ("Adapters/math.gguf", 0.9f)
        };
        
        // Merge with progress callback
        string mergedPath = await LoraPreprocessor.MergeLorasIntoModel(
            baseModel,
            loras,
            progress => Debug.Log($"Merge progress: {progress * 100:F1}%")
        );
        
        if (mergedPath != null) {
            Debug.Log($"Merged model ready: {mergedPath}");
            // Now load this merged model in LM Studio
        }
    }
    
    public void ViewCachedModels() {
        var cached = LoraPreprocessor.GetCachedMergedModels();
        Debug.Log($"Cached merged models: {string.Join(", ", cached)}");
    }
    
    public void FreeDiskSpace() {
        LoraPreprocessor.ClearMergedModelCache();
        Debug.Log("Cleared LORA merge cache");
    }
}
```

### With LMStudioClient Directly

```csharp
using LLMUnity;

public class MyLMStudioManager : MonoBehaviour {
    
    private LMStudioClient client;
    
    private async void Start() {
        client = new LMStudioClient("localhost", 1234);
        
        // Prepare model with LORA adapters
        var loras = new List<(string, float)>
        {
            ("Models/adapters/lora1.gguf", 0.7f),
        };
        
        string modelPath = await client.PrepareModelWithLora(
            "Models/base-model.gguf",
            loras,
            progress => SetUIProgress(progress)
        );
        
        if (modelPath != null) {
            Debug.Log($"Using merged model: {modelPath}");
            // Now you can use this model for completions
        }
    }
}
```

## Configuration in Inspector

In your LLMClient component, configure LORA adapters as usual:

1. Click **Add LORA** to add adapters
2. Set **Path** (relative to StreamingAssets or PersistentDataPath)
3. Set **Weight** (influence of this adapter, 0.0 to 1.0)
4. **Automatic handling**: When remote mode is active, these are merged automatically

```
┌─ LLM Client Settings ──────────────────┐
│                                        │
│  ✓ Remote                              │
│  Host: localhost                       │
│  Port: 1234                            │
│                                        │
│  ✓ LORA Adapters                       │
│    [0] Path: adapters/chemistry.gguf   │
│        Weight: 0.8                     │
│    [+] Add LORA                        │
│                                        │
└────────────────────────────────────────┘
```

## Caching & Performance

### First Run (Cold Cache)
- **Time**: 30 seconds to 3 minutes (depends on model size)
- **Disk space used**: ~4GB for 7B model
- **Result**: Merged model saved to `PersistentDataPath/MergedModels/`

### Subsequent Runs (Warm Cache)
- **Time**: <100ms (just loads from disk)
- **Disk space**: Reuses existing merged model

### Cache Location
```
Windows:    C:\Users\{Username}\AppData\LocalLow\DefaultCompany\GameName\MergedModels\
macOS:      ~/Library/Application Support/MyGame/MergedModels/
Linux:      ~/.config/MyGame/MergedModels/
Android:    /data/data/com.example.game/files/MergedModels/
iOS:        (cached in app sandbox)
```

### Naming Convention
Merged models are named with a hash to ensure uniqueness:
```
mistral-7b_merged_a7f3c9e2.gguf
                    └─── hash of (base model + loru names/weights)
```

This prevents conflicts when using different LORA combinations.

## Troubleshooting

### Issue: "Quantize binary not found"

**Cause**: The llama.cpp quantize tool is not installed or not in PATH

**Solutions**:
1. Build llama.cpp from source (see Installation section)
2. Add to `Assets/StreamingAssets/llama.cpp/build/bin/` on Windows
3. Add the llama.cpp build directory to system PATH

**Workaround**: Pre-merge LORA adapters using external tools:
```bash
# Using llama.cpp Python scripts
python convert-lora-to-gguf.py base.gguf lora.gguf merged.gguf
```

### Issue: Merge takes too long (hours)

**Cause**: Merging is CPU-intensive; slow disk or large model

**Solutions**:
- Use an SSD (faster I/O)
- Run merge in background before gameplay
- Pre-merge models during development:
  ```csharp
  // Editor-only preprocessing
  #if UNITY_EDITOR
  EditorApplication.quitting += () => {
      LoraPreprocessor.MergeLorasIntoModel(baseModel, loras).Wait();
  };
  #endif
  ```

### Issue: "LORA merge process completed successfully" but LORA has no effect

**Cause**: LORA adapters may not be compatible with base model

**Verify**:
1. LORA was trained for the exact model architecture
2. Tokens/embeddings match the base model
3. Test with known-compatible LORA from the model's author

### Issue: Out of disk space during merging

**Solutions**:
1. Clear cached merged models:
   ```csharp
   LoraPreprocessor.ClearMergedModelCache();
   ```
2. Delete unused LORA adapters
3. Use a smaller base model

## Limitations

| Feature | Status | Notes |
|---------|--------|-------|
| LORA merging | ✅ Supported | Creates standalone GGUF files |
| Multiple LORA | ✅ Supported | All weighted individually |
| Quantized models | ✅ Supported | Works with any GGUF quantization |
| Caching | ✅ Supported | Avoids re-merging identical configs |
| Real-time LORA switching | ❌ Not Supported | Requires model reload in LM Studio |
| LORA unloading | ❌ Not Supported | Restart LM Studio to use base model |
| Grammar constraints | ❌ Not Supported | LM Studio API limitation |

## Comparison: Local vs Remote LORA Support

| Aspect | Local (LlamaLib) | Remote (LM Studio) |
|--------|------------------|-------------------|
| LORA support | ✅ Native API | ✅ Preprocessor |
| Dynamic switching | ✅ Runtime | ❌ Requires reload |
| Performance | ⚡ Native (fast) | ⚡ Merge cached |
| Disk overhead | ✅ None | ⚠️ +4GB per config |
| Setup complexity | ⚠️ PInvoke required | ✅ Just HTTP |
| Multi-adapter | ✅ Yes | ✅ Yes (merged) |

## FAQ

**Q: Can I use LORA adapters not trained on the same model?**
A: No, LORA adapters must be trained on the specific base model. Mismatched adapters will cause errors or give nonsensical results.

**Q: How much disk space do I need?**
A: Roughly 1.3x the model size per merged configuration. A 7B model needs ~8-10GB for 2-3 different LORA combinations.

**Q: Can I run LM Studio while merging?**
A: Yes, merging happens in a background process. LM Studio can continue serving requests (but won't load the merged model until you switch to it).

**Q: What if I want to switch LORA adapters at runtime?**
A: With LM Studio, you'd need to:
1. Merge a new model with different LORA adapters
2. Reload the model in LM Studio via its UI or API

The local backend (LlamaLib) supports dynamic LORA switching without reloading.

**Q: Does merging preserve the original model?**
A: Yes, the base model is never modified. A new `.gguf` file is created with LORA weights baked in.

## Performance Tips

1. **Merge during setup**, not gameplay:
   ```csharp
   private async void Awake() {
       // Merge all needed LORA configs before game starts
       await LoraPreprocessor.MergeLorasIntoModel(baseModel, loras);
   }
   ```

2. **Use warm caches**: After first run, merges load instantly

3. **Keep LORA adapters on fast storage** (SSD preferred)

4. **Pre-select LORA combinations** to avoid merge overhead

## Next Steps

- Review [LM_STUDIO_SETUP.md](../LM_STUDIO_SETUP.md) for general LM Studio integration
- Check [MIGRATION_GUIDE.md](../MIGRATION_GUIDE.md) for migrating from local to remote
- See [examples/LMStudioChatBot](../../Samples~/LMStudioChatBot/) for a working sample

# LM Studio with LORA Adapters Sample

This sample demonstrates how to use LORA (Low-Rank Adaptation) adapters with LLMUnity's LM Studio backend.

## Overview

The sample shows:
- **Automatic LORA merging** - Combines LORA adapters with base models
- **Multiple LORA configurations** - Switch between different LORA setups
- **Testing different expertise** - Use domain-specific LORA adapters (Chemistry, Math)
- **Cache management** - Clear merged models to free disk space
- **Real-time testing** - Verify LORA adapters are working with test prompts

## Setup

### Prerequisites

1. **LM Studio** - Running on localhost:1234
2. **Base Model** - Loaded in LM Studio (e.g., Mistral 7B)
3. **LORA Adapters** - Place .gguf LORA files in `Assets/StreamingAssets/Models/`

Example structure:
```
Assets/StreamingAssets/Models/
├── mistral-7b.gguf          (base model)
├── chemistry-lora.gguf      (chemistry expert adapter)
└── math-lora.gguf           (math expert adapter)
```

### Scene Setup

1. Create a new scene with:
   - Canvas with UI elements (Dropdown, Text, InputField)
   - LLMAgent component configured for LM Studio (remote=true)
   - LMStudioLoraDemo script attached to GameObject

2. Configure in inspector:
   - **LLM Agent**: Assign your configured LLMAgent
   - **LORA Selector**: Assign Dropdown UI element
   - **Status Text**: Assign Text element for status messages
   - **Response Text**: Assign Text element for model responses
   - **Available Loras**: Configure LORA paths and weights

### Available LORA Configurations

The sample includes these pre-configured LORA setups:

| Name | Path | Weight | Use Case |
|------|------|--------|----------|
| Generic | (none) | - | Base model without LORA |
| Chemistry Expert | chemistry-lora.gguf | 0.8 | Chemistry/science questions |
| Math Expert | math-lora.gguf | 0.9 | Math/calculus questions |
| Combined | chemistry + math | 0.7 ea | Mixed domain questions |

## Usage

### Via Dropdown

1. Launch the scene
2. Select a LORA configuration from the dropdown
3. Wait for automatic merge (first time takes 30-60s)
4. See test results in the Response text area
5. Type custom prompts in InputField to test "Run" button

### Via Code

```csharp
// Manually trigger LORA loading
var demo = GetComponent<LMStudioLoraDemo>();
await demo.PrepareLoraConfiguration(2); // Load "Math Expert"

// Check cache stats
demo.ShowCacheStats();

// Free up disk space
demo.ClearCache();
```

## How LORA Merging Works

```
┌─ Base Model ─┐
│ mistral-7b   │
└──────┬───────┘
       │
       ├────────────────────────────┐
       │                            │
       v                            v
┌─────────────────┐        ┌───────────────┐
│ Chemistry LORA  │        │ Math LORA     │
│ (weight=0.8)    │        │ (weight=0.9)  │
└─────────────────┘        └───────────────┘
       │                            │
       └────────────────┬───────────┘
                        │
                        v
        ┌──────────────────────────┐
        │  Merged Model (.gguf)    │
        │  (Cached for reuse)      │
        └──────────────┬───────────┘
                       │
                       v
        ┌──────────────────────────┐
        │    LM Studio Server       │
        │    (Runs merged model)    │
        └──────────────────────────┘
```

## Key Features

### Automatic Caching
- First merge: Creates `PersistentDataPath/MergedModels/`
- Subsequent runs: Uses cached version <100ms)
- Same LORA configuration = same cache file

### Progress Tracking
- See merge progress in UI: "⏳ Merging... 45%"
- Callback updates in real-time

### Error Handling
- Invalid LORA paths → Clear error message
- Merge failures → Detailed error logging
- Network issues → Graceful fallback

## Testing Different LORA Adapters

The sample includes domain-specific test prompts:

```csharp
// Chemistry test
"Explain the process of photosynthesis in plant cells."

// Math test
"What is the derivative of x^3 + 2x?"

// Combined test
"Calculate the kinetic energy of a moving object."
```

## Troubleshooting

### "Merge takes too long"
- Expected for first run: 30 seconds to 3 minutes
- Warm cache: <100ms subsequent runs
- Use SSD for faster I/O

### "LORA has no effect"
- Verify LORA was trained for base model
- Check LORA file integrity
- Test with known-compatible adapter from model author

### "Out of disk space"
- Click "Clear Cache" or call `demo.ClearCache()`
- Removes cached merged models
- Will be re-merged on next use

## Advanced Usage

### Custom LORA Configurations

Edit the `availableLoras` array in the inspector to add your own:

```csharp
availableLoras = new LoraConfig[]
{
    new LoraConfig { 
        name = "Custom Expert", 
        path = "Models/my-custom-lora.gguf", 
        weight = 0.75f 
    }
};
```

### Multi-Adapter Merging

To combine multiple LORA adapters (if preprocessor supports it):

```csharp
var loras = new List<(string, float)>
{
    ("chemistry-lora.gguf", 0.7f),
    ("writing-lora.gguf", 0.6f),
    ("reasoning-lora.gguf", 0.8f)
};

var mergedPath = await LoraPreprocessor.MergeLorasIntoModel(
    baseModel,
    loras
);
```

## See Also

- [LM_STUDIO_LORA_GUIDE.md](../../LM_STUDIO_LORA_GUIDE.md) - Complete LORA documentation
- [LM_STUDIO_SETUP.md](../../LM_STUDIO_SETUP.md) - LM Studio integration guide
- [MIGRATION_GUIDE.md](../../MIGRATION_GUIDE.md) - Switching from local to remote

## Performance Tips

1. **Pre-merge during development** to avoid merge delays during testing
2. **Use warm cache** - Subsequent runs load instantly
3. **Keep LORA files on SSD** for faster I/O
4. **Clear unused cache** to free ~4GB per cached configuration

## Support

For issues or questions:
1. Check the console for detailed error messages
2. Review [LM_STUDIO_LORA_GUIDE.md](../../LM_STUDIO_LORA_GUIDE.md) troubleshooting section
3. Ensure LM Studio is running on localhost:1234

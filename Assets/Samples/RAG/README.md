# RAG (Retrieval Augmented Generation) Sample - Local & Remote

This sample demonstrates integrating a **knowledge base with LLM** for context-aware responses. It works with both **local (LlamaLib)** and **remote (LM Studio)** backends.

## Use Cases

- **Game Lore System**: Let NPCs answer questions about game world
- **Customer Support**: Chatbot trained on documentation
- **Educational Games**: AI tutor with subject knowledge
- **Story Generation**: NPC dialogue grounded in world state

---

## Quick Start (5 Minutes)

### Option A: Remote Mode (Easiest)

#### 1. Start LM Studio
```
1. Open LM Studio
2. Go to Developer tab
3. Click "Start Server"
4. Model loads automatically (requires ≥500MB VRAM)
```

#### 2. Prepare Knowledge Base
```
Create a text file (e.g., game_lore.txt):

The protagonist must gather three artifacts to save the kingdom.
The Fire Stone is hidden in the volcano's crater.
The Water Gem rests at the bottom of the Ancient Well.
The Wind Crystal floats above the Sky Towers.
```

#### 3. Run the Sample
```
1. Open Samples~/RAG/Scene.unity
2. Assign your knowledge base file
3. Press Play
4. Ask: "Where is the Fire Stone?"
5. AI responds with relevant context
```

### Option B: Local Mode (More Latency, Better Performance)

#### 1. Configure Local LLM
```
- Add LLM component with model path
- Set GPU layers
- Set up embeddings-only model (for search)
```

#### 2-3. Same as above (knowledge base + run)

---

## How RAG Works

### The RAG Pipeline

```
User Query: "Where is the Fire Stone?"
        ↓
[Embedding] (Convert to semantic vector)
        ↓
[Search] (Find relevant passages)
        ↓ Found:
        "Fire Stone is in volcano's crater"
        ↓
[Context Injection] (Include in prompt)
        ↓
Prompt: "Based on: Fire Stone is in volcano's crater...
         Where is the Fire Stone?
         Answer:"
        ↓
[LLM Response] (Grounded in knowledge)
        ↓
Response: "According to the lore, the Fire Stone is hidden 
          in the volcano's crater."
```

### Key Components

| Component | Purpose | Backend |
|-----------|---------|---------|
| **Embedder** | Converts text → vectors | Both (LM Studio model or local) |
| **Search** | Finds similar passages | Local (no network calls) |
| **LLMClient** | Executes inference | Remote (LM Studio) or Local |
| **RAG** | Orchestrates pipeline | Both |

---

## Scene Setup

### Inspector Configuration

```
RAGExample (Script)
├─ Knowledge Base File
│  └─ Path to .txt file
├─ LLM Agent
│  └─ Configured for remote
├─ Embedder Model
│  └─ Embedding model selection
└─ UI Elements
   ├─ Query Input
   ├─ Response Text
   └─ Citations (source passages)
```

---

## Configuration Guide

### Remote Mode (LM Studio)

```csharp
// In RAGExample.cs Start():
var rag = new RAG(llmAgent);

// Prepare knowledge base
await rag.Prepare(knowledgeBaseFilePath);

// Answer queries with context
string response = await rag.Query(
    userQuery: "Where is the Fire Stone?",
    numContext: 3,  // Use top 3 most relevant passages
    streamCallback: (chunk) => UpdateUI(chunk)
);
```

**What you need:**
1. ✅ LM Studio running (inference)
2. ✅ Knowledge base text file (search)
3. ✅ Embeddings model (optional - LM Studio provides)

### Local Mode

```csharp
var rag = new RAG(llmAgent);

// Uses local embeddings for search
await rag.Prepare(knowledgeBaseFilePath);

// Same query API
string response = await rag.Query(userQuery);
```

**What you need:**
1. ✅ Local LLM configured
2. ✅ Embeddings model specified
3. ✅ Knowledge base text file

---

## Template: RAGExample.cs

```csharp
using UnityEngine;
using undream.llmunity.Runtime;

public class RAGExample : MonoBehaviour
{
    [SerializeField] private LLMAgent llmAgent;
    [SerializeField] private string knowledgeBasePath = "Assets/lore.txt";
    [SerializeField] private InputField queryInput;
    [SerializeField] private Text responseText;
    [SerializeField] private Text citationsText;
    
    private RAG rag;
    
    private async void Start()
    {
        // Setup RAG
        rag = new RAG(llmAgent);
        
        // Configure for LM Studio
        LMStudioSetup.ConfigureForLMStudio(llmAgent.llmClient);
        
        // Load knowledge base
        string knowledgeBase = System.IO.File.ReadAllText(knowledgeBasePath);
        await rag.Prepare(knowledgeBase);
        
        Debug.Log($"RAG ready. System prompt: {llmAgent.systemPrompt}");
    }
    
    public async void OnQuerySubmitted()
    {
        string query = queryInput.text;
        if (string.IsNullOrEmpty(query)) return;
        
        responseText.text = "Thinking...";
        citationsText.text = "";
        
        try
        {
            // Execute RAG query with streaming
            string response = await rag.Query(
                query,
                numContext: 3,
                streamCallback: (chunk) => {
                    responseText.text += chunk;
                }
            );
            
            // Show citations (which documents were used)
            var sources = rag.GetLastQuerySources();
            citationsText.text = "Sources:\n" + string.Join("\n", sources);
        }
        catch (System.Exception ex)
        {
            responseText.text = $"Error: {ex.Message}";
        }
    }
}
```

---

## Knowledge Base Format

### Simple Format (Text File)

```
# lore.txt

The protagonist must gather three artifacts to save the kingdom.

The Fire Stone is hidden in the volcano's crater.
It powers the ancient weapon that can seal the demon's gate.

The Water Gem rests at the bottom of the Ancient Well.
Legend says it grants insight into the future.

The Wind Crystal floats above the Sky Towers.
It allegedly lets one communicate across vast distances.
```

### Structured Format (JSON)

```json
{
  "documents": [
    {
      "id": "artifact_fire",
      "title": "Fire Stone",
      "content": "The Fire Stone is hidden in the volcano's crater...",
      "category": "Artifact"
    },
    {
      "id": "artifact_water",
      "title": "Water Gem",
      "content": "The Water Gem rests at the bottom...",
      "category": "Artifact"
    }
  ]
}
```

---

## Chunking Strategies

The RAG system automatically splits large documents for better search:

### Sentence Chunking (Default)
```
Strategy: Split on sentence boundaries
Use when: Natural language documents
Size: Adaptive (varies by sentence)

Example chunks:
1. "The Fire Stone is hidden in the volcano's crater."
2. "It powers the ancient weapon that can seal the demon's gate."
```

### Token Chunking
```
Strategy: Split at token count
Use when: Code, technical docs
Size: Fixed (e.g., 256 tokens)

Config:
var searcher = new SimpleSearch(llmEmbedder);
rag.SetChunder(new TokenSplitter(size: 256));
```

### Word Chunking
```
Strategy: Split at word count
Use when: Simple content
Size: Fixed (e.g., 50 words)

Config:
rag.SetChunker(new WordSplitter(size: 50));
```

---

## Feature Support

| Feature | Remote (LM Studio) | Local (LlamaLib) | Notes |
|---------|-------------------|------------------|-------|
| **Text Search** | ✅ | ✅ | Both use semantic embeddings |
| **Embeddings** | ✅ | ✅ Must configure | Via LM Studio or local model |
| **Streaming** | ✅ | ✅ | AI response streams to UI |
| **Chunking** | ✅ | ✅ | Automatic document splitting |
| **Citations** | ✅ | ✅ | Show which docs were used |
| **Parallel Queries** | ⚠️ Sequential | ✅ | Local is faster/parallel |

---

## Troubleshooting

### "No embeddings model available"

Remote Mode:
```
ERROR: Need embeddings model for semantic search

Solutions:
1. In LM Studio, search models
2. Download embedding model (e.g., "all-MiniLM-L6-v2")
3. Select it in LM Studio
4. Restart LM Studio  
5. RAG will auto-detect

Alternative:
- Use local embeddings (see Local Mode setup)
```

Local Mode:
```
ERROR: Embeddings model not configured

Solution:
1. In LLMEmbedder inspector, set model path
2. Use embedding-only model (smaller, faster)
3. Recommended models:
   - sentence-transformers/all-MiniLM-L6-v2
   - instructor-base
```

### "Search results are irrelevant"

Causes:
```
1. Knowledge base too small
   → Add more content
   
2. Weak chunking strategy
   → Try sentence splitting
   
3. Weak embedding model
   → Use larger model (all-mpnet-base-v2)
   
4. Number of results too low
   → Increase numContext (3 → 5)
```

Solutions:
```csharp
// Add more knowledge
string largeKb = LoadAllDocuments();
await rag.Prepare(largeKb);

// Use more context passages
string response = await rag.Query(query, numContext: 5);

// Better chunking
rag.SetChunker(new SentenceSplitter());
```

### "Memory usage too high"

Causes:
```
1. Knowledge base too large
2. Embeddings not optimized
3. High-dimensional embeddings
```

Solutions:
```csharp
// Reduce context passages
await rag.Query(query, numContext: 1);

// Use lightweight embedding model
// (smaller = faster, less accurate)

// Use approximate search instead of brute force
var dbSearch = new DBSearch(usearch_path);
rag.SetSearcher(dbSearch);
```

---

## Advanced: Custom Search Strategies

```csharp
// Brute force search (simple, good for <1000 docs)
var searcher = new SimpleSearch(llmEmbedder);

// Approximate nearest neighbor (scalable, for large KBs)
var searcher = new DBSearch(usearchPath);

// Hybrid: BM25 + semantic
var searcher = new HybridSearch(
    bm25: new BM25SearchIndex(),
    semantic: new SimpleSearch(embedder)
);

rag.SetSearcher(searcher);
```

---

## Next Steps

### Learn More
- [Feature Completeness Guide](../../FEATURE_COMPLETENESS_GUIDE.md) - RAG section
- [Local vs Remote Guide](../../LOCAL_VS_REMOTE_DECISION_GUIDE.md) - Choosing mode
- [API Documentation](../../README.md) - Full RAG API

### Samples
- [Simple Interaction](../SimpleInteraction/) - Basic LLM usage (easier intro)
- [Chat Bot](../LMStudioChatBot/) - Better conversation UI
- [Knowledge Base Game](../KnowledgeBaseGame/) - Full game integration

### Production
- [Deployment Guide](../../DEPLOYMENT_GUIDE.md) - Ship with RAG
- [Performance Tips](../../FEATURE_COMPLETENESS_GUIDE.md#performance) - Optimize latency

---

## API Quick Reference

```csharp
// Initialization
var rag = new RAG(llmAgent);

// Load knowledge base
await rag.Prepare(knowledgeBaseText);

// Query with context injection
string response = await rag.Query(
    query: "Where is the Fire Stone?",
    numContext: 3,
    streamCallback: (chunk) => Debug.Log(chunk)
);

// Get which documents were used
string[] sources = rag.GetLastQuerySources();

// Clear and reload
rag.Clear();
await rag.Prepare(newKnowledgeBase);

// Configure search strategy
rag.SetSearcher(new SimpleSearch(embedder));
rag.SetChunker(new SentenceSplitter());
```

---

## Files in This Sample

```
RAG/
├─ README.md              ← You are here
├─ Scene.unity            ← Unity scene with RAG setup
├─ RAGExample.cs          ← Main RAG script
├─ RAGExample.cs.meta
├─ SampleLore.txt         ← Example knowledge base
└─ SampleLore.txt.meta
```

---

## Frequently Asked Questions

**Q: What's the difference between local and remote for RAG?**
```
Local: Embeddings + inference both local → fast, no network
Remote: Embeddings + inference on LM Studio → flexible, cloud-ready
```

**Q: How much knowledge can RAG handle?**
```
Rule of thumb:
- Small (<100KB): Any approach works
- Medium (100KB-10MB): Use SimpleSearch locally
- Large (10MB+): Use DBSearch with approximate matching
```

**Q: Can I use RAG with multiple agents?**
```
Yes! Each agent can have its own RAG instance:

var npcLore = new RAG(npc1Agent);
var playerKnowledge = new RAG(npc2Agent);

await npcLore.Prepare(npcContent);
await playerKnowledge.Prepare(playerContent);
```

**Q: How do I update knowledge at runtime?**
```csharp
// Clear and reload
rag.Clear();
await rag.Prepare(newContent);

// Or use incremental add (if supported)
rag.AddDocument(id, content);
```

---

## Performance Benchmarks

Typical latency for 100KB knowledge base on modern hardware:

```
Operation        | Local   | Remote (LM Studio)
=================|=========|==================
Search Query     | 50ms    | 100ms (network)
Generate Context | 5ms     | 5ms
LLM Response     | 2-5s    | 2-5s (same backend)
─────────────────────────────────────────────
Total (streaming)| 2-5s    | 2-5s (+100ms)
```

---

## Need Help?

- **LM Studio Setup?** → [LM_STUDIO_SETUP.md](../../LM_STUDIO_SETUP.md)
- **Embeddings Issues?** → Check FEATURE_COMPLETENESS_GUIDE.md embeddings section
- **Performance?** → [DEPLOYMENT_GUIDE.md](../../DEPLOYMENT_GUIDE.md#monitoring--logging)
- **API Questions?** → Check LLM header documentation


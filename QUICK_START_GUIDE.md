# LMStudioUnity Quick Start Guide

Get LLM integration working in **5 minutes** with this step-by-step guide.

---

## ⚡ Super Quick Start (5 Minutes)

### 1. Install LM Studio (2 min)
```
1. Go to lmstudio.ai
2. Download and install
3. Open LM Studio
```

### 2. Start the Server (1 min)
```
1. Click Developer tab (code icon)
2. Click "Start Server"
3. Wait for download/loading
4. You'll see "Server running on localhost:1234"
```

### 3. Run a Sample (2 min)
```
1. Open Samples~/SimpleInteraction/Scene.unity
2. Press Play
3. Type "Hello" in the input field
4. Watch it respond!
```

Done! Your LLM is running.

---

## 📚 Choose Your Path

### Path 1: Simple Chat Bot (Easiest)
**Best for**: Getting started, testing, UI demonstrations

```
→ Open: Samples~/LMStudioChatBot/README.md
→ Setup time: 5 minutes
→ Features: Streaming, history, adjustable parameters
→ Complexity: Intermediate
```

**What you'll get:**
- Full chat interface with UI
- Adjustable temperature/top_p
- Streaming responses
- Error handling

**Next:** Add RAG for knowledge base

---

### Path 2: Simple Interaction (Beginner)
**Best for**: Learning, minimal setup

```
→ Open: Samples~/SimpleInteraction/README.md
→ Setup time: 2 minutes
→ Features: Basic chat
→ Complexity: Beginner
```

**What you'll get:**
- Input/output examples
- Local vs Remote comparison
- Basic configuration
- Troubleshooting

**Next:** Try Chat Bot or RAG

---

### Path 3: Knowledge Base (RAG) (Intermediate)
**Best for**: Games with lore, customer support, Q&A

```
→ Open: Samples~/RAG/README.md
→ Setup time: 10 minutes
→ Features: Semantic search, context injection
→ Complexity: Intermediate
```

**What you'll get:**
- Knowledge base integration
- Semantic search pipeline
- Citation tracking
- Document chunking

**Next:** Combine with LORA for expertise

---

### Path 4: Multiple Characters (Advanced)
**Best for**: RPG NPCs, party systems, dialogue scenes

```
→ Open: Samples~/MultipleCharacters/README.md
→ Setup time: 15 minutes
→ Features: Parallel agents, distinct personalities
→ Complexity: Advanced
```

**What you'll get:**
- Multiple independent agents
- Personality system
- Parallel processing (local mode)
- NPC dialogue management

**Next:** Add LORA for unique voices

---

### Path 5: Custom LORA Adapters (Expert)
**Best for**: Character-specific personalities, domain expertise

```
→ Open: Samples~/LMStudioWithLORA/README.md
→ Setup time: 20 minutes
→ Features: Custom adapters, personality merging
→ Complexity: Expert
```

**What you'll get:**
- LORA adapter integration
- Automatic merging & caching
- Multiple adapter stacking
- Custom fine-tuning examples

**Next:** Deploy to production

---

## 🎯 Choose by Use Case

### Game with NPCs
```
Simple Interaction → Multiple Characters → LORA Adapters
Goal: Each NPC has unique personality
Resources:
- MultipleCharacters/README.md
- LMStudioWithLORA/README.md
- LOCAL_VS_REMOTE_DECISION_GUIDE.md
```

### Chat Bot for Users
```
Chat Bot → RAG (optional) → Production
Goal: Answer questions from knowledge base
Resources:
- LMStudioChatBot/README.md
- RAG/README.md
- DEPLOYMENT_GUIDE.md
```

### Game with Lore System
```
RAG → Multiple Characters → Production
Goal: NPCs answer questions about world
Resources:
- RAG/README.md
- FEATURE_COMPLETENESS_GUIDE.md
- DEPLOYMENT_GUIDE.md
```

### Educational Game
```
Simple Interaction → RAG → Multiple Characters
Goal: AI tutor with subject knowledge
Resources:
- SimpleInteraction/README.md
- RAG/README.md
- LOCAL_VS_REMOTE_DECISION_GUIDE.md
```

---

## 🏗️ Local vs Remote Decision

### Remote Mode (LM Studio) - **Recommended to Start**
```
✅ Easiest setup (5 minutes)
✅ No GPU configuration needed
✅ Works cross-network
❌ Sequential processing (slower for multiple agents)

When to use:
- Learning/testing
- Single character/agent
- Rapid prototyping
- Different machines

Setup: Just run LM Studio
```

### Local Mode (LlamaLib) - **Better Performance**
```
✅ True parallel processing (fast)
✅ Same GPU supports multiple agents
✅ No network latency
❌ More setup complexity
❌ Requires GPU memory

When to use:
- Multiple NPCs
- Performance-critical
- Offline games
- Mobile/VR

Setup: Technical, see LOCAL_VS_REMOTE_DECISION_GUIDE.md
```

**Decision Help:** [LOCAL_VS_REMOTE_DECISION_GUIDE.md](LOCAL_VS_REMOTE_DECISION_GUIDE.md)

---

## 📖 Documentation Map

### For Getting Started
| Document | Purpose | Read Time |
|----------|---------|-----------|
| **This file** | Overview & navigation | 5 min |
| `SimpleInteraction/README.md` | Minimal working example | 10 min |
| `LMStudioChatBot/README.md` | Full UI example | 15 min |

### For Learning
| Document | Purpose | Read Time |
|----------|---------|-----------|
| `LOCAL_VS_REMOTE_DECISION_GUIDE.md` | Architecture choice | 15 min |
| `FEATURE_COMPLETENESS_GUIDE.md` | What works where | 20 min |
| `LORA_PREPROCESSOR_IMPLEMENTATION.md` | Technical details | 10 min |

### For Sample Projects
| Document | Purpose | Read Time |
|----------|---------|-----------|
| `Samples~/RAG/README.md` | Knowledge base system | 15 min |
| `Samples~/MultipleCharacters/README.md` | Managing multiple agents | 15 min |
| `Samples~/LMStudioWithLORA/README.md` | Custom adapters | 20 min |

### For Deployment
| Document | Purpose | Read Time |
|----------|---------|-----------|
| `LM_STUDIO_SETUP.md` | Server configuration | 10 min |
| `DEPLOYMENT_GUIDE.md` | Production deployment | 30 min |
| `MIGRATION_GUIDE.md` | Switching backends | 10 min |

---

## 🚀 Next Steps by Path

### Just Started (Nothing yet)
```
1. Open: Samples~/SimpleInteraction/Scene.unity
2. Press Play
3. See it work!
4. Read: SimpleInteraction/README.md
```

### Have Simple Interaction Working
```
Choose path:
A) Want chat UI? → Read LMStudioChatBot/README.md
B) Want knowledge base? → Read RAG/README.md
C) Want multiple NPCs? → Read MultipleCharacters/README.md
```

### Have Chat Bot Working
```
Options:
A) Add knowledge base? → Read RAG/README.md
B) Add multiple NPCs? → Read MultipleCharacters/README.md
C) Deploy to production? → Read DEPLOYMENT_GUIDE.md
```

### Ready to Deploy
```
1. Read: DEPLOYMENT_GUIDE.md
2. Choose scenario (cloud/docker/local network)
3. Follow step-by-step instructions
4. Deploy!
```

---

## 🆘 Quick Troubleshooting

### "Can't connect to LM Studio"
```
1. Is LM Studio running? (open the app)
2. Did you click "Start Server"? (Developer tab)
3. Wait for model to load
4. Try again
```

### "Response very slow"
```
1. Use quantized model (Q4_K_M)
2. Enable GPU in LM Studio settings
3. Use smaller model (7B instead of 13B)
```

### "Don't know which path to take"
```
Ask these questions:
Q1: Want multiple NPCs? → Yes = MultipleCharacters
Q2: Need knowledge base? → Yes = RAG
Q3: Need custom personality? → Yes = LORA
Q4: Want full chat UI? → Yes = ChatBot
Default: Start with SimpleInteraction
```

---

## 📚 Complete Resource List

### Getting Started
- [SimpleInteraction Sample](Samples~/SimpleInteraction/README.md) ⭐ Start here
- [Chat Bot Sample](Samples~/LMStudioChatBot/README.md)
- [Quick Video Tutorials](https://github.com/undreamio/LMStudioUnity/discussions)

### Learning & Decision Making
- [Local vs Remote Decision Guide](LOCAL_VS_REMOTE_DECISION_GUIDE.md) ⭐ Important
- [Feature Completeness Guide](FEATURE_COMPLETENESS_GUIDE.md) ⭐ What works where
- [API Reference](README.md#api-reference)

### Advanced Features
- [RAG Sample](Samples~/RAG/README.md)
- [Multiple Characters Sample](Samples~/MultipleCharacters/README.md)
- [LORA Adapters Sample](Samples~/LMStudioWithLORA/README.md)

### Setup & Deployment
- [LM Studio Setup](LM_STUDIO_SETUP.md)
- [Deployment Guide](DEPLOYMENT_GUIDE.md) ⭐ For shipping
- [Migration Guide](MIGRATION_GUIDE.md)

### Technical Details
- [LORA Preprocessor Implementation](LORA_PREPROCESSOR_IMPLEMENTATION.md)
- [LM Studio Conversion Summary](LM_STUDIO_CONVERSION_SUMMARY.md)

---

## 🎓 Learning Paths

### 👶 Beginner (Zero Experience)
```
1. Read this file (you're here!)
2. Run: SimpleInteraction/Scene.unity
3. Read: SimpleInteraction/README.md
4. Customize system prompt
5. Celebrate! 🎉
```
**Time: 20 minutes**

### 👨‍💻 Intermediate (Understand Basics)
```
1. Complete Beginner path
2. Run: LMStudioChatBot/Scene.unity
3. Read: LOCAL_VS_REMOTE_DECISION_GUIDE.md
4. Read: FEATURE_COMPLETENESS_GUIDE.md
5. Choose: RAG or MultipleCharacters
5. Implement sample
```
**Time: 1-2 hours**

### 🧑‍🔬 Advanced (Production Ready)
```
1. Complete Intermediate path
2. Read: DEPLOYMENT_GUIDE.md
3. Read: LORA_PREPROCESSOR_IMPLEMENTATION.md
4. Setup: Local mode or cloud deployment
5. Integrate: Multiple samples together
6. Deploy: To production
```
**Time: 4-8 hours**

---

## 💡 Pro Tips

### Speed Up Development
```
1. Use Remote mode (LM Studio) for rapid prototyping
2. Switch to Local mode when optimizing performance
3. Keep LORA preprocessing results cached
4. Use quantized models (Q4_K_M)
```

### Performance
```
1. Local mode: True parallel processing
2. Smaller models (3B, 7B) for speed
3. Quantized vs FP32: 50% smaller, 5-10% quality loss trade
4. Cache embeddings for RAG
```

### Production
```
1. Load models on startup
2. Pre-warm cache before shipping
3. Monitor GPU VRAM usage
4. Set sensible timeouts
5. Log errors for debugging
```

---

## ❓ Common Questions

**Q: Can I use local LLM instead of LM Studio?**
> Yes. Set `useRemote = false` in LLMAgent. See LOCAL_VS_REMOTE_DECISION_GUIDE.md

**Q: Do I need a GPU?**
> No, but it helps significantly. CPU works but is slow. LM Studio handles GPU auto-detection.

**Q: What models should I use?**
> Mistral-7B-Instruct (best balance)  
> Llama-2-7B-Chat (good alternative)  
> neural-chat-7b (compact)  
> Use quantized versions (Q4_K_M)

**Q: Can I deploy to mobile?**
> Yes, with local mode. See DEPLOYMENT_GUIDE.md mobile section

**Q: How much VRAM do I need?**
> 7B model: ~8-12 GB  
> 3B model: ~4-6 GB  
> Quantized: 30-50% less

**Q: Can I run multiple models?**
> Remote: Switch in LM Studio UI anytime  
> Local: Load one at a time

**Q: Do I need internet?**
> Remote LM Studio: No, works on local network only  
> API calls: No built-in external dependenc

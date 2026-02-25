# Multiple Characters Sample - Managing Multiple LLM Agents

This sample demonstrates managing **multiple independent AI characters** in a single scene. Perfect for games with multiple NPCs, dialogue trees, or party-based systems.

## Use Cases

- **RPG NPCs**: Multiple unique character personalities
- **Dialogue Scenes**: Multi-character conversations
- **Party Systems**: Team members with different voices
- **Chat Games**: Multiple AI players
- **Narrative Games**: Story characters with distinct voices

---

## Quick Start (10 Minutes)

### 1. Open the Scene
```
Samples~/MultipleCharacters/Scene.unity
```

### 2. See Pre-configured Characters
```
Hierarchy:
├─ [LLM] (local LLM server)
├─ Character_Wizard (LLMAgent)
├─ Character_Knight (LLMAgent)
├─ Character_Rogue (LLMAgent)
└─ Canvas (UI)
   ├─ Character Selector (dropdown)
   ├─ Chat History (scroll area)
   ├─ Input Field
   └─ Submit Button
```

### 3. Run and Interact
```
1. Press Play
2. Select a character from dropdown
3. Type a message
4. Click Submit
5. That character responds (with their personality)
```

---

## Architecture

### Multiple Independent Agents

```
Single Character (LocalMode):
User Input
    ↓
┌─────────────────────┐
│ LLMAgent (Wizard)   │
│ System: "You are..." │
│ History: [...]      │
└────────────────────┬┘
                     ↓
                  Response

Multiple Characters (This Sample):
User Input
    ↓
┌─────────────────────────────────────────┐
│  Character Selector Dropdown             │
│  "Select: Wizard / Knight / Rogue"      │
└────────────────┬────────────────────────┘
                 │
     ┌───────────┼───────────┐
     ↓           ↓           ↓
┌──────────┐ ┌──────────┐ ┌──────────┐
│ Wizard   │ │ Knight   │ │ Rogue    │
│ Agent 1  │ │ Agent 2  │ │ Agent 3  │
└────┬─────┘ └────┬─────┘ └────┬─────┘
     │            │            │
     └────────────┼────────────┘
                  │
              Response from
              selected agent
```

### Local vs Remote

```
LOCAL MODE (Default):
[LLM Service] ← Shared by all agents
    ↓
[Wizard Agent] [Knight Agent] [Rogue Agent]
    ↑               ↑               ↑
    └───────────────┴───────────────┘
         All use same inference engine
         (parallel processing works!)

REMOTE MODE (LM Studio):
[LM Studio Server] (localhost:1234)
    ↓
[Wizard Agent] [Knight Agent] [Rogue Agent]
    ↑               ↑               ↑
    └───────────────┴───────────────┘
         Sequential on server
         (one response at a time)
```

---

## Code Example: Multi-Character Manager

### Simple Version

```csharp
public class MultipleCharacterManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterConfig
    {
        public string name = "Wizard";
        public string systemPrompt = "You are a wise wizard.";
        public LLMAgent agent;
    }
    
    [SerializeField] private List<CharacterConfig> characters = new();
    [SerializeField] private Dropdown characterSelector;
    [SerializeField] private Text responseText;
    [SerializeField] private InputField chatInput;
    
    private CharacterConfig currentCharacter;
    
    private void Start()
    {
        // Setup UI
        characterSelector.AddOptions(
            characters.Select(c => c.name).ToList()
        );
        characterSelector.onValueChanged.AddListener(OnCharacterSelected);
        
        // Initialize first character
        OnCharacterSelected(0);
    }
    
    private void OnCharacterSelected(int index)
    {
        currentCharacter = characters[index];
        currentCharacter.agent.systemPrompt = currentCharacter.systemPrompt;
        currentCharacter.agent.ClearHistory();
        Debug.Log($"Switched to: {currentCharacter.name}");
    }
    
    public async void OnSubmit()
    {
        string message = chatInput.text;
        if (string.IsNullOrEmpty(message)) return;
        
        chatInput.text = "";
        responseText.text = $"{currentCharacter.name}: Thinking...";
        
        try
        {
            // Get response from current character
            string response = await currentCharacter.agent.Chat(
                message,
                streamCallback: (chunk) => {
                    responseText.text += chunk;
                }
            );
        }
        catch (System.Exception ex)
        {
            responseText.text = $"Error: {ex.Message}";
        }
    }
}
```

### Advanced Version (With Personalities)

```csharp
[System.Serializable]
public class CharacterPersonality
{
    public string name;
    public string role;
    public string personality;
    public string background;
    
    public string GetSystemPrompt()
    {
        return $@"You are {name}, a {role}.
Personality: {personality}
Background: {background}
Respond in character, staying true to your personality.";
    }
}

public class MultiCharacterGame : MonoBehaviour
{
    private Dictionary<string, (LLMAgent agent, CharacterPersonality personality)> characters;
    
    private void Start()
    {
        characters = new Dictionary<string, (LLMAgent, CharacterPersonality)>
        {
            ["Wizard"] = (
                wizardAgent,
                new CharacterPersonality
                {
                    name = "Aldrich",
                    role = "Wise Mage",
                    personality = "Thoughtful, mysterious, speaks in metaphors",
                    background = "1000 years old, witnessed kingdoms rise and fall"
                }
            ),
            ["Knight"] = (
                knightAgent,
                new CharacterPersonality
                {
                    name = "Theron",
                    role = "Valiant Knight",
                    personality = "Honorable, direct, action-oriented",
                    background = "Champion of the realm, trained since childhood"
                }
            ),
            ["Rogue"] = (
                rogueAgent,
                new CharacterPersonality
                {
                    name = "Serina",
                    role = "Cunning Rogue",
                    personality = "Quick-witted, sarcastic, street-smart",
                    background = "Former thief, reformed and loyal to the party"
                }
            )
        };
        
        // Setup each agent with its personality
        foreach (var (key, (agent, personality)) in characters)
        {
            agent.systemPrompt = personality.GetSystemPrompt();
            agent.ClearHistory();
        }
    }
    
    public async void TalkToCharacter(string characterName)
    {
        var (agent, personality) = characters[characterName];
        
        string response = await agent.Chat("Tell me about yourself");
        Debug.Log($"{personality.name}: {response}");
    }
}
```

---

## Inspector Setup

### Hierarchy

```
Scene (Multiple Characters Example)
├─ [LLM] GameObject
│  └─ LLM Component
│     ├─ Model Path: path/to/model.gguf
│     ├─ GPU Layers: 33
│     └─ Threads: 8
│
├─ [Characters] (empty GameObject for organization)
│  ├─ Wizard
│  │  └─ LLMAgent
│  │     ├─ LLM (reference to [LLM])
│  │     ├─ System Prompt: "You are a wise wizard..."
│  │     └─ Parallel Prompts: 8
│  │
│  ├─ Knight
│  │  └─ LLMAgent
│  │     ├─ LLM (reference to [LLM])
│  │     ├─ System Prompt: "You are a valiant knight..."
│  │     └─ Parallel Prompts: 8
│  │
│  └─ Rogue
│     └─ LLMAgent
│        ├─ LLM (reference to [LLM])
│        ├─ System Prompt: "You are a cunning rogue..."
│        └─ Parallel Prompts: 8
│
└─ Canvas
   ├─ Character Selector (Dropdown)
   ├─ Chat History (Text)
   ├─ User Input (InputField)
   └─ Submit Button (Button)
```

### Configuration Values

For each LLMAgent:
```
LLM: [Select shared LLM component]
Parallel Prompts: 8 (allows parallel conversations if using LlamaLib)
System Prompt: [Character-specific prompt]
```

For local mode:
```
Remote: false
LLM: [Assign the shared [LLM] component]
```

For remote mode:
```
Remote: true
Host: localhost
Port: 1234
LLM: [Leave empty/unused]
```

---

## Local vs Remote Comparison

### Local Mode (Recommended for Multiple Characters)

```
Pros:
✅ True parallel processing (no network latency)
✅ Multiple agents can respond simultaneously
✅ Perfect for RPG/party scenarios
✅ Single GPU supports all agents

Cons:
❌ More complex setup
❌ Requires GPU VRAM for all layers
```

```csharp
// Local mode setup
foreach (var character in characters)
{
    character.agent.useRemote = false;
    character.agent.llm = sharedLLMComponent;  // All share same LLM
}
```

### Remote Mode (LM Studio)

```
Pros:
✅ Easiest setup (just run LM Studio)
✅ Can use different models
✅ Cross-machine architecture

Cons:
⚠️ Sequential responses (one at a time)
⚠️ Network latency per response
⚠️ Server becomes bottleneck
```

```csharp
// Remote mode setup
foreach (var character in characters)
{
    character.agent.useRemote = true;
    character.agent.host = "localhost";
    character.agent.port = 1234;
}
```

---

## Advanced: Parallel Conversations

### Scenario: Player talks to 3 NPCs Simultaneously

```csharp
public async void TalkToAllCharacters(string userMessage)
{
    // Start all conversations in parallel
    var tasks = new List<System.Threading.Tasks.Task<string>>();
    
    foreach (var character in characters)
    {
        // Fire off parallel requests
        var task = character.Chat(
            userMessage,
            streamCallback: (chunk) => UpdateUI(character.name, chunk)
        );
        tasks.Add(task);
    }
    
    // Wait for all to complete
    string[] responses = await System.Threading.Tasks.Task.WhenAll(tasks);
    
    // Display all responses
    for (int i = 0; i < characters.Count; i++)
    {
        Debug.Log($"{characters[i].name}: {responses[i]}");
    }
}
```

**Performance (Local LLM, Parallel Prompts = 8):**
```
1 character response: 5 seconds
3 characters (parallel): 5 seconds (+0 overhead)
8 characters (parallel): 5 seconds (+0 overhead)

Remote (LM Studio, Sequential):
1 character: 5 seconds
3 characters: 15 seconds (5+5+5)
```

---

## Character Personality System

### Method 1: System Prompts (Simple)

```csharp
var wizard = new CharacterConfig
{
    name = "Wizard",
    systemPrompt = @"You are Aldrich, a wise wizard known for:
- Speaking in mystical riddles
- Referencing ancient magic
- Using archaic language like 'thee' and 'thou'
Respond in character."
};
```

### Method 2: Dynamic Personality (Advanced)

```csharp
[System.Serializable]
public class DynamicPersonality
{
    public string name;
    public string[] traits;           // ["wise", "mysterious"]
    public string[] speakingStyle;    // ["riddles", "mystical"]
    public string[] knowledge;        // ["magic", "history"]
    
    public string BuildPrompt()
    {
        var prompt = $"You are {name}.\n\n";
        prompt += "Personality traits: " + string.Join(", ", traits) + "\n";
        prompt += "Speaking style: " + string.Join(", ", speakingStyle) + "\n";
        prompt += "Knowledge: " + string.Join(", ", knowledge) + "\n";
        prompt += "Always stay in character.";
        return prompt;
    }
}
```

---

## UI Management

### Character Selector Pattern

```csharp
public void OnCharacterDropdownChanged(int index)
{
    currentCharacterIndex = index;
    currentCharacter = characters[index];
    
    // Clear UI for new character
    chatHistoryText.text = "";
    
    // Show character info
    characterNameText.text = currentCharacter.name;
    characterBioText.text = currentCharacter.bio;
    
    // Load conversation history (if saved)
    LoadCharacterHistory(currentCharacter.name);
}
```

### Conversation History Management

```csharp
private Dictionary<string, List<string>> conversationHistories;

public void SaveCharacterHistory(string characterName)
{
    conversationHistories[characterName] = 
        currentCharacter.agent.GetConversationHistory();
}

public void LoadCharacterHistory(string characterName)
{
    if (conversationHistories.TryGetValue(characterName, out var history))
    {
        currentCharacter.agent.SetConversationHistory(history);
    }
    else
    {
        currentCharacter.agent.ClearHistory();
    }
}
```

---

## Performance Considerations

### Local Mode (LlamaLib)

```
Setup:
├─ 1 LLM service (runs inference)
├─ 3+ LLMAgent instances (parallel slots)
└─ All share same GPU

Latency per character:
- 1 agent: 5 seconds
- 3 agents parallel: 5 seconds (same)
- 8 agents parallel: 8 seconds (slots fill)

Memory:
- Model: 4-13 GB
- Agents: ~10 MB each
- Total: ~4-13 GB (model dominates)
```

### Remote Mode (LM Studio)

```
Setup:
├─ 1 LM Studio server
└─ 3+ LLMAgent instances (sequential)

Latency per character:
- 1 agent: 5 seconds
- 3 agents sequential: 15 seconds (5+5+5)
- Queue: O(n) complexity

Memory:
- Server: 4-13 GB
- Client agents: ~100 KB each
- Total: ~4-13 GB (server dominates)
```

---

## Troubleshooting

### "One agent doesn't respond while another talks"

**Local Mode:**
```
This is normal. Responses play sequentially on UI.
To fix: Use async/await properly:

// ❌ Blocks UI
string response = await agent.Chat(message);

// ✅ Non-blocking
_ = agent.Chat(message);  // Fire and forget
```

**Remote Mode:**
```
LM Studio can only handle one request at a time.
To send parallel requests, queue them properly:

var responses = await System.Threading.Tasks.Task.WhenAll(
    agent1.Chat(msg),
    agent2.Chat(msg),
    agent3.Chat(msg)
);
// Will execute sequentially on server
```

### "Memory usage exploding"

```
Causes:
1. Too many agents (each holds history)
2. Conversation history never cleared
3. Model loaded but unused

Solutions:
- Limit agents to 4-6 in parallel
- Clear history when switching: agent.ClearHistory()
- Use UnloadScene for unused characters
```

### "Responses are similar (lack personality)"

```
Solutions:
1. Improve system prompt with more details
2. Use longer, more specific prompts
3. Add examples in system prompt

Better prompt:
"You are Aldrich, a scholarly wizard who:
- Speaks in riddles and mystical terms
- Always references ancient magic
- Uses dramatic pauses in speech
- Is skeptical of common folk knowledge

Example conversation:
User: 'How does magic work?'
Aldrich: 'Ah, a seeker of arcane knowledge...
Magic flows through the threads of reality...'"
```

---

## File Structure

```
MultipleCharacters/
├─ README.md                           ← You are here
├─ Scene.unity                         ← Main scene
├─ MultipleCharacters.cs               ← Manager script
└─ MultipleCharacters.cs.meta
```

---

## Advanced Patterns

### Character Instances from Data

```csharp
[System.Serializable]
public class CharacterData
{
    public string name;
    public string prefabPath;
    public CharacterPersonality personality;
}

public async void SpawnCharacter(CharacterData data)
{
    var prefab = Resources.Load<GameObject>(data.prefabPath);
    var instance = Instantiate(prefab);
    
    var agent = instance.GetComponent<LLMAgent>();
    agent.systemPrompt = data.personality.GetSystemPrompt();
    
    characters.Add(data.name, agent);
}
```

### Group NPC Conversations

```csharp
public async void GroupConversation(string topic, int numPeaks)
{
    var allResponses = new Dictionary<string, string>();
    
    for (int turn = 0; turn < numPeaks; turn++)
    {
        var tasks = characters
            .Select(c => c.Chat($"[Topic: {topic}] {turn}/10 rounds"))
            .ToList();
        
        var responses = await System.Threading.Tasks.Task.WhenAll(tasks);
        
        // Process responses (edit, filter, regroup)
        // ...
    }
}
```

---

## Next Steps

### Learn More
- [Simple Interaction](../SimpleInteraction/) - Single character intro
- [Chat Bot](../LMStudioChatBot/) - Better conversation UI
- [RAG](../RAG/) - Add knowledge bases to characters
- [LORA](../LMStudioWithLORA/) - Character-specific LORA adapters

### Documentation
- [Feature Completeness Guide](../../FEATURE_COMPLETENESS_GUIDE.md) - What works where
- [Local vs Remote Decision](../../LOCAL_VS_REMOTE_DECISION_GUIDE.md) - Architecture choice
- [Deployment Guide](../../DEPLOYMENT_GUIDE.md) - Production deployment

---

## API Quick Reference

```csharp
// Initialize multiple agents
var agents = new List<LLMAgent> { wizard, knight, rogue };

// Talk to one character
string response = await currentAgent.Chat("Hello!");

// Talk to all characters (parallel, local mode)
var allResponses = await System.Threading.Tasks.Task.WhenAll(
    agents.Select(a => a.Chat("Hello!"))
);

// Talk to all characters (sequential, remote mode)
var responses = new List<string>();
foreach (var agent in agents)
{
    responses.Add(await agent.Chat("Hello!"));
}

// Switch character
currentAgent = agents[newIndex];
currentAgent.ClearHistory();  // Optional: reset history

// Save/load conversation
var history = currentAgent.GetConversationHistory();
currentAgent.SetConversationHistory(savedHistory);
```

---

## FAQ

**Q: How many characters can I manage?**
```
Local: 8-12 agents (with parallel processing)
Remote: 3-5 agents (sequential, practical limit)

Depends on:
- GPU VRAM
- Response time tolerance
- Conversation complexity
```

**Q: Can I use different models for different characters?**
```
Local: Yes, but they share GPU
       More complex to manage

Remote: Yes, easily!
        Just switch in LM Studio between requests
        Or use different servers
```

**Q: How do I make dialogue feel natural?**
```
1. Use detailed system prompts (200+ characters)
2. Give each character distinct vocabulary/tone
3. Include example dialogue in system prompt
4. Use temperature/top_p parameters to vary style
5. Test and refine prompts iteratively
```

**Q: Can characters remember each other?**
```
Yes, with shared context:

var context = "You are in a group with Wizard, Knight, Rogue.\n";
agent.SetSystemPrompt(context + personalityPrompt);

Or implement group history:
sharedGroupHistory = [turns from all agents];
```

---

## Performance Optimization Tips

1. **Pre-load all agents** at start
2. **Use parallel processing** (local mode)
3. **Cache system prompts** (don't rebuild each turn)
4. **Clear history** when switching characters to save memory
5. **Use slots efficiently** (set parallel prompts = num characters)

---

## Need Help?

- **Character Setup?** → See "Code Example" section
- **Performance?** → Check "Performance Considerations"
- **Local vs Remote?** → [LOCAL_VS_REMOTE_DECISION_GUIDE.md](../../LOCAL_VS_REMOTE_DECISION_GUIDE.md)
- **Deployment?** → [DEPLOYMENT_GUIDE.md](../../DEPLOYMENT_GUIDE.md)


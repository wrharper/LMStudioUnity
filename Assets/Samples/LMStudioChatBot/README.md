# LM Studio Chat Bot Sample - Interactive Conversational AI

This sample demonstrates building a **feature-rich chat interface** powered by LLMUnity and LM Studio. It includes streaming responses, conversation history, adjustable parameters, and graceful error handling.

## Quick Start (5 Minutes)

### Step 1: Start LM Studio
```
1. Open LM Studio
2. Go to Developer tab (code icon)
3. Click "Start Server"
4. Model loads on localhost:1234
```

### Step 2: Run the Sample
```
1. Open Samples~/LMStudioChatBot/Scene.unity
2. Press Play
3. Type a message
4. Hit Enter (or click Send)
5. Watch the response stream in real-time
```

### Step 3: Customize (Optional)
```
Edit the system prompt to change personality:
- Teacher → "You are a helpful tutor..."
- Pirate → "You are a swashbuckling pirate..."
- Therapist → "You are a caring counselor..."
```

---

## Architecture

### How It Works

```
User Types Message
        ↓
[Input Field or Voice]
        ↓
LMStudioChatBot.OnSubmit()
        ↓
┌─────────────────────────────────┐
│ LLMAgent (with history)         │
│ ├─ System Prompt (personality)  │
│ ├─ Chat History (memory)        │
│ └─ Connect to LM Studio         │
└────────┬────────────────────────┘
         ↓
[Network] localhost:1234 (LM Studio)
         ↓
┌─────────────────────┐
│ LM Studio Server    │
│ ├─ Model loaded     │
│ ├─ Running inference │
│ └─ Streams response │
└────────┬────────────┘
         ↓
[Network Streaming]
         ↓
UI Updates in Real-time
         ↓
Display Complete Response
```

### Key Components

| Component | Purpose |
|-----------|---------|
| **LLMAgent** | Manages conversation history and calls LM Studio |
| **LLMClient** | Handles network connection parameters |
| **LMStudioChatBot** | UI controller (input/output) |
| **LM Studio Server** | Runs the actual LLM inference |

---

## Scene Setup

### Inspector Configuration

```
Hierarchy:
├─ Canvas
│  ├─ ChatDisplay (Scroll View with Text)
│  ├─ InputField (for user message)
│  ├─ SendButton
│  └─ StatusText (for errors/status)
│
└─ ChatManager
   └─ LMStudioChatBot Script
      ├─ LLM Agent
      │  └─ LLMClient
      │     ├─ Host: localhost
      │     ├─ Port: 1234
      │     ├─ Temperature: 0.7
      │     └─ Top P: 0.9
      ├─ Chat Display: (Text component)
      ├─ Input Field: (InputField component)
      └─ Send Button: (Button component)
```

### Step-by-Step Setup

**1. Create UI Canvas**
```
Create → Canvas
│
├─ Panel (Background)
│  ├─ ChatDisplay (Scroll View)
│  │  └─ Content
│  │     └─ Text (for messages)
│  │
│  ├─ InputField (with placeholder "Type message...")
│  └─ SendButton
```

**2. Create Chat Manager**
```
Create → Empty GameObject (rename to ChatManager)
├─ Add Component: LMStudioChatBot
├─ Assign UI elements in inspector
└─ Configure LLMClient:
   └─ Create → Empty (rename to LLMAgent)
      └─ Add Component: LLMAgent
         └─ Configure for remote (host=localhost, port=1234)
```

**3. Inspector Assignment**
```
LMStudioChatBot
├─ Chat Display: [Drag Text component]
├─ Input Field: [Drag InputField component]
├─ Send Button: [Drag Button component]
├─ Status Text: [Drag Text component]
└─ LLM Agent: [Drag LLMAgent component]
```

## Customization

### Change System Prompt

Edit the `SYSTEM_PROMPT` constant in `LMStudioChatBot.cs`:

```csharp
private const string SYSTEM_PROMPT = @"You are a creative writer. Your responses should be imaginative and engaging.";
```

### Adjust Generation Parameters

Modify the `LLMClient` component in the inspector:
- **Temperature**: 0.2 (deterministic) to 1.5 (creative)
- **Top P**: 0.9 (nucleus sampling)
- **Max Tokens**: Number of tokens to generate
- **Top K**: Limit to top K tokens

### Change Server Connection

If your LM Studio is on a different machine:

```csharp
llmClient.host = "192.168.1.100"; // Your server IP
llmClient.port = 1234;
```

## Features

- ✅ Streaming responses
- ✅ Conversation history
- ✅ Auto-scrolling UI
- ✅ Error handling
- ✅ Processing state management
- ✅ Easy to extend

## Troubleshooting

### "Server is not alive" error
1. Check LM Studio is running
2. Verify "Start Server" is clicked in Developer tab
3. Check localhost:1234 is accessible

### No response from model
1. Verify a model is loaded in LM Studio
2. Check GPU is being used (if available)
3. Try a different model

### Slow responses
1. Use a smaller quantized model (Q4, Q5)
2. Enable GPU acceleration in LM Studio
3. Reduce context/max tokens

## Next Steps

- Extend with RAG (Retrieval Augmented Generation)
- Add more system prompts for different personalities
- Implement conversation saving
- Add voice input/output
- Create a multi-turn conversation system

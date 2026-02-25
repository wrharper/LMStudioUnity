using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;
using LLMUnity;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LLMUnitySamples
{
    public class ChatBot : MonoBehaviour
    {
        public Transform chatContainer;
        public Color playerColor = new Color32(75, 70, 80, 255);
        public Color aiColor = new Color32(70, 80, 80, 255);
        public Color fontColor = Color.white;
        public Font font;
        public int fontSize = 16;
        public int bubbleWidth = 600;
        public LLMAgent llmAgent;
        public float textPadding = 10f;
        public float bubbleSpacing = 10f;
        public Sprite sprite;
        public Button stopButton;

        private InputBubble inputBubble;
        private List<Bubble> chatBubbles = new List<Bubble>();
        private bool blockInput = true;
        private BubbleUI playerUI, aiUI;
        private bool warmUpDone = false;
        private int lastBubbleOutsideFOV = -1;

        void Start()
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playerUI = new BubbleUI
            {
                sprite = sprite,
                font = font,
                fontSize = fontSize,
                fontColor = fontColor,
                bubbleColor = playerColor,
                bottomPosition = 0,
                leftPosition = 0,
                textPadding = textPadding,
                bubbleOffset = bubbleSpacing,
                bubbleWidth = bubbleWidth,
                bubbleHeight = -1
            };
            aiUI = playerUI;
            aiUI.bubbleColor = aiColor;
            aiUI.leftPosition = 1;

            inputBubble = new InputBubble(chatContainer, playerUI, "InputBubble", "", 4);
            inputBubble.AddSubmitListener(OnInputFieldSubmit);
            inputBubble.AddValueChangedListener(OnValueChanged);
            inputBubble.setInteractable(false);
            stopButton.gameObject.SetActive(true);
            
            // Set initial button state based on warmup
            Button[] buttons = stopButton.GetComponentsInParent<Button>();
            foreach (var btn in buttons)
            {
                // Disable all send-like buttons until warmup completes
                if (btn.name.Contains("Send") || btn.name.Contains("submit"))
                    btn.interactable = false;
            }
            
            ShowLoadedMessages();
            
            // Start warmup - this enables input when complete
            StartCoroutine(InitializeWithWarmup());
        }

        private System.Collections.IEnumerator InitializeWithWarmup()
        {
            Debug.Log("[ChatBot] Starting LLMAgent warmup...");
            bool warmupCompleted = false;
            
            // Call warmup with callback
            llmAgent.Warmup(() => 
            {
                Debug.Log("[ChatBot] Warmup callback executed");
                warmupCompleted = true;
                WarmUpCallback();
            });
            
            // Wait for warmup to complete with timeout
            float timeout = Time.time + 15f; // 15 second timeout
            while (!warmupCompleted && Time.time < timeout)
            {
                yield return null;
            }
            
            if (!warmupCompleted)
            {
                Debug.LogWarning("[ChatBot] Warmup timeout - enabling input anyway");
                WarmUpCallback();
            }
        }

        Bubble AddBubble(string message, bool isPlayerMessage)
        {
            Bubble bubble = new Bubble(chatContainer, isPlayerMessage ? playerUI : aiUI, isPlayerMessage ? "PlayerBubble" : "AIBubble", message);
            chatBubbles.Add(bubble);
            bubble.OnResize(UpdateBubblePositions);
            return bubble;
        }

        void ShowLoadedMessages()
        {
            for (int i = 1; i < llmAgent.chat.Count; i++) AddBubble(llmAgent.chat[i].content, i % 2 == 1);
        }

        void OnInputFieldSubmit(string newText)
        {
            inputBubble.ActivateInputField();
#if ENABLE_INPUT_SYSTEM
            // new input system for latest Unity version
            bool shiftHeld = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
            // old input system
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
            if (blockInput || newText.Trim() == "" || shiftHeld)
            {
                StartCoroutine(BlockInteraction());
                return;
            }
            blockInput = true;
            // replace vertical_tab
            string message = inputBubble.GetText().Replace("\v", "\n");

            AddBubble(message, true);
            Bubble aiBubble = AddBubble("...", false);
            Task chatTask = llmAgent.Chat(message, aiBubble.SetText, AllowInput);
            inputBubble.SetText("");
        }

        public void WarmUpCallback()
        {
            warmUpDone = true;
            inputBubble.SetPlaceHolderText("Message me");
            AllowInput();
        }

        public void AllowInput()
        {
            blockInput = false;
            inputBubble.setInteractable(true);  // Show placeholder and enable interactivity
            inputBubble.ReActivateInputField();
        }

        public void CancelRequests()
        {
            llmAgent.CancelRequests();
            AllowInput();
        }

        IEnumerator BlockInteraction()
        {
            // prevent from change until next frame
            inputBubble.setInteractable(false);
            yield return null;
            inputBubble.setInteractable(true);
            // change the caret position to the end of the text
            inputBubble.MoveTextEnd();
        }

        void OnValueChanged(string newText)
        {
            // Remove newline added by Enter
#if ENABLE_INPUT_SYSTEM
            // new input system for latest Unity version
            bool enterPressed = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
#else
            // old input system
            bool enterPressed = Input.GetKey(KeyCode.Return);
#endif
            if (enterPressed)
            {
                if (inputBubble.GetText().Trim() == "")
                    inputBubble.SetText("");
            }
        }

        public void UpdateBubblePositions()
        {
            float y = inputBubble.GetSize().y + inputBubble.GetRectTransform().offsetMin.y + bubbleSpacing;
            float containerHeight = chatContainer.GetComponent<RectTransform>().rect.height;
            for (int i = chatBubbles.Count - 1; i >= 0; i--)
            {
                Bubble bubble = chatBubbles[i];
                RectTransform childRect = bubble.GetRectTransform();
                childRect.anchoredPosition = new Vector2(childRect.anchoredPosition.x, y);

                // last bubble outside the container
                if (y > containerHeight && lastBubbleOutsideFOV == -1)
                {
                    lastBubbleOutsideFOV = i;
                }
                y += bubble.GetSize().y + bubbleSpacing;
            }
        }

        void Update()
        {
            // Only auto-focus if warmup is done and input is enabled
            if (warmUpDone && !blockInput && !inputBubble.inputFocused())
            {
                inputBubble.ActivateInputField();
                StartCoroutine(BlockInteraction());
            }
            
            // Destroy bubbles outside the container
            if (lastBubbleOutsideFOV != -1)
            {
                for (int i = 0; i <= lastBubbleOutsideFOV; i++)
                {
                    chatBubbles[i].Destroy();
                }
                chatBubbles.RemoveRange(0, lastBubbleOutsideFOV + 1);
                lastBubbleOutsideFOV = -1;
            }
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }

        bool onValidateWarning = true;
        void OnValidate()
        {
            if (onValidateWarning && !llmAgent.remote && llmAgent.llm != null && llmAgent.llm.model == "")
            {
                Debug.LogWarning($"Please select a model in the {llmAgent.llm.gameObject.name} GameObject!");
                onValidateWarning = false;
            }
        }
    }
}

/// <summary>
/// Example demonstrating LORA adapter usage with LM Studio backend.
/// Shows how to automatically merge LORA adapters for use with LM Studio.
/// </summary>
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnity.Samples
{
    public class LMStudioLoraDemo : MonoBehaviour
    {
        [System.Serializable]
        public class LoraConfig
        {
            public string name;
            public string path;
            public float weight = 0.8f;
        }

        [SerializeField]
        private LLMAgent llmAgent;

        [SerializeField]
        private LoraConfig[] availableLoras = new LoraConfig[]
        {
            new LoraConfig { name = "Generic", path = "" },
            new LoraConfig { name = "Chemistry Expert", path = "Models/chemistry-lora.gguf", weight = 0.8f },
            new LoraConfig { name = "Math Expert", path = "Models/math-lora.gguf", weight = 0.9f },
            new LoraConfig { name = "Combined (Chem + Math)", path = "Models/chemistry-lora.gguf", weight = 0.7f }
        };

        [SerializeField]
        private Dropdown loraSelector;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private Text responseText;

        private int currentLoraIndex = 0;
        private bool isMerging = false;

        private void Start()
        {
            if (loraSelector != null)
            {
                loraSelector.ClearOptions();
                var options = new List<string>();
                foreach (var lora in availableLoras)
                {
                    options.Add(lora.name);
                }
                loraSelector.AddOptions(options);
                loraSelector.onValueChanged.AddListener(OnLoraSelected);
            }

            UpdateStatusText("Initializing...");
        }

        /// <summary>
        /// Called when user selects a different LORA configuration
        /// </summary>
        private async void OnLoraSelected(int index)
        {
            currentLoraIndex = index;
            await PrepareLoraConfiguration(index);
        }

        /// <summary>
        /// Prepares and merges LORA adapters for the selected configuration
        /// </summary>
        private async Task PrepareLoraConfiguration(int loraIndex)
        {
            if (isMerging)
            {
                UpdateStatusText("⏳ Already merging, please wait...");
                return;
            }

            isMerging = true;

            try
            {
                var config = availableLoras[loraIndex];
                
                if (string.IsNullOrEmpty(config.path))
                {
                    UpdateStatusText($"✓ Using base model (no LORA)");
                    isMerging = false;
                    return;
                }

                UpdateStatusText($"⏳ Preparing {config.name}...");
                Debug.Log($"Starting LORA merge for: {config.name}");

                // Get the model path from LLMAgent
                string baseModelPath = llmAgent.model;
                if (string.IsNullOrEmpty(baseModelPath))
                {
                    UpdateStatusText("❌ No model configured in LLMAgent");
                    isMerging = false;
                    return;
                }

                // Prepare LORA list
                var loras = new List<(string path, float weight)>
                {
                    (config.path, config.weight)
                };

                // Use the preprocessor to merge
                string mergedPath = await LoraPreprocessor.MergeLorasIntoModel(
                    baseModelPath,
                    loras,
                    (progress) => UpdateStatusText($"⏳ Merging... {progress * 100:F0}%")
                );

                if (string.IsNullOrEmpty(mergedPath))
                {
                    UpdateStatusText("❌ LORA merge failed. Check console for details.");
                    isMerging = false;
                    return;
                }

                // Update the agent to use the merged model
                llmAgent.model = mergedPath;
                UpdateStatusText($"✓ {config.name} ready!\nTesting with a prompt...");

                // Test the LORA with a sample prompt
                await TestLoraWithPrompt(config.name);
            }
            catch (System.Exception ex)
            {
                UpdateStatusText($"❌ Error: {ex.Message}");
                Debug.LogError($"LORA preparation failed: {ex}");
            }
            finally
            {
                isMerging = false;
            }
        }

        /// <summary>
        /// Tests the LORA configuration by running a test prompt
        /// </summary>
        private async Task TestLoraWithPrompt(string loraName)
        {
            try
            {
                string testPrompt = loraName switch
                {
                    "Chemistry Expert" => "Explain the process of photosynthesis in plant cells.\n\nAnswer:",
                    "Math Expert" => "What is the derivative of x^3 + 2x?\n\nAnswer:",
                    "Combined (Chem + Math)" => "Calculate the kinetic energy of a moving object.\n\nAnswer:",
                    _ => "What is machine learning?\n\nAnswer:"
                };

                responseText.text = "Testing LORA configuration...";
                UpdateStatusText($"⏳ Running test prompt with {loraName}...");

                var reply = await llmAgent.Chat(testPrompt);

                if (!string.IsNullOrEmpty(reply.text))
                {
                    responseText.text = $"[{loraName}]\n{reply.text}";
                    UpdateStatusText($"✓ {loraName} is working!");
                }
                else
                {
                    UpdateStatusText($"❌ No response from model");
                }
            }
            catch (System.Exception ex)
            {
                UpdateStatusText($"❌ Test failed: {ex.Message}");
                Debug.LogError($"Test prompt failed: {ex}");
            }
        }

        /// <summary>
        /// Shows statistics about cached merged models
        /// </summary>
        public void ShowCacheStats()
        {
            var cached = LoraPreprocessor.GetCachedMergedModels();
            Debug.Log($"Cached merged models: {cached.Count}");
            foreach (var model in cached)
            {
                Debug.Log($"  - {System.IO.Path.GetFileName(model)}");
            }
        }

        /// <summary>
        /// Clears all cached merged models to free disk space
        /// </summary>
        public void ClearCache()
        {
            LoraPreprocessor.ClearMergedModelCache();
            UpdateStatusText("Cache cleared. LORA models will be re-merged on next use.");
            Debug.Log("LORA merge cache cleared");
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log(message);
        }

        /// <summary>
        /// UI button callback for running a custom prompt
        /// </summary>
        public async void RunCustomPrompt(InputField promptInput)
        {
            if (string.IsNullOrEmpty(promptInput.text))
            {
                UpdateStatusText("Please enter a prompt");
                return;
            }

            if (isMerging)
            {
                UpdateStatusText("Still preparing LORA, please wait...");
                return;
            }

            try
            {
                UpdateStatusText("⏳ Generating response...");
                var reply = await llmAgent.Chat(promptInput.text);
                
                if (!string.IsNullOrEmpty(reply.text))
                {
                    responseText.text = reply.text;
                    UpdateStatusText("✓ Done!");
                }
                else
                {
                    UpdateStatusText("❌ Empty response from model");
                }
            }
            catch (System.Exception ex)
            {
                UpdateStatusText($"❌ Error: {ex.Message}");
                Debug.LogError($"Prompt failed: {ex}");
            }
        }
    }
}

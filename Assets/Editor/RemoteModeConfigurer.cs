using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LLMUnity;

namespace LLMUnityTools
{
    /// <summary>
    /// Automatically configures LLMAgent components for remote LM Studio mode.
    /// Run this via: Tools > LLMUnity > Configure for Remote Mode
    /// </summary>
    public class RemoteModeConfigurer
    {
        [MenuItem("Tools/LLMUnity/Configure Current Scene for Remote Mode")]
        public static void ConfigureSceneForRemote()
        {
            // Get all LLMAgent components in the current scene
            LLMAgent[] agents = UnityEngine.Object.FindObjectsOfType<LLMAgent>();
            
            if (agents.Length == 0)
            {
                EditorUtility.DisplayDialog("No LLMAgent Found", 
                    "Could not find any LLMAgent components in the current scene.", "OK");
                return;
            }

            int configured = 0;
            foreach (LLMAgent agent in agents)
            {
                // Configure for remote mode
                agent.remote = true;
                agent.host = "localhost";
                agent.port = 1234;
                
                // Clear local LLM reference (not needed for remote)
                if (agent.llm != null)
                {
                    // Disable the LLM GameObject so it doesn't initialize locally
                    agent.llm.gameObject.SetActive(false);
                    agent.llm = null;
                    Debug.Log($"[RemoteModeConfigurer] Disabled local LLM GameObject");
                }
                
                Debug.Log($"[RemoteModeConfigurer] Configured {agent.gameObject.name} for remote LM Studio mode (localhost:1234)");
                configured++;
            }

            // Mark scene as dirty and save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Configuration Complete", 
                $"Successfully configured {configured} LLMAgent(s) for remote mode.\n\n" +
                "✓ Remote mode enabled\n" +
                "✓ Local LLM GameObject disabled\n\n" +
                "Make sure LM Studio is running on localhost:1234 before playing the scene.", "OK");
        }

        [MenuItem("Tools/LLMUnity/Configure All Scenes for Remote Mode")]
        public static void ConfigureAllScenesForRemote()
        {
            // Get all scene files
            string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
            
            if (sceneGUIDs.Length == 0)
            {
                EditorUtility.DisplayDialog("No Scenes Found", 
                    "Could not find any scene files in the project.", "OK");
                return;
            }

            int totalConfigured = 0;

            foreach (string sceneGUID in sceneGUIDs)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGUID);
                
                // Load the scene
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                // Configure all LLMAgent components in this scene
                LLMAgent[] agents = UnityEngine.Object.FindObjectsOfType<LLMAgent>();
                foreach (LLMAgent agent in agents)
                {
                    agent.remote = true;
                    agent.host = "localhost";
                    agent.port = 1234;
                    
                    if (agent.llm != null)
                    {
                        agent.llm.gameObject.SetActive(false);
                        agent.llm = null;
                    }
                    
                    Debug.Log($"[RemoteModeConfigurer] Configured {agent.gameObject.name} in scene {scenePath}");
                    totalConfigured++;
                }

                // Save the scene if there were modifications
                if (agents.Length > 0)
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                }
            }

            EditorUtility.DisplayDialog("Batch Configuration Complete", 
                $"Successfully configured {totalConfigured} LLMAgent(s) across {sceneGUIDs.Length} scene(s) for remote mode.\n\n" +
                "Make sure LM Studio is running on localhost:1234 before playing any scenes.", "OK");
        }

        [MenuItem("Tools/LLMUnity/Configure for Remote Mode/Reset to Default (localhost:1234)")]
        public static void ResetRemoteDefaults()
        {
            // Get all LLMAgent components in the current scene
            LLMAgent[] agents = UnityEngine.Object.FindObjectsOfType<LLMAgent>();
            
            foreach (LLMAgent agent in agents)
            {
                agent.host = "localhost";
                agent.port = 1234;
                Debug.Log($"[RemoteModeConfigurer] Reset {agent.gameObject.name} to default LM Studio settings (localhost:1234)");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}

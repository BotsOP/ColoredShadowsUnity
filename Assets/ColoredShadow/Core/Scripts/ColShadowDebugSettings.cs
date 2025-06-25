using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

#if UNITY_EDITOR

// ScriptableObject to store the settings persistently
[CreateAssetMenu(fileName = "ColShadowDebugSettings", menuName = "ColShadow/Debug Settings")]
public class ColShadowDebugSettings : ScriptableObject
{
    [Header("Debug Settings")]
    public bool enableDebugMode = true;
    
    [Header("Debug Visual Settings")]
    public float customLightNumberSize = 4f;
    public float casterNumberSize = 2f;
    public float shadowNumberSize = 1f;
    
    [Header("Color Settings")]
    public Color casterNumberColor = Color.magenta;
    public Color customLightNumberColor = Color.green;

    // Static instance for easy access
    private static ColShadowDebugSettings _instance;
    public static ColShadowDebugSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ColShadowDebugSettings>("ColShadowDebugSettings");
                if (_instance == null)
                {
                    // Create default settings if none exist
                    _instance = CreateInstance<ColShadowDebugSettings>();
                    #if UNITY_EDITOR
                    // Save to Resources folder
                    string resourcesPath = "Assets/ColoredShadow/Core/Resources";
                    if (!AssetDatabase.IsValidFolder(resourcesPath))
                    {
                        AssetDatabase.CreateFolder("Assets/ColoredShadow/Core", "Resources");
                    }
                    AssetDatabase.CreateAsset(_instance, "Assets/ColoredShadow/Core/Resources/ColShadowDebugSettings.asset");
                    AssetDatabase.SaveAssets();
                    #endif
                }
            }
            return _instance;
        }
    }
    
    // Event for when settings change
    public System.Action OnSettingsChanged;
    
    void OnValidate()
    {
        OnSettingsChanged?.Invoke();
    }
}

// The custom editor window
public class ColShadowDebugWindow : EditorWindow
{
    private ColShadowDebugSettings settings;
    private Vector2 scrollPosition;
    
    // Menu item to open the window
    [MenuItem("Tools/ColoredShadow")]
    public static void ShowWindow()
    {
        ColShadowDebugWindow window = GetWindow<ColShadowDebugWindow>("ColShadow Debug");
        window.minSize = new Vector2(350, 300);
        window.Show();
    }
    
    void OnEnable()
    {
        settings = ColShadowDebugSettings.Instance;
    }
    
    void OnGUI()
    {
        if (settings == null)
        {
            EditorGUILayout.HelpBox("Settings not found. Creating default settings...", MessageType.Warning);
            settings = ColShadowDebugSettings.Instance;
            return;
        }
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Header
        EditorGUILayout.LabelField("ColShadow Debug Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Main toggle
        EditorGUI.BeginChangeCheck();
        
        settings.enableDebugMode = EditorGUILayout.Toggle("Enable Debug Mode", settings.enableDebugMode);
        
        EditorGUILayout.Space();
        
        // Float settings
        EditorGUILayout.LabelField("Float Values", EditorStyles.boldLabel);
        settings.customLightNumberSize = EditorGUILayout.FloatField("Custom Light Number Size", settings.customLightNumberSize);
        settings.casterNumberSize = EditorGUILayout.FloatField("Caster Number Size", settings.casterNumberSize);
        settings.shadowNumberSize = EditorGUILayout.FloatField("Shadow Number Size", settings.shadowNumberSize);
        
        EditorGUILayout.Space();
        
        // Color settings
        EditorGUILayout.LabelField("Color Settings", EditorStyles.boldLabel);
        settings.customLightNumberColor = EditorGUILayout.ColorField("Custom Light Number Color", settings.customLightNumberColor);
        settings.casterNumberColor = EditorGUILayout.ColorField("Caster Number Color", settings.casterNumberColor);
        
        EditorGUILayout.Space();

        // Utility buttons
        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset to Defaults"))
        {
            ResetToDefaults();
        }
        
        if (GUILayout.Button("Save Settings"))
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        EditorGUILayout.EndHorizontal();
        
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(settings);
            settings.OnSettingsChanged?.Invoke();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void ResetToDefaults()
    {
        settings.enableDebugMode = true;
        settings.customLightNumberSize = 4.0f;
        settings.casterNumberSize = 2.0f;
        settings.shadowNumberSize = 1.0f;
        settings.casterNumberColor = Color.magenta;
        settings.customLightNumberColor = Color.green;
        
        EditorUtility.SetDirty(settings);
        settings.OnSettingsChanged?.Invoke();
    } 
}

// Static utility class for easy access throughout the project
public static class ColShadowDebug
{
    public static bool IsEnabled
    {
        get
        {
#if UNITY_EDITOR
            return ColShadowDebugSettings.Instance.enableDebugMode;
#else
            return false;
#endif
        }
    }
    
    public static float CustomLightNumberSize
    {
        get
        {
#if UNITY_EDITOR
            return ColShadowDebugSettings.Instance.customLightNumberSize;
#else
            return 1.0f;
#endif
        }
    }

    public static float CasterNumberSize
    {
        get
        {
#if UNITY_EDITOR
            return ColShadowDebugSettings.Instance.casterNumberSize;
#else
            return 1.0f;
#endif
        }
    }

    public static Color CasterNumberColor
    {
        get
        {
#if UNITY_EDITOR
            return ColShadowDebugSettings.Instance.casterNumberColor;
#else
            return Color.black;
#endif
        }
    }

    public static Color CustomLightNumberColor
    {
        get
        {
#if UNITY_EDITOR
            return ColShadowDebugSettings.Instance.customLightNumberColor;
#else
            return Color.yellow;
#endif
        }
    }

    public static float ShadowNumberSize
    {
        get
        {
#if UNITY_EDITOR
            return ColShadowDebugSettings.Instance.shadowNumberSize;
#else
            return 1.0f;
#endif
        }
    }
}
#endif
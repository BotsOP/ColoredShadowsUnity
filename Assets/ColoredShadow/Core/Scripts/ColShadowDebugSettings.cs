using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

#if UNITY_EDITOR

// ScriptableObject to store the settings persistently
[CreateAssetMenu(fileName = "ColShadowDebugSettings", menuName = "ColShadow/Debug Settings")]
public class ColShadowDebugSettings : ScriptableObject
{
    [Header("Global Shadow Settings")] 
    public int amountShadowBlur = 0;
    public bool useDepthMask = true;
    public bool useBlurMask = true;
    public bool useUVs = true;
    public bool use8bitID = true;
    
    [Header("Debug Settings")]
    public bool enableDebugMode = true;
    
    [Header("Debug Visual Settings")]
    public float customLightNumberSize = 4f;
    public float casterNumberSize = 2f;
    public float shadowNumberSize = 1f;
    
    [Header("Color Settings")]
    public Color casterNumberColor = Color.magenta;
    public Color customLightNumberColor = Color.green;
    public Color shadowNumberColor = Color.white;

    // Static instance for easy access
    private static ColShadowDebugSettings _instance;
    public static ColShadowDebugSettings Instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            
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
        
        EditorGUILayout.LabelField("Global Shadow Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        settings.amountShadowBlur = EditorGUILayout.IntField("Amount Shadow Blur", settings.amountShadowBlur);
        
        EditorGUILayout.Space();
        
        // Header
        EditorGUILayout.LabelField("ColShadow Debug Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Main toggle
        EditorGUI.BeginChangeCheck();
        
        settings.enableDebugMode = EditorGUILayout.Toggle("Enable Debug Mode", settings.enableDebugMode);
        
        EditorGUILayout.Space();
        
        // Float settings
        settings.customLightNumberSize = EditorGUILayout.FloatField("Custom Light Number Size", settings.customLightNumberSize);
        settings.casterNumberSize = EditorGUILayout.FloatField("Caster Number Size", settings.casterNumberSize);
        settings.shadowNumberSize = EditorGUILayout.FloatField("Shadow Number Size", settings.shadowNumberSize);
        
        EditorGUILayout.Space();
        
        // Color settings
        settings.customLightNumberColor = EditorGUILayout.ColorField("Custom Light Number Color", settings.customLightNumberColor);
        settings.casterNumberColor = EditorGUILayout.ColorField("Caster Number Color", settings.casterNumberColor);
        settings.shadowNumberColor = EditorGUILayout.ColorField("Shadow Number Color", settings.shadowNumberColor);
        
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
        settings.shadowNumberColor = Color.white;
        
        EditorUtility.SetDirty(settings);
        settings.OnSettingsChanged?.Invoke();
    } 
}

// Static utility class for easy access throughout the project
public static class ColShadowSettings
{
    public static int AmountShadowBlur => ColShadowDebugSettings.Instance.amountShadowBlur;
    public static bool IsEnabled => ColShadowDebugSettings.Instance.enableDebugMode;

    public static float CustomLightNumberSize => ColShadowDebugSettings.Instance.customLightNumberSize;

    public static float CasterNumberSize => ColShadowDebugSettings.Instance.casterNumberSize;

    public static Color CasterNumberColor => ColShadowDebugSettings.Instance.casterNumberColor;

    public static Color CustomLightNumberColor => ColShadowDebugSettings.Instance.customLightNumberColor;

    public static Color ShadowNumberColor => ColShadowDebugSettings.Instance.shadowNumberColor;

    public static float ShadowNumberSize => ColShadowDebugSettings.Instance.shadowNumberSize;

    public static GraphicsFormat ShadowMapFormat
    {
        get
        {
            uint formatUint = GetFormatUint();

            return graphicsFormats[formatUint];
        }
    }
    
    public static ShadowFormat shadowFormat
    {
        get
        {
            uint formatUint = GetFormatUint();

            return (ShadowFormat)formatUint;
        }
    }
    
    private static uint GetFormatUint()
    {
        uint formatUint = 0;
        formatUint |= (uint)(ColShadowDebugSettings.Instance.useUVs ? 1 : 0);
        formatUint |= (uint)(ColShadowDebugSettings.Instance.useDepthMask ? 2 : 0);
        formatUint |= (uint)(ColShadowDebugSettings.Instance.useBlurMask ? 4 : 0);
        if (formatUint == 0)
        {
            formatUint |= (uint)(ColShadowDebugSettings.Instance.use8bitID ? 8 : 0);
        }
        return formatUint;
    }

    private static GraphicsFormat[] graphicsFormats = {
        GraphicsFormat.R16_SFloat,
        GraphicsFormat.R32G32_SFloat,
        GraphicsFormat.R16G16_SFloat,
        GraphicsFormat.R32G32B32_SFloat,
        GraphicsFormat.R16G16_SFloat,
        GraphicsFormat.R32G32B32_SFloat,
        GraphicsFormat.R16G16B16_SFloat,
        GraphicsFormat.R32G32B32_SFloat,
        GraphicsFormat.R8_UInt,
    };
}
#endif


public enum ShadowFormat
{
    ID16, //0000
    ID16UV48, //0001
    ID16Depth16, //0010
    ID16UV48Depth32, // 0011
    ID16Blur16, // 0100
    ID16UV48Blur32, // 0101
    ID16Depth16Blur16, // 0110
    ID16UV48Depth16Blur16, // 0111
    ID8, // 1000
}














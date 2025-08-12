using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ColoredShadows.Scripts
{
    [CustomEditor(typeof(CustomLight))]
    public class CustomLightEditor : Editor
    {
        private bool showAdvancedSettings = false;
        private bool showVFXSettings = false;
        
        // Serialized Properties
        private SerializedProperty lightIndexProp;
        private SerializedProperty lightModeProp;
        private SerializedProperty radiusProp;
        private SerializedProperty farPlaneProp;
        private SerializedProperty sizeProp;
        private SerializedProperty fovProp;
        private SerializedProperty aspectRatioProp;
        private SerializedProperty fallOffRangeProp;
        private SerializedProperty addShadowIDProp;
        private SerializedProperty shadowTextureSizeProp;
        private SerializedProperty overrideShaderProp;
        private SerializedProperty shadowCastingMaskProp;
        private SerializedProperty shadowReceivingMaskProp;
        private SerializedProperty customValuesProp;
        private SerializedProperty enableVFXSupportProp;
        private SerializedProperty visualEffectsProp;
        private SerializedProperty vfxSamplingSizeProp;
        private SerializedProperty vfxUVSizeProp;
        private SerializedProperty relativeUVSizeProp;
        private SerializedProperty blockPassthroughShadowsProp;
        
        void OnEnable()
        {
            // Find all serialized properties
            lightIndexProp = serializedObject.FindProperty("lightIndex");
            lightModeProp = serializedObject.FindProperty("lightMode");
            radiusProp = serializedObject.FindProperty("radius");
            farPlaneProp = serializedObject.FindProperty("farPlane");
            sizeProp = serializedObject.FindProperty("size");
            fovProp = serializedObject.FindProperty("fov");
            aspectRatioProp = serializedObject.FindProperty("aspectRatio");
            fallOffRangeProp = serializedObject.FindProperty("fallOffRange");
            addShadowIDProp = serializedObject.FindProperty("addToShadowID");
            shadowTextureSizeProp = serializedObject.FindProperty("shadowTextureSize");
            overrideShaderProp = serializedObject.FindProperty("overrideShader");
            shadowCastingMaskProp = serializedObject.FindProperty("shadowCastingMask");
            shadowReceivingMaskProp = serializedObject.FindProperty("shadowReceivingMask");
            customValuesProp = serializedObject.FindProperty("customValues");
            enableVFXSupportProp = serializedObject.FindProperty("enableVFXSupport");
            visualEffectsProp = serializedObject.FindProperty("visualEffects");
            vfxSamplingSizeProp = serializedObject.FindProperty("vfxSamplingSize");
            vfxUVSizeProp = serializedObject.FindProperty("vfxUVSize");
            relativeUVSizeProp = serializedObject.FindProperty("relativeUVSize");
            blockPassthroughShadowsProp = serializedObject.FindProperty("blockPassthroughShadows");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            GUI.enabled = false;
            EditorGUILayout.PropertyField(lightIndexProp);
            GUI.enabled = true;
            
            EditorGUILayout.PropertyField(lightModeProp);
            
            EditorGUILayout.Space(5);
            
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;
            
            LightMode currentMode = (LightMode)lightModeProp.enumValueIndex;
            
            switch (currentMode)
            {
                case LightMode.Point:
                    EditorGUILayout.LabelField("Point Light Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(radiusProp);
                    EditorGUILayout.PropertyField(fallOffRangeProp);
                    break;
                    
                case LightMode.Spot:
                    EditorGUILayout.LabelField("Spot Light Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(farPlaneProp);
                    EditorGUILayout.PropertyField(fovProp);
                    EditorGUILayout.PropertyField(aspectRatioProp);
                    EditorGUILayout.PropertyField(fallOffRangeProp);
                    break;
                    
                case LightMode.Directional:
                    EditorGUILayout.LabelField("Directional Light Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(farPlaneProp);
                    EditorGUILayout.PropertyField(sizeProp);
                    break;
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Advanced Settings Foldout
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true, EditorStyles.foldoutHeader);
            
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(blockPassthroughShadowsProp);
                EditorGUILayout.PropertyField(addShadowIDProp);
                if (currentMode == LightMode.Point)
                {
                    shadowTextureSizeProp.intValue = Mathf.Clamp(shadowTextureSizeProp.intValue, 1, 2730);
                }
                else
                {
                    shadowTextureSizeProp.intValue = Mathf.Clamp(shadowTextureSizeProp.intValue, 1, 16384);
                }
                EditorGUILayout.PropertyField(shadowTextureSizeProp);
                EditorGUILayout.PropertyField(overrideShaderProp);
                EditorGUILayout.PropertyField(shadowCastingMaskProp);
                
                // Custom Values List - Standard Unity List View
                EditorGUILayout.Space(5);
                if (customValuesProp.arraySize > 12)
                {
                    customValuesProp.arraySize = 12;
                }
            
                // Draw the property field
                EditorGUILayout.PropertyField(customValuesProp, true);
            
                // Check again after drawing in case user tried to add more
                if (customValuesProp.arraySize > 12)
                {
                    customValuesProp.arraySize = 12;
                    Debug.LogError($"Can't have more than 12 entries of custom values");
                }
                
                EditorGUI.indentLevel--;
            }
            
            showVFXSettings = EditorGUILayout.Foldout(showVFXSettings, "VFX Settings", true, EditorStyles.foldoutHeader);

            if (showVFXSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(enableVFXSupportProp);
                GUI.enabled = enableVFXSupportProp.boolValue;
                
                int maxValue = shadowTextureSizeProp.intValue;
                int sliderValue = EditorGUILayout.IntSlider(
                    vfxSamplingSizeProp.displayName,
                    vfxSamplingSizeProp.intValue,
                    1,
                    maxValue
                );
                vfxSamplingSizeProp.intValue = Mathf.ClosestPowerOfTwo(sliderValue);
                EditorGUILayout.PropertyField(vfxUVSizeProp);
                EditorGUILayout.PropertyField(relativeUVSizeProp);
                
                EditorGUILayout.PropertyField(shadowReceivingMaskProp);
                EditorGUILayout.PropertyField(visualEffectsProp);
                GUI.enabled = true;
            }
            
            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }
}

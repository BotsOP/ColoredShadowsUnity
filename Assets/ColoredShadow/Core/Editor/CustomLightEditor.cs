using UnityEngine;
using UnityEditor;

namespace ColoredShadows.Scripts
{
    [CustomEditor(typeof(CustomLight))]
    public class CustomLightEditor : Editor
    {
        private bool showAdvancedSettings = false;
        
        public override void OnInspectorGUI()
        {
            CustomLight customLight = (CustomLight)target;
            
            // Light Index (read-only)
            GUI.enabled = false;
            EditorGUILayout.IntField("Light Index", customLight.lightIndex);
            GUI.enabled = true;
            
            // Light Mode
            customLight.lightMode = (LightMode)EditorGUILayout.EnumPopup("Light Mode", customLight.lightMode);
            
            // Light Mode dependent properties in grey rect
            EditorGUILayout.Space(5);
            
            // Create grey background
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.3f);
            
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = originalColor;
            
            switch (customLight.lightMode)
            {
                case LightMode.Point:
                    EditorGUILayout.LabelField("Point Light Settings", EditorStyles.boldLabel);
                    customLight.radius = EditorGUILayout.FloatField("Radius", customLight.radius);
                    customLight.fallOffRange = EditorGUILayout.FloatField("Fall Off Range", customLight.fallOffRange);
                    break;
                    
                case LightMode.Spot:
                    EditorGUILayout.LabelField("Spot Light Settings", EditorStyles.boldLabel);
                    customLight.farPlane = EditorGUILayout.FloatField("Far Plane", customLight.farPlane);
                    customLight.fov = EditorGUILayout.FloatField("FOV", customLight.fov);
                    customLight.aspectRatio = EditorGUILayout.FloatField("Aspect Ratio", customLight.aspectRatio);
                    break;
                    
                case LightMode.Directional:
                    EditorGUILayout.LabelField("Directional Light Settings", EditorStyles.boldLabel);
                    customLight.farPlane = EditorGUILayout.FloatField("Far Plane", customLight.farPlane);
                    customLight.size = EditorGUILayout.FloatField("Size", customLight.size);
                    break;
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // Advanced Settings Foldout
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true, EditorStyles.foldoutHeader);
            
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                customLight.addShadowID = EditorGUILayout.IntField("Add Shadow ID", customLight.addShadowID);
                customLight.shadowTextureSize = EditorGUILayout.IntField("Shadow Texture Size", customLight.shadowTextureSize);
                customLight.layerMask = EditorGUILayout.LayerField("Layer Mask", customLight.layerMask);
                customLight.overrideShader = (Shader)EditorGUILayout.ObjectField("Override Shader", customLight.overrideShader, typeof(Shader), false);
                
                // Custom Values List
                EditorGUILayout.Space(5);
                SerializedProperty customValuesProperty = serializedObject.FindProperty("customValues");
                EditorGUILayout.PropertyField(customValuesProperty, true);

                
                EditorGUI.indentLevel--;
            }
            
            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }
}

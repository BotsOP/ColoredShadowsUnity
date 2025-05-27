using ColoredShadows.Scripts;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(CustomLightData))]
public class CustomLightDataPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get properties
        var lightModeProperty = property.FindPropertyRelative("lightMode");
        var radiusProperty = property.FindPropertyRelative("radius");
        var nearPlaneProperty = property.FindPropertyRelative("nearPlane");
        var farPlaneProperty = property.FindPropertyRelative("farPlane");
        var horizontalSizeProperty = property.FindPropertyRelative("horizontalSize");
        var verticalSizeProperty = property.FindPropertyRelative("verticalSize");
        var fovProperty = property.FindPropertyRelative("fov");
        var aspectRatioProperty = property.FindPropertyRelative("aspectRatio");
        var fallOffRangeProperty = property.FindPropertyRelative("fallOffRange");

        // Calculate positions
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        Rect currentRect = new Rect(position.x, position.y, position.width, lineHeight);

        // Draw foldout
        property.isExpanded = EditorGUI.Foldout(currentRect, property.isExpanded, label, true);
        currentRect.y += lineHeight + spacing;

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // Always display lightMode
            EditorGUI.PropertyField(currentRect, lightModeProperty);
            currentRect.y += lineHeight + spacing;

            // Always display nearPlane and farPlane
            EditorGUI.PropertyField(currentRect, nearPlaneProperty);
            currentRect.y += lineHeight + spacing;

            EditorGUI.PropertyField(currentRect, farPlaneProperty);
            currentRect.y += lineHeight + spacing;

            // Display fields based on light mode
            CustomLightData.LightMode lightMode = (CustomLightData.LightMode)lightModeProperty.enumValueIndex;

            switch (lightMode)
            {
                case CustomLightData.LightMode.Directional:
                    EditorGUI.PropertyField(currentRect, horizontalSizeProperty);
                    currentRect.y += lineHeight + spacing;
                    EditorGUI.PropertyField(currentRect, verticalSizeProperty);
                    break;

                case CustomLightData.LightMode.Spot:
                    EditorGUI.PropertyField(currentRect, fovProperty);
                    currentRect.y += lineHeight + spacing;
                    EditorGUI.PropertyField(currentRect, aspectRatioProperty);
                    currentRect.y += lineHeight + spacing;
                    EditorGUI.PropertyField(currentRect, fallOffRangeProperty);
                    break;

                case CustomLightData.LightMode.Point:
                    EditorGUI.PropertyField(currentRect, radiusProperty);
                    currentRect.y += lineHeight + spacing;
                    EditorGUI.PropertyField(currentRect, fallOffRangeProperty);
                    break;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Base height: foldout + lightMode + nearPlane + farPlane
        float height = (lineHeight + spacing) * 4;

        // Add height based on light mode
        var lightModeProperty = property.FindPropertyRelative("lightMode");
        CustomLightData.LightMode lightMode = (CustomLightData.LightMode)lightModeProperty.enumValueIndex;

        switch (lightMode)
        {
            case CustomLightData.LightMode.Directional:
                height += (lineHeight + spacing) * 2; // horizontalSize + verticalSize
                break;
            case CustomLightData.LightMode.Spot:
                height += (lineHeight + spacing) * 3; // fov + aspectRatio + fallOffRange
                break;
            case CustomLightData.LightMode.Point:
                height += (lineHeight + spacing) * 2; // radius + fallOffRange
                break;
        }

        return height - spacing; // Remove last spacing
    }
}
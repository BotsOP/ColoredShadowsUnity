using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(CustomLightData))]
public class CustomLightDataDrawer : PropertyDrawer
{
    // Amount of extra space to add where needed
    private const float EXTRA_SPACING = 20f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get references to all properties
        SerializedProperty lightModeProperty = property.FindPropertyRelative("lightMode");
        SerializedProperty radiusProperty = property.FindPropertyRelative("radius");
        SerializedProperty nearPlaneProperty = property.FindPropertyRelative("nearPlane");
        SerializedProperty farPlaneProperty = property.FindPropertyRelative("farPlane");
        SerializedProperty horizontalSizeProperty = property.FindPropertyRelative("horizontalSize");
        SerializedProperty verticalSizeProperty = property.FindPropertyRelative("verticalSize");
        SerializedProperty fovProperty = property.FindPropertyRelative("fov");
        SerializedProperty aspectRatioProperty = property.FindPropertyRelative("aspectRatio");

        // Calculate rects for each property field
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        
        // Add light mode field
        Rect lightModeRect = new Rect(position.x, position.y, position.width, lineHeight);
        EditorGUI.PropertyField(lightModeRect, lightModeProperty, new GUIContent("Light Mode"));
        
        // Add extra space after light mode
        float currentY = lightModeRect.y + lineHeight + spacing + EXTRA_SPACING;
        
        // Calculate rect for the grey box
        float greyBoxPadding = 5f;
        float conditionalSectionHeight = (lineHeight * 2) + spacing + (greyBoxPadding * 2);
        Rect greyBoxRect = new Rect(
            position.x, 
            currentY, 
            position.width, 
            conditionalSectionHeight
        );

        // Calculate rects for conditional fields with padding
        Rect firstConditionalRect = new Rect(
            position.x + greyBoxPadding, 
            greyBoxRect.y + greyBoxPadding, 
            position.width - (greyBoxPadding * 2), 
            lineHeight
        );
        
        Rect secondConditionalRect = new Rect(
            position.x + greyBoxPadding, 
            firstConditionalRect.y + lineHeight + spacing, 
            position.width - (greyBoxPadding * 2), 
            lineHeight
        );

        // Get the current light mode enum value (0 = Ortho, 1 = FOV)
        CustomLightData.LightMode currentMode = (CustomLightData.LightMode)lightModeProperty.enumValueIndex;

        // Draw conditional properties based on light mode
        if (currentMode == CustomLightData.LightMode.Directional)
        {
            EditorGUI.DrawRect(greyBoxRect, new Color(0.7f, 0.7f, 0.7f, 0.3f));

            // Draw label for the section
            EditorGUI.LabelField(
                new Rect(greyBoxRect.x + 3, greyBoxRect.y - lineHeight, position.width, lineHeight),
                "Orthographic Properties",
                EditorStyles.boldLabel
            );
            
            // Display Ortho-specific fields
            EditorGUI.PropertyField(firstConditionalRect, horizontalSizeProperty, new GUIContent("Horizontal Size"));
            EditorGUI.PropertyField(secondConditionalRect, verticalSizeProperty, new GUIContent("Vertical Size"));
            
            currentY = secondConditionalRect.y + lineHeight + spacing + EXTRA_SPACING;
        
            // Add near and far plane fields
            Rect nearPlaneRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(nearPlaneRect, nearPlaneProperty, new GUIContent("Near Plane"));
        
            currentY = nearPlaneRect.y + lineHeight + spacing;
        
            Rect farPlaneRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(farPlaneRect, farPlaneProperty, new GUIContent("Far Plane"));
        }
        else if(currentMode == CustomLightData.LightMode.Spot)
        {
            EditorGUI.DrawRect(greyBoxRect, new Color(0.7f, 0.7f, 0.7f, 0.3f));

            // Draw label for the section
            EditorGUI.LabelField(
                new Rect(greyBoxRect.x + 3, greyBoxRect.y - lineHeight, position.width, lineHeight),
                "Perspective Properties",
                EditorStyles.boldLabel
            );
            
            // Display FOV-specific fields
            EditorGUI.PropertyField(firstConditionalRect, fovProperty, new GUIContent("Field of View"));
            EditorGUI.PropertyField(secondConditionalRect, aspectRatioProperty, new GUIContent("Aspect Ratio"));
            
            currentY = secondConditionalRect.y + lineHeight + spacing + EXTRA_SPACING;
        
            // Add near and far plane fields
            Rect nearPlaneRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(nearPlaneRect, nearPlaneProperty, new GUIContent("Near Plane"));
        
            currentY = nearPlaneRect.y + lineHeight + spacing;
        
            Rect farPlaneRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(farPlaneRect, farPlaneProperty, new GUIContent("Far Plane"));
        }
        else
        {
            EditorGUI.LabelField(
                new Rect(greyBoxRect.x + 3, greyBoxRect.y - lineHeight, position.width, lineHeight),
                "Point Properties",
                EditorStyles.boldLabel
            );
            EditorGUI.PropertyField(firstConditionalRect, radiusProperty, new GUIContent("Radius"));
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float padding = 5f;
        
        // Calculate total height including extra spacing
        return (lineHeight * 3) + // Three base fields
               (spacing * 3) +    // Standard spacing between those fields 
               (EXTRA_SPACING * 2) + // Extra space after light mode and before grey box
               ((lineHeight * 2) + spacing + (padding * 2)); // Grey box with content
    }
}
using Attributes;
using UnityEditor;
using UnityEngine;

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Save the original label width
            float originalLabelWidth = EditorGUIUtility.labelWidth;
        
            // For boolean properties, reduce the label width to prevent overlap
            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                EditorGUIUtility.labelWidth = position.width - 20; // Leave space for checkbox
            }
        
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        
            // Restore the original label width
            EditorGUIUtility.labelWidth = originalLabelWidth;
        }
    
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
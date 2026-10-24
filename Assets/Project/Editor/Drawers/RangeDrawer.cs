using LumberJack.Core;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// Desenha <see cref="FloatRange"/>/<see cref="IntRange"/> numa linha: [min] [slider] [max] quando o campo
    /// tem <see cref="RangeLimitsAttribute"/>, ou só [min] [max] sem ele.
    /// </summary>
    [CustomPropertyDrawer(typeof(FloatRange))]
    [CustomPropertyDrawer(typeof(IntRange))]
    public class RangeDrawer : PropertyDrawer
    {
        private const string MinField = "_min";
        private const string MaxField = "_max";
        private const float NumberWidth = 48f;
        private const float Spacing = 4f;

        // O atributo do campo não muda: lê por reflexão uma vez, não a cada repintura do inspector
        private RangeLimitsAttribute _limits;
        private bool _hasLimits;
        private bool _hasReadLimits;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect content = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            SerializedProperty min = property.FindPropertyRelative(MinField);
            SerializedProperty max = property.FindPropertyRelative(MaxField);
            if (!_hasReadLimits) ReadLimits();

            if (_hasLimits) DrawWithSlider(content, min, max, _limits);
            else DrawFields(content, min, max);

            EditorGUI.indentLevel = previousIndent;
            EditorGUI.EndProperty();
        }

        private void ReadLimits()
        {
            _limits = (RangeLimitsAttribute)System.Attribute.GetCustomAttribute(fieldInfo, typeof(RangeLimitsAttribute));
            _hasLimits = _limits != null;
            _hasReadLimits = true;
        }

        private static void DrawWithSlider(Rect rect, SerializedProperty min, SerializedProperty max, RangeLimitsAttribute limits)
        {
            bool isInt = min.propertyType == SerializedPropertyType.Integer;
            var minRect = new Rect(rect.x, rect.y, NumberWidth, rect.height);
            var maxRect = new Rect(rect.xMax - NumberWidth, rect.y, NumberWidth, rect.height);
            var sliderRect = new Rect(minRect.xMax + Spacing, rect.y, maxRect.x - minRect.xMax - Spacing * 2f, rect.height);

            float minValue = isInt ? min.intValue : min.floatValue;
            float maxValue = isInt ? max.intValue : max.floatValue;

            EditorGUI.BeginChangeCheck();
            minValue = EditorGUI.FloatField(minRect, minValue);
            EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, limits.Lower, limits.Upper);
            maxValue = EditorGUI.FloatField(maxRect, maxValue);
            if (!EditorGUI.EndChangeCheck()) return;

            minValue = Mathf.Clamp(minValue, limits.Lower, limits.Upper);
            maxValue = Mathf.Clamp(maxValue, minValue, limits.Upper);
            Write(min, max, minValue, maxValue, isInt);
        }

        private static void DrawFields(Rect rect, SerializedProperty min, SerializedProperty max)
        {
            float half = (rect.width - Spacing) * 0.5f;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, half, rect.height), min, GUIContent.none);
            EditorGUI.PropertyField(new Rect(rect.x + half + Spacing, rect.y, half, rect.height), max, GUIContent.none);
        }

        private static void Write(SerializedProperty min, SerializedProperty max, float minValue, float maxValue, bool isInt)
        {
            if (isInt)
            {
                min.intValue = Mathf.RoundToInt(minValue);
                max.intValue = Mathf.RoundToInt(maxValue);
            }
            else
            {
                min.floatValue = minValue;
                max.floatValue = maxValue;
            }
        }
    }
}

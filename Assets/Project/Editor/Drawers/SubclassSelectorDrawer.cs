using System;
using System.Collections.Generic;
using System.Reflection;
using LumberJack.Core;
using UnityEditor;
using UnityEngine;

namespace LumberJack.Editor
{
    /// <summary>
    /// Para campos <c>[SerializeReference, SubclassSelector]</c>: um botão na linha do campo lista as
    /// implementações concretas do tipo do campo e troca a instância pela escolhida.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer
    {
        private const string NoneLabel = "(nenhum)";
        private const float ButtonSpacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.LabelField(position, label.text, "Use junto com [SerializeReference]");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            // O botão vem antes do campo para receber o clique antes do foldout
            float labelWidth = EditorGUIUtility.labelWidth + ButtonSpacing;
            var buttonRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(CurrentTypeName(property)), FocusType.Keyboard))
            {
                ShowTypeMenu(property, buttonRect);
            }

            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }

        private static string CurrentTypeName(SerializedProperty property)
        {
            object value = property.managedReferenceValue;
            return value == null ? NoneLabel : ObjectNames.NicifyVariableName(value.GetType().Name);
        }

        private void ShowTypeMenu(SerializedProperty property, Rect buttonRect)
        {
            // Guarda objeto e caminho: o SerializedProperty do OnGUI não vale mais quando o menu responde
            SerializedObject serializedObject = property.serializedObject;
            string propertyPath = property.propertyPath;
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent(NoneLabel), property.managedReferenceValue == null,
                () => Assign(serializedObject, propertyPath, null));
            foreach (Type type in FindAssignableTypes(FieldBaseType()))
            {
                bool isCurrent = property.managedReferenceValue != null && property.managedReferenceValue.GetType() == type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), isCurrent,
                    () => Assign(serializedObject, propertyPath, type));
            }
            menu.DropDown(buttonRect);
        }

        private static void Assign(SerializedObject serializedObject, string propertyPath, Type type)
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
            property.isExpanded = true;
            serializedObject.ApplyModifiedProperties();
        }

        // Tipo declarado do campo (ou do elemento, se o campo for array/lista)
        private Type FieldBaseType()
        {
            Type type = fieldInfo.FieldType;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return type.GetGenericArguments()[0];
            return type;
        }

        private static IEnumerable<Type> FindAssignableTypes(Type baseType)
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType))
            {
                if (type.IsAbstract || type.IsGenericType || typeof(UnityEngine.Object).IsAssignableFrom(type)) continue;
                if (!type.IsDefined(typeof(SerializableAttribute), false)) continue;
                if (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) == null) continue;
                yield return type;
            }
        }
    }
}

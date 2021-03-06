﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Northwind.Editors.Shaders
{
    public static class MatEdit
    {
        #region MatEdit_Customizations

        public enum PackagePart {x, y, z, w};

        public enum GroupStyles {Main = 0, Sub = 1};
        private static GUIStyle[] groupStyles = new GUIStyle[] {EditorStyles.miniButton, EditorStyles.helpBox};

        public enum TextureFieldType {Small = 16, Medium = 32, Large = 64};

        #endregion MatEdit_Customizations

        #region MatEdit_Stats

        private static Material scopeMaterial;

        private static Material focusMaterial;

        private static bool markedForSave;

        #endregion MatEdit_Stats

        #region MatEdit_HelperFunctions

        private static Texture2D AnimationCurveToTexture(AnimationCurve curve, int steps, bool debug = false)
        {
            System.Diagnostics.Stopwatch lWatch = new System.Diagnostics.Stopwatch();
            if (debug)
            {
                lWatch.Start();
            }

            Texture2D lResult = new Texture2D(steps, 1);

            Color[] lPixels = new Color[steps];
            float length = steps;
            for (int p = 0; p < steps; p++)
            {
                float point = p;
                float lVal = curve.Evaluate(point / length);
                lPixels[p] = new Color(lVal, (lVal - 1f), (lVal - 2f), 1f);
            }

            lResult.SetPixels(lPixels);
            lResult.Apply();

            if (debug)
            {
                lWatch.Stop();
                Debug.Log("<color=green>Success:</color> Converted AnimationCurve to Texture2D in " + lWatch.ElapsedMilliseconds + "ms");
            }

            return lResult;
        }

        private static Texture2D GradientToTexture(Gradient gradiant, int steps, bool debug = false)
        {
            System.Diagnostics.Stopwatch lWatch = new System.Diagnostics.Stopwatch();
            if (debug)
            {
                lWatch.Start();
            }

            Texture2D lResult = new Texture2D(steps, 1);

            Color[] lPixels = new Color[steps];
            float length = steps;
            for (int p = 0; p < steps; p++)
            {
                float point = p;
                lPixels[p] = gradiant.Evaluate(point / length);
            }

            lResult.SetPixels(lPixels);
            lResult.Apply();

            if (debug)
            {
                lWatch.Stop();
                Debug.Log("<color=green>Success:</color> Converted Gradient to Texture2D in " + lWatch.ElapsedMilliseconds + "ms");
            }

            return lResult;
        }

        private static MatEditData GetMatEditData(Material material)
        {
            string lMaterialPath = AssetDatabase.GetAssetPath(material);
            MatEditData lData = AssetDatabase.LoadAssetAtPath<MatEditData>(lMaterialPath);
            if (lData == null)
            {
                lData = ScriptableObject.CreateInstance(typeof(MatEditData)) as MatEditData;
                lData.name = "MatEditData";
                AssetDatabase.AddObjectToAsset(lData, material);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return lData;
        }

        private static void CheckToSave()
        {
            MatEditData lData = GetMatEditData(focusMaterial);

            foreach(KeyValuePair<string, Texture2D> tex in lData.unsavedTextures)
            {
                if (lData.generatedTextures.ContainsKey(tex.Key))
                {
                    Texture2D oldTexture = lData.generatedTextures[tex.Key];
                    oldTexture.SetPixels(tex.Value.GetPixels());
                    
                    EditorUtility.SetDirty(oldTexture);
                }
                else
                {
                    tex.Value.name = tex.Key;
                    lData.generatedTextures.Add(tex.Key, tex.Value);
                    AssetDatabase.AddObjectToAsset(tex.Value, focusMaterial);
                }
                focusMaterial.SetTexture(tex.Key, lData.generatedTextures[tex.Key]);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            lData.unsavedTextures.Clear();
            EditorUtility.SetDirty(lData);

            markedForSave = false;
            Selection.selectionChanged -= CheckToSave;
        }

        private static void MarkForSave(Material material)
        {
            if (markedForSave == false)
            {
                Selection.selectionChanged += CheckToSave;
            }
            markedForSave = true;
            focusMaterial = material;
        }

        #endregion MatEdit_HelperFunctions

        #region SettingsFunctions

        public static void SetScope(Material material)
        {
            scopeMaterial = material;
        }

        #endregion SettingsFunctions

        //Editor Part

        #region GroupFields

        //Fold Group
        public static bool BeginFoldGroup(GUIContent content, string toggleID, GroupStyles style = GroupStyles.Main, bool spacing = false, bool writeToShader = false)
        {
            return BeginFoldGroup(content, toggleID, scopeMaterial, style, spacing, writeToShader);
        }

        public static bool BeginFoldGroup(GUIContent content, string toggleID, Material material, GroupStyles style = GroupStyles.Main, bool spacing = false, bool writeToShader = false)
        {
            MatEditData lData = GetMatEditData(material);
            if (!writeToShader)
            {
                if (!lData.toggles.ContainsKey(toggleID))
                {
                    lData.toggles.Add(toggleID, false);
                }
            }

            EditorGUILayout.BeginVertical(groupStyles[(int)style]);

            if (GUILayout.Button(content, EditorStyles.boldLabel))
            {
                if (writeToShader)
                {
                    material.SetInt(toggleID, scopeMaterial.GetInt(toggleID) == 1 ? 0 : 1);
                }
                else
                {
                    lData.toggles[toggleID] = !lData.toggles[toggleID];
                }
            }

            if (spacing)
            {
                EditorGUILayout.Space();
            }

            if (writeToShader)
            {
                return material.GetInt(toggleID) == 1;
            }
            else
            {
                return lData.toggles[toggleID];
            }
        }

        //Toggle Group
        public static bool BeginToggleGroup(GUIContent content, string toggleID, GroupStyles style = GroupStyles.Main, bool spacing = false, bool writeToShader = false)
        {
            return BeginToggleGroup(content, toggleID, scopeMaterial, style, spacing, writeToShader);
        }

        public static bool BeginToggleGroup(GUIContent content, string toggleID, Material material, GroupStyles style = GroupStyles.Main, bool spacing = false, bool writeToShader = false)
        {
            MatEditData lData = GetMatEditData(material);
            if (!writeToShader)
            {
                if (!lData.toggles.ContainsKey(toggleID))
                {
                    lData.toggles.Add(toggleID, false);
                }
            }

            EditorGUILayout.BeginVertical(groupStyles[(int)style]);

            bool toggle = false;
            if (writeToShader)
            {
                toggle = material.GetInt(toggleID) == 1;
            }
            else
            {
                toggle = lData.toggles[toggleID];
            }
            toggle = EditorGUILayout.BeginToggleGroup(content, toggle);
            EditorGUILayout.EndToggleGroup();

            if (writeToShader)
            {
                material.SetInt(toggleID, toggle ? 1 : 0);
            }
            else
            {
                lData.toggles[toggleID] = toggle;
            }

            if (spacing)
            {
                EditorGUILayout.Space();
            }

            return toggle;
        }

        //Static Group
        public static void BeginGroup(GroupStyles style = GroupStyles.Main, bool spacing = false)
        {
            BeginGroup(new GUIContent(), scopeMaterial, style, spacing);
        }

        public static void BeginGroup(GUIContent content, GroupStyles style = GroupStyles.Main, bool spacing = false)
        {
            BeginGroup(content, scopeMaterial, style, spacing);
        }

        public static void BeginGroup(GUIContent content, Material material, GroupStyles style = GroupStyles.Main, bool spacing = false)
        {
            EditorGUILayout.BeginVertical(groupStyles[(int)style]);
            if (content.text != "")
            {
                EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            }

            if (spacing)
            {
                EditorGUILayout.Space();
            }
        }

        //End Current Group
        public static void EndGroup(bool spacing = false)
        {
            if (spacing)
            {
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndVertical();
        }

        #endregion GroupFields

        #region TextureFields

        public static void TextureField(GUIContent content, string property, TextureFieldType size = TextureFieldType.Small)
        {
            TextureField(content, property, scopeMaterial, size);
        }

        public static void TextureField(GUIContent content, string property, Material material, TextureFieldType size = TextureFieldType.Small)
        {
            Texture2D mainTexture = (Texture2D)EditorGUILayout.ObjectField(content, material.GetTexture(property), typeof(Texture2D), false, GUILayout.Height((float)size));
            material.SetTexture(property, mainTexture);
        }

        public static void NormalTextureField(GUIContent content, string property, TextureFieldType size = TextureFieldType.Small)
        {
            NormalTextureField(content, property, scopeMaterial, size);
        }

        public static void NormalTextureField(GUIContent content, string property, Material material, TextureFieldType size = TextureFieldType.Small)
        {
            Texture2D normalTexture = (Texture2D)EditorGUILayout.ObjectField(content, material.GetTexture(property), typeof(Texture), false, GUILayout.Height((float)size));
            if (normalTexture != null)
            {
                TextureImporter lImporter = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(normalTexture.GetInstanceID()));
                if (lImporter.textureType != TextureImporterType.NormalMap)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("Texture is no normal map!");
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Fix now"))
                    {
                        lImporter.textureType = TextureImporterType.NormalMap;
                        lImporter.convertToNormalmap = true;
                    }
                    if (GUILayout.Button("To Settings"))
                    {
                        Selection.activeObject = lImporter;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
            }
            material.SetTexture(property, normalTexture);
        }

        public static void TextureDataField(GUIContent content, string property)
        {
            TextureDataField(content, property, scopeMaterial);
        }

        public static void TextureDataField(GUIContent content, string property, Material material)
        {
            if (content.text != "")
            {
                EditorGUILayout.LabelField(content);
            }

            MatEdit.VectorField(new GUIContent("Tiling", ""), property, PackagePart.x, PackagePart.y);
            MatEdit.VectorField(new GUIContent("Offset", ""), property, PackagePart.z, PackagePart.w);
        }

        #endregion TextureFields

        #region SimpleFields

        //Color Field
        public static void ColorField(GUIContent content, string property)
        {
            ColorField(content, property, scopeMaterial);
        }

        public static void ColorField(GUIContent content, string property, Material material)
        {
            if (!material.HasProperty(property) || material.GetColor(property) == null)
            {
                return;
            }
            material.SetColor(property, EditorGUILayout.ColorField(content, material.GetColor(property)));
        }

        //Toggle Field
        public static void ToggleField(GUIContent content, string property)
        {
            ToggleField(content, property, scopeMaterial);
        }

        public static void ToggleField(GUIContent content, string property, Material material)
        {
            material.SetInt(property, EditorGUILayout.Toggle(content, material.GetInt(property) == 1 ? true : false) ? 1 : 0);
        }

        //Int Field
        public static void IntField(GUIContent content, string property)
        {
            IntField(content, property, scopeMaterial);
        }

        public static void IntField(GUIContent content, string property, Material material)
        {
            material.SetInt(property, EditorGUILayout.IntField(content, material.GetInt(property)));
        }

        //Enum Field
        public static int EnumField(GUIContent content, string property, GUIContent[] options)
        {
            return EnumField(content, property, options, scopeMaterial);
        }

        public static int EnumField(GUIContent content, string property, GUIContent[] options, Material material)
        {
            int lResult = EditorGUILayout.Popup(content, material.GetInt(property), options);
            material.SetInt(property, lResult);
            return lResult;
        }

        //Float Field
        public static void FloatField(GUIContent content, string property)
        {
            FloatField(content, property, scopeMaterial);
        }

        public static void FloatField(GUIContent content, string property, Material material)
        {
            material.SetFloat(property, EditorGUILayout.FloatField(content, material.GetFloat(property)));
        }

        //Slider Field
        public static void SliderField(GUIContent content, string property, float min, float max, bool round = false)
        {
            SliderField(content, property, min, max, scopeMaterial, round);
        }

        public static void SliderField(GUIContent content, string property, float min, float max, Material material, bool round = false)
        {
            float lValue = EditorGUILayout.Slider(content, material.GetFloat(property), min, max);
            if (round)
            {
                lValue = Mathf.Round(lValue);
            }
            material.SetFloat(property, lValue);
        }

        //Min-Max Slider Field
        public static void MinMaxSliderField(GUIContent content, string startProperty, string endProperty, float min, float max, bool drawFloatFields = false)
        {
            MinMaxSliderField(content, startProperty, endProperty, min, max, scopeMaterial, drawFloatFields);
        }

        public static void MinMaxSliderField(GUIContent content, string startProperty, string endProperty, float min, float max, Material material, bool drawFloatFields = false)
        {
            float lMinValue = material.GetFloat(startProperty);
            float lMaxValue = material.GetFloat(endProperty);

            EditorGUILayout.MinMaxSlider(content, ref lMinValue, ref lMaxValue, min, max);

            if (drawFloatFields)
            {
                EditorGUILayout.BeginHorizontal();

                lMinValue = EditorGUILayout.FloatField(lMinValue);
                lMaxValue = EditorGUILayout.FloatField(lMaxValue);

                lMinValue = Mathf.Clamp(Mathf.Min(lMinValue, lMaxValue), min, max);
                lMaxValue = Mathf.Clamp(Mathf.Max(lMaxValue, lMinValue), min, max);

                EditorGUILayout.EndHorizontal();
            }

            material.SetFloat(startProperty, lMinValue);
            material.SetFloat(endProperty, lMaxValue);
        }

        //Packed Enum Field
        public static int EnumPackedField(GUIContent content, string property, GUIContent[] options, PackagePart part)
        {
            return EnumPackedField(content, property, options, part, scopeMaterial);
        }

        public static int EnumPackedField(GUIContent content, string property, GUIContent[] options, PackagePart part, Material material)
        {
            Vector4 lOriginal = material.GetVector(property);
            int lResult = EditorGUILayout.Popup(content, (int)(lOriginal[(int)part]), options);
            lOriginal[(int)part] = lResult;
            material.SetVector(property, lOriginal);
            return lResult;
        }

        //Packed Float Field
        public static void FloatPackedField(GUIContent content, string property, PackagePart part)
        {
            FloatPackedField(content, property, scopeMaterial, part);
        }

        public static void FloatPackedField(GUIContent content, string property, Material material, PackagePart part)
        {
            Vector4 lOriginal = material.GetVector(property);

            lOriginal[(int)part] = EditorGUILayout.FloatField(content, lOriginal[(int)part]);
            material.SetVector(property, lOriginal);
        }

        //Packed Slider Field
        public static void SliderPackedField(GUIContent content, string property, float min, float max, PackagePart part, bool round = false)
        {
            SliderPackedField(content, property, min, max, scopeMaterial, part, round);
        }

        public static void SliderPackedField(GUIContent content, string property, float min, float max, Material material, PackagePart part, bool round = false)
        {
            Vector4 lOriginal = material.GetVector(property);

            lOriginal[(int)part] = EditorGUILayout.Slider(content, lOriginal[(int)part], min, max);
            if (round)
            {
                lOriginal[(int)part] = Mathf.Round(lOriginal[(int)part]);
            }
            material.SetVector(property, lOriginal);
        }

        //Packed Min-Max Slider Field
        public static void MinMaxSliderPackedField(GUIContent content, string property, PackagePart startPart, PackagePart endPart, float min, float max, bool drawFloatFields = false)
        {
            MinMaxSliderPackedField(content, property, startPart, endPart, min, max, scopeMaterial, drawFloatFields);
        }

        public static void MinMaxSliderPackedField(GUIContent content, string property, PackagePart startPart, PackagePart endPart, float min, float max, Material material, bool drawFloatFields = false)
        {
            Vector4 lOriginal = material.GetVector(property);
            float lMinValue = lOriginal[(int)startPart];
            float lMaxValue = lOriginal[(int)endPart];

            EditorGUILayout.MinMaxSlider(content, ref lMinValue, ref lMaxValue, min, max);

            if (drawFloatFields)
            {
                EditorGUILayout.BeginHorizontal();

                lMinValue = EditorGUILayout.FloatField(lMinValue);
                lMaxValue = EditorGUILayout.FloatField(lMaxValue);

                lMinValue = Mathf.Clamp(Mathf.Min(lMinValue, lMaxValue), min, max);
                lMaxValue = Mathf.Clamp(Mathf.Max(lMaxValue, lMinValue), min, max);

                EditorGUILayout.EndHorizontal();
            }

            lOriginal[(int)startPart] = lMinValue;
            lOriginal[(int)endPart] = lMaxValue;

            material.SetVector(property, lOriginal);
        }

        //Float As Vector Field
        public static void FloatAsVectorField(GUIContent content, params string[] properties)
        {
            FloatAsVectorField(content, scopeMaterial, properties);
        }

        public static void FloatAsVectorField(GUIContent content, Material material, params string[] properties)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(content);
            for (int p = 0; p < properties.Length; p++)
            {
                float lVal = EditorGUILayout.FloatField(material.GetFloat(properties[p]));
                material.SetFloat(properties[p], lVal);
            }
            EditorGUILayout.EndHorizontal();
        }

        //Vector Field
        public static void VectorField(GUIContent content, string property, params PackagePart[] part)
        {
            VectorField(content, property, scopeMaterial, part);
        }

        public static void VectorField(GUIContent content, string property, Material material, params PackagePart[] part)
        {
            if (!material.HasProperty(property) || material.GetVector(property) == null)
            {
                return;
            }
            Vector4 lOriginal = material.GetVector(property);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(content);
            for (int p = 0; p < part.Length; p++)
            {
                lOriginal[(int)part[p]] = EditorGUILayout.FloatField(lOriginal[(int)part[p]]);
            }
            EditorGUILayout.EndHorizontal();
            material.SetVector(property, lOriginal);
        }

        #endregion SimpleFields

        #region SpecialFields

        //AnimationCurve Field
        public static void AnimationCurveField(GUIContent content, string property, int quality, bool debug = false)
        {
            AnimationCurveField(content, property, quality, scopeMaterial, debug);
        }

        public static void AnimationCurveField(GUIContent content, string property, int quality, Material material, bool debug = false)
        {
            MatEditData lData = GetMatEditData(material);
            AnimationCurve curve = new AnimationCurve();
            if (lData.animationCurves.ContainsKey(property))
            {
                curve = lData.animationCurves[property];
            }
            else
            {
                lData.animationCurves.Add(property, curve);
            }

            EditorGUI.BeginChangeCheck();
            curve = EditorGUILayout.CurveField(content, curve);
            bool lEdited = EditorGUI.EndChangeCheck();

            lData.animationCurves[property] = curve;
            EditorUtility.SetDirty(lData);

            if (lEdited)
            {
                Texture2D mainTexture = AnimationCurveToTexture(curve, quality, debug);
            
                if (lData.unsavedTextures.ContainsKey(property))
                {
                    lData.unsavedTextures[property] = mainTexture;
                }
                else
                {
                    lData.unsavedTextures.Add(property, mainTexture);
                }

                material.SetTexture(property, mainTexture);
            }

            MarkForSave(material);
        }

        //Gradient Field
        public static void GradientField(GUIContent content, string property, int quality, bool debug = false)
        {
            GradientField(content, property, quality, scopeMaterial, debug);
        }

        public static void GradientField(GUIContent content, string property, int quality, Material material, bool debug = false)
        {
            MatEditData lData = GetMatEditData(material);
            Gradient gradient = new Gradient();
            if (lData.gradients.ContainsKey(property)) {
                gradient = lData.gradients[property];
            } else
            {
                lData.gradients.Add(property, gradient);
            }

            EditorGUI.BeginChangeCheck();
            MethodInfo method = typeof(EditorGUILayout).GetMethod("GradientField", BindingFlags.Static | BindingFlags.NonPublic, null, new System.Type[] { typeof(GUIContent), typeof(Gradient), typeof(GUILayoutOption[]) }, null);
            if (method != null)
            {
                gradient = (Gradient)method.Invoke(null, new object[] { content, gradient, new GUILayoutOption[] { } });
            }
            bool lEdited = EditorGUI.EndChangeCheck();

            if (lEdited)
            {
                lData.gradients[property] = gradient;
                EditorUtility.SetDirty(lData);

                Texture2D mainTexture = GradientToTexture(gradient, quality, debug);

                if (lData.unsavedTextures.ContainsKey(property))
                {
                    lData.unsavedTextures[property] = mainTexture;
                }
                else
                {
                    lData.unsavedTextures.Add(property, mainTexture);
                }

                material.SetTexture(property, mainTexture);
            }

            MarkForSave(material);
        }

        #endregion SpecialFields

        #region AutoFields

        public static void PropertyField(MaterialProperty property, string context = "")
        {
            PropertyField(property, scopeMaterial, context);
        }

        public static void PropertyField(MaterialProperty property, Material material, string context = "")
        {
            MaterialProperty.PropType lType = property.type;

            switch (lType)
            {
                case MaterialProperty.PropType.Color: ColorField(new GUIContent(property.displayName, context), property.name, material);
                    break;
                case MaterialProperty.PropType.Float: FloatField(new GUIContent(property.displayName, context), property.name, material);
                    break;
                case MaterialProperty.PropType.Range: SliderField(new GUIContent(property.displayName, context), property.name, property.rangeLimits.x, property.rangeLimits.y, material);
                    break;
                case MaterialProperty.PropType.Texture: TextureField(new GUIContent(property.displayName, context), property.name, material, TextureFieldType.Small);
                    TextureDataField(new GUIContent(), property.name + "_ST");
                    break;
                case MaterialProperty.PropType.Vector: VectorField(new GUIContent(property.displayName, context), property.name, material, PackagePart.x, PackagePart.y, PackagePart.z, PackagePart.w);
                    break;
            }
        }

        public static void PropertyField(string property, string context = "")
        {
            MaterialProperty lProp = MaterialEditor.GetMaterialProperty(new Object[] { scopeMaterial }, property);
            if (scopeMaterial.HasProperty(property) && lProp != null)
            {
                PropertyField(lProp, context);
            }
        }

        public static void PropertyField(string property, Material material, string context = "")
        {
            MaterialProperty lProp = MaterialEditor.GetMaterialProperty(new Object[] { material }, property);
            if (scopeMaterial.HasProperty(property) && lProp != null)
            {
                PropertyField(lProp, material, context);
            }
        }

        #endregion AutoFields
    }
}
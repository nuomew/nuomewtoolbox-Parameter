/*
 * 材质复制工具 - Material Copy Tool
 * 功能：将材质A的各种设置复制到材质B上，支持选择性同步各种属性
 * 作者：诺喵工具箱
 * 用途：快速同步材质属性，提高工作效率
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NyameauToolbox.Editor
{
    public partial class MaterialCopyWindow : EditorWindow
    {
        // 源材质和目标材质
        private Material sourceMaterial;
        private Material targetMaterial;
        
        // 滚动位置
        private Vector2 scrollPosition;
        
        // UI样式
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle buttonStyle;
        
        // UI状态变量
        
        // 复制选项 - 基本设置
        private bool copyBasicSettings = false;
        private bool copyLightingSettings = false;
        private bool copyUVSettings = false;
        private bool copyVRChatSettings = false;
        
        // 复制选项 - 颜色设置
        private bool copyColorSettings = false;
        private bool copyMainColorAlpha = false;
        private bool copyShadowSettings = false;
        private bool copyRimShadeSettings = false;
        
        // 复制选项 - 发光和法线
        private bool copyEmissionSettings = false;
        private bool copyNormalReflectionSettings = false;
        private bool copyNormalMapSettings = false;
        private bool copyBacklightSettings = false;
        private bool copyReflectionSettings = false;
        
        // 复制选项 - 特效设置
        private bool copyMatCapSettings = false;
        private bool copyRimLightSettings = false;
        private bool copyGlitterSettings = false;
        
        // 复制选项 - 扩展设置
        private bool copyOutlineSettings = false;
        private bool copyParallaxSettings = false;
        private bool copyDistanceFadeSettings = false;
        private bool copyAudioLinkSettings = false;
        private bool copyDissolveSettings = false;
        private bool copyIDMaskSettings = false;
        private bool copyUVTileDiscardSettings = false;
        private bool copyStencilSettings = false;
        
        // 复制选项 - 渲染设置
        private bool copyRenderSettings = false;
        private bool copyLightmapSettings = false;
        private bool copyTessellationSettings = false;
        private bool copyOptimizationSettings = false;
        
        // 全面复制选项
        private bool useComprehensiveCopy = false;
        
        [MenuItem("诺喵工具箱/材质复制器", false, 13)]
        public static void ShowWindow()
        {
            var window = GetWindow<MaterialCopyWindow>("材质复制工具");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.6f, 1f) }
            };
            
            sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.3f, 0.3f, 0.3f) }
            };
            
            buttonStyle = new GUIStyle("Button")
            {
                fontSize = 12,
                fixedHeight = 30
            };
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawMaterialSelection();
            DrawCopyOptions();
            DrawActionButtons();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("🎨 材质复制工具", headerStyle);
            GUILayout.Label("将材质A的设置复制到材质B上", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(10);
        }
        
        private void DrawMaterialSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("材质选择", sectionStyle);
            EditorGUILayout.Space(5);
            
            sourceMaterial = (Material)EditorGUILayout.ObjectField("源材质 (复制自)", sourceMaterial, typeof(Material), false);
            targetMaterial = (Material)EditorGUILayout.ObjectField("目标材质 (复制到)", targetMaterial, typeof(Material), false);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawCopyOptions()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 显示选择性复制选项
                // 基本设置组
                DrawSectionGroup("基本设置", new System.Action[]
                {
                    () => copyBasicSettings = EditorGUILayout.Toggle("基本设置", copyBasicSettings),
                    () => copyLightingSettings = EditorGUILayout.Toggle("照明设置", copyLightingSettings),
                    () => copyUVSettings = EditorGUILayout.Toggle("UV 设置", copyUVSettings),
                    () => copyVRChatSettings = EditorGUILayout.Toggle("VRChat", copyVRChatSettings)
                });
            
            // 颜色设置组
            DrawSectionGroup("颜色设置", new System.Action[]
            {
                () => copyColorSettings = EditorGUILayout.Toggle("颜色设置", copyColorSettings),
                () => copyMainColorAlpha = EditorGUILayout.Toggle("主色/Alpha 设置", copyMainColorAlpha),
                () => copyShadowSettings = EditorGUILayout.Toggle("阴影设置", copyShadowSettings),
                () => copyRimShadeSettings = EditorGUILayout.Toggle("RimShade", copyRimShadeSettings)
            });
            
            // 发光和法线设置组
            DrawSectionGroup("发光和法线设置", new System.Action[]
            {
                () => copyEmissionSettings = EditorGUILayout.Toggle("发光设置", copyEmissionSettings),
                () => copyNormalReflectionSettings = EditorGUILayout.Toggle("法线贴图&反射设置", copyNormalReflectionSettings),
                () => copyNormalMapSettings = EditorGUILayout.Toggle("法线贴图设置", copyNormalMapSettings),
                () => copyBacklightSettings = EditorGUILayout.Toggle("背光灯设置", copyBacklightSettings),
                () => copyReflectionSettings = EditorGUILayout.Toggle("反射设置", copyReflectionSettings)
            });
            
            // 特效设置组
            DrawSectionGroup("特效设置", new System.Action[]
            {
                () => copyMatCapSettings = EditorGUILayout.Toggle("MatCap 设置", copyMatCapSettings),
                () => copyRimLightSettings = EditorGUILayout.Toggle("Rim Light 设置", copyRimLightSettings),
                () => copyGlitterSettings = EditorGUILayout.Toggle("Glitter设置", copyGlitterSettings)
            });
            
            // 扩展设置组
            DrawSectionGroup("扩展设置", new System.Action[]
            {
                () => copyOutlineSettings = EditorGUILayout.Toggle("轮廓设置", copyOutlineSettings),
                () => copyParallaxSettings = EditorGUILayout.Toggle("视差", copyParallaxSettings),
                () => copyDistanceFadeSettings = EditorGUILayout.Toggle("距离淡化", copyDistanceFadeSettings),
                () => copyAudioLinkSettings = EditorGUILayout.Toggle("AudioLink", copyAudioLinkSettings),
                () => copyDissolveSettings = EditorGUILayout.Toggle("Dissolve", copyDissolveSettings),
                () => copyIDMaskSettings = EditorGUILayout.Toggle("ID Mask", copyIDMaskSettings),
                () => copyUVTileDiscardSettings = EditorGUILayout.Toggle("UV TileDiscard", copyUVTileDiscardSettings),
                () => copyStencilSettings = EditorGUILayout.Toggle("Stencil 设置", copyStencilSettings)
            });
            
            // 渲染设置组
            DrawSectionGroup("渲染设置", new System.Action[]
            {
                () => copyRenderSettings = EditorGUILayout.Toggle("渲染设置", copyRenderSettings),
                () => copyLightmapSettings = EditorGUILayout.Toggle("光照烘培设置", copyLightmapSettings),
                () => copyTessellationSettings = EditorGUILayout.Toggle("镶嵌（极高负载)", copyTessellationSettings),
                () => copyOptimizationSettings = EditorGUILayout.Toggle("优化", copyOptimizationSettings)
            });

            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawSectionGroup(string title, System.Action[] toggleActions)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(title, sectionStyle);
            EditorGUILayout.Space(5);
            
            EditorGUI.indentLevel++;
            foreach (var action in toggleActions)
            {
                action.Invoke();
            }
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            // 全选按钮
            if (GUILayout.Button("全选", buttonStyle))
            {
                SetAllOptions(true);
            }
            
            // 全不选按钮
            if (GUILayout.Button("全不选", buttonStyle))
            {
                SetAllOptions(false);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
            
            // 复制按钮
            GUI.enabled = sourceMaterial != null && targetMaterial != null && HasAnyOptionSelected();
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            string buttonText = "🚀 开始复制材质属性";
            if (GUILayout.Button(buttonText, GUILayout.Height(40)))
            {
                PerformCopy();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
            
            if (sourceMaterial == null || targetMaterial == null)
            {
                EditorGUILayout.HelpBox("请选择源材质和目标材质", MessageType.Warning);
            }
            else if (!HasAnyOptionSelected())
            {
                EditorGUILayout.HelpBox("请至少选择一个复制选项", MessageType.Warning);
            }
        }
        
        private void SetAllOptions(bool value)
        {
                copyBasicSettings = value;
                copyLightingSettings = value;
                copyUVSettings = value;
                copyVRChatSettings = value;
                copyColorSettings = value;
                copyMainColorAlpha = value;
                copyShadowSettings = value;
                copyRimShadeSettings = value;
                copyEmissionSettings = value;
                copyNormalReflectionSettings = value;
                copyNormalMapSettings = value;
                copyBacklightSettings = value;
                copyReflectionSettings = value;
                copyMatCapSettings = value;
                copyRimLightSettings = value;
                copyGlitterSettings = value;
                copyOutlineSettings = value;
                copyParallaxSettings = value;
                copyDistanceFadeSettings = value;
                copyAudioLinkSettings = value;
                copyDissolveSettings = value;
                copyIDMaskSettings = value;
                copyUVTileDiscardSettings = value;
                copyStencilSettings = value;
                copyRenderSettings = value;
                copyLightmapSettings = value;
                copyTessellationSettings = value;
                copyOptimizationSettings = value;
        }
        
        private bool HasAnyOptionSelected()
        {
            return copyBasicSettings || copyLightingSettings || copyUVSettings || copyVRChatSettings ||
                   copyColorSettings || copyMainColorAlpha || copyShadowSettings || copyRimShadeSettings ||
                   copyEmissionSettings || copyNormalReflectionSettings || copyNormalMapSettings || 
                   copyBacklightSettings || copyReflectionSettings || copyMatCapSettings || 
                   copyRimLightSettings || copyGlitterSettings || copyOutlineSettings || 
                   copyParallaxSettings || copyDistanceFadeSettings || copyAudioLinkSettings || 
                   copyDissolveSettings || copyIDMaskSettings || copyUVTileDiscardSettings || 
                   copyStencilSettings || copyRenderSettings || copyLightmapSettings || 
                   copyTessellationSettings || copyOptimizationSettings;
        }
        
        private void PerformCopy()
        {
            if (sourceMaterial == null || targetMaterial == null)
            {
                EditorUtility.DisplayDialog("错误", "请选择源材质和目标材质", "确定");
                return;
            }
            
            // 记录撤销操作
            Undo.RecordObject(targetMaterial, "复制材质属性");
            
            int copiedCount = 0;
            bool shaderChanged = false;
            
            try
            {
                // 检查着色器是否相同，如果不同则先复制着色器
                if (sourceMaterial.shader != targetMaterial.shader)
                {
                    targetMaterial.shader = sourceMaterial.shader;
                    shaderChanged = true;
                    Debug.Log($"[材质复制工具] 着色器已从 '{sourceMaterial.shader.name}' 复制到目标材质");
                }
                
                Debug.Log("[材质复制工具] 开始选择性复制材质属性");
                
                // 基本设置
                if (copyBasicSettings)
                {
                    copiedCount += CopyBasicSettings();
                }
                
                // 照明设置
                if (copyLightingSettings)
                {
                    copiedCount += CopyLightingSettings();
                }
                
                // UV设置
                if (copyUVSettings)
                {
                    copiedCount += CopyUVSettings();
                }
                
                // VRChat设置
                if (copyVRChatSettings)
                {
                    copiedCount += CopyVRChatSettings();
                }
                
                // 颜色设置
                if (copyColorSettings)
                {
                    copiedCount += CopyColorSettings();
                }
                
                // 主色/Alpha设置
                if (copyMainColorAlpha)
                {
                    copiedCount += CopyMainColorAlphaSettings();
                }
                
                // 阴影设置
                if (copyShadowSettings)
                {
                    copiedCount += CopyShadowSettings();
                }
                
                // RimShade设置
                if (copyRimShadeSettings)
                {
                    copiedCount += CopyRimShadeSettings();
                }
                
                // 发光设置
                if (copyEmissionSettings)
                {
                    copiedCount += CopyEmissionSettings();
                }
                
                // 法线贴图&反射设置
                if (copyNormalReflectionSettings)
                {
                    copiedCount += CopyNormalReflectionSettings();
                }
                
                // 法线贴图设置
                if (copyNormalMapSettings)
                {
                    copiedCount += CopyNormalMapSettings();
                }
                
                // 背光灯设置
                if (copyBacklightSettings)
                {
                    copiedCount += CopyBacklightSettings();
                }
                
                // 反射设置
                if (copyReflectionSettings)
                {
                    copiedCount += CopyReflectionSettings();
                }
                
                // MatCap设置
                if (copyMatCapSettings)
                {
                    copiedCount += CopyMatCapSettings();
                }
                
                // Rim Light设置
                if (copyRimLightSettings)
                {
                    copiedCount += CopyRimLightSettings();
                }
                
                // Glitter设置
                if (copyGlitterSettings)
                {
                    copiedCount += CopyGlitterSettings();
                }
                
                // 轮廓设置
                if (copyOutlineSettings)
                {
                    copiedCount += CopyOutlineSettings();
                }
                
                // 视差设置
                if (copyParallaxSettings)
                {
                    copiedCount += CopyParallaxSettings();
                }
                
                // 距离淡化设置
                if (copyDistanceFadeSettings)
                {
                    copiedCount += CopyDistanceFadeSettings();
                }
                
                // AudioLink设置
                if (copyAudioLinkSettings)
                {
                    copiedCount += CopyAudioLinkSettings();
                }
                
                // Dissolve设置
                if (copyDissolveSettings)
                {
                    copiedCount += CopyDissolveSettings();
                }
                
                // ID Mask设置
                if (copyIDMaskSettings)
                {
                    copiedCount += CopyIDMaskSettings();
                }
                
                // UV TileDiscard设置
                if (copyUVTileDiscardSettings)
                {
                    copiedCount += CopyUVTileDiscardSettings();
                }
                
                // Stencil设置
                if (copyStencilSettings)
                {
                    copiedCount += CopyStencilSettings();
                }
                
                // 渲染设置
                if (copyRenderSettings)
                {
                    copiedCount += CopyRenderSettings();
                }
                
                // 光照烘培设置
                if (copyLightmapSettings)
                {
                    copiedCount += CopyLightmapSettings();
                }
                
                // 镶嵌设置
                if (copyTessellationSettings)
                {
                    copiedCount += CopyTessellationSettings();
                }
                
                // 优化设置
                if (copyOptimizationSettings)
                {
                    copiedCount += CopyOptimizationSettings();
                }
                
                // 仅在有选项被勾选时，执行补充复制确保完整性
                if (HasAnyOptionSelected())
                {
                    Debug.Log("[材质复制工具] 执行补充属性复制，确保选中项目完整复制");
                    copiedCount += CopySelectedUnknownProperties();
                }
                
                // 标记材质为脏数据，确保保存
                EditorUtility.SetDirty(targetMaterial);
                
                // 显示完成消息
                string message = $"成功复制了 {copiedCount} 个属性到目标材质\n\n" +
                    $"源材质: {sourceMaterial.name}\n" +
                    $"目标材质: {targetMaterial.name}";
                
                if (shaderChanged)
                {
                    message += "\n\n✓ 着色器已同步复制";
                }
                
                EditorUtility.DisplayDialog("复制完成", message, "确定");
                    
                string logMessage = $"[材质复制工具] 成功复制 {copiedCount} 个属性从 '{sourceMaterial.name}' 到 '{targetMaterial.name}'";
                if (shaderChanged)
                {
                    logMessage += " (包含着色器)";
                }
                Debug.Log(logMessage);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("复制失败", $"复制过程中发生错误:\n{e.Message}", "确定");
                Debug.LogError($"[材质复制工具] 复制失败: {e.Message}");
            }
        }
    }
}
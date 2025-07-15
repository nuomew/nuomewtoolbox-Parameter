/*
 * 网格删除器窗口 - 诺喵工具箱
 * 基于纹理的网格删除工具，集成自MeshDeleterWithTexture
 * 功能：可视化纹理编辑、精确网格删除、实时预览效果
 */

using UnityEngine;
using UnityEditor;
using System;
using Gatosyocora.MeshDeleterWithTexture.Utilities;
using Gatosyocora.MeshDeleterWithTexture.Views;
using Gatosyocora.MeshDeleterWithTexture.Models;

namespace NyameauToolbox.Editor
{
    public class MeshDeleterWindow : EditorWindow
    {
        private const float CANVAS_SIZE_RAITO = 0.6f;


        private CanvasView canvasView;
        private ToolView toolView;
        private MeshDeleterWithTextureModel model;
        private LocalizedText localizedText;
        
        // UI样式
        private GUIStyle headerStyle;
        private Color primaryColor = new Color(1f, 0.75f, 0.85f, 1f);   // 粉色主题
        
        [MenuItem("诺喵工具箱/网格删除器", false, 15)]
        public static void ShowWindow()
        {
            var window = GetWindow<MeshDeleterWindow>("网格删除器 - 诺喵工具箱");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();
            InitializeMeshDeleter();
        }
        
        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
        
        private void InitializeMeshDeleter()
        {
            canvasView = CreateInstance<CanvasView>();
            toolView = CreateInstance<ToolView>();
            model = new MeshDeleterWithTextureModel();
            localizedText = new LocalizedText();
            ChangeLanguage(localizedText.SelectedLanguage);
        }

        private void OnDisable()
        {
            if (model != null)
            {
                model.Dispose();
            }

            if (canvasView != null)
            {
                canvasView.Dispose();
            }
            
            if (toolView != null)
            {
                toolView.Dispose();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            DrawHeader();
            
            // 检查Android构建目标支持
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                DrawNotSupportBuildTarget();
                return;
            }

            DrawToolbar();
            DrawMainContent();
            
            // 快捷键支持
            HandleKeyboardInput();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            
            GUI.backgroundColor = primaryColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("✂️ 网格删除器 - 诺喵工具箱", headerStyle);
            GUILayout.Label("基于纹理的精确网格删除工具", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(10);
        }
        
        private void DrawToolbar()
        {
            // 检查本地化文本是否正确加载
            if (localizedText?.Data == null)
            {
                EditorGUILayout.HelpBox("语言包加载失败，请检查资源文件", MessageType.Error);
                return;
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                // 渲染器选择
                GatoGUILayout.ObjectField(
                    localizedText.Data.rendererLabelText,
                    model.renderer,
                    renderer => model.OnChangeRenderer(canvasView, renderer)
                );


                
                // 返回工具箱按钮
                GUI.backgroundColor = primaryColor;
                if (GUILayout.Button(localizedText.Data.backToToolboxButtonText, GUILayout.Width(100)))
                {
                    NyameauToolboxWindow.ShowWindow();
                    Close();
                }
                GUI.backgroundColor = Color.white;
            }
        }
        
        private void DrawMainContent()
        {
            // 检查本地化文本是否正确加载
            if (localizedText?.Data == null)
            {
                EditorGUILayout.HelpBox("语言包加载失败，无法显示主界面", MessageType.Error);
                return;
            }
            
            GUILayout.Space(20);

            using (new EditorGUILayout.HorizontalScope())
            {
                // 左侧画布区域
                using (new EditorGUILayout.VerticalScope())
                {
                    canvasView.Render(model.HasTexture(), CANVAS_SIZE_RAITO);

                    // 画布控制按钮
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GatoGUILayout.Slider(
                            localizedText.Data.scaleLabelText,
                            canvasView.ZoomScale,
                            0.1f,
                            1.0f,
                            scale => canvasView.ZoomScale = scale
                        );

                        GatoGUILayout.Button(
                            localizedText.Data.resetButtonText,
                            () => canvasView.ResetScrollOffsetAndZoomScale()
                        );
                    }
                }

                // 右侧工具面板
                toolView.Render(model, localizedText, canvasView, 1 - CANVAS_SIZE_RAITO);
            }
        }
        
        private void HandleKeyboardInput()
        {
            if (InputKeyDown(KeyCode.Z))
            {
                canvasView.UndoPreviewTexture();
            }
        }

        private void DrawNotSupportBuildTarget()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox(
                    localizedText?.Data?.androidNotSupportWarningText ?? "⚠️ 网格删除器不支持Android构建目标\n请切换到其他平台后使用此工具",
                    MessageType.Warning
                );
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        private bool InputKeyDown(KeyCode keyCode)
        {
            return Event.current.type == EventType.KeyDown && 
                Event.current.keyCode == keyCode;
        }



        private void ChangeLanguage(Language language)
        {
            localizedText.SetLanguage(language);
            toolView.OnChangeLanguage(localizedText);
        }


    }
}
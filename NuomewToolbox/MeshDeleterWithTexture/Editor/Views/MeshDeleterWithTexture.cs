using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using Gatosyocora.MeshDeleterWithTexture.Utilities;
using Gatosyocora.MeshDeleterWithTexture.Views;
using Gatosyocora.MeshDeleterWithTexture.Models;

/*
 * Copyright (c) 2019 gatosyocora
 * Released under the MIT license.
 * see LICENSE.txt
 */

// MeshDeleterWithTexture v0.8.2

namespace Gatosyocora.MeshDeleterWithTexture
{
#if UNITY_EDITOR
    public class MeshDeleterWithTexture : EditorWindow
    {
        private const float CANVAS_SIZE_RAITO = 0.6f;
        private const string HELP_PAGE_URL = "http://cream-period-a2e.notion.site/e14c9dda72d343b49c94f8f1b40fc351";

        private CanvasView canvasView;
        private ToolView toolView;

        private MeshDeleterWithTextureModel model;

        private LocalizedText localizedText;

        // [MenuItem("GatoTool/MeshDeleter with Texture")] // 已移除菜单项，功能已集成到诺喵工具箱
        // private static void Open()
        // {
        //     GetWindow<MeshDeleterWithTexture>(nameof(MeshDeleterWithTexture));
        // }

        private void OnEnable()
        {
            canvasView = CreateInstance<CanvasView>();
            toolView = CreateInstance<ToolView>();
            model = new MeshDeleterWithTextureModel();
            localizedText = new LocalizedText();
            ChangeLanguage(localizedText.SelectedLanguage);
        }

        private void OnDisable()
        {
            model.Dispose();

            canvasView.Dispose();
            toolView.Dispose();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            // 检查本地化文本是否正确加载
            if (localizedText?.Data == null)
            {
                EditorGUILayout.HelpBox("语言包加载失败，请检查资源文件", MessageType.Error);
                return;
            }
            
            // TODO: ComputeShaderがAndroidBuildだと使えないから警告文を出す
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                DrawNotSupportBuildTarget();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GatoGUILayout.ObjectField(
                    localizedText.Data.rendererLabelText,
                    model.renderer,
                    renderer => model.OnChangeRenderer(canvasView, renderer)
                );

                GatoGUILayout.Button(
                    localizedText.Data.helpButtonText,
                    () => OpenHelpPage(),
                    GUILayout.Width(80f)
                );
            }

            GUILayout.Space(20);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    canvasView.Render(model.HasTexture(), CANVAS_SIZE_RAITO);

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

                toolView.Render(model, localizedText, canvasView, 1 - CANVAS_SIZE_RAITO);
            }

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
                GUILayout.Label(localizedText?.Data?.androidNotSupportMessageText ?? "不支持Android构建目标。\n请切换构建目标到PC平台");
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

        private void OpenHelpPage()
        {
            Application.OpenURL(HELP_PAGE_URL);
        }
    }
#endif
}

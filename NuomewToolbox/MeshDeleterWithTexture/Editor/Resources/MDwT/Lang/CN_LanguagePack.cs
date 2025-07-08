/*
 * 中文语言包创建脚本
 * 用于在Unity编辑器中创建中文语言包资源
 */

using UnityEngine;
using UnityEditor;
using Gatosyocora.MeshDeleterWithTexture.Models;

namespace Gatosyocora.MeshDeleterWithTexture.Editor
{
    public static class ChineseLanguagePackCreator
    {
        [MenuItem("Tools/MeshDeleter/Create Chinese Language Pack")]
        public static void CreateChineseLanguagePack()
        {
            // 创建中文语言包
            var chineseLanguagePack = ScriptableObject.CreateInstance<LanguagePack>();
            
            // 设置中文文本
            chineseLanguagePack.language = Language.CN;
            chineseLanguagePack.rendererLabelText = "渲染器";
            chineseLanguagePack.scaleLabelText = "缩放";
            chineseLanguagePack.resetButtonText = "重置";
            chineseLanguagePack.importDeleteMaskButtonText = "导入删除遮罩";
            chineseLanguagePack.exportDeleteMaskButtonText = "导出删除遮罩";
            chineseLanguagePack.dragAndDropDeleteMaskTextureAreaText = "拖拽删除遮罩纹理到此处";
            chineseLanguagePack.uvMapLineColorLabelText = "UV贴图线条颜色";
            chineseLanguagePack.exportUvMapButtonText = "导出UV贴图";
            chineseLanguagePack.textureLabelText = "纹理 (材质)";
            chineseLanguagePack.toolsTitleText = "工具";
            chineseLanguagePack.drawTypeLabelText = "绘制类型";
            chineseLanguagePack.penToolNameText = "画笔";
            chineseLanguagePack.eraserToolNameText = "橡皮擦";
            chineseLanguagePack.selectToolNameText = "选择";
            chineseLanguagePack.penColorLabelText = "画笔颜色";
            chineseLanguagePack.colorBlackButtonText = "黑色";
            chineseLanguagePack.colorRedButtonText = "红";
            chineseLanguagePack.colorGreenButtonText = "绿";
            chineseLanguagePack.colorBlueButtonText = "蓝";
            chineseLanguagePack.penEraserSizeLabelText = "画笔/橡皮擦大小";
            chineseLanguagePack.inverseSelectAreaButtonText = "反选区域";
            chineseLanguagePack.applySelectAreaButtonText = "应用选择区域";
            chineseLanguagePack.inverseFillAreaButtonText = "反向填充区域";
            chineseLanguagePack.clearAllDrawingButtonText = "清除所有绘制";
            chineseLanguagePack.undoDrawingButtonText = "撤销绘制";
            chineseLanguagePack.modelInformationTitleText = "模型信息";
            chineseLanguagePack.triangleCountLabelText = "三角面数量";
            chineseLanguagePack.outputMeshTitleText = "输出网格";
            chineseLanguagePack.saveFolderLabelText = "保存文件夹";
            chineseLanguagePack.selectFolderButtonText = "选择文件夹";
            chineseLanguagePack.outputFileNameLabelText = "文件名";
            chineseLanguagePack.revertMeshToPrefabButtonText = "恢复网格到预制体";
            chineseLanguagePack.revertMeshToPreviouslyButtonText = "恢复网格到之前状态";
            chineseLanguagePack.deleteMeshButtonText = "删除网格";
            chineseLanguagePack.androidNotSupportMessageText = "不支持Android构建目标。\n请切换构建目标到PC平台";
            chineseLanguagePack.errorDialogTitleText = "发生错误";
            chineseLanguagePack.notFoundVerticesExceptionDialogMessageText = "未找到要删除的顶点。";
            chineseLanguagePack.errorDialogOkText = "确定";
            chineseLanguagePack.helpButtonText = "帮助";
            
            // 保存资源
            string assetPath = "Assets/NuomewToolbox/MeshDeleterWithTexture/Editor/Resources/MDwT/Lang/CN.asset";
            AssetDatabase.CreateAsset(chineseLanguagePack, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"中文语言包已创建: {assetPath}");
            
            // 选中创建的资源
            Selection.activeObject = chineseLanguagePack;
            EditorGUIUtility.PingObject(chineseLanguagePack);
        }
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NyameauToolbox.Editor
{
    public class NyameauToolboxWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private int selectedToolIndex = 0;
        
        // UI样式
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle descriptionStyle;
        private GUIStyle toolTitleStyle;
        
        // 颜色主题
        private Color primaryColor = new Color(1f, 0.75f, 0.85f, 1f);   // 粉色
        private Color secondaryColor = new Color(0.9f, 0.85f, 1f, 1f);  // 淡紫色
        private Color accentColor = new Color(1f, 0.9f, 0.95f, 1f);     // 浅粉色
        
        // 工具列表
        private ToolInfo[] tools = new ToolInfo[]
        {
            new ToolInfo
            {
                name = "模型分析器",
                description = "分析VRChat模型的各项参数占用情况\n• Bits参数占用分析\n• 纹理显存占用统计\n• 动骨数量检查\n• 模型大小估算",
                icon = "🔍",
                action = () => VRChatAvatarAnalyzer.ShowWindow()
            },
            new ToolInfo
            {
                name = "网格删除器",
                description = "基于纹理的网格删除工具\n• 可视化纹理编辑\n• 精确网格删除\n• 实时预览效果\n• 支持多种绘制模式",
                icon = "✂️",
                action = () => MeshDeleterWindow.ShowWindow()
            },
            new ToolInfo
            {
                name = "动骨复制工具",
                description = "将Avatar Descriptor路径下物品A的动骨数据复制到物品B上，支持相同路径的一键复制",
                icon = "🔄",
                action = () => PhysBoneCopyWindow.ShowWindow()
            },
            new ToolInfo
            {
                name = "材质复制工具",
                description = "将材质A的各种设置复制到材质B上\n• 支持选择性复制各种属性\n• 基本设置、照明、UV、VRChat等\n• 颜色、发光、法线、特效设置",
                icon = "🎨",
                action = () => MaterialCopyWindow.ShowWindow()
            },
            new ToolInfo
            {
                name = "模型优化建议",
                description = "为您的VRChat模型提供优化建议\n• 纹理压缩建议\n• 网格优化提示\n• 性能优化指南",
                icon = "⚡",
                action = () => Debug.Log("模型优化建议工具即将推出！")
            },
            new ToolInfo
            {
                name = "批量处理工具",
                description = "批量处理模型文件\n• 批量纹理压缩\n• 批量材质优化\n• 批量导出设置",
                icon = "📦",
                action = () => Debug.Log("批量处理工具即将推出！")
            }
        };
        
        // 已移除工具箱主页显示
        // [MenuItem("诺喵工具箱/工具箱主页")]
        public static void ShowWindow()
        {
            var window = GetWindow<NyameauToolboxWindow>("诺喵工具箱");
            window.minSize = new Vector2(350, 300);
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
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            
            toolTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            
            buttonStyle = new GUIStyle("Button")
            {
                fontSize = 12,
                fixedHeight = 30,
                alignment = TextAnchor.MiddleCenter
            };
            
            descriptionStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 11,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawToolsList();
            DrawFooter();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            // 渐变背景效果
            GUI.backgroundColor = primaryColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("🌸 诺喵工具箱 🌸", headerStyle);
            GUILayout.Label("VRChat模型制作者的专属工具集", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(15);
        }
        
        private void DrawToolsList()
        {
            EditorGUILayout.LabelField("可用工具:", toolTitleStyle);
            EditorGUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < tools.Length; i++)
            {
                DrawToolCard(tools[i], i);
                EditorGUILayout.Space(10);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawToolCard(ToolInfo tool, int index)
        {
            // 卡片背景
            Color cardColor = index == selectedToolIndex ? accentColor : secondaryColor;
            GUI.backgroundColor = cardColor;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.BeginHorizontal();
            
            // 图标和标题
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            GUILayout.Label(tool.icon, new GUIStyle(EditorStyles.boldLabel) 
            { 
                fontSize = 24, 
                alignment = TextAnchor.MiddleCenter 
            }, GUILayout.Height(40));
            EditorGUILayout.EndVertical();
            
            // 工具信息
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(120), GUILayout.ExpandWidth(true));
            
            EditorGUILayout.LabelField(tool.name, new GUIStyle(EditorStyles.boldLabel) 
            { 
                fontSize = 14 
            });
            
            EditorGUILayout.LabelField(tool.description, descriptionStyle);
            
            EditorGUILayout.EndVertical();
            
            // 启动按钮
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            GUI.backgroundColor = primaryColor;
            
            if (GUILayout.Button("启动", buttonStyle))
            {
                selectedToolIndex = index;
                tool.action?.Invoke();
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("💡 提示:", EditorStyles.boldLabel);
            GUILayout.Label("选择工具后点击启动按钮来使用相应功能", descriptionStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📖 使用说明", GUILayout.Width(100)))
            {
                ShowHelpDialog();
            }
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label("版本: 1.0.7", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }
        
        private void ShowHelpDialog()
        {
            EditorUtility.DisplayDialog(
                "诺喵工具箱使用说明",
                "🌸 欢迎使用诺喵工具箱！🌸\n\n" +
                "模型分析器功能说明：\n" +
                "• 选择场景中的VRChat Avatar Descriptor\n" +
                "• 点击分析按钮查看各项参数占用\n" +
                "• 绿色表示安全，黄色表示警告，红色表示超限\n\n" +
                "参数限制说明：\n" +
                "• Bits参数: 最高256位\n" +
                "• 纹理显存: 最高500MB\n" +
                "• 动骨数量: 最高256个\n" +
                "• 解压后大小: 最高500MB\n" +
                "• 上传大小: 最高200MB\n\n" +
                "如有问题，请检查VRChat SDK是否正确安装。",
                "确定"
            );
        }
    }
    
    // 工具信息数据结构
    [System.Serializable]
    public class ToolInfo
    {
        public string name;
        public string description;
        public string icon;
        public System.Action action;
    }
}
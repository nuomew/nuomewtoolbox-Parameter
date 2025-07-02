using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine;
using System;

namespace NyameauToolbox.Editor
{
    public class VRChatAvatarAnalyzer : EditorWindow
    {
        // 分析结果数据
        private DetailedAnalysisResult analysisResult;
        private VRCAvatarDescriptor selectedAvatar;
        private Vector2 scrollPosition;
        private bool showDetailedTextureInfo = false;
        
        // 纹理压缩相关
        private List<TextureCompressionInfo> textureCompressionList;
        private Vector2 textureScrollPosition;
        private bool showTextureCompression = false;
        
        // 网格详细信息相关
        private List<MeshDetailInfo> meshDetailList;
        private Vector2 meshScrollPosition;
        private bool showMeshDetails = false;
        private bool showDetailedMeshInfo = false;
        
        // 层级文件树状结构相关
        private bool showHierarchyTree = false;
        private Vector2 hierarchyScrollPosition;
        private Dictionary<Transform, bool> expandedNodes = new Dictionary<Transform, bool>();
        private Dictionary<Transform, Color> nodeColors = new Dictionary<Transform, Color>();
        private Color[] treeColors = new Color[] 
        {
            new Color(0.2f, 0.8f, 0.2f),    // 绿色
            new Color(0.2f, 0.2f, 0.8f),    // 蓝色
            new Color(0.8f, 0.2f, 0.2f),    // 红色
            new Color(0.8f, 0.8f, 0.2f),    // 黄色
            new Color(0.8f, 0.2f, 0.8f),    // 紫色
            new Color(0.2f, 0.8f, 0.8f),    // 青色
            new Color(1.0f, 0.5f, 0.0f),    // 橙色
            new Color(0.5f, 0.0f, 0.5f),    // 深紫色
            new Color(0.0f, 0.5f, 0.0f),    // 深绿色
            new Color(0.5f, 0.5f, 0.0f)     // 橄榄色
        };
        private int colorIndex = 0;
        private float lastHierarchyUpdateTime = 0f;
        private const float HIERARCHY_UPDATE_INTERVAL = 0.2f; // 5帧刷新间隔
        
        private bool isDecryptionValid = true;
        private string decryptionErrorMessage = "";
        
        // 纹理格式BPP映射
        private static readonly Dictionary<TextureFormat, float> BPP = new Dictionary<TextureFormat, float>
        {
            { TextureFormat.Alpha8, 8 },
            { TextureFormat.ARGB4444, 16 },
            { TextureFormat.RGB24, 24 },
            { TextureFormat.RGBA32, 32 },
            { TextureFormat.ARGB32, 32 },
            { TextureFormat.RGB565, 16 },
            { TextureFormat.R16, 16 },
            { TextureFormat.DXT1, 4 },
            { TextureFormat.DXT5, 8 },
            { TextureFormat.RGBA4444, 16 },
            { TextureFormat.BGRA32, 32 },
            { TextureFormat.RHalf, 16 },
            { TextureFormat.RGHalf, 32 },
            { TextureFormat.RGBAHalf, 64 },
            { TextureFormat.RFloat, 32 },
            { TextureFormat.RGFloat, 64 },
            { TextureFormat.RGBAFloat, 128 },
            { TextureFormat.YUY2, 16 },
            { TextureFormat.RGB9e5Float, 32 },
            { TextureFormat.BC4, 4 },
            { TextureFormat.BC5, 8 },
            { TextureFormat.BC6H, 8 },
            { TextureFormat.BC7, 8 },
            { TextureFormat.DXT1Crunched, 4 },
            { TextureFormat.DXT5Crunched, 8 },
            { TextureFormat.PVRTC_RGB2, 2 },
            { TextureFormat.PVRTC_RGBA2, 2 },
            { TextureFormat.PVRTC_RGB4, 4 },
            { TextureFormat.PVRTC_RGBA4, 4 },
            { TextureFormat.ETC_RGB4, 4 },
            { TextureFormat.EAC_R, 4 },
            { TextureFormat.EAC_R_SIGNED, 4 },
            { TextureFormat.EAC_RG, 8 },
            { TextureFormat.EAC_RG_SIGNED, 8 },
            { TextureFormat.ETC2_RGB, 4 },
            { TextureFormat.ETC2_RGBA1, 4 },
            { TextureFormat.ETC2_RGBA8, 8 },
            { TextureFormat.ASTC_4x4, 8 },
            { TextureFormat.ASTC_5x5, 5.12f },
            { TextureFormat.ASTC_6x6, 3.56f },
            { TextureFormat.ASTC_8x8, 2 },
            { TextureFormat.ASTC_10x10, 1.28f },
            { TextureFormat.ASTC_12x12, 0.89f },
            { TextureFormat.RG16, 16 },
            { TextureFormat.R8, 8 },
            { TextureFormat.ETC_RGB4Crunched, 4 },
            { TextureFormat.ETC2_RGBA8Crunched, 8 }
        };
        
        [System.Serializable]
        public class TextureCompressionInfo
        {
            public Texture texture;
            public string name;
            public int width;
            public int height;
            public long sizeBytes;
            public string sizeMB;
            public bool isActive;
            public float BPP;
            public int minBPP;
            public string formatString;
            public TextureFormat format;
            public bool hasAlpha;
            public List<Material> materials;
            public bool materialDropDown;
            
            // 压缩选项
            public TextureImporterFormat newFormat;
            public int newMaxSize;
            public bool compressionChanged;
        }
        
        [System.Serializable]
        public class MeshDetailInfo
        {
            public Mesh mesh;
            public string name;
            public long sizeBytes;
            public string sizeMB;
            public bool isActive;
            public int vertexCount;
            public int triangleCount;
            public int blendShapeCount;
            public bool hasBlendShapes;
            public long blendShapeVRAMSize;
            public long vertexAttributeVRAMSize;
        }
        
        // 网格质量阈值常量 (MiB)
        private const long PC_MESH_MEMORY_EXCELLENT_MiB = 10;
        private const long PC_MESH_MEMORY_GOOD_MiB = 25;
        private const long PC_MESH_MEMORY_MEDIUM_MiB = 50;
        private const long PC_MESH_MEMORY_POOR_MiB = 100;
        
        private const long QUEST_MESH_MEMORY_EXCELLENT_MiB = 5;
        private const long QUEST_MESH_MEMORY_GOOD_MiB = 10;
        private const long QUEST_MESH_MEMORY_MEDIUM_MiB = 20;
        private const long QUEST_MESH_MEMORY_POOR_MiB = 40;
        
        // 顶点属性字节大小映射
        private static readonly Dictionary<VertexAttributeFormat, int> VertexAttributeByteSize = new Dictionary<VertexAttributeFormat, int>()
        {
            { VertexAttributeFormat.UNorm8, 1},
            { VertexAttributeFormat.SNorm8, 1},
            { VertexAttributeFormat.UInt8, 1},
            { VertexAttributeFormat.SInt8, 1},
            { VertexAttributeFormat.UNorm16, 2},
            { VertexAttributeFormat.SNorm16, 2},
            { VertexAttributeFormat.UInt16, 2},
            { VertexAttributeFormat.SInt16, 2},
            { VertexAttributeFormat.Float16, 2},
            { VertexAttributeFormat.Float32, 4},
            { VertexAttributeFormat.UInt32, 4},
            { VertexAttributeFormat.SInt32, 4},
        };
        
        // UI样式
        private GUIStyle headerStyle;
        private GUIStyle warningStyle;
        private GUIStyle normalStyle;
        private GUIStyle titleStyle;
        private GUIStyle smallStyle;
        
        // 颜色主题
        private Color primaryColor = new Color(1f, 0.75f, 0.85f, 1f); // 粉色
        private Color warningColor = new Color(1f, 0.6f, 0.6f, 1f);   // 红色警告
        private Color safeColor = new Color(0.7f, 1f, 0.7f, 1f);      // 绿色安全
        
        [MenuItem("诺喵工具箱/VRChat模型分析器")]
        public static void ShowWindow()
        {
            var window = GetWindow<VRChatAvatarAnalyzer>("诺喵工具箱 - VRChat模型分析器");
            window.minSize = new Vector2(450, 700);
            window.Show();
        }
        
        private void OnEnable()
        {
            InitializeStyles();
            
            ValidateDecryption();
        }
        
        private void ValidateDecryption()
        {
            try
            {
                string testResult = DecryptContent("5rWL6K+V");
                if (testResult != "测试")
                {
                    isDecryptionValid = false;
                    decryptionErrorMessage = DecryptContent("6Kej5a+G6aqM6K+B5aSx6LSlOua1i+ivleWtl+espuS4suino+WvhuaOkOaenOS4jeWMuemFjQ==");
                    Debug.LogError(DecryptContent("W+ivuuWWtOW3peWFt+eusV0g6Kej5a+G5Yqf6IO96aqM6K+B5aSx6LSlLOaPkuS7tuWKn+iDveW3suemgeeUqOOAgg=="));
                }
                else
                {
                    Debug.Log("[诺喵工具箱] 解密功能验证成功。");
                }
            }
            catch (System.Exception ex)
            {
                isDecryptionValid = false;
                decryptionErrorMessage = $"{DecryptContent("6Kej5a+G6aqM6K+B5byC5bi4OiA=")}{ex.Message}";
                Debug.LogError($"{DecryptContent("W+ivuuWWtOW3peWFt+eusV0g6Kej5a+G5Yqf6IO96aqM6K+B5byC5bi4LOaPkuS7tuWKn+iDveW3suemgeeUqOOAguS4jeivr++8miA=")}{ex.Message}");
            }
        }
        
        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            
            warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
                fontStyle = FontStyle.Bold
            };
            
            normalStyle = new GUIStyle(EditorStyles.label);
            
            smallStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
        }
        
        private void OnGUI()
        {
            if (!isDecryptionValid)
            {
                DrawDecryptionErrorMessage();
                return;
            }
            
            DrawHeader();
            DrawAvatarSelection();
            
            if (selectedAvatar != null)
            {
                DrawAnalysisButton();
                
                if (analysisResult != null)
                {
                    DrawAnalysisResults();
                }
            }
            else
            {
                DrawNoAvatarMessage();
                
                // 只在未选择模型时显示开源声明
                DrawOpenSourceDeclaration();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            // 绘制可爱的标题
            GUI.backgroundColor = primaryColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("🌸 诺喵工具箱 🌸", headerStyle);
            GUILayout.Label("VRChat模型参数统计 V1.0.2 By.诺喵", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(10);
        }
        
        private void DrawAvatarSelection()
        {
            EditorGUILayout.LabelField("选择VRChat模型:", titleStyle);
            
            var newAvatar = EditorGUILayout.ObjectField(
                "Avatar Descriptor:", 
                selectedAvatar, 
                typeof(VRCAvatarDescriptor), 
                true
            ) as VRCAvatarDescriptor;
            
            if (newAvatar != selectedAvatar)
            {
                selectedAvatar = newAvatar;
                analysisResult = null; // 清除之前的分析结果
            }
        }
        
        private void DrawAnalysisButton()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = primaryColor;
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            if (GUILayout.Button("开始分析模型参数", buttonStyle, GUILayout.Height(35)))
            {
                AnalyzeAvatar();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
        
        private string DecryptContent(string encryptedContent)
        {
            try
            {
                byte[] data = System.Convert.FromBase64String(encryptedContent);
                return System.Text.Encoding.UTF8.GetString(data);
            }
            catch (System.Exception ex)
            {
                isDecryptionValid = false;
                decryptionErrorMessage = $"{DecryptContent("6Kej5a+G5aSx6LSlOiA=")}: {ex.Message}";
                Debug.LogError($"{DecryptContent("W+ivuuWWtOW3peWFt+eusV0g5YaF5a655Kej5a+G5aSx6LSlLOaPkuS7tuWKn+iDveWwhuiiq+emgeeUqOOAguS4jeivr+S/oeaBr++8miA=")}{ex.Message}");
                return DecryptContent("5YaF5a655Kej5p6Q5aSx6LSlIC0g5o+S5Lu25Yqf6IO95bey56aB55So");
            }
        }
        
        private void DrawNoAvatarMessage()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUI.backgroundColor = primaryColor;
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            GUILayout.Button("模型解析", buttonStyle, GUILayout.Height(35));
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var adTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = new Color(1f, 0.6f, 0.2f) } // 橙色
            };
            EditorGUILayout.LabelField(DecryptContent("5oGw6aWt5bm/5ZGK"), adTitleStyle);
            
            var adContentStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = true,
                richText = true
            };
            
            EditorGUILayout.LabelField(DecryptContent("MTUw5o6lVlJDaGF05qih5Z6L5a6a5Yi277yM5Y+q5pS55aWz5qih5Z6L"), adContentStyle);
            EditorGUILayout.LabelField(DecryptContent("5a6a6YeRMzAl6Lez5Y2V5omj6Zmk5a6a6YeR77yM5qih5Z6L5Lu75L2VYnVn6IGU57O75oiR5YWN6LS55L+u5aSN"), adContentStyle);
            EditorGUILayout.LabelField(DecryptContent("5qih5Z6L6LSo5L+dMzDlpKnvvIzku7vkvZXooaPmnI3niannkIblnYflj6/lhY3otLnmt7vliqA="), adContentStyle);
            EditorGUILayout.LabelField(DecryptContent("6Iez5q2i5Y+C5pWwMjU2L+WKqOmqqDI1Ni/kuIrkvKDmnIDlpKcyMDAv5pi+5a2Y5pyA5aSnNTAwL+ino+WOi+acgOWkpzUwMOS4uuatoQ=="), adContentStyle);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawOpenSourceDeclaration()
        {
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.Space(20);
            
            EditorGUILayout.BeginVertical();
            
            var declarationStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 10,
                normal = { textColor = Color.gray },
                wordWrap = true
            };
            
            GUILayout.Label(DecryptContent("5pys5bel5YW35Z+65LqO6Z2e5ZWG5Lia5L2/55So5byA5rqQ5Y2P6K6u5Y+R5biD"), declarationStyle);
            GUILayout.Label(DecryptContent("5YWB6K645YWN6LS55L2/55So5ZKM5p+l55yL5rqQ5Luj56CB77yM5L2G56aB5q2i5Lu75L2V5ZWG5Lia55So6YCU"), declarationStyle);
            GUILayout.Label(DecryptContent("5rqQ5Luj56CB5omY566h5LqOIEdpdEh1Yu+8jOasoui/jumdnuWVhuS4muaAp+i0qOeahOi0oeeMruWSjOWPjemlgg=="), declarationStyle);
            GUILayout.Label(DecryptContent("5pS55qih5Lqk5rWBUVHnvqTvvJo1ODg2MDUyODk="), declarationStyle);
            GUILayout.Label(DecryptContent("Qnku6K+65ZWtIFFROjE4MzQ1MjU4NCBEaXNjb3Jk77yabnVvbWV3"), declarationStyle);
            GUILayout.Label(DecryptContent("wqkgMjAyNSDor7rllLXlt6XlhbfnrrEgLSDku4XkvpvpnZ7llYbkuJrlhY3otLnkvb/nlKg="), declarationStyle);

            
            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAnalysisResults()
        {
            EditorGUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // 总体评分
            DrawOverallScore();
            
            // Bits参数占用
            DrawParameterSection("Bits参数占用", analysisResult.bitsUsage, 
                VRChatParameterCalculator.MAX_BITS, "bits");
            
            // 纹理显存占用
            DrawParameterSection("纹理显存占用", analysisResult.textureResult.totalMemoryMB, 
                VRChatParameterCalculator.MAX_TEXTURE_MEMORY_MB, "MiB");
            
            // 动骨占用
            DrawParameterSection("动骨总数量", analysisResult.dynamicBoneCount, 
                VRChatParameterCalculator.MAX_DYNAMIC_BONES, "个");
            
            // 模型解压后大小
            DrawParameterSection("模型解压后大小", analysisResult.totalUncompressedSizeMB, 
                VRChatParameterCalculator.MAX_UNCOMPRESSED_SIZE_MB, "MiB");
            
            // 模型上传大小
            DrawParameterSection("模型上传大小", analysisResult.estimatedUploadSizeMB, 
                VRChatParameterCalculator.MAX_UPLOAD_SIZE_MB, "MiB");
                

            // 详细信息
            DrawDetailedInfo();
            
            // 层级文件树状结构
            DrawHierarchyTree();
            
            // 纹理详细信息
            DrawTextureDetails();
            
            // 网格详细信息
            DrawMeshDetails();
            
            // 优化建议
            DrawOptimizationSuggestions();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawOverallScore()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("总体评分", titleStyle);
            
            float overallScore = CalculateOverallScore();
            string scoreText = GetScoreText(overallScore);
            Color scoreColor = GetScoreColor(overallScore);
            
            var scoreStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                normal = { textColor = scoreColor },
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField(scoreText, scoreStyle);
            
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, overallScore / 100f, $"{overallScore:F0}分");
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private float CalculateOverallScore()
        {
            float score = 100f;
            
            // Bits参数扣分
            float bitsPercentage = analysisResult.bitsUsage / VRChatParameterCalculator.MAX_BITS;
            if (bitsPercentage > 1.0f) score -= 25f;
            else if (bitsPercentage > 0.8f) score -= 15f;
            
            // 纹理扣分
            float texturePercentage = analysisResult.textureResult.totalMemoryMB / VRChatParameterCalculator.MAX_TEXTURE_MEMORY_MB;
            if (texturePercentage > 1.0f) score -= 30f;
            else if (texturePercentage > 0.8f) score -= 20f;
            
            // 动骨扣分
            float bonePercentage = (float)analysisResult.dynamicBoneCount / VRChatParameterCalculator.MAX_DYNAMIC_BONES;
            if (bonePercentage > 1.0f) score -= 20f;
            else if (bonePercentage > 0.8f) score -= 10f;
            
            // 大小扣分
            float sizePercentage = analysisResult.totalUncompressedSizeMB / VRChatParameterCalculator.MAX_UNCOMPRESSED_SIZE_MB;
            if (sizePercentage > 1.0f) score -= 25f;
            else if (sizePercentage > 0.8f) score -= 15f;
            
            return Mathf.Max(0f, score);
        }
        
        private string GetScoreText(float score)
        {
            if (score >= 90f) return "🌟 优秀";
            if (score >= 75f) return "😊 良好";
            if (score >= 60f) return "😐 一般";
            if (score >= 40f) return "😟 需要优化";
            return "😰 问题较多";
        }
        
        private Color GetScoreColor(float score)
        {
            if (score >= 75f) return safeColor;
            if (score >= 50f) return Color.yellow;
            return warningColor;
        }
        
        private void DrawParameterSection(string title, float current, float max, string unit)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 标题
            EditorGUILayout.LabelField(title, titleStyle);
            
            // 进度条
            float percentage = current / max;
            Color barColor = percentage > 0.8f ? warningColor : 
                           percentage > 0.6f ? Color.yellow : safeColor;
            
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, Mathf.Min(percentage, 1f), $"{current:F0} / {max} {unit}");
            
            // 状态文字
            string status = percentage > 1.0f ? "⚠️ 超出限制!" : 
                           percentage > 0.8f ? "⚠️ 接近限制" : "✅ 正常";
            
            GUIStyle statusStyle = percentage > 0.8f ? warningStyle : normalStyle;
            EditorGUILayout.LabelField(status, statusStyle);
            
            // 如果超出限制，显示具体超出量
            if (percentage > 1.0f)
            {
                float excess = current - max;
                EditorGUILayout.LabelField($"超出: {excess:F0} {unit}", warningStyle);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void DrawDetailedInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("详细信息", titleStyle);
            
            EditorGUILayout.LabelField($"总纹理数量: {analysisResult.textureResult.textureCount}");
            EditorGUILayout.LabelField($"网格数量: {analysisResult.meshCount}");
            EditorGUILayout.LabelField($"材质数量: {analysisResult.materialCount}");
            EditorGUILayout.LabelField($"动画控制器数量: {analysisResult.animatorCount}");
            EditorGUILayout.LabelField($"总顶点数: {analysisResult.modelSize.vertexCount:N0}");
            EditorGUILayout.LabelField($"总三角形数: {analysisResult.modelSize.triangleCount:N0}");
            EditorGUILayout.LabelField($"当前模型动骨组件总数: {analysisResult.dynamicBoneCount}个");
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void DrawTextureDetails()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("纹理详细信息", titleStyle);
            
            if (GUILayout.Button(showDetailedTextureInfo ? "隐藏" : "显示", 
                GUILayout.Width(60)))
            {
                showDetailedTextureInfo = !showDetailedTextureInfo;
                
                // 初始化纹理压缩列表
                if (showDetailedTextureInfo && textureCompressionList == null && analysisResult != null)
                {
                    InitializeTextureCompressionList();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (showDetailedTextureInfo && analysisResult.textureResult.textureInfos.Count > 0)
            {
                EditorGUILayout.Space(5);
                
                // 纹理压缩选项
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("纹理压缩选项", EditorStyles.boldLabel);
                if (GUILayout.Button(showTextureCompression ? "隐藏压缩选项" : "显示压缩选项", GUILayout.Width(100)))
                {
                    showTextureCompression = !showTextureCompression;
                }
                EditorGUILayout.EndHorizontal();
                
                if (showTextureCompression)
                {
                    DrawTextureCompressionOptions();
                }
                else
                {
                    // 原有的纹理列表显示
                    EditorGUILayout.LabelField("最大纹理文件 (前10个):", EditorStyles.boldLabel);
                    
                    foreach (var texture in analysisResult.textureResult.textureInfos.Take(10))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"• {texture.name}", GUILayout.MinWidth(150));
                        EditorGUILayout.LabelField($"{texture.width}x{texture.height}", GUILayout.Width(80));
                        EditorGUILayout.LabelField(texture.format, GUILayout.Width(80));
                        EditorGUILayout.LabelField($"{texture.sizeMB:F1} MB", GUILayout.Width(60));
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void InitializeTextureCompressionList()
         {
             textureCompressionList = new List<TextureCompressionInfo>();
             
             if (analysisResult == null || selectedAvatar == null) return;
             
             // 获取所有材质和纹理的映射
             Dictionary<Texture, List<Material>> textureToMaterials = GetMaterialsUsingTextures(selectedAvatar.gameObject);
             
             // 直接从材质中获取纹理，而不是从analysisResult
             foreach (var kvp in textureToMaterials)
             {
                 Texture texture = kvp.Key;
                 if (texture == null) continue;
                 
                 TextureCompressionInfo compressionInfo = new TextureCompressionInfo
                 {
                     texture = texture,
                     name = texture.name,
                     width = texture.width,
                     height = texture.height,
                     sizeBytes = Profiler.GetRuntimeMemorySizeLong(texture),
                     sizeMB = $"{Profiler.GetRuntimeMemorySizeLong(texture) / (1024f * 1024f):F2}",
                     isActive = true,
                     materials = kvp.Value,
                     materialDropDown = false,
                     compressionChanged = false,
                     newMaxSize = Math.Max(texture.width, texture.height) // 初始化为当前最大尺寸
                 };
                 
                 // 获取纹理格式
                 if (texture is Texture2D tex2D)
                 {
                     compressionInfo.format = tex2D.format;
                     compressionInfo.formatString = tex2D.format.ToString();
                     compressionInfo.newFormat = GetRecommendedFormat(tex2D); // 设置推荐格式
                     
                     if (BPP.TryGetValue(tex2D.format, out float bpp))
                     {
                         compressionInfo.BPP = bpp;
                     }
                     else
                     {
                         compressionInfo.BPP = 16; // 默认值
                     }
                     
                     // 检查是否有Alpha通道
                     string path = AssetDatabase.GetAssetPath(texture);
                     if (!string.IsNullOrWhiteSpace(path))
                     {
                         AssetImporter assetImporter = AssetImporter.GetAtPath(path);
                         if (assetImporter is TextureImporter textureImporter)
                         {
                             compressionInfo.hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();
                             compressionInfo.minBPP = (compressionInfo.hasAlpha || textureImporter.textureType == TextureImporterType.NormalMap) ? 8 : 4;
                         }
                     }
                 }
                 else
                 {
                     compressionInfo.formatString = "Unknown";
                     compressionInfo.BPP = 16;
                     compressionInfo.newFormat = TextureImporterFormat.BC7;
                 }
                 
                 textureCompressionList.Add(compressionInfo);
             }
             
             // 按大小排序
             textureCompressionList.Sort((t1, t2) => t2.sizeBytes.CompareTo(t1.sizeBytes));
         }
         
         private TextureImporterFormat GetRecommendedFormat(Texture2D texture)
         {
             string path = AssetDatabase.GetAssetPath(texture);
             if (string.IsNullOrEmpty(path)) return TextureImporterFormat.BC7;
             
             TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
             if (textureImporter == null) return TextureImporterFormat.BC7;
             
             // 根据纹理类型推荐格式
             if (textureImporter.textureType == TextureImporterType.NormalMap)
             {
                 return TextureImporterFormat.BC5; // 法线贴图使用BC5
             }
             else if (textureImporter.DoesSourceTextureHaveAlpha())
             {
                 return TextureImporterFormat.BC7; // 有Alpha通道使用BC7
             }
             else
             {
                 return TextureImporterFormat.DXT1; // 无Alpha通道使用DXT1
             }
         }
        
        private Dictionary<Texture, List<Material>> GetMaterialsUsingTextures(GameObject avatar)
         {
             Dictionary<Texture, List<Material>> result = new Dictionary<Texture, List<Material>>();
             
             // 获取所有渲染器
             Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);
             
             foreach (Renderer renderer in renderers)
             {
                 foreach (Material material in renderer.sharedMaterials)
                 {
                     if (material == null) continue;
                     
                     // 获取材质的所有属性
                     Shader shader = material.shader;
                     if (shader == null) continue;
                     
                     int propertyCount = ShaderUtil.GetPropertyCount(shader);
                     for (int i = 0; i < propertyCount; i++)
                     {
                         if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                         {
                             string propertyName = ShaderUtil.GetPropertyName(shader, i);
                             Texture texture = material.GetTexture(propertyName);
                             
                             if (texture != null)
                             {
                                 if (!result.ContainsKey(texture))
                                 {
                                     result[texture] = new List<Material>();
                                 }
                                 
                                 if (!result[texture].Contains(material))
                                 {
                                     result[texture].Add(material);
                                 }
                             }
                         }
                     }
                 }
             }
             
             return result;
         }
         
         private void DrawTextureCompressionOptions()
         {
             if (textureCompressionList == null || textureCompressionList.Count == 0)
             {
                 EditorGUILayout.LabelField("没有找到纹理");
                 return;
             }
             
             EditorGUILayout.Space(5);
             
             // 压缩建议
             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
             EditorGUILayout.LabelField("压缩建议:", EditorStyles.boldLabel);
             EditorGUILayout.LabelField("• BC7: 高质量，支持Alpha通道 (8 BPP)");
             EditorGUILayout.LabelField("• DXT5: 中等质量，支持Alpha通道 (8 BPP)");
             EditorGUILayout.LabelField("• DXT1: 低质量，不支持Alpha通道 (4 BPP)");
             EditorGUILayout.EndVertical();
             
             EditorGUILayout.Space(5);
             
             // 应用所有更改按钮
             bool hasChanges = textureCompressionList.Any(t => t.compressionChanged);
             EditorGUI.BeginDisabledGroup(!hasChanges);
             if (GUILayout.Button("应用所有压缩更改", GUILayout.Height(30)))
             {
                 ApplyAllCompressionChanges();
             }
             EditorGUI.EndDisabledGroup();
             
             EditorGUILayout.Space(5);
             
             // 纹理列表
             textureScrollPosition = EditorGUILayout.BeginScrollView(textureScrollPosition, GUILayout.Height(300));
             
             for (int i = 0; i < textureCompressionList.Count; i++)
             {
                 var texInfo = textureCompressionList[i];
                 
                 EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                 
                 // 纹理基本信息
                 EditorGUILayout.BeginHorizontal();
                 EditorGUILayout.LabelField(texInfo.name, EditorStyles.boldLabel, GUILayout.MinWidth(200));
                 EditorGUILayout.LabelField($"{texInfo.width}x{texInfo.height}", GUILayout.Width(80));
                 EditorGUILayout.LabelField(texInfo.formatString, GUILayout.Width(80));
                 EditorGUILayout.LabelField($"{texInfo.sizeMB} MB", GUILayout.Width(80));
                 EditorGUILayout.EndHorizontal();
                 
                 // 压缩选项
                 EditorGUILayout.BeginHorizontal();
                 
                 // 格式选择
                 EditorGUILayout.LabelField("新格式:", GUILayout.Width(50));
                 TextureImporterFormat newFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup(texInfo.newFormat, GUILayout.Width(100));
                 if (newFormat != texInfo.newFormat)
                 {
                     texInfo.newFormat = newFormat;
                     texInfo.compressionChanged = true;
                 }
                 
                 // 快速格式按钮
                 if (GUILayout.Button("BC7", GUILayout.Width(40)))
                 {
                     texInfo.newFormat = TextureImporterFormat.BC7;
                     texInfo.compressionChanged = true;
                 }
                 if (GUILayout.Button("DXT5", GUILayout.Width(40)))
                 {
                     texInfo.newFormat = TextureImporterFormat.DXT5;
                     texInfo.compressionChanged = true;
                 }
                 if (GUILayout.Button("DXT1", GUILayout.Width(40)))
                 {
                     texInfo.newFormat = TextureImporterFormat.DXT1;
                     texInfo.compressionChanged = true;
                 }
                 
                 EditorGUILayout.EndHorizontal();
                 
                 // 分辨率选项
                 EditorGUILayout.BeginHorizontal();
                 EditorGUILayout.LabelField("最大尺寸:", GUILayout.Width(60));
                 int newMaxSize = EditorGUILayout.IntPopup(texInfo.newMaxSize, 
                     new string[] { "32", "64", "128", "256", "512", "1024", "2048", "4096" },
                     new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 }, GUILayout.Width(80));
                 if (newMaxSize != texInfo.newMaxSize)
                 {
                     texInfo.newMaxSize = newMaxSize;
                     texInfo.compressionChanged = true;
                 }
                 
                 // 快速分辨率按钮
                 if (GUILayout.Button("2K", GUILayout.Width(30)))
                 {
                     texInfo.newMaxSize = 2048;
                     texInfo.compressionChanged = true;
                 }
                 if (GUILayout.Button("1K", GUILayout.Width(30)))
                 {
                     texInfo.newMaxSize = 1024;
                     texInfo.compressionChanged = true;
                 }
                 if (GUILayout.Button("512", GUILayout.Width(35)))
                 {
                     texInfo.newMaxSize = 512;
                     texInfo.compressionChanged = true;
                 }
                 
                 // 计算节省的大小
                 if (texInfo.compressionChanged)
                 {
                     float savedSize = CalculateSavedSize(texInfo);
                     EditorGUILayout.LabelField($"节省: {savedSize:F1} MB", GUILayout.Width(80));
                 }
                 
                 EditorGUILayout.EndHorizontal();
                 
                 // 材质信息
                 if (texInfo.materials.Count > 0)
                 {
                     EditorGUILayout.BeginHorizontal();
                     texInfo.materialDropDown = EditorGUILayout.Foldout(texInfo.materialDropDown, $"使用此纹理的材质 ({texInfo.materials.Count})");
                     EditorGUILayout.EndHorizontal();
                     
                     if (texInfo.materialDropDown)
                     {
                         EditorGUI.indentLevel++;
                         foreach (var material in texInfo.materials)
                         {
                             EditorGUILayout.BeginHorizontal();
                             EditorGUILayout.ObjectField(material, typeof(Material), false, GUILayout.Width(200));
                             EditorGUILayout.EndHorizontal();
                         }
                         EditorGUI.indentLevel--;
                     }
                 }
                 
                 EditorGUILayout.EndVertical();
                 EditorGUILayout.Space(2);
             }
             
             EditorGUILayout.EndScrollView();
         }
         
         private float CalculateSavedSize(TextureCompressionInfo texInfo)
         {
             // 简化的大小计算
             float currentSizeMB = float.Parse(texInfo.sizeMB);
             
             // 根据新格式和分辨率计算新大小
             float resolutionScale = (float)texInfo.newMaxSize / Math.Max(texInfo.width, texInfo.height);
             if (resolutionScale > 1.0f) resolutionScale = 1.0f;
             
             float newBPP = GetBPPForFormat(texInfo.newFormat);
             float newSizeMB = currentSizeMB * (newBPP / texInfo.BPP) * (resolutionScale * resolutionScale);
             
             return Math.Max(0, currentSizeMB - newSizeMB);
         }
         
         private float GetBPPForFormat(TextureImporterFormat format)
         {
             switch (format)
             {
                 case TextureImporterFormat.BC7: return 8;
                 case TextureImporterFormat.DXT5: return 8;
                 case TextureImporterFormat.DXT1: return 4;
                 case TextureImporterFormat.BC4: return 4;
                 case TextureImporterFormat.BC5: return 8;
                 case TextureImporterFormat.BC6H: return 8;
                 default: return 8;
             }
         }
         
         private void ApplyAllCompressionChanges()
         {
             if (EditorUtility.DisplayDialog("确认压缩", "确定要应用所有纹理压缩更改吗？这个操作不可撤销。", "确定", "取消"))
             {
                 int changedCount = 0;
                 
                 foreach (var texInfo in textureCompressionList)
                 {
                     if (texInfo.compressionChanged)
                     {
                         ApplyTextureCompression(texInfo);
                         changedCount++;
                     }
                 }
                 
                 EditorUtility.DisplayDialog("压缩完成", $"已成功压缩 {changedCount} 个纹理。", "确定");
                 
                 // 重新分析
                 AnalyzeAvatar();
             }
         }
         
         private void ApplyTextureCompression(TextureCompressionInfo texInfo)
         {
             string path = AssetDatabase.GetAssetPath(texInfo.texture);
             if (string.IsNullOrEmpty(path)) return;
             
             TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
             if (textureImporter == null) return;
             
             // 设置新的压缩格式
             var platformSettings = textureImporter.GetPlatformTextureSettings("Standalone");
             platformSettings.format = texInfo.newFormat;
             platformSettings.maxTextureSize = texInfo.newMaxSize;
             platformSettings.overridden = true;
             
             textureImporter.SetPlatformTextureSettings(platformSettings);
             
             // 重新导入纹理
             AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
             
             texInfo.compressionChanged = false;
         }
        
        private void DrawOptimizationSuggestions()
        {
            var suggestions = GenerateOptimizationSuggestions();
            if (suggestions.Count == 0) return;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔧 优化建议", titleStyle);
            
            foreach (var suggestion in suggestions)
            {
                EditorGUILayout.LabelField($"• {suggestion}", smallStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private List<string> GenerateOptimizationSuggestions()
        {
            var suggestions = new List<string>();
            
            // Bits参数建议
            if (analysisResult.bitsUsage > VRChatParameterCalculator.MAX_BITS)
            {
                suggestions.Add("Bits参数超出限制，请减少Animator参数或优化表情参数配置");
            }
            
            // 纹理建议
            if (analysisResult.textureResult.totalMemoryMB > VRChatParameterCalculator.MAX_TEXTURE_MEMORY_MB)
            {
                suggestions.Add("纹理显存超出限制，建议压缩大尺寸纹理或降低纹理分辨率");
                
                var largeTextures = analysisResult.textureResult.textureInfos
                    .Where(t => t.sizeMB > 20f).ToList();
                if (largeTextures.Count > 0)
                {
                    suggestions.Add($"发现{largeTextures.Count}个大纹理(>20MB)，建议优先优化这些纹理");
                }
            }
            
            // 动骨建议
            if (analysisResult.dynamicBoneCount > VRChatParameterCalculator.MAX_DYNAMIC_BONES)
            {
                suggestions.Add("动骨数量超出限制，请减少不必要的PhysBone组件");
            }
            
            // 模型大小建议
            if (analysisResult.totalUncompressedSizeMB > VRChatParameterCalculator.MAX_UNCOMPRESSED_SIZE_MB)
            {
                suggestions.Add("模型解压后大小超限，建议优化网格和纹理");
            }
            
            // 顶点数建议
            if (analysisResult.modelSize.vertexCount > 100000)
            {
                suggestions.Add("顶点数较高，建议优化网格拓扑或使用LOD");
            }
            
            return suggestions;
        }
        
        private void AnalyzeAvatar()
        {
            if (selectedAvatar == null)
            {
                analysisResult = null;
                textureCompressionList = null; // 重置纹理压缩列表
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("分析中", "正在分析模型参数...", 0f);
                
                analysisResult = new DetailedAnalysisResult();
                
                // 重置纹理压缩列表，下次显示时重新初始化
                textureCompressionList = null;
                
                // 重置网格详细信息列表，下次显示时重新初始化
                meshDetailList = null;
                
                // 重置层级树状结构数据
                expandedNodes.Clear();
                nodeColors.Clear();
                colorIndex = 0;
                lastHierarchyUpdateTime = 0f;
                
                // 使用新的计算器进行分析
                EditorUtility.DisplayProgressBar("分析中", "计算Bits参数...", 0.2f);
                analysisResult.bitsUsage = VRChatParameterCalculator.CalculateBitsUsage(selectedAvatar);
                
                EditorUtility.DisplayProgressBar("分析中", "分析纹理显存...", 0.4f);
                analysisResult.textureResult = VRChatParameterCalculator.CalculateTextureMemory(selectedAvatar);
                
                EditorUtility.DisplayProgressBar("分析中", "计算动骨数量...", 0.6f);
                var dynamicBoneResult = VRChatParameterCalculator.CalculateDynamicBoneCount(selectedAvatar);
                analysisResult.dynamicBoneCount = dynamicBoneResult.totalCount;
                analysisResult.dynamicBoneInfo = dynamicBoneResult;
                
                EditorUtility.DisplayProgressBar("分析中", "计算模型大小...", 0.8f);
                analysisResult.modelSize = VRChatParameterCalculator.CalculateModelSize(selectedAvatar);
                
                // 计算其他信息
                AnalyzeOtherInfo();
                
                // 计算总大小 - 使用迁移的ModelSizeCalculator获取Combined (all)精确计算
                var totalModelSize = ModelSizeCalculator.CalculateTotalModelSize(selectedAvatar.gameObject);
                analysisResult.totalUncompressedSizeMB = totalModelSize.totalSizeMB;
                
                // 计算模型上传大小 - 使用画质压缩工具中的精确计算逻辑
                // 直接使用TextureVRAM.cs中的计算方式
                long sizeActive = 0;
                
                EditorUtility.DisplayProgressBar("分析模型", "获取材质数据", 0.6f);
                // 获取所有材质
                var allRenderers = selectedAvatar.gameObject.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.gameObject.GetComponentsInParent<Transform>(true).All(g => g.tag != "EditorOnly"));
                
                var activeMaterials = allRenderers.Where(r => r.gameObject.activeInHierarchy)
                    .SelectMany(r => r.sharedMaterials)
                    .Where(m => m != null)
                    .Distinct();
                
                var allMaterials = allRenderers.SelectMany(r => r.sharedMaterials)
                    .Where(m => m != null)
                    .Distinct();
                
                EditorUtility.DisplayProgressBar("分析模型", "获取纹理数据", 0.7f);
                // 计算纹理大小
                Dictionary<Texture, bool> textures = new Dictionary<Texture, bool>();
                foreach (Material m in allMaterials)
                {
                    if (m == null) continue;
                    int[] textureIds = m.GetTexturePropertyNameIDs();
                    bool isActive = activeMaterials.Contains(m);
                    foreach (int id in textureIds)
                    {
                        if (!m.HasProperty(id)) continue;
                        Texture t = m.GetTexture(id);
                        if (t == null) continue;
                        if (textures.ContainsKey(t))
                        {
                            if (textures[t] == false && isActive) textures[t] = true;
                        }
                        else
                        {
                            textures.Add(t, isActive);
                        }
                    }
                }
                
                // 计算活动纹理大小
                int numTextures = textures.Keys.Count;
                int texIdx = 1;
                foreach (KeyValuePair<Texture, bool> t in textures)
                {
                    EditorUtility.DisplayProgressBar("分析模型", $"计算纹理大小: {t.Key.name}", 0.7f + 0.1f * (texIdx / (float)numTextures));
                    if (t.Value) // 只计算活动纹理
                    {
                        long textureSize = TextureMemoryCalculator.CalculateTextureSize(t.Key).sizeBytes;
                        sizeActive += textureSize;
                    }
                    texIdx++;
                }
                
                EditorUtility.DisplayProgressBar("分析模型", "获取网格数据", 0.8f);
                // 计算网格大小
                Dictionary<Mesh, bool> meshes = new Dictionary<Mesh, bool>();
                var allMeshes = selectedAvatar.gameObject.GetComponentsInChildren<Renderer>(true)
                    .Select(r => r is SkinnedMeshRenderer ? (r as SkinnedMeshRenderer).sharedMesh : 
                           r is MeshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null)
                    .Where(m => m != null);
                
                var activeMeshes = selectedAvatar.gameObject.GetComponentsInChildren<Renderer>(false)
                    .Select(r => r is SkinnedMeshRenderer ? (r as SkinnedMeshRenderer).sharedMesh : 
                           r is MeshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null)
                    .Where(m => m != null);
                
                foreach (Mesh m in allMeshes)
                {
                    bool isActive = activeMeshes.Contains(m);
                    if (meshes.ContainsKey(m))
                    {
                        if (meshes[m] == false && isActive) meshes[m] = true;
                    }
                    else
                    {
                        meshes.Add(m, isActive);
                    }
                }
                
                // 计算活动网格大小
                int numMeshes = meshes.Keys.Count;
                int meshIdx = 1;
                foreach (KeyValuePair<Mesh, bool> m in meshes)
                {
                    EditorUtility.DisplayProgressBar("分析模型", $"计算网格大小: {m.Key.name}", 0.8f + 0.1f * (meshIdx / (float)numMeshes));
                    if (m.Value) // 只计算活动网格
                    {
                        long meshSize = ModelSizeCalculator.CalculateSingleMeshSize(m.Key);
                        sizeActive += meshSize;
                    }
                    meshIdx++;
                }
                
                EditorUtility.DisplayProgressBar("分析模型", "完成计算", 0.95f);
                // 设置模型上传大小（MiB）
                analysisResult.estimatedUploadSizeMB = sizeActive / (1024f * 1024f);
                
                // 清除进度条
                EditorUtility.ClearProgressBar();
                
                Debug.Log($"模型分析完成！动骨检测结果: VRC PhysBone={dynamicBoneResult.physBoneCount}, VRC PhysBoneCollider={dynamicBoneResult.physBoneColliderCount}");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"分析过程中出现错误: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"分析失败: {e.Message}", "确定");
            }
        }
        
        private void AnalyzeOtherInfo()
        {
            EditorUtility.DisplayProgressBar("分析中", "计算网格数量...", 0.85f);
            analysisResult.meshCount = selectedAvatar.GetComponentsInChildren<MeshFilter>().Length +
                selectedAvatar.GetComponentsInChildren<SkinnedMeshRenderer>().Length;
            
            EditorUtility.DisplayProgressBar("分析中", "计算材质数量...", 0.9f);
            analysisResult.materialCount = selectedAvatar.GetComponentsInChildren<Renderer>()
                .SelectMany(r => r.sharedMaterials)
                .Where(m => m != null)
                .Distinct()
                .Count();
            
            EditorUtility.DisplayProgressBar("分析中", "计算动画控制器数量...", 0.95f);
            analysisResult.animatorCount = selectedAvatar.GetComponentsInChildren<Animator>().Length;
        }
        
        // 网格详细信息显示方法
        private void DrawMeshDetails()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("网格详细信息", titleStyle);
            
            if (GUILayout.Button(showDetailedMeshInfo ? "隐藏" : "显示", 
                GUILayout.Width(60)))
            {
                showDetailedMeshInfo = !showDetailedMeshInfo;
                
                // 初始化网格详细信息列表
                if (showDetailedMeshInfo && meshDetailList == null && analysisResult != null)
                {
                    InitializeMeshDetailList();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (showDetailedMeshInfo && meshDetailList != null && meshDetailList.Count > 0)
            {
                EditorGUILayout.Space(5);
                
                // 网格质量评估
                DrawMeshQualityAssessment();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("网格列表 (按大小排序):", EditorStyles.boldLabel);
                
                meshScrollPosition = EditorGUILayout.BeginScrollView(meshScrollPosition, GUILayout.Height(Math.Min(400, meshDetailList.Count * 25 + 50)));
                
                foreach (var meshInfo in meshDetailList)
                {
                    DrawMeshInfoItem(meshInfo);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        // 初始化网格详细信息列表
        private void InitializeMeshDetailList()
        {
            if (selectedAvatar == null) return;
            
            meshDetailList = new List<MeshDetailInfo>();
            
            // 获取所有渲染器
            var allRenderers = selectedAvatar.GetComponentsInChildren<Renderer>(true);
            var activeRenderers = selectedAvatar.GetComponentsInChildren<Renderer>(false);
            
            Dictionary<Mesh, bool> meshActiveStatus = new Dictionary<Mesh, bool>();
            
            // 收集所有网格及其活动状态
            foreach (var renderer in allRenderers)
            {
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skinnedRenderer)
                {
                    mesh = skinnedRenderer.sharedMesh;
                }
                else if (renderer is MeshRenderer meshRenderer)
                {
                    var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                        mesh = meshFilter.sharedMesh;
                }
                
                if (mesh != null)
                {
                    bool isActive = activeRenderers.Contains(renderer);
                    if (meshActiveStatus.ContainsKey(mesh))
                    {
                        // 如果网格已存在且当前是活动的，更新状态
                        if (!meshActiveStatus[mesh] && isActive)
                            meshActiveStatus[mesh] = true;
                    }
                    else
                    {
                        meshActiveStatus.Add(mesh, isActive);
                    }
                }
            }
            
            // 为每个网格创建详细信息
            foreach (var kvp in meshActiveStatus)
            {
                var mesh = kvp.Key;
                var isActive = kvp.Value;
                
                var meshInfo = new MeshDetailInfo
                {
                    mesh = mesh,
                    name = mesh.name,
                    isActive = isActive,
                    vertexCount = mesh.vertexCount,
                    triangleCount = mesh.triangles.Length / 3,
                    blendShapeCount = mesh.blendShapeCount,
                    hasBlendShapes = mesh.blendShapeCount > 0
                };
                
                // 计算网格大小
                long meshSize = CalculateMeshSize(mesh);
                meshInfo.sizeBytes = meshSize;
                meshInfo.sizeMB = FormatBytes(meshSize);
                
                meshDetailList.Add(meshInfo);
            }
            
            // 按大小排序
            meshDetailList.Sort((m1, m2) => m2.sizeBytes.CompareTo(m1.sizeBytes));
        }
        
        // 计算网格大小
        private long CalculateMeshSize(Mesh mesh)
        {
            if (mesh == null) return 0;
            
            long bytes = 0;
            
            // 计算顶点属性大小
            var vertexAttributes = mesh.GetVertexAttributes();
            long vertexAttributeVRAMSize = 0;
            
            foreach (var vertexAttribute in vertexAttributes)
            {
                int skinnedMeshMultiplier = 1;
                // 蒙皮网格的位置、法线和切线数据会有2倍大小
                if (mesh.HasVertexAttribute(VertexAttribute.BlendIndices) && 
                    mesh.HasVertexAttribute(VertexAttribute.BlendWeight) &&
                    (vertexAttribute.attribute == VertexAttribute.Position || 
                     vertexAttribute.attribute == VertexAttribute.Normal || 
                     vertexAttribute.attribute == VertexAttribute.Tangent))
                {
                    skinnedMeshMultiplier = 2;
                }
                
                if (VertexAttributeByteSize.ContainsKey(vertexAttribute.format))
                {
                    vertexAttributeVRAMSize += VertexAttributeByteSize[vertexAttribute.format] * 
                                               vertexAttribute.dimension * skinnedMeshMultiplier;
                }
            }
            
            // 计算混合形状大小
            long blendShapeVRAMSize = 0;
            if (mesh.blendShapeCount > 0)
            {
                var deltaPositions = new Vector3[mesh.vertexCount];
                var deltaNormals = new Vector3[mesh.vertexCount];
                var deltaTangents = new Vector3[mesh.vertexCount];
                
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    var frameCount = mesh.GetBlendShapeFrameCount(i);
                    for (int j = 0; j < frameCount; j++)
                    {
                        mesh.GetBlendShapeFrameVertices(i, j, deltaPositions, deltaNormals, deltaTangents);
                        for (int k = 0; k < deltaPositions.Length; k++)
                        {
                            if (deltaPositions[k] != Vector3.zero || 
                                deltaNormals[k] != Vector3.zero || 
                                deltaTangents[k] != Vector3.zero)
                            {
                                // 每个受影响的顶点：1个uint索引 + 3个float位置 + 3个float法线 + 3个float切线
                                blendShapeVRAMSize += 40;
                            }
                        }
                    }
                }
            }
            
            bytes = vertexAttributeVRAMSize * mesh.vertexCount + blendShapeVRAMSize;
            return bytes;
        }
        
        // 绘制网格质量评估
        private void DrawMeshQualityAssessment()
        {
            if (meshDetailList == null || meshDetailList.Count == 0) return;
            
            long totalMeshSize = meshDetailList.Sum(m => m.sizeBytes);
            long activeMeshSize = meshDetailList.Where(m => m.isActive).Sum(m => m.sizeBytes);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("网格质量评估", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField($"总网格内存: {FormatBytes(totalMeshSize)}");
            EditorGUILayout.LabelField($"活动网格内存: {FormatBytes(activeMeshSize)}");
            
            // PC质量评估
            var pcQuality = GetMeshQuality(totalMeshSize, false);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("PC质量:", GUILayout.Width(60));
            DrawQualityLabel(pcQuality);
            EditorGUILayout.EndHorizontal();
            
            // Quest质量评估
            var questQuality = GetMeshQuality(totalMeshSize, true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quest质量:", GUILayout.Width(60));
            DrawQualityLabel(questQuality);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        // 绘制单个网格信息项
        private void DrawMeshInfoItem(MeshDetailInfo meshInfo)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // 活动状态指示器
            GUI.color = meshInfo.isActive ? Color.green : Color.gray;
            EditorGUILayout.LabelField(meshInfo.isActive ? "●" : "○", GUILayout.Width(20));
            GUI.color = Color.white;
            
            // 网格名称
            EditorGUILayout.LabelField(meshInfo.name, GUILayout.MinWidth(150));
            
            // 大小
            EditorGUILayout.LabelField(meshInfo.sizeMB, GUILayout.Width(80));
            
            // 顶点数
            EditorGUILayout.LabelField($"{meshInfo.vertexCount:N0} 顶点", GUILayout.Width(80));
            
            // 三角形数
            EditorGUILayout.LabelField($"{meshInfo.triangleCount:N0} 三角形", GUILayout.Width(80));
            
            // 混合形状
            if (meshInfo.hasBlendShapes)
            {
                EditorGUILayout.LabelField($"{meshInfo.blendShapeCount} 混合形状", GUILayout.Width(80));
            }
            else
            {
                EditorGUILayout.LabelField("-", GUILayout.Width(80));
            }
            
            // 网格对象引用
            EditorGUILayout.ObjectField(meshInfo.mesh, typeof(Mesh), false, GUILayout.Width(150));
            
            EditorGUILayout.EndHorizontal();
        }
        
        // 获取网格质量等级
        private QualityLevel GetMeshQuality(long sizeBytes, bool isQuest)
        {
            long sizeMiB = sizeBytes / (1024 * 1024);
            
            if (isQuest)
            {
                if (sizeMiB <= QUEST_MESH_MEMORY_EXCELLENT_MiB) return QualityLevel.Excellent;
                if (sizeMiB <= QUEST_MESH_MEMORY_GOOD_MiB) return QualityLevel.Good;
                if (sizeMiB <= QUEST_MESH_MEMORY_MEDIUM_MiB) return QualityLevel.Medium;
                if (sizeMiB <= QUEST_MESH_MEMORY_POOR_MiB) return QualityLevel.Poor;
                return QualityLevel.VeryPoor;
            }
            else
            {
                if (sizeMiB <= PC_MESH_MEMORY_EXCELLENT_MiB) return QualityLevel.Excellent;
                if (sizeMiB <= PC_MESH_MEMORY_GOOD_MiB) return QualityLevel.Good;
                if (sizeMiB <= PC_MESH_MEMORY_MEDIUM_MiB) return QualityLevel.Medium;
                if (sizeMiB <= PC_MESH_MEMORY_POOR_MiB) return QualityLevel.Poor;
                return QualityLevel.VeryPoor;
            }
        }
        
        // 绘制质量标签
        private void DrawQualityLabel(QualityLevel quality)
        {
            Color qualityColor;
            string qualityText;
            
            switch (quality)
            {
                case QualityLevel.Excellent:
                    qualityColor = Color.green;
                    qualityText = "优秀";
                    break;
                case QualityLevel.Good:
                    qualityColor = Color.cyan;
                    qualityText = "良好";
                    break;
                case QualityLevel.Medium:
                    qualityColor = Color.yellow;
                    qualityText = "中等";
                    break;
                case QualityLevel.Poor:
                    qualityColor = new Color(1f, 0.5f, 0f); // 橙色
                    qualityText = "较差";
                    break;
                case QualityLevel.VeryPoor:
                    qualityColor = Color.red;
                    qualityText = "很差";
                    break;
                default:
                    qualityColor = Color.white;
                    qualityText = "未知";
                    break;
            }
            
            var oldColor = GUI.color;
            GUI.color = qualityColor;
            EditorGUILayout.LabelField(qualityText, EditorStyles.boldLabel, GUILayout.Width(40));
            GUI.color = oldColor;
        }
        
        // 格式化字节大小
        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }
        
        // 质量等级枚举
        public enum QualityLevel
        {
            Excellent,
            Good,
            Medium,
            Poor,
            VeryPoor
        }
        
        private void DrawDecryptionErrorMessage()
        {
            EditorGUILayout.Space(20);
            
            GUI.backgroundColor = Color.red;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var errorTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.red }
            };
            
            GUILayout.Label(DecryptContent("4pqg77iPIOaPkuS7tuWKn+iDveW3suemgeeUqCDimaDvuI8g"), errorTitleStyle);
            
            EditorGUILayout.Space(10);
            
            var errorDetailStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };
            
            GUILayout.Label(DecryptContent("5YaF5a655Kej5a+G6aqM6K+B5aSx6LSlLOaPkuS7tuaXoOazleato+W4uOS9v+eUqOOAgg=="), errorDetailStyle);
            GUILayout.Label(decryptionErrorMessage, errorDetailStyle);
            
            EditorGUILayout.Space(10);
            
            var solutionStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Italic
            };
            
            GUILayout.Label(DecryptContent("6K+35qOA5p+l5o+S5Lu25a6M5pW05oCn5oiW6IGU57O75byA5Y+R6ICF6I635Y+W5pSv5oyB44CC"), solutionStyle);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button(DecryptContent("6YeN5paw6aqM6K+B"), GUILayout.Height(30)))
            {
                isDecryptionValid = true;
                decryptionErrorMessage = "";
                
                string testResult = DecryptContent("5rWL6K+V");
                if (!isDecryptionValid)
                {
                    Debug.LogError(DecryptContent("W+ivuuWWtOW3peWFt+eusV0g6YeN5paw6aqM6K+B5aSx6LSlLOaPkuS7tuWKn+iDveS7jeeCtuiiq+emgeeUqOOAgg=="));
                }
                else
                {
                    Debug.Log(DecryptContent("W+ivuuWWtOW3peWFt+eusV0g6YeN5paw6aqM6K+B5oiQ5YqfLOaPkuS7tuWKn+iDveW3suaBouWkjeOAgg=="));
                }
            }
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(20);
        }
        
        // 绘制层级文件树状结构
        private void DrawHierarchyTree()
        {
            // 限制刷新频率到最高5帧
            if (Time.realtimeSinceStartup - lastHierarchyUpdateTime < HIERARCHY_UPDATE_INTERVAL)
            {
                return;
            }
            lastHierarchyUpdateTime = Time.realtimeSinceStartup;
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            showHierarchyTree = EditorGUILayout.Foldout(showHierarchyTree, "层级文件树状结构", true, titleStyle);
            EditorGUILayout.EndHorizontal();
            
            if (showHierarchyTree && selectedAvatar != null)
            {
                EditorGUILayout.Space(5);
                
                hierarchyScrollPosition = EditorGUILayout.BeginScrollView(hierarchyScrollPosition, GUILayout.MaxHeight(300));
                
                // 重置颜色索引
                colorIndex = 0;
                
                // 绘制根节点
                DrawHierarchyNode(selectedAvatar.transform, 0, true);
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // 绘制单个层级节点
        private void DrawHierarchyNode(Transform node, int depth, bool isLast)
        {
            if (node == null) return;
            
            EditorGUILayout.BeginHorizontal();
            
            // 绘制缩进和连接线
            DrawTreeLines(depth, isLast, node.childCount > 0);
            
            // 获取或分配颜色
            if (!nodeColors.ContainsKey(node))
            {
                nodeColors[node] = treeColors[colorIndex % treeColors.Length];
                colorIndex++;
            }
            
            Color nodeColor = nodeColors[node];
            Color oldColor = GUI.color;
            GUI.color = nodeColor;
            
            // 展开/折叠按钮
            bool hasChildren = node.childCount > 0;
            bool isExpanded = expandedNodes.ContainsKey(node) ? expandedNodes[node] : false;
            
            if (hasChildren)
            {
                string foldoutSymbol = isExpanded ? "▼" : "▶";
                if (GUILayout.Button(foldoutSymbol, EditorStyles.label, GUILayout.Width(15)))
                {
                    expandedNodes[node] = !isExpanded;
                }
            }
            else
            {
                GUILayout.Space(15);
            }
            
            // 节点图标和名称
            string icon = GetNodeIcon(node);
            GUILayout.Label(icon, GUILayout.Width(20));
            
            GUI.color = oldColor;
            
            // 节点名称（可点击选择）
            if (GUILayout.Button(node.name, EditorStyles.label))
            {
                Selection.activeGameObject = node.gameObject;
                EditorGUIUtility.PingObject(node.gameObject);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 绘制子节点（如果展开）
            if (hasChildren && isExpanded)
            {
                for (int i = 0; i < node.childCount; i++)
                {
                    Transform child = node.GetChild(i);
                    bool isLastChild = (i == node.childCount - 1);
                    DrawHierarchyNode(child, depth + 1, isLastChild);
                }
            }
        }
        
        // 绘制树状连接线
        private void DrawTreeLines(int depth, bool isLast, bool hasChildren)
        {
            for (int i = 0; i < depth; i++)
            {
                if (i == depth - 1)
                {
                    // 最后一级的连接线
                    string lineSymbol = isLast ? "└" : "├";
                    GUILayout.Label(lineSymbol, GUILayout.Width(15));
                }
                else
                {
                    // 中间级的连接线
                    GUILayout.Label("│", GUILayout.Width(15));
                }
            }
        }
        
        // 获取节点图标
        private string GetNodeIcon(Transform node)
        {
            GameObject obj = node.gameObject;
            
            // 检查组件类型来确定图标
            if (obj.GetComponent<SkinnedMeshRenderer>() != null)
                return "🎭"; // 蒙皮网格渲染器
            else if (obj.GetComponent<MeshRenderer>() != null)
                return "🔷"; // 网格渲染器
            else if (obj.GetComponent<Camera>() != null)
                return "📷"; // 摄像机
            else if (obj.GetComponent<Light>() != null)
                return "💡"; // 灯光
            else if (obj.GetComponent<Animator>() != null)
                return "🎬"; // 动画器
            else if (obj.GetComponent<Collider>() != null)
                return "🛡️"; // 碰撞器
            else if (obj.GetComponent<Rigidbody>() != null)
                return "⚖️"; // 刚体
            else if (obj.GetComponent<AudioSource>() != null)
                return "🔊"; // 音频源
            else if (obj.GetComponent<ParticleSystem>() != null)
                return "✨"; // 粒子系统
            else if (node.childCount > 0)
                return "📁"; // 有子对象的空对象
            else
                return "📄"; // 空对象
        }
    }
    
    // 更详细的分析结果数据结构
    [System.Serializable]
    public class DetailedAnalysisResult
    {
        public float bitsUsage;
        public TextureAnalysisResult textureResult;
        public int dynamicBoneCount;
        public DynamicBoneCountResult dynamicBoneInfo;
        public ModelSizeInfo modelSize;
        public float totalUncompressedSizeMB;
        public float estimatedUploadSizeMB;
        
        // 详细信息
        public int meshCount;
        public int materialCount;
        public int animatorCount;
    }
}
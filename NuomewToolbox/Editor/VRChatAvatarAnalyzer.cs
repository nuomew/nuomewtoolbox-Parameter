using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
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
        private bool needsTextureListUpdate = false; // 添加更新标志
        private Dictionary<string, bool> textureFoldoutStates = new Dictionary<string, bool>(); // 保存折叠状态
        
        // 网格详细信息相关
        private List<MeshDetailInfo> meshDetailList;
        private Vector2 meshScrollPosition;
        private bool showDetailedMeshInfo = false;
        
        // 网格压缩相关
        private List<MeshCompressionInfo> meshCompressionList;
        private Vector2 meshCompressionScrollPosition;
        private bool showMeshCompression = false;
        private Dictionary<string, bool> meshFoldoutStates = new Dictionary<string, bool>();
        
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
        
        [System.Serializable]
        public class MeshCompressionInfo
        {
            public Mesh mesh;
            public string name;
            public long originalSizeBytes;
            public string originalSizeMB;
            public bool isActive;
            public int vertexCount;
            public int triangleCount;
            public int blendShapeCount;
            public bool hasBlendShapes;
            public List<Renderer> renderers;
            
            // 压缩选项
            public bool compressVertexPosition = false;
            public bool compressNormals = false;
            public bool compressUVs = false;
            public bool compressColors = false;
            public bool removeUnusedVertexStreams = false;
            public bool optimizeIndexBuffer = false;
            public bool compressionChanged = false;
            
            // 压缩质量设置
            public ModelImporterMeshCompression compressionQuality = ModelImporterMeshCompression.Medium;
            // 移除不兼容的属性，改为简单的压缩参数
            public bool optimizeMesh = true;
            public bool weldenVertices = true;
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
            window.minSize = new Vector2(350, 500);
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
                decryptionErrorMessage = $"{DecryptContent("6Kej5a+G5aSx6LSlOiA=")}{ex.Message}";
                Debug.LogError($"{DecryptContent("W+ivuuWWtOW3peWFt+eusV0g6Kej5a+G5Yqf6IO96aqM6K+B5byC5bi4LOaPkuS7tuWKn+iDveW3suemgeeUqOOAguS4jeivr+S/oeaBr++8miA=")}{ex.Message}");
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
            GUILayout.Label("VRChat模型参数统计 V1.0.6 By.诺喵", EditorStyles.centeredGreyMiniLabel);
            
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
            
            EditorGUILayout.LabelField(DecryptContent("MTUw5o6lVlJDaGF05qih5Z6L5a6a5Yi277yM5Y+q5pS55aWz5qih5Z6L5Lu75L2VYnVn6IGU57O75oiR5YWN6LS55L+u5aSN"), adContentStyle);
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
            
            // 模型上传大小 - 使用向上取整显示
            DrawUploadSizeSection("模型上传大小", analysisResult.estimatedUploadSizeMB,
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

            // 解压后大小扣分
            float sizePercentage = analysisResult.totalUncompressedSizeMB / VRChatParameterCalculator.MAX_UNCOMPRESSED_SIZE_MB;
            if (sizePercentage > 1.0f) score -= 25f;
            else if (sizePercentage > 0.8f) score -= 15f;

            // 上传大小扣分 - 使用向上取整
            float uploadSizeCeiled = Mathf.Ceil(analysisResult.estimatedUploadSizeMB);
            float uploadPercentage = uploadSizeCeiled / VRChatParameterCalculator.MAX_UPLOAD_SIZE_MB;
            if (uploadPercentage > 1.0f) score -= 20f;
            else if (uploadPercentage > 0.8f) score -= 10f;

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

        /// <summary>
        /// 绘制模型上传大小参数区域 - 使用向上取整显示
        /// </summary>
        private void DrawUploadSizeSection(string title, float current, float max, string unit)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题
            EditorGUILayout.LabelField(title, titleStyle);

            // 对上传大小进行向上取整
            float displayCurrent = Mathf.Ceil(current);

            // 进度条 - 使用向上取整后的值计算百分比
            float percentage = displayCurrent / max;
            Color barColor = percentage > 0.8f ? warningColor :
                           percentage > 0.6f ? Color.yellow : safeColor;

            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            EditorGUI.ProgressBar(rect, Mathf.Min(percentage, 1f), $"{displayCurrent:F0} / {max} {unit}");

            // 状态文字
            string status = percentage > 1.0f ? "⚠️ 超出限制!" :
                           percentage > 0.8f ? "⚠️ 接近限制" : "✅ 正常";

            GUIStyle statusStyle = percentage > 0.8f ? warningStyle : normalStyle;
            EditorGUILayout.LabelField(status, statusStyle);

            // 如果超出限制，显示具体超出量
            if (percentage > 1.0f)
            {
                float excess = displayCurrent - max;
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
                
                // 确保在显示压缩选项时初始化纹理列表
                if (showDetailedTextureInfo && textureCompressionList == null)
                {
                    InitializeTextureCompressionList();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 显示模式提示
            if (showDetailedTextureInfo)
            {
                EditorGUILayout.LabelField(position.width < 450 ? "💖 小窗模式启动～ 可拖拽窗口切换哦！" : "💫 宽屏模式中～ 拖小一点试试看！", EditorStyles.miniLabel);
            }
            
            if (showDetailedTextureInfo && analysisResult != null)
            {
                EditorGUILayout.Space(5);
                
                // 检查窗口宽度，决定使用简化模式还是表格模式
                bool useCompactMode = position.width < 450;
                
                if (useCompactMode)
                {
                    // 简化模式 - 更紧凑的单行显示
                    EditorGUILayout.LabelField("🎨 最大纹理文件 (前10个, 简化模式) 🎨", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("💡 想要压缩纹理？点击下方的'显示压缩选项'按钮", EditorStyles.miniLabel);
                    
                    foreach (var texture in analysisResult.textureResult.textureInfos.Take(10))
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        
                        // 纹理名称 (主要信息)
                        EditorGUILayout.LabelField($"• {texture.name}", EditorStyles.boldLabel, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                        
                        // 紧凑的详细信息
                        EditorGUILayout.LabelField($"{texture.sizeMB:F1}MB", EditorStyles.miniLabel, GUILayout.Width(50));
                        EditorGUILayout.LabelField($"{texture.width}x{texture.height}", EditorStyles.miniLabel, GUILayout.Width(70));
                        EditorGUILayout.LabelField(texture.format.Length > 8 ? texture.format.Substring(0,8) : texture.format, EditorStyles.miniLabel, GUILayout.Width(60));
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    // 表格模式 - 原有的显示方式
                    // 纹理压缩选项切换
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("纹理压缩选项", EditorStyles.boldLabel);
                    if (GUILayout.Button(showTextureCompression ? "隐藏压缩选项" : "显示压缩选项", GUILayout.Width(100)))
                    {
                        showTextureCompression = !showTextureCompression;
                        
                        // 确保在显示压缩选项时初始化纹理列表
                        if (showTextureCompression && textureCompressionList == null)
                        {
                            InitializeTextureCompressionList();
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    // 纹理质量评估 - 始终显示，不需要点击显示压缩选项
                    if (analysisResult.textureResult != null && analysisResult.textureResult.textureInfos.Count > 0)
                    {
                        DrawTextureQualityAssessmentFromAnalysisResult();
                    }

                    if (showTextureCompression)
                    {
                        DrawTextureCompressionOptions();
                    }
                    else if (analysisResult.textureResult != null && analysisResult.textureResult.textureInfos.Count > 0)
                    {
                        // 原有的纹理列表显示（简化版本）
                        EditorGUILayout.LabelField("最大纹理文件 (前10个):", EditorStyles.boldLabel);
                        
                        // 添加表头
                        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                        EditorGUILayout.LabelField("纹理名称", EditorStyles.boldLabel, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                        EditorGUILayout.LabelField("分辨率", EditorStyles.boldLabel, GUILayout.Width(80));
                        EditorGUILayout.LabelField("格式", EditorStyles.boldLabel, GUILayout.Width(80));
                        EditorGUILayout.LabelField("大小", EditorStyles.boldLabel, GUILayout.Width(60));
                        EditorGUILayout.EndHorizontal();
                        
                        foreach (var texture in analysisResult.textureResult.textureInfos.Take(10))
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"• {texture.name}", GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                            EditorGUILayout.LabelField($"{texture.width}x{texture.height}", GUILayout.Width(80));
                            EditorGUILayout.LabelField(texture.format, GUILayout.Width(80));
                            EditorGUILayout.LabelField($"{texture.sizeMB:F1} MB", GUILayout.Width(60));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("分析结果中没有找到纹理信息");
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void InitializeTextureCompressionList()
         {
             // 如果不需要更新且列表已存在，直接返回
             if (!needsTextureListUpdate && textureCompressionList != null)
             {
                 return;
             }
             
             textureCompressionList = new List<TextureCompressionInfo>();
             
             if (analysisResult == null || selectedAvatar == null) 
             {
                 Debug.Log("[纹理压缩] 初始化失败：分析结果或模型为空");
                 needsTextureListUpdate = false;
                 return;
             }

             try
             {
                 Debug.Log("[纹理压缩] 开始初始化纹理压缩列表...");
             
             // 获取所有材质和纹理的映射
             Dictionary<Texture, List<Material>> textureToMaterials = GetMaterialsUsingTextures(selectedAvatar.gameObject);
             
                 Debug.Log($"[纹理压缩] 从材质中找到 {textureToMaterials.Count} 个纹理");
                 
                 // 创建一个集合来跟踪已添加的纹理，避免重复
                 HashSet<string> addedTextureNames = new HashSet<string>();
                 
                 // 优先添加前10个最大纹理（从分析结果中获取）
                 if (analysisResult.textureResult != null && analysisResult.textureResult.textureInfos.Count > 0)
                 {
                     Debug.Log($"[纹理压缩] 优先添加前10个最大纹理到压缩列表");
                     
                     var top10Textures = analysisResult.textureResult.textureInfos.Take(10);
                     foreach (var textureInfo in top10Textures)
                     {
                         if (addedTextureNames.Contains(textureInfo.name)) continue;
                         
                         // 首先尝试从textureToMaterials字典中直接查找匹配的纹理
                         Texture foundTexture = null;
                         List<Material> materials = new List<Material>();
                         
                         // 方法1: 在textureToMaterials中查找名称匹配的纹理（优先方法）
                         foreach (var kvp in textureToMaterials)
                         {
                             if (kvp.Key != null && 
                                 (kvp.Key.name == textureInfo.name || 
                                  kvp.Key.name.Contains(textureInfo.name) || 
                                  textureInfo.name.Contains(kvp.Key.name)))
                             {
                                 foundTexture = kvp.Key;
                                 materials = kvp.Value;
                                 Debug.Log($"[纹理压缩] 纹理 {textureInfo.name} 在材质字典中匹配到 {materials.Count} 个材质 (匹配的纹理名: {kvp.Key.name})");
                                 break;
                             }
                         }
                         
                         // 方法2: 如果在字典中没找到，使用FindTextureByName方法
                         if (foundTexture == null)
                         {
                             foundTexture = FindTextureByName(textureInfo.name);
                             if (foundTexture != null)
                             {
                                 Debug.Log($"[纹理压缩] 通过FindTextureByName找到纹理: {textureInfo.name}，但未找到材质关联");
                                 // 材质列表保持为空，这表示该纹理未被当前模型使用
                             }
                         }
                         
                         // 如果仍然找不到纹理对象，跳过这个纹理
                         if (foundTexture == null)
                         {
                             Debug.LogWarning($"[纹理压缩] 无法找到纹理对象: {textureInfo.name}，跳过此纹理");
                             continue;
                         }
                         
                         // 记录材质关联状态
                         if (materials.Count == 0)
                         {
                             Debug.LogWarning($"[纹理压缩] 纹理 {textureInfo.name} 未找到关联的材质（可能未被当前模型使用）");
                         }
                         
                         TextureCompressionInfo compressionInfo = new TextureCompressionInfo
                         {
                             texture = foundTexture,
                             name = textureInfo.name,
                             width = textureInfo.width,
                             height = textureInfo.height,
                             sizeBytes = (long)(textureInfo.sizeMB * 1024 * 1024), // 从MB转换为字节
                             sizeMB = textureInfo.sizeMB.ToString("F2"),
                             isActive = true,
                             materials = materials,
                             compressionChanged = false,
                             newMaxSize = Math.Max(textureInfo.width, textureInfo.height)
                         };
                         
                         // 从分析结果设置格式信息
                         if (Enum.TryParse<TextureFormat>(textureInfo.format, out TextureFormat parsedFormat))
                         {
                             compressionInfo.format = parsedFormat;
                             compressionInfo.formatString = textureInfo.format;
                             
                             if (BPP.TryGetValue(parsedFormat, out float bpp))
                             {
                                 compressionInfo.BPP = bpp;
                             }
                             else
                             {
                                 compressionInfo.BPP = 16; // 默认值
                             }
                         }
                         else
                         {
                             compressionInfo.formatString = textureInfo.format;
                             compressionInfo.BPP = 16;
                         }
                         
                         if (foundTexture is Texture2D tex2D)
                         {
                             compressionInfo.newFormat = GetRecommendedFormat(tex2D);
                             
                             // 检查是否有Alpha通道
                             string path = AssetDatabase.GetAssetPath(foundTexture);
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
                             compressionInfo.newFormat = TextureImporterFormat.BC7;
                         }
                         
                         textureCompressionList.Add(compressionInfo);
                         addedTextureNames.Add(textureInfo.name);
                         
                         Debug.Log($"[纹理压缩] 已添加前10大纹理: {textureInfo.name} ({textureInfo.sizeMB:F2}MB)");
                     }
                 }
                 
                 // 添加其他材质中的纹理（如果还没有被添加）
                 if (textureToMaterials.Count > 0)
                 {
                     Debug.Log($"[纹理压缩] 添加其他材质纹理到压缩列表");
                     
                     foreach (var kvp in textureToMaterials)
                     {
                         Texture texture = kvp.Key;
                         if (texture == null || addedTextureNames.Contains(texture.name)) continue;
                         
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
                             compressionChanged = false,
                             newMaxSize = Math.Max(texture.width, texture.height)
                         };
                         
                         // 获取纹理格式
                         if (texture is Texture2D tex2D)
                         {
                             compressionInfo.format = tex2D.format;
                             compressionInfo.formatString = tex2D.format.ToString();
                             compressionInfo.newFormat = GetRecommendedFormat(tex2D);
                             
                             if (BPP.TryGetValue(tex2D.format, out float bpp))
                             {
                                 compressionInfo.BPP = bpp;
                             }
                             else
                             {
                                 compressionInfo.BPP = 16;
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
                         addedTextureNames.Add(texture.name);
                     }
                 }
             
             // 按大小排序，如果大小相同则随机排列
             var random = new System.Random();
             textureCompressionList.Sort((t1, t2) => 
             {
                 int sizeComparison = t2.sizeBytes.CompareTo(t1.sizeBytes);
                 if (sizeComparison == 0)
                 {
                     // 大小相同时随机排列
                     return random.Next(-1, 2);
                 }
                 return sizeComparison;
             });
                 
                 Debug.Log($"[纹理压缩] 成功初始化，共 {textureCompressionList.Count} 个纹理");
             }
             catch (System.Exception ex)
             {
                 Debug.LogError($"[纹理压缩] 初始化纹理压缩列表时出错: {ex.Message}");
                 Debug.LogError($"[纹理压缩] 堆栈跟踪: {ex.StackTrace}");
                 textureCompressionList = new List<TextureCompressionInfo>();
             }
             finally
             {
                 needsTextureListUpdate = false;
             }
         }
         
         /// <summary>
         /// 通过名称查找纹理对象的辅助方法
         /// </summary>
         private Texture FindTextureByName(string textureName)
         {
             try
             {
                 Debug.Log($"[纹理压缩] 开始查找纹理: {textureName}");
                 
                 // 方法1: 在当前选择的模型中查找
                 if (selectedAvatar != null)
                 {
                     var renderers = selectedAvatar.GetComponentsInChildren<Renderer>(true);
                     Debug.Log($"[纹理压缩] 在 {renderers.Length} 个渲染器中查找纹理");
                     
                     foreach (var renderer in renderers)
                     {
                         foreach (var material in renderer.sharedMaterials)
                         {
                             if (material == null) continue;
                             
                             var shader = material.shader;
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
                                         // 尝试多种匹配方式
                                         if (texture.name == textureName || 
                                             texture.name.Contains(textureName) || 
                                             textureName.Contains(texture.name))
                                         {
                                             Debug.Log($"[纹理压缩] 在材质 {material.name} 中找到纹理: {texture.name}");
                                             return texture;
                                         }
                                     }
                                 }
                             }
                         }
                     }
                 }
                 
                 // 方法2: 使用AssetDatabase搜索（更宽松的搜索）
                 string[] guids = AssetDatabase.FindAssets($"t:Texture2D");
                 Debug.Log($"[纹理压缩] 在 {guids.Length} 个纹理资源中搜索");
                 
                 foreach (string guid in guids)
                 {
                     string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                     Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
                     
                     if (texture != null)
                     {
                         // 尝试多种匹配方式
                         if (texture.name == textureName || 
                             texture.name.Contains(textureName) || 
                             textureName.Contains(texture.name))
                         {
                             Debug.Log($"[纹理压缩] 通过AssetDatabase找到纹理: {texture.name} (路径: {assetPath})");
                             return texture;
                         }
                     }
                 }
                 
                 Debug.LogWarning($"[纹理压缩] 无法找到名为 '{textureName}' 的纹理对象");
                 return null;
             }
             catch (System.Exception ex)
             {
                 Debug.LogError($"[纹理压缩] 查找纹理 '{textureName}' 时出错: {ex.Message}");
                 return null;
             }
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
                 EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                 EditorGUILayout.LabelField("🔍 纹理检测状态", EditorStyles.boldLabel);
                 
                 if (selectedAvatar == null)
                 {
                     EditorGUILayout.LabelField("• 未选择VRChat模型");
                 }
                 else if (analysisResult == null)
                 {
                     EditorGUILayout.LabelField("• 请先点击'开始分析模型参数'");
                 }
                 else
                 {
                     EditorGUILayout.LabelField("• 正在重新检测纹理...");
                     
                     // 手动重试初始化
                     if (GUILayout.Button("重新扫描纹理", GUILayout.Height(25)))
                     {
                         textureCompressionList = null;
                         needsTextureListUpdate = true;
                         InitializeTextureCompressionList();
                         
                         if (textureCompressionList == null || textureCompressionList.Count == 0)
                         {
                             // 提供诊断信息
                             var renderers = selectedAvatar.GetComponentsInChildren<Renderer>(true);
                             var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).ToList();
                             
                             EditorGUILayout.LabelField($"• 找到 {renderers.Length} 个渲染器");
                             EditorGUILayout.LabelField($"• 找到 {materials.Count} 个材质");
                             
                             if (materials.Count == 0)
                             {
                                 EditorGUILayout.LabelField("• ❌ 模型中没有材质，无法找到纹理");
                             }
                             else
                             {
                                 EditorGUILayout.LabelField("• ⚠️ 材质中没有有效的纹理属性");
                             }
                         }
                     }
                 }
                 
                 EditorGUILayout.EndVertical();
                 return;
             }
             
             // 检查窗口宽度，决定使用简化模式还是表格模式
             bool useCompactMode = position.width < 450;
             
             // 使用变化检测来减少不必要的重绘
             EditorGUI.BeginChangeCheck();
             
             EditorGUILayout.Space(5);
             
             // 纹理状态信息
             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
             EditorGUILayout.LabelField($"✅ 找到 {textureCompressionList.Count} 个纹理", EditorStyles.boldLabel);
             EditorGUILayout.EndVertical();

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
             
             if (useCompactMode)
             {
                 // 简化模式 - 更紧凑的单行显示
                 EditorGUILayout.LabelField("✨ 纹理压缩选项 (简化模式) ✨", EditorStyles.boldLabel);
                 
                 textureScrollPosition = EditorGUILayout.BeginScrollView(textureScrollPosition, GUILayout.Height(300));
                 
                 for (int i = 0; i < textureCompressionList.Count; i++)
                 {
                     var texInfo = textureCompressionList[i];
                     
                     EditorGUI.BeginChangeCheck();
                     
                     EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                     
                     // 纹理名称 (主要信息)
                     EditorGUILayout.LabelField(texInfo.name, EditorStyles.boldLabel, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                     
                     // 紧凑的基本信息
                     EditorGUILayout.LabelField($"{texInfo.sizeMB}MB", EditorStyles.miniLabel, GUILayout.Width(50));
                     EditorGUILayout.LabelField($"{texInfo.width}x{texInfo.height}", EditorStyles.miniLabel, GUILayout.Width(70));
                     
                     // 紧凑的压缩设置
                     TextureImporterFormat newFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup(texInfo.newFormat, GUILayout.Width(80));
                     int newMaxSize = EditorGUILayout.IntPopup(texInfo.newMaxSize, 
                         new string[] { "512", "800", "1K", "2K", "4K" },
                         new int[] { 512, 800, 1024, 2048, 4096 }, GUILayout.Width(50));
                     
                     // 检查变化
                     if (EditorGUI.EndChangeCheck())
                     {
                         if (newFormat != texInfo.newFormat)
                         {
                             texInfo.newFormat = newFormat;
                             texInfo.compressionChanged = true;
                         }
                         if (newMaxSize != texInfo.newMaxSize)
                         {
                             texInfo.newMaxSize = newMaxSize;
                             texInfo.compressionChanged = true;
                         }
                     }
                     
                     // 显示更改状态
                     if (texInfo.compressionChanged)
                     {
                         GUI.color = Color.yellow;
                         EditorGUILayout.LabelField("✓", GUILayout.Width(15));
                         GUI.color = Color.white;
                     }
                     
                     EditorGUILayout.EndHorizontal();
                 }
                 
                 EditorGUILayout.EndScrollView();
             }
             else
             {
                 // 表格模式 - 原有的显示方式
                 // 纹理列表
                 textureScrollPosition = EditorGUILayout.BeginScrollView(textureScrollPosition, GUILayout.Height(300));
                 
                 // 添加表头
                 EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                 EditorGUILayout.LabelField("纹理名称", EditorStyles.boldLabel, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                 EditorGUILayout.LabelField("分辨率", EditorStyles.boldLabel, GUILayout.Width(80));
                 EditorGUILayout.LabelField("格式", EditorStyles.boldLabel, GUILayout.Width(80));
                 EditorGUILayout.LabelField("大小", EditorStyles.boldLabel, GUILayout.Width(60));
                 EditorGUILayout.EndHorizontal();
                 
                 for (int i = 0; i < textureCompressionList.Count; i++)
                 {
                     var texInfo = textureCompressionList[i];
                     
                     // 为每个纹理项目添加独立的变化检测
                     EditorGUI.BeginChangeCheck();
                     
                     EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                     
                     // 纹理基本信息
                     EditorGUILayout.BeginHorizontal();
                     EditorGUILayout.LabelField(texInfo.name, EditorStyles.boldLabel, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                     EditorGUILayout.LabelField($"{texInfo.width}x{texInfo.height}", GUILayout.Width(80));
                     EditorGUILayout.LabelField(texInfo.formatString, GUILayout.Width(80));
                     EditorGUILayout.LabelField($"{texInfo.sizeMB} MB", GUILayout.Width(60));
                     EditorGUILayout.EndHorizontal();
                     
                     // 压缩选项
                     EditorGUILayout.BeginHorizontal();
                     
                     // 格式选择
                     EditorGUILayout.LabelField("新格式:", GUILayout.Width(60));
                     TextureImporterFormat newFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup(texInfo.newFormat, GUILayout.Width(120));
                     
                     // 快速格式按钮
                     if (GUILayout.Button("BC7", GUILayout.Width(50)))
                     {
                         newFormat = TextureImporterFormat.BC7;
                     }
                     if (GUILayout.Button("DXT5", GUILayout.Width(50)))
                     {
                         newFormat = TextureImporterFormat.DXT5;
                     }
                     if (GUILayout.Button("DXT1", GUILayout.Width(50)))
                     {
                         newFormat = TextureImporterFormat.DXT1;
                     }
                     
                     EditorGUILayout.EndHorizontal();
                     
                     // 分辨率选项
                     EditorGUILayout.BeginHorizontal();
                     EditorGUILayout.LabelField("最大尺寸:", GUILayout.Width(60));
                     int newMaxSize = EditorGUILayout.IntPopup(texInfo.newMaxSize, 
                         new string[] { "32", "64", "128", "256", "512", "800", "1024", "2048", "4096" },
                         new int[] { 32, 64, 128, 256, 512, 800, 1024, 2048, 4096 }, GUILayout.Width(120));
                     
                     // 快速分辨率按钮
                     if (GUILayout.Button("2K", GUILayout.Width(50)))
                     {
                         newMaxSize = 2048;
                     }
                     if (GUILayout.Button("1K", GUILayout.Width(50)))
                     {
                         newMaxSize = 1024;
                     }
                     if (GUILayout.Button("512", GUILayout.Width(50)))
                     {
                         newMaxSize = 512;
                     }
                     
                     // 只有当值实际改变时才更新状态
                     if (EditorGUI.EndChangeCheck())
                     {
                         if (newFormat != texInfo.newFormat)
                         {
                             texInfo.newFormat = newFormat;
                             texInfo.compressionChanged = true;
                         }
                         if (newMaxSize != texInfo.newMaxSize)
                         {
                             texInfo.newMaxSize = newMaxSize;
                             texInfo.compressionChanged = true;
                         }
                     }
                     
                     // 计算节省的大小（只在有变化时显示）
                     if (texInfo.compressionChanged)
                     {
                         float savedSize = CalculateSavedSize(texInfo);
                         EditorGUILayout.LabelField($"节省: {savedSize:F1} MB", GUILayout.Width(120));
                     }
                     
                     EditorGUILayout.EndHorizontal();
                     
                     // 材质信息 - 使用缓存的折叠状态
                     if (texInfo.materials.Count > 0)
                     {
                         string materialKey = $"material_{texInfo.name}_{i}";
                         if (!textureFoldoutStates.ContainsKey(materialKey))
                         {
                             textureFoldoutStates[materialKey] = true; // 改为默认展开状态
                         }
                         
                         EditorGUILayout.BeginHorizontal();
                         bool currentFoldout = textureFoldoutStates[materialKey];
                         bool newFoldout = EditorGUILayout.Foldout(currentFoldout, $"使用此纹理的材质 ({texInfo.materials.Count})");
                         if (newFoldout != currentFoldout)
                         {
                             textureFoldoutStates[materialKey] = newFoldout;
                         }
                         EditorGUILayout.EndHorizontal();
                         
                         if (newFoldout)
                         {
                             EditorGUI.indentLevel++;
                             foreach (var material in texInfo.materials)
                             {
                                 EditorGUILayout.BeginHorizontal();
                                 EditorGUILayout.ObjectField(material, typeof(Material), false, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                                 EditorGUILayout.EndHorizontal();
                             }
                             EditorGUI.indentLevel--;
                         }
                     }
                     else
                     {
                         // 当没有找到材质关联时显示纹理预览
                         EditorGUILayout.BeginVertical();
                         
                         // 显示警告信息
                         EditorGUILayout.BeginHorizontal();
                         var warningStyle = new GUIStyle(EditorStyles.label)
                         {
                             normal = { textColor = new Color(1f, 0.8f, 0f) }, // 橙色警告
                             fontStyle = FontStyle.Italic
                         };
                         EditorGUILayout.LabelField("⚠️ 未找到使用此纹理的材质", warningStyle);
                         EditorGUILayout.EndHorizontal();
                         
                         // 显示纹理预览
                         if (texInfo.texture != null)
                         {
                             EditorGUILayout.BeginHorizontal();
                             EditorGUILayout.LabelField("纹理预览:", GUILayout.Width(60));
                             
                             // 创建可点击的纹理预览区域
                             if (texInfo.texture is Texture2D)
                             {
                                 // 创建纹理预览按钮区域
                                 Rect textureRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
                                 
                                 // 绘制纹理预览
                                 EditorGUI.DrawPreviewTexture(textureRect, texInfo.texture);
                                 
                                 // 创建透明按钮覆盖在纹理预览上，用于检测点击
                                 if (GUI.Button(textureRect, "", GUIStyle.none))
                                 {
                                     // 点击时定位到纹理资源
                                     Selection.activeObject = texInfo.texture;
                                     EditorGUIUtility.PingObject(texInfo.texture);
                                 }
                             }
                             else
                             {
                                 // 对于非Texture2D类型，显示可点击的纹理对象字段
                                 if (GUILayout.Button(texInfo.texture.name, EditorStyles.objectField, GUILayout.Width(150)))
                                 {
                                     // 点击时定位到纹理资源
                                     Selection.activeObject = texInfo.texture;
                                     EditorGUIUtility.PingObject(texInfo.texture);
                                 }
                             }
                             
                             EditorGUILayout.EndHorizontal();
                         }
                         
                         EditorGUILayout.EndVertical();
                     }
                     
                     EditorGUILayout.EndVertical();
                     EditorGUILayout.Space(2);
                 }
                 
                 EditorGUILayout.EndScrollView();
             }
             
             // 只有在有变化时才请求重绘
             if (EditorGUI.EndChangeCheck())
             {
                 Repaint();
             }
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
            if (suggestions.Count == 0) 
            {
                // 即使没有优化建议，也显示通用优化说明
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("🎉 优化建议", titleStyle);
                EditorGUILayout.LabelField("✅ 模型各项参数均在推荐范围内！", EditorStyles.boldLabel);
                DrawGeneralOptimizationTips();
                EditorGUILayout.EndVertical();
                return;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔧 优化建议", titleStyle);
            
            // 显示具体的优化建议
            foreach (var suggestion in suggestions)
            {
                // 主要建议使用正常大小字体
                var suggestionStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
                EditorGUILayout.LabelField($"• {suggestion.message}", suggestionStyle);
                
                if (!string.IsNullOrEmpty(suggestion.details))
                {
                    // 详细说明也使用正常大小字体，只是颜色稍淡
                    var detailStyle = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                    };
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(suggestion.details, detailStyle);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.Space(5);
            
            // 显示通用优化技巧
            DrawGeneralOptimizationTips();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGeneralOptimizationTips()
        {
            EditorGUILayout.LabelField("💡 通用优化技巧", EditorStyles.boldLabel);
            
            var categoryStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            
            var tipStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("🖼️ 纹理优化：", categoryStyle);
            EditorGUILayout.LabelField("• 使用工具箱的纹理压缩功能批量优化纹理格式", tipStyle);
            EditorGUILayout.LabelField("• 法线贴图推荐使用BC5格式", tipStyle);
            EditorGUILayout.LabelField("• 漫反射贴图使用DXT1/BC7格式", tipStyle);
            EditorGUILayout.LabelField("• 不重要的纹理可以降低分辨率", tipStyle);
            EditorGUILayout.LabelField("  (如：512x512 → 256x256)", tipStyle);
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("🎭 网格优化：", categoryStyle);
            EditorGUILayout.LabelField("• 使用工具箱的网格压缩功能减少文件大小", tipStyle);
            EditorGUILayout.LabelField("• 移除不必要的细节和隐藏的几何体", tipStyle);
            EditorGUILayout.LabelField("• 合并相似的材质以减少Draw Call", tipStyle);
            EditorGUILayout.LabelField("• 优化网格拓扑，减少不必要的边线", tipStyle);
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("⚙️ 系统优化：", categoryStyle);
            EditorGUILayout.LabelField("• 删除未使用的Expression Parameters参数", tipStyle);
            EditorGUILayout.LabelField("• 优化PhysBone设置，避免过多的碰撞体", tipStyle);
            EditorGUILayout.LabelField("• 使用Constraint替代部分动骨以提高性能", tipStyle);
            EditorGUILayout.LabelField("• 清理无用的Animator Controller状态", tipStyle);
        }
        
        private struct OptimizationSuggestion
        {
            public string message;
            public string details;
            
            public OptimizationSuggestion(string message, string details = "")
            {
                this.message = message;
                this.details = details;
            }
        }
        
        private List<OptimizationSuggestion> GenerateOptimizationSuggestions()
        {
            var suggestions = new List<OptimizationSuggestion>();
            
            // Bits参数详细建议
            if (analysisResult.bitsUsage > VRChatParameterCalculator.MAX_BITS)
            {
                float overUsage = analysisResult.bitsUsage - VRChatParameterCalculator.MAX_BITS;
                suggestions.Add(new OptimizationSuggestion(
                    $"⚠️ Bits参数超出限制 {overUsage:F0} bits",
                    "检查Expression Parameters中的参数设置，删除未使用的参数，或将Bool类型参数改为Int类型以节省空间。每个Bool占用1bit，Int占用8bit但可存储0-255的值。"
                ));
            }
            else if (analysisResult.bitsUsage > VRChatParameterCalculator.MAX_BITS * 0.8f)
            {
                float remaining = VRChatParameterCalculator.MAX_BITS - analysisResult.bitsUsage;
                suggestions.Add(new OptimizationSuggestion(
                    $"🔶 Bits参数接近限制，剩余 {remaining:F0} bits",
                    "建议清理不必要的Expression Parameters，为未来功能预留空间。"
                ));
            }
            
            // 纹理详细建议
            if (analysisResult.textureResult.totalMemoryMB > VRChatParameterCalculator.MAX_TEXTURE_MEMORY_MB)
            {
                float overUsage = analysisResult.textureResult.totalMemoryMB - VRChatParameterCalculator.MAX_TEXTURE_MEMORY_MB;
                suggestions.Add(new OptimizationSuggestion(
                    $"⚠️ 纹理显存超出限制 {overUsage:F1} MiB",
                    "使用工具箱的纹理压缩功能优化大纹理。建议优先处理>10MB的纹理，将DXT5改为BC7，RGBA32改为DXT5/BC7。"
                ));
                
                var largeTextures = analysisResult.textureResult.textureInfos
                    .Where(t => t.sizeMB > 10f).ToList();
                if (largeTextures.Count > 0)
                {
                    var totalLargeSize = largeTextures.Sum(t => t.sizeMB);
                    suggestions.Add(new OptimizationSuggestion(
                        $"🎯 发现 {largeTextures.Count} 个大纹理，占用 {totalLargeSize:F1} MiB",
                        "这些大纹理是优化的重点目标。考虑降低分辨率或使用更高效的压缩格式。"
                    ));
                }
            }
            else if (analysisResult.textureResult.totalMemoryMB > VRChatParameterCalculator.MAX_TEXTURE_MEMORY_MB * 0.8f)
            {
                float remaining = VRChatParameterCalculator.MAX_TEXTURE_MEMORY_MB - analysisResult.textureResult.totalMemoryMB;
                suggestions.Add(new OptimizationSuggestion(
                    $"🔶 纹理显存接近限制，剩余 {remaining:F1} MiB",
                    "建议预先优化纹理压缩设置，为模型未来的纹理添加预留空间。"
                ));
            }
            
            // 动骨详细建议
            if (analysisResult.dynamicBoneCount > VRChatParameterCalculator.MAX_DYNAMIC_BONES)
            {
                int overCount = analysisResult.dynamicBoneCount - VRChatParameterCalculator.MAX_DYNAMIC_BONES;
                suggestions.Add(new OptimizationSuggestion(
                    $"⚠️ 动骨数量超出限制 {overCount} 个",
                    "PhysBone组件过多会影响性能。检查是否有重复的PhysBone，合并相似的骨骼链，或删除不必要的物理效果。"
                ));
            }
            else if (analysisResult.dynamicBoneCount > VRChatParameterCalculator.MAX_DYNAMIC_BONES * 0.8f)
            {
                int remaining = VRChatParameterCalculator.MAX_DYNAMIC_BONES - analysisResult.dynamicBoneCount;
                suggestions.Add(new OptimizationSuggestion(
                    $"🔶 动骨数量接近限制，剩余 {remaining} 个",
                    "建议优化PhysBone设置，确保每个物理骨骼都有明确的用途。"
                ));
            }
            
            // 模型大小详细建议
            if (analysisResult.totalUncompressedSizeMB > VRChatParameterCalculator.MAX_UNCOMPRESSED_SIZE_MB)
            {
                float overSize = analysisResult.totalUncompressedSizeMB - VRChatParameterCalculator.MAX_UNCOMPRESSED_SIZE_MB;
                suggestions.Add(new OptimizationSuggestion(
                    $"⚠️ 模型解压后大小超限 {overSize:F1} MiB",
                    "使用工具箱的纹理和网格压缩功能。网格压缩可节省20-50%空间，纹理压缩效果更明显。"
                ));
            }
            
            // 上传大小建议 - 使用向上取整逻辑
            float uploadSizeCeiled = Mathf.Ceil(analysisResult.estimatedUploadSizeMB);
            if (uploadSizeCeiled > VRChatParameterCalculator.MAX_UPLOAD_SIZE_MB)
            {
                float overSize = uploadSizeCeiled - VRChatParameterCalculator.MAX_UPLOAD_SIZE_MB;
                suggestions.Add(new OptimizationSuggestion(
                    $"⚠️ 模型上传大小超限 {overSize:F0} MiB",
                    "上传限制主要受纹理影响。建议先优化纹理压缩，再考虑网格优化。"
                ));
            }
            
            // 顶点数详细建议
            if (analysisResult.modelSize.vertexCount > 100000)
            {
                suggestions.Add(new OptimizationSuggestion(
                    $"🔶 顶点数较高: {analysisResult.modelSize.vertexCount:N0}",
                    "高顶点数可能影响渲染性能。考虑简化网格拓扑，移除不可见的几何体，或为距离较远的情况制作LOD模型。"
                ));
            }
            else if (analysisResult.modelSize.vertexCount > 70000)
            {
                suggestions.Add(new OptimizationSuggestion(
                    $"💡 顶点数适中: {analysisResult.modelSize.vertexCount:N0}",
                    "顶点数在合理范围内，可以考虑进一步优化以提升性能。"
                ));
            }
            
            // 三角形数建议
            if (analysisResult.modelSize.triangleCount > 150000)
            {
                suggestions.Add(new OptimizationSuggestion(
                    $"🔶 三角形数较高: {analysisResult.modelSize.triangleCount:N0}",
                    "高三角形数可能影响渲染性能，特别是在Quest等移动平台上。建议优化网格细节级别。"
                ));
            }
            
            // 材质数量建议
            if (analysisResult.materialCount > 20)
            {
                suggestions.Add(new OptimizationSuggestion(
                    $"🔶 材质数量较多: {analysisResult.materialCount} 个",
                    "过多的材质会增加Draw Call。尝试合并使用相同着色器的材质，或使用纹理图集技术。"
                ));
            }
            
            // 网格数量建议
            if (analysisResult.meshCount > 50)
            {
                suggestions.Add(new OptimizationSuggestion(
                    $"💡 网格数量较多: {analysisResult.meshCount} 个",
                    "考虑合并静态网格以减少渲染批次，但保持服装分离以便于切换。"
                ));
            }
            
            // 纹理格式建议
            if (analysisResult.textureResult.textureInfos.Any(t => t.format.Contains("RGBA32") || t.format.Contains("ARGB32")))
            {
                var uncompressedTextures = analysisResult.textureResult.textureInfos
                    .Where(t => t.format.Contains("RGBA32") || t.format.Contains("ARGB32")).ToList();
                suggestions.Add(new OptimizationSuggestion(
                    $"💡 发现 {uncompressedTextures.Count} 个未压缩纹理",
                    "RGBA32/ARGB32格式占用空间大。建议改为BC7(高质量)或DXT5(兼容性好)格式以节省显存。"
                ));
            }
            
            return suggestions;
        }
        
        private void AnalyzeAvatar()
        {
            if (selectedAvatar == null)
            {
                analysisResult = null;
                textureCompressionList = null; // 重置纹理压缩列表
                textureFoldoutStates.Clear(); // 清除折叠状态
                needsTextureListUpdate = false;
                return;
            }
            
            try
            {
                EditorUtility.DisplayProgressBar("分析中", "正在分析模型参数...", 0f);
                
                analysisResult = new DetailedAnalysisResult();
                
                // 重置相关状态
                textureCompressionList = null;
                textureFoldoutStates.Clear();
                meshDetailList = null;
                meshCompressionList = null; // 重置网格压缩列表
                meshFoldoutStates.Clear(); // 清除网格折叠状态
                needsTextureListUpdate = true;
                
                // 重置层级树状结构数据
                expandedNodes.Clear();
                nodeColors.Clear();
                colorIndex = 0;
                lastHierarchyUpdateTime = 0f;
                
                // 使用新的计算器进行分析
                EditorUtility.DisplayProgressBar("分析中", "计算Bits参数...", 0.2f);
                analysisResult.bitsUsage = VRChatParameterCalculator.CalculateBitsUsage(selectedAvatar);
                
                EditorUtility.DisplayProgressBar("分析中", "分析纹理显存...", 0.4f);
                analysisResult.textureResult = CalculateTextureMemoryUsingThryLogic(selectedAvatar);
                
                EditorUtility.DisplayProgressBar("分析中", "计算动骨数量...", 0.6f);
                var dynamicBoneResult = VRChatParameterCalculator.CalculateDynamicBoneCount(selectedAvatar);
                analysisResult.dynamicBoneCount = dynamicBoneResult.totalCount;
                analysisResult.dynamicBoneInfo = dynamicBoneResult;
                
                EditorUtility.DisplayProgressBar("分析中", "计算模型大小...", 0.8f);
                analysisResult.modelSize = VRChatParameterCalculator.CalculateModelSize(selectedAvatar);
                
                // 计算其他信息
                AnalyzeOtherInfo();
                
                // 计算总大小 - 使用完整的画质压缩工具Combined (all)计算逻辑
                var combinedAllSize = CalculateCombinedAllSizeUsingThryLogic(selectedAvatar);
                analysisResult.totalUncompressedSizeMB = combinedAllSize / (1024f * 1024f);
                
                // 计算模型上传大小 - 完整迁移画质压缩工具Combined (only active)计算逻辑
                var combinedOnlyActiveSize = CalculateCombinedOnlyActiveSizeUsingThryLogic(selectedAvatar);
                analysisResult.estimatedUploadSizeMB = combinedOnlyActiveSize / (1024f * 1024f);
                
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
            
            // 显示模式提示
            if (showDetailedMeshInfo)
            {
                EditorGUILayout.LabelField(position.width < 450 ? "💖 小窗模式启动～ 可拖拽窗口切换哦！" : "💫 宽屏模式中～ 拖小一点试试看！", EditorStyles.miniLabel);
            }
            
            if (showDetailedMeshInfo && meshDetailList != null && meshDetailList.Count > 0)
            {
                EditorGUILayout.Space(5);
                
                // 检查窗口宽度，决定使用简化模式还是表格模式
                bool useCompactMode = position.width < 450;
                
                if (useCompactMode)
                {
                    // 简化模式 - 更紧凑的单行显示
                    EditorGUILayout.LabelField("🌟 网格列表 (简化模式) 🌟", EditorStyles.boldLabel);
                    
                    meshScrollPosition = EditorGUILayout.BeginScrollView(meshScrollPosition, GUILayout.Height(Math.Min(300, meshDetailList.Count * 22 + 30)));
                    
                    foreach (var meshInfo in meshDetailList)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        
                        // 状态指示器 - 居中对齐
                        var compactStatusCenterStyle = new GUIStyle(EditorStyles.miniLabel) { 
                            alignment = TextAnchor.MiddleCenter
                        };
                        GUI.color = meshInfo.isActive ? Color.green : Color.gray;
                        EditorGUILayout.LabelField(meshInfo.isActive ? "●" : "○", compactStatusCenterStyle, GUILayout.Width(15));
                        GUI.color = Color.white;
                        
                        // 网格名称 (主要信息) - 左对齐居中
                        var compactNameLeftStyle = new GUIStyle(EditorStyles.boldLabel) { 
                            alignment = TextAnchor.MiddleLeft
                        };
                        EditorGUILayout.LabelField(meshInfo.name, compactNameLeftStyle, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                        
                        // 大小 - 使用等宽字体和居中对齐样式
                        var compactSizeCenterAlignStyle = new GUIStyle(EditorStyles.miniLabel) { 
                            alignment = TextAnchor.MiddleCenter,
                            font = EditorStyles.miniLabel.font
                        };
                        EditorGUILayout.LabelField($"{meshInfo.sizeMB}", compactSizeCenterAlignStyle, GUILayout.Width(60));
                        
                        // 使用等宽字体和居中对齐样式显示数字
                        var compactCenterAlignStyle = new GUIStyle(EditorStyles.miniLabel) { 
                            alignment = TextAnchor.MiddleCenter,
                            font = EditorStyles.miniLabel.font
                        };
                        EditorGUILayout.LabelField($"{meshInfo.vertexCount:N0}v", compactCenterAlignStyle, GUILayout.Width(50));
                        EditorGUILayout.LabelField($"{meshInfo.triangleCount:N0}t", compactCenterAlignStyle, GUILayout.Width(50));
                        
                        // 混合形状 - 使用等宽字体和居中对齐
                        var blendShapeCenterAlignStyle = new GUIStyle(EditorStyles.miniLabel) { 
                            alignment = TextAnchor.MiddleCenter,
                            font = EditorStyles.miniLabel.font
                        };
                        if (meshInfo.hasBlendShapes)
                        {
                            EditorGUILayout.LabelField($"{meshInfo.blendShapeCount}bs", blendShapeCenterAlignStyle, GUILayout.Width(40));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("-", blendShapeCenterAlignStyle, GUILayout.Width(40));
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    // 表格模式 - 原有的表格显示
                    // 网格压缩选项切换
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("网格压缩选项", EditorStyles.boldLabel);
                    if (GUILayout.Button(showMeshCompression ? "隐藏压缩选项" : "显示压缩选项", GUILayout.Width(100)))
                    {
                        showMeshCompression = !showMeshCompression;
                        
                        // 确保在显示压缩选项时初始化网格压缩列表
                        if (showMeshCompression && meshCompressionList == null)
                        {
                            InitializeMeshCompressionList();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    if (showMeshCompression)
                    {
                        DrawMeshCompressionOptions();
                    }
                    else
                    {
                    // 网格质量评估
                    DrawMeshQualityAssessment();
                    
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("网格列表 (按大小排序):", EditorStyles.boldLabel);
                    
                    // 添加表头
                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    
                    // 状态表头居中对齐
                    var statusHeaderCenterStyle = new GUIStyle(EditorStyles.boldLabel) {
                        alignment = TextAnchor.MiddleCenter
                    };
                    EditorGUILayout.LabelField("状态", statusHeaderCenterStyle, GUILayout.Width(30));

                    // 网格名称表头左对齐
                    var nameHeaderLeftStyle = new GUIStyle(EditorStyles.boldLabel) {
                        alignment = TextAnchor.MiddleLeft
                    };
                    EditorGUILayout.LabelField("网格名称", nameHeaderLeftStyle, GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));

                    // 大小表头居中对齐
                    var sizeHeaderCenterStyle = new GUIStyle(EditorStyles.boldLabel) {
                        alignment = TextAnchor.MiddleCenter,
                        font = EditorStyles.miniLabel.font
                    };
                    EditorGUILayout.LabelField("大小", sizeHeaderCenterStyle, GUILayout.Width(70));

                    // 顶点数/三角形数表头居中对齐
                    var vertexTriangleHeaderCenterStyle = new GUIStyle(EditorStyles.boldLabel) {
                        alignment = TextAnchor.MiddleCenter,
                        font = EditorStyles.miniLabel.font
                    };
                    EditorGUILayout.LabelField("顶点数/三角形数", vertexTriangleHeaderCenterStyle, GUILayout.Width(140));

                    // 混合形状表头居中对齐
                    var blendShapeHeaderCenterStyle = new GUIStyle(EditorStyles.boldLabel) {
                        alignment = TextAnchor.MiddleCenter,
                        font = EditorStyles.miniLabel.font
                    };
                    EditorGUILayout.LabelField("混合形状", blendShapeHeaderCenterStyle, GUILayout.Width(70));
                    EditorGUILayout.EndHorizontal();
                    
                    meshScrollPosition = EditorGUILayout.BeginScrollView(meshScrollPosition, GUILayout.Height(Math.Min(400, meshDetailList.Count * 25 + 50)));
                    
                    foreach (var meshInfo in meshDetailList)
                    {
                        DrawMeshInfoItem(meshInfo);
                    }
                    
                    EditorGUILayout.EndScrollView();
                    }
                }
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
            
            // 按大小排序，如果大小相同则随机排列
            var random = new System.Random();
            meshDetailList.Sort((m1, m2) => 
            {
                int sizeComparison = m2.sizeBytes.CompareTo(m1.sizeBytes);
                if (sizeComparison == 0)
                {
                    // 大小相同时随机排列
                    return random.Next(-1, 2);
                }
                return sizeComparison;
            });
        }
        
        // 初始化网格压缩列表
        private void InitializeMeshCompressionList()
        {
            if (selectedAvatar == null)
            {
                meshCompressionList = null;
                return;
            }
            
            try
            {
                Debug.Log("[网格压缩] 开始初始化网格压缩列表");
                
                meshCompressionList = new List<MeshCompressionInfo>();
                
                // 获取所有渲染器
                var allRenderers = selectedAvatar.GetComponentsInChildren<Renderer>(true);
                var activeRenderers = selectedAvatar.GetComponentsInChildren<Renderer>(false);
                
                // 创建网格到渲染器的映射
                Dictionary<Mesh, List<Renderer>> meshToRenderers = new Dictionary<Mesh, List<Renderer>>();
                Dictionary<Mesh, bool> meshActiveStatus = new Dictionary<Mesh, bool>();
                
                // 收集所有网格及其使用的渲染器
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
                        // 添加到网格-渲染器映射
                        if (!meshToRenderers.ContainsKey(mesh))
                        {
                            meshToRenderers[mesh] = new List<Renderer>();
                        }
                        meshToRenderers[mesh].Add(renderer);
                        
                        // 更新活动状态
                        bool isActive = activeRenderers.Contains(renderer);
                        if (meshActiveStatus.ContainsKey(mesh))
                        {
                            if (!meshActiveStatus[mesh] && isActive)
                                meshActiveStatus[mesh] = true;
                        }
                        else
                        {
                            meshActiveStatus.Add(mesh, isActive);
                        }
                    }
                }
                
                // 为每个网格创建压缩信息
                foreach (var kvp in meshToRenderers)
                {
                    var mesh = kvp.Key;
                    var renderers = kvp.Value;
                    bool isActive = meshActiveStatus.ContainsKey(mesh) ? meshActiveStatus[mesh] : false;
                    
                    var compressionInfo = new MeshCompressionInfo
                    {
                        mesh = mesh,
                        name = mesh.name,
                        isActive = isActive,
                        vertexCount = mesh.vertexCount,
                        triangleCount = mesh.triangles.Length / 3,
                        blendShapeCount = mesh.blendShapeCount,
                        hasBlendShapes = mesh.blendShapeCount > 0,
                        renderers = renderers,
                        compressionChanged = false
                    };
                    
                    // 计算原始网格大小
                    long meshSize = CalculateMeshSize(mesh);
                    compressionInfo.originalSizeBytes = meshSize;
                    compressionInfo.originalSizeMB = FormatBytes(meshSize);
                    
                    // 获取当前模型导入设置来设置默认压缩选项
                    string assetPath = AssetDatabase.GetAssetPath(mesh);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                        if (importer != null)
                        {
                            compressionInfo.compressionQuality = importer.meshCompression;
                            compressionInfo.optimizeMesh = importer.optimizeMeshVertices;
                            compressionInfo.weldenVertices = importer.optimizeMeshPolygons;
                        }
                    }
                    
                    meshCompressionList.Add(compressionInfo);
                }
                
                // 按大小排序，如果大小相同则随机排列
                var random = new System.Random();
                meshCompressionList.Sort((m1, m2) => 
                {
                    int sizeComparison = m2.originalSizeBytes.CompareTo(m1.originalSizeBytes);
                    if (sizeComparison == 0)
                    {
                        // 大小相同时随机排列
                        return random.Next(-1, 2);
                    }
                    return sizeComparison;
                });
                
                Debug.Log($"[网格压缩] 初始化完成，找到 {meshCompressionList.Count} 个网格");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[网格压缩] 初始化失败: {e.Message}");
                meshCompressionList = null;
            }
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

        // 绘制纹理质量评估
        private void DrawTextureQualityAssessment()
        {
            if (textureCompressionList == null || textureCompressionList.Count == 0) return;

            long totalTextureSize = textureCompressionList.Sum(t => t.sizeBytes);
            long activeTextureSize = textureCompressionList.Where(t => t.isActive).Sum(t => t.sizeBytes);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("纹理质量评估", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"总纹理内存: {FormatBytes(totalTextureSize)}");
            EditorGUILayout.LabelField($"活动纹理内存: {FormatBytes(activeTextureSize)}");

            // PC质量评估
            var pcQuality = GetTextureQuality(totalTextureSize, false);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("PC质量:", GUILayout.Width(60));
            DrawQualityLabel(pcQuality);
            EditorGUILayout.EndHorizontal();

            // Quest质量评估
            var questQuality = GetTextureQuality(totalTextureSize, true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quest质量:", GUILayout.Width(60));
            DrawQualityLabel(questQuality);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // 基于分析结果绘制纹理质量评估（始终显示）
        private void DrawTextureQualityAssessmentFromAnalysisResult()
        {
            if (analysisResult?.textureResult == null) return;

            // 计算总纹理大小（字节）
            long totalTextureSizeBytes = (long)(analysisResult.textureResult.totalMemoryMB * 1024 * 1024);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("纹理质量评估", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"总纹理内存: {analysisResult.textureResult.totalMemoryMB:F2} MB");
            EditorGUILayout.LabelField($"纹理数量: {analysisResult.textureResult.textureCount} 个");

            // PC质量评估
            var pcQuality = GetTextureQuality(totalTextureSizeBytes, false);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("PC质量:", GUILayout.Width(60));
            DrawQualityLabel(pcQuality);
            EditorGUILayout.EndHorizontal();

            // Quest质量评估
            var questQuality = GetTextureQuality(totalTextureSizeBytes, true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quest质量:", GUILayout.Width(60));
            DrawQualityLabel(questQuality);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        // 获取纹理质量评估（从画质压缩工具迁移）
        private QualityLevel GetTextureQuality(long size, bool quest)
        {
            // 纹理质量阈值常量 (MiB) - 从画质压缩工具完全迁移
            const long PC_TEXTURE_MEMORY_EXCELLENT_MiB = 40 * 1024 * 1024;
            const long PC_TEXTURE_MEMORY_GOOD_MiB = 75 * 1024 * 1024;
            const long PC_TEXTURE_MEMORY_MEDIUM_MiB = 110 * 1024 * 1024;
            const long PC_TEXTURE_MEMORY_POOR_MiB = 150 * 1024 * 1024;

            const long QUEST_TEXTURE_MEMORY_EXCELLENT_MiB = 10 * 1024 * 1024;
            const long QUEST_TEXTURE_MEMORY_GOOD_MiB = 18 * 1024 * 1024;
            const long QUEST_TEXTURE_MEMORY_MEDIUM_MiB = 25 * 1024 * 1024;
            const long QUEST_TEXTURE_MEMORY_POOR_MiB = 40 * 1024 * 1024;

            if (quest)
                return GetQualityLevel(size, QUEST_TEXTURE_MEMORY_EXCELLENT_MiB, QUEST_TEXTURE_MEMORY_GOOD_MiB, QUEST_TEXTURE_MEMORY_MEDIUM_MiB, QUEST_TEXTURE_MEMORY_POOR_MiB);
            else
                return GetQualityLevel(size, PC_TEXTURE_MEMORY_EXCELLENT_MiB, PC_TEXTURE_MEMORY_GOOD_MiB, PC_TEXTURE_MEMORY_MEDIUM_MiB, PC_TEXTURE_MEMORY_POOR_MiB);
        }

        // 通用质量评估方法（从画质压缩工具迁移）
        private QualityLevel GetQualityLevel(long size, long excellent, long good, long medium, long poor)
        {
            if (size <= excellent) return QualityLevel.Excellent;
            if (size <= good) return QualityLevel.Good;
            if (size <= medium) return QualityLevel.Medium;
            if (size <= poor) return QualityLevel.Poor;
            return QualityLevel.VeryPoor;
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
            
            // 活动状态指示器 - 居中对齐
            var statusCenterStyle = new GUIStyle(GUI.skin.label) { 
                alignment = TextAnchor.MiddleCenter
            };
            GUI.color = meshInfo.isActive ? Color.green : Color.gray;
            EditorGUILayout.LabelField(meshInfo.isActive ? "●" : "○", statusCenterStyle, GUILayout.Width(30));
            GUI.color = Color.white;
            
            // 网格名称 - 居中对齐
            var nameCenterStyle = new GUIStyle(GUI.skin.label) { 
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField(meshInfo.name, nameCenterStyle, GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));
            
            // 大小 - 使用等宽字体和居中对齐
            var sizeCenterAlignStyle = new GUIStyle(GUI.skin.label) { 
                alignment = TextAnchor.MiddleCenter,
                font = EditorStyles.miniLabel.font
            };
            EditorGUILayout.LabelField(meshInfo.sizeMB, sizeCenterAlignStyle, GUILayout.Width(70));
            
            // 顶点数/三角形数 - 使用等宽字体和居中对齐，增加宽度
            var centerAlignStyle = new GUIStyle(GUI.skin.label) { 
                alignment = TextAnchor.MiddleCenter,
                font = EditorStyles.miniLabel.font
            };
            EditorGUILayout.LabelField($"{meshInfo.vertexCount:N0}/{meshInfo.triangleCount:N0}", centerAlignStyle, GUILayout.Width(140));
            
            // 混合形状 - 使用等宽字体和居中对齐
            var blendShapeCenterAlignStyle = new GUIStyle(GUI.skin.label) { 
                alignment = TextAnchor.MiddleCenter,
                font = EditorStyles.miniLabel.font
            };
            if (meshInfo.hasBlendShapes)
            {
                EditorGUILayout.LabelField($"{meshInfo.blendShapeCount}", blendShapeCenterAlignStyle, GUILayout.Width(70));
            }
            else
            {
                EditorGUILayout.LabelField("-", blendShapeCenterAlignStyle, GUILayout.Width(70));
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 如果需要查看网格对象，可以在点击时显示
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && 
                GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                Selection.activeObject = meshInfo.mesh;
                EditorGUIUtility.PingObject(meshInfo.mesh);
                Event.current.Use();
            }
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("层级文件树状结构", titleStyle);
            
            if (GUILayout.Button(showHierarchyTree ? "隐藏" : "显示", 
                GUILayout.Width(60)))
            {
                showHierarchyTree = !showHierarchyTree;
            }
            EditorGUILayout.EndHorizontal();
            
            if (showHierarchyTree && selectedAvatar != null)
            {
                EditorGUILayout.Space(5);
                
                // 使用变化检测来控制重绘
                EditorGUI.BeginChangeCheck();
                
                hierarchyScrollPosition = EditorGUILayout.BeginScrollView(hierarchyScrollPosition, GUILayout.MaxHeight(300));
                
                // 重置颜色索引
                colorIndex = 0;
                
                // 绘制根节点
                DrawHierarchyNode(selectedAvatar.transform, 0, true);
                
                EditorGUILayout.EndScrollView();
                
                // 只有在有变化时才请求重绘
                bool hierarchyChanged = EditorGUI.EndChangeCheck();
                
                // 限制刷新频率，但允许用户交互立即响应
                if (hierarchyChanged || Time.realtimeSinceStartup - lastHierarchyUpdateTime > HIERARCHY_UPDATE_INTERVAL)
                {
                    lastHierarchyUpdateTime = Time.realtimeSinceStartup;
                    if (hierarchyChanged)
                    {
                        Repaint();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
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
        
        // 绘制网格压缩选项
        private void DrawMeshCompressionOptions()
        {
            if (meshCompressionList == null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("❌ 网格压缩列表未初始化", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("状态: 正在尝试初始化...");
                
                if (GUILayout.Button("重新扫描网格"))
                {
                    meshCompressionList = null;
                    InitializeMeshCompressionList();
                    
                    if (meshCompressionList == null || meshCompressionList.Count == 0)
                    {
                        // 提供诊断信息
                        var renderers = selectedAvatar.GetComponentsInChildren<Renderer>(true);
                        var meshes = renderers.SelectMany(r => 
                            r is SkinnedMeshRenderer ? new[] { (r as SkinnedMeshRenderer).sharedMesh } : 
                            r is MeshRenderer ? new[] { r.GetComponent<MeshFilter>()?.sharedMesh } : 
                            new Mesh[0]).Where(m => m != null).ToList();
                        
                        EditorGUILayout.LabelField($"• 找到 {renderers.Length} 个渲染器");
                        EditorGUILayout.LabelField($"• 找到 {meshes.Count} 个网格");
                        
                        if (meshes.Count == 0)
                        {
                            EditorGUILayout.LabelField("• ❌ 模型中没有网格，无法压缩");
                        }
                        else
                        {
                            EditorGUILayout.LabelField("• ⚠️ 网格加载失败，请检查模型");
                        }
                    }
                }
                
                EditorGUILayout.EndVertical();
                return;
            }
            
            // 使用变化检测来减少不必要的重绘
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.Space(5);
            
            // 网格状态信息
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"✅ 找到 {meshCompressionList.Count} 个网格", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 压缩建议
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("压缩建议:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• 高压缩: 大幅减少文件大小，可能影响质量");
            EditorGUILayout.LabelField("• 中等压缩: 平衡大小和质量 (推荐)");
            EditorGUILayout.LabelField("• 低压缩: 保持质量，适度减少大小");
            EditorGUILayout.LabelField("• 移除无用顶点流: 删除不需要的顶点属性");
            EditorGUILayout.LabelField("• 优化索引: 重新排列三角形索引以提高性能");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 应用所有更改按钮
            bool hasChanges = meshCompressionList.Any(m => m.compressionChanged);
            EditorGUI.BeginDisabledGroup(!hasChanges);
            if (GUILayout.Button("应用所有网格压缩更改", GUILayout.Height(30)))
            {
                ApplyAllMeshCompressionChanges();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            
            // 网格列表
            meshCompressionScrollPosition = EditorGUILayout.BeginScrollView(meshCompressionScrollPosition, GUILayout.Height(400));
            
            // 添加表头
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("状态", EditorStyles.boldLabel, GUILayout.Width(20));
            EditorGUILayout.LabelField("网格名称", EditorStyles.boldLabel, GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("顶点数", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("三角形数", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("大小", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < meshCompressionList.Count; i++)
            {
                var meshInfo = meshCompressionList[i];
                
                // 为每个网格项目添加独立的变化检测
                EditorGUI.BeginChangeCheck();
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // 网格基本信息
                EditorGUILayout.BeginHorizontal();
                
                // 活动状态指示器 - 居中对齐
                var statusCenterStyle = new GUIStyle(GUI.skin.label) { 
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.color = meshInfo.isActive ? Color.green : Color.gray;
                EditorGUILayout.LabelField(meshInfo.isActive ? "●" : "○", statusCenterStyle, GUILayout.Width(20));
                GUI.color = Color.white;
                
                // 网格名称 - 左对齐
                var nameLeftStyle = new GUIStyle(EditorStyles.boldLabel) { 
                    alignment = TextAnchor.MiddleLeft
                };
                EditorGUILayout.LabelField(meshInfo.name, nameLeftStyle, GUILayout.MinWidth(60), GUILayout.ExpandWidth(true));
                
                // 顶点数 - 居中对齐
                var vertexCenterStyle = new GUIStyle(GUI.skin.label) { 
                    alignment = TextAnchor.MiddleCenter,
                    font = EditorStyles.miniLabel.font
                };
                EditorGUILayout.LabelField($"{meshInfo.vertexCount:N0}", vertexCenterStyle, GUILayout.Width(60));
                
                // 三角形数 - 居中对齐
                var triangleCenterStyle = new GUIStyle(GUI.skin.label) { 
                    alignment = TextAnchor.MiddleCenter,
                    font = EditorStyles.miniLabel.font
                };
                EditorGUILayout.LabelField($"{meshInfo.triangleCount:N0}", triangleCenterStyle, GUILayout.Width(60));
                
                // 大小 - 居中对齐
                var sizeCenterStyle = new GUIStyle(GUI.skin.label) { 
                    alignment = TextAnchor.MiddleCenter,
                    font = EditorStyles.miniLabel.font
                };
                EditorGUILayout.LabelField(meshInfo.originalSizeMB, sizeCenterStyle, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                // 压缩质量选项
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("压缩质量:", GUILayout.Width(60));
                ModelImporterMeshCompression newCompression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup(meshInfo.compressionQuality, GUILayout.Width(100));
                
                // 快速压缩按钮
                if (GUILayout.Button("高", GUILayout.Width(25)))
                {
                    newCompression = ModelImporterMeshCompression.High;
                }
                if (GUILayout.Button("中", GUILayout.Width(25)))
                {
                    newCompression = ModelImporterMeshCompression.Medium;
                }
                if (GUILayout.Button("低", GUILayout.Width(25)))
                {
                    newCompression = ModelImporterMeshCompression.Low;
                }
                if (GUILayout.Button("关", GUILayout.Width(25)))
                {
                    newCompression = ModelImporterMeshCompression.Off;
                }
                EditorGUILayout.EndHorizontal();
                
                // 详细压缩选项
                EditorGUILayout.LabelField("压缩选项:", EditorStyles.boldLabel);
                
                // 使用固定列宽的2x4布局，确保完美对齐
                EditorGUILayout.BeginVertical();
                
                // 第一行：基础压缩
                EditorGUILayout.BeginHorizontal();
                bool newCompressVertexPosition = GUILayout.Toggle(meshInfo.compressVertexPosition, " 压缩顶点位置", GUILayout.Width(140));
                bool newCompressNormals = GUILayout.Toggle(meshInfo.compressNormals, " 压缩法线", GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                // 第二行：纹理相关
                EditorGUILayout.BeginHorizontal();
                bool newCompressUVs = GUILayout.Toggle(meshInfo.compressUVs, " 压缩UV", GUILayout.Width(140));
                bool newCompressColors = GUILayout.Toggle(meshInfo.compressColors, " 压缩颜色", GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                // 第三行：优化选项
                EditorGUILayout.BeginHorizontal();
                bool newRemoveUnused = GUILayout.Toggle(meshInfo.removeUnusedVertexStreams, " 移除无用顶点流", GUILayout.Width(140));
                bool newOptimizeIndex = GUILayout.Toggle(meshInfo.optimizeIndexBuffer, " 优化索引", GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                // 第四行：网格优化
                EditorGUILayout.BeginHorizontal();
                bool newOptimizeMesh = GUILayout.Toggle(meshInfo.optimizeMesh, " 优化网格", GUILayout.Width(140));
                bool newWeldenVertices = GUILayout.Toggle(meshInfo.weldenVertices, " 合并顶点", GUILayout.Width(100));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                // 只有当值实际改变时才更新状态
                if (EditorGUI.EndChangeCheck())
                {
                    if (newCompression != meshInfo.compressionQuality ||
                        newCompressVertexPosition != meshInfo.compressVertexPosition ||
                        newCompressNormals != meshInfo.compressNormals ||
                        newCompressUVs != meshInfo.compressUVs ||
                        newCompressColors != meshInfo.compressColors ||
                        newRemoveUnused != meshInfo.removeUnusedVertexStreams ||
                        newOptimizeIndex != meshInfo.optimizeIndexBuffer ||
                        newOptimizeMesh != meshInfo.optimizeMesh ||
                        newWeldenVertices != meshInfo.weldenVertices)
                    {
                        meshInfo.compressionQuality = newCompression;
                        meshInfo.compressVertexPosition = newCompressVertexPosition;
                        meshInfo.compressNormals = newCompressNormals;
                        meshInfo.compressUVs = newCompressUVs;
                        meshInfo.compressColors = newCompressColors;
                        meshInfo.removeUnusedVertexStreams = newRemoveUnused;
                        meshInfo.optimizeIndexBuffer = newOptimizeIndex;
                        meshInfo.optimizeMesh = newOptimizeMesh;
                        meshInfo.weldenVertices = newWeldenVertices;
                        meshInfo.compressionChanged = true;
                    }
                }
                
                // 显示估计的压缩效果
                if (meshInfo.compressionChanged)
                {
                    float estimatedSavings = EstimateMeshCompressionSavings(meshInfo);
                    EditorGUILayout.LabelField($"预计节省: ~{estimatedSavings:F1}%", GUILayout.Width(100));
                }
                
                // 使用此网格的渲染器信息
                if (meshInfo.renderers.Count > 0)
                {
                    string rendererKey = $"renderer_{meshInfo.name}_{i}";
                    if (!meshFoldoutStates.ContainsKey(rendererKey))
                    {
                        meshFoldoutStates[rendererKey] = true; // 默认展开状态
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    bool currentFoldout = meshFoldoutStates[rendererKey];
                    bool newFoldout = EditorGUILayout.Foldout(currentFoldout, $"使用此网格的渲染器 ({meshInfo.renderers.Count})");
                    if (newFoldout != currentFoldout)
                    {
                        meshFoldoutStates[rendererKey] = newFoldout;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    if (newFoldout)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var renderer in meshInfo.renderers)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.ObjectField(renderer, typeof(Renderer), true, GUILayout.MinWidth(80), GUILayout.ExpandWidth(true));
                            EditorGUILayout.LabelField($"({renderer.GetType().Name})", GUILayout.Width(80));
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
            
            // 只有在有变化时才请求重绘
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }
        }
        
        // 估计网格压缩节省空间
        private float EstimateMeshCompressionSavings(MeshCompressionInfo meshInfo)
        {
            float savingsPercentage = 0f;
            
            // 基于压缩质量估算
            switch (meshInfo.compressionQuality)
            {
                case ModelImporterMeshCompression.Off:
                    savingsPercentage = 0f;
                    break;
                case ModelImporterMeshCompression.Low:
                    savingsPercentage = 15f;
                    break;
                case ModelImporterMeshCompression.Medium:
                    savingsPercentage = 30f;
                    break;
                case ModelImporterMeshCompression.High:
                    savingsPercentage = 50f;
                    break;
            }
            
            // 额外的压缩选项估算
            if (meshInfo.compressVertexPosition) savingsPercentage += 10f;
            if (meshInfo.compressNormals) savingsPercentage += 8f;
            if (meshInfo.compressUVs) savingsPercentage += 5f;
            if (meshInfo.compressColors) savingsPercentage += 3f;
            if (meshInfo.removeUnusedVertexStreams) savingsPercentage += 12f;
            if (meshInfo.optimizeIndexBuffer) savingsPercentage += 5f;
            
            // 限制最大节省百分比
            return Mathf.Min(savingsPercentage, 75f);
        }
        
        // 应用所有网格压缩更改
        private void ApplyAllMeshCompressionChanges()
        {
            if (meshCompressionList == null) return;
            
            int appliedCount = 0;
            int totalChanges = meshCompressionList.Count(m => m.compressionChanged);
            
            try
            {
                EditorUtility.DisplayProgressBar("应用网格压缩", "正在处理网格压缩设置...", 0f);
                
                foreach (var meshInfo in meshCompressionList.Where(m => m.compressionChanged))
                {
                    string assetPath = AssetDatabase.GetAssetPath(meshInfo.mesh);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        Debug.LogWarning($"[网格压缩] 无法找到网格 {meshInfo.name} 的资源路径，跳过");
                        continue;
                    }
                    
                    var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    if (importer == null)
                    {
                        Debug.LogWarning($"[网格压缩] 无法获取网格 {meshInfo.name} 的模型导入器，跳过");
                        continue;
                    }
                    
                    bool importerChanged = false;
                    
                    // 应用基本压缩设置
                    if (importer.meshCompression != meshInfo.compressionQuality)
                    {
                        importer.meshCompression = meshInfo.compressionQuality;
                        importerChanged = true;
                    }
                    
                    // 应用网格优化选项
                    if (meshInfo.optimizeIndexBuffer && !importer.optimizeMeshPolygons)
                    {
                        importer.optimizeMeshPolygons = true;
                        importerChanged = true;
                    }
                    
                    if (meshInfo.removeUnusedVertexStreams && !importer.optimizeMeshVertices)
                    {
                        importer.optimizeMeshVertices = true;
                        importerChanged = true;
                    }
                    
                    // 应用其他优化设置
                    if (meshInfo.optimizeMesh != importer.optimizeMeshVertices)
                    {
                        importer.optimizeMeshVertices = meshInfo.optimizeMesh;
                        importerChanged = true;
                    }
                    
                    if (meshInfo.weldenVertices != importer.optimizeMeshPolygons)
                    {
                        importer.optimizeMeshPolygons = meshInfo.weldenVertices;
                        importerChanged = true;
                    }
                    
                    // 保存更改并重新导入
                    if (importerChanged)
                    {
                        importer.SaveAndReimport();
                        appliedCount++;
                        Debug.Log($"[网格压缩] 已应用压缩设置到网格: {meshInfo.name}");
                    }
                    
                    // 重置变化标志
                    meshInfo.compressionChanged = false;
                    
                    // 更新进度
                    EditorUtility.DisplayProgressBar("应用网格压缩", 
                        $"正在处理: {meshInfo.name} ({appliedCount}/{totalChanges})", 
                        (float)appliedCount / totalChanges);
                }
                
                EditorUtility.ClearProgressBar();
                
                // 刷新资源数据库
                AssetDatabase.Refresh();
                
                // 重新初始化列表以反映更改
                InitializeMeshCompressionList();
                
                EditorUtility.DisplayDialog("网格压缩完成", 
                    $"成功应用了 {appliedCount} 个网格的压缩设置！\n\n" +
                    "模型资源已重新导入，压缩设置已生效。", "确定");
                    
                Debug.Log($"[网格压缩] 批量压缩完成！成功处理 {appliedCount} 个网格");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[网格压缩] 应用压缩设置时发生错误: {e.Message}");
                EditorUtility.DisplayDialog("错误", $"应用网格压缩时发生错误:\n{e.Message}", "确定");
            }
        }
        
        // 使用完整的Thry's Avatar Tools算法计算纹理显存
        private TextureAnalysisResult CalculateTextureMemoryUsingThryLogic(VRCAvatarDescriptor avatar)
        {
            var result = new TextureAnalysisResult();
            
            if (avatar == null)
            {
                Debug.LogWarning("[纹理计算] Avatar为null");
                return result;
            }
            
            try
            {
                Debug.Log("[纹理计算] 开始使用Thry's Avatar Tools完整算法计算纹理显存");
                
                // 使用完全相同的材质获取逻辑
                var materials = GetMaterialsFromAvatarThryLogic(avatar.gameObject);
                var activeMaterials = materials[0]; // 激活材质
                var allMaterials = materials[1];     // 所有材质
                
                Debug.Log($"[纹理计算] 找到激活材质: {activeMaterials.Count()}, 总材质: {allMaterials.Count()}");
                
                // 使用与原始TextureVRAM.GetTextures完全相同的逻辑
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
                
                Debug.Log($"[纹理计算] 找到纹理总数: {textures.Count}");
                
                long totalMemoryBytes = 0;
                result.textureInfos = new List<TextureInfo>();
                
                // 计算每个纹理的大小
                foreach (var kvp in textures)
                {
                    var texture = kvp.Key;
                    var isActive = kvp.Value;
                    
                    // 使用完全相同的纹理大小计算逻辑
                    var textureSize = CalculateTextureSizeThryLogic(texture);
                    totalMemoryBytes += textureSize.sizeBytes;
                    
                    // 创建纹理信息
                    var textureInfo = new TextureInfo
                    {
                        name = texture.name,
                        width = texture.width,
                        height = texture.height,
                        format = textureSize.formatString,
                        mipmapCount = texture.mipmapCount,
                        sizeMB = textureSize.sizeMB
                    };
                    
                    result.textureInfos.Add(textureInfo);
                    
                    Debug.Log($"[纹理计算] 纹理 '{texture.name}': {textureSize.sizeMB:F2} MB ({textureSize.formatString}), 活跃: {isActive}");
                }
                
                // 按大小排序，如果大小相同则随机排列
                var random = new System.Random();
                result.textureInfos.Sort((t1, t2) => 
                {
                    int sizeComparison = t2.sizeMB.CompareTo(t1.sizeMB);
                    if (sizeComparison == 0)
                    {
                        // 大小相同时随机排列
                        return random.Next(-1, 2);
                    }
                    return sizeComparison;
                });
                
                result.totalMemoryMB = totalMemoryBytes / (1024f * 1024f);
                result.textureCount = textures.Count;
                
                Debug.Log($"[纹理计算] 完成！总纹理显存: {result.totalMemoryMB:F2} MB, 纹理数量: {result.textureCount}");
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[纹理计算] 计算纹理显存时出错: {e.Message}");
                return new TextureAnalysisResult();
            }
        }
        
        // 完全按照AvatarEvaluator.GetMaterials的逻辑 - 精确复制原始实现
        private IEnumerable<Material>[] GetMaterialsFromAvatarThryLogic(GameObject avatar)
        {
            var allBuiltRenderers = avatar.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.gameObject.GetComponentsInParent<Transform>(true)
                    .All(g => g.tag != "EditorOnly"));

            var materialsActive = allBuiltRenderers
                .Where(r => r.gameObject.activeInHierarchy)
                .SelectMany(r => r.sharedMaterials)
                .ToList();
            
            var materialsAll = allBuiltRenderers
                .SelectMany(r => r.sharedMaterials)
                .ToList();
            
            // 动画材质 - 使用与原始代码完全相同的逻辑
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            if (descriptor != null)
            {
                try
                {
                    var clips = descriptor.baseAnimationLayers
                        .Select(l => l.animatorController)
                        .Where(a => a != null)
                        .SelectMany(a => a.animationClips)  // 直接使用animationClips属性，无需转换
                        .Distinct();
                    
                    foreach (var clip in clips)
                    {
                        var clipMaterials = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                            .Where(b => b.isPPtrCurve && 
                                       b.type.IsSubclassOf(typeof(Renderer)) && 
                                       b.propertyName.StartsWith("m_Materials"))
                            .SelectMany(b => AnimationUtility.GetObjectReferenceCurve(clip, b))
                            .Select(r => r.value as Material);
                        materialsAll.AddRange(clipMaterials);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[纹理计算] 获取动画材质时出错: {e.Message}");
                }
            }
            
            return new IEnumerable<Material>[] { 
                materialsActive.Distinct(), 
                materialsAll.Distinct() 
            };
        }
        
        // 完全按照Thry's Avatar Tools的CalculateTextureSize方法
        private ThryTextureSizeInfo CalculateTextureSizeThryLogic(Texture texture)
        {
            var info = new ThryTextureSizeInfo
            {
                texture = texture,
                formatString = "Unknown"
            };
            
            if (texture is Texture2D tex2D)
            {
                TextureFormat format = tex2D.format;
                if (!ThryBPP.TryGetValue(format, out info.BPP))
                    info.BPP = 16;
                    
                info.formatString = format.ToString();
                info.format = format;
                info.sizeBytes = TextureToBytesUsingBPPThryLogic(texture, info.BPP);
                
                // 添加与原始代码相同的hasAlpha和minBPP处理
                string path = AssetDatabase.GetAssetPath(texture);
                if (texture != null && !string.IsNullOrWhiteSpace(path))
                {
                    AssetImporter assetImporter = AssetImporter.GetAtPath(path);
                    if (assetImporter is TextureImporter textureImporter)
                    {
                        info.hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();
                        info.minBPP = (info.hasAlpha || textureImporter.textureType == TextureImporterType.NormalMap) ? 8 : 4;
                    }
                }
            }
            else if (texture is Texture2DArray tex2DArray)
            {
                if (!ThryBPP.TryGetValue(tex2DArray.format, out info.BPP))
                    info.BPP = 16;
                info.formatString = tex2DArray.format.ToString();
                info.format = tex2DArray.format;
                info.sizeBytes = TextureToBytesUsingBPPThryLogic(texture, info.BPP) * tex2DArray.depth;
            }
            else if (texture is Cubemap cubemap)
            {
                if (!ThryBPP.TryGetValue(cubemap.format, out info.BPP))
                    info.BPP = 16;
                info.formatString = cubemap.format.ToString();
                info.format = cubemap.format;
                info.sizeBytes = TextureToBytesUsingBPPThryLogic(texture, info.BPP);
                // 完全按照原始代码：检查TextureDimension.Tex3D（虽然对Cubemap来说很奇怪）
                if (cubemap.dimension == TextureDimension.Tex3D)
                {
                    info.sizeBytes *= 6;
                }
            }
            else if (texture is RenderTexture renderTexture)
            {
                if (!ThryRT_BPP.TryGetValue(renderTexture.format, out info.BPP))
                    info.BPP = 16;
                info.BPP += renderTexture.depth;
                info.formatString = renderTexture.format.ToString();
                info.hasAlpha = renderTexture.format == RenderTextureFormat.ARGB32 || 
                               renderTexture.format == RenderTextureFormat.ARGBHalf || 
                               renderTexture.format == RenderTextureFormat.ARGBFloat;
                info.sizeBytes = TextureToBytesUsingBPPThryLogic(texture, info.BPP);
            }
            else
            {
                info.sizeBytes = Profiler.GetRuntimeMemorySizeLong(texture);
            }
            
            info.sizeMB = info.sizeBytes / (1024f * 1024f);
            return info;
        }
        
        // 完全按照Thry's Avatar Tools的TextureToBytesUsingBPP算法
        private long TextureToBytesUsingBPPThryLogic(Texture texture, float bpp, float resolutionScale = 1f)
        {
            int width = (int)(texture.width * resolutionScale);
            int height = (int)(texture.height * resolutionScale);
            long bytes = 0;
            
            if (texture is Texture2D || texture is Texture2DArray || texture is Cubemap)
            {
                // 使用与Thry's Avatar Tools完全相同的mipmap计算方法
                for (int index = 0; index < texture.mipmapCount; ++index)
                {
                    bytes += (long)Mathf.RoundToInt((float)((width * height) >> (2 * index)) * bpp / 8);
                }
            }
            else if (texture is RenderTexture renderTexture)
            {
                // 使用与Thry's Avatar Tools完全相同的RenderTexture计算方法
                double mipmaps = 1;
                for (int i = 0; i < renderTexture.mipmapCount; i++) 
                {
                    mipmaps += System.Math.Pow(0.25, i + 1);
                }
                bytes = (long)((ThryRT_BPP[renderTexture.format] + renderTexture.depth) * width * height * (renderTexture.useMipMap ? mipmaps : 1) / 8);
            }
            else
            {
                bytes = Profiler.GetRuntimeMemorySizeLong(texture);
            }
            
            return bytes;
        }
        
        // Thry's Avatar Tools的完整BPP字典
        private static readonly Dictionary<TextureFormat, float> ThryBPP = new Dictionary<TextureFormat, float>()
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
            { TextureFormat.BC6H, 8 },
            { TextureFormat.BC7, 8 },
            { TextureFormat.BC4, 4 },
            { TextureFormat.BC5, 8 },
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
            { TextureFormat.ASTC_6x6, 3.55f },
            { TextureFormat.ASTC_8x8, 2 },
            { TextureFormat.ASTC_10x10, 1.28f },
            { TextureFormat.ASTC_12x12, 1 },
            { TextureFormat.RG16, 16 },
            { TextureFormat.R8, 8 },
            { TextureFormat.ETC_RGB4Crunched, 4 },
            { TextureFormat.ETC2_RGBA8Crunched, 8 },
            { TextureFormat.ASTC_HDR_4x4, 8 },
            { TextureFormat.ASTC_HDR_5x5, 5.12f },
            { TextureFormat.ASTC_HDR_6x6, 3.55f },
            { TextureFormat.ASTC_HDR_8x8, 2 },
            { TextureFormat.ASTC_HDR_10x10, 1.28f },
            { TextureFormat.ASTC_HDR_12x12, 1 },
            { TextureFormat.RG32, 32 },
            { TextureFormat.RGB48, 48 },
            { TextureFormat.RGBA64, 64 }
        };
        
        // Thry's Avatar Tools的RenderTexture BPP字典
        private static readonly Dictionary<RenderTextureFormat, float> ThryRT_BPP = new Dictionary<RenderTextureFormat, float>()
        {
            { RenderTextureFormat.ARGB32, 32 },
            { RenderTextureFormat.Depth, 0 },
            { RenderTextureFormat.ARGBHalf, 64 },
            { RenderTextureFormat.Shadowmap, 8 },
            { RenderTextureFormat.RGB565, 16 },
            { RenderTextureFormat.ARGB4444, 16 },
            { RenderTextureFormat.ARGB1555, 16 },
            { RenderTextureFormat.Default, 32 },
            { RenderTextureFormat.ARGB2101010, 32 },
            { RenderTextureFormat.DefaultHDR, 128 },
            { RenderTextureFormat.ARGB64, 64 },
            { RenderTextureFormat.ARGBFloat, 128 },
            { RenderTextureFormat.RGFloat, 64 },
            { RenderTextureFormat.RGHalf, 32 },
            { RenderTextureFormat.RFloat, 32 },
            { RenderTextureFormat.RHalf, 16 },
            { RenderTextureFormat.R8, 8 },
            { RenderTextureFormat.ARGBInt, 128 },
            { RenderTextureFormat.RGInt, 64 },
            { RenderTextureFormat.RInt, 32 },
            { RenderTextureFormat.BGRA32, 32 },
            { RenderTextureFormat.RGB111110Float, 32 },
            { RenderTextureFormat.RG32, 32 },
            { RenderTextureFormat.RGBAUShort, 64 },
            { RenderTextureFormat.RG16, 16 },
            { RenderTextureFormat.BGRA10101010_XR, 40 },
            { RenderTextureFormat.BGR101010_XR, 30 },
            { RenderTextureFormat.R16, 16 }
        };
        
        // Thry纹理大小信息结构
        private struct ThryTextureSizeInfo
        {
            public Texture texture;
            public long sizeBytes;
            public float sizeMB;
            public float BPP;
            public int minBPP;
            public string formatString;
            public TextureFormat format;
            public bool hasAlpha;
        }

        // 完全按照画质压缩工具Combined (only active)逻辑计算模型上传大小
        private long CalculateCombinedOnlyActiveSizeUsingThryLogic(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return 0;

            try
            {
                Debug.Log("[Combined Only Active] 开始使用画质压缩工具完整算法计算Combined (only active)大小");

                long sizeActive = 0;

                EditorUtility.DisplayProgressBar("分析模型", "获取材质数据", 0.6f);

                // 第一部分：获取材质数据（完全按照TextureVRAM.Calc逻辑）
                var materials = GetMaterialsFromAvatarThryLogic(avatar.gameObject);
                var activeMaterials = materials[0];
                var allMaterials = materials[1];

                Debug.Log($"[Combined Only Active] 活动材质数量: {activeMaterials.Count()}, 总材质数量: {allMaterials.Count()}");

                EditorUtility.DisplayProgressBar("分析模型", "获取纹理数据", 0.7f);

                // 第二部分：计算纹理大小（完全按照TextureVRAM.GetTextures和Calc逻辑）
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

                Debug.Log($"[Combined Only Active] 纹理总数: {textures.Count}, 活动纹理数: {textures.Values.Count(v => v)}");

                // 计算活动纹理大小
                int numTextures = textures.Keys.Count;
                int texIdx = 1;
                foreach (KeyValuePair<Texture, bool> t in textures)
                {
                    EditorUtility.DisplayProgressBar("分析模型", $"计算纹理大小: {t.Key.name}", 0.7f + 0.1f * (texIdx / (float)numTextures));
                    if (t.Value) // 只计算活动纹理
                    {
                        // 使用画质压缩工具的纹理大小计算方法
                        long textureSize = CalculateTextureSizeUsingThryMethod(t.Key);
                        sizeActive += textureSize;
                    }
                    texIdx++;
                }

                Debug.Log($"[Combined Only Active] 活动纹理大小: {sizeActive / (1024f * 1024f):F2} MiB");

                EditorUtility.DisplayProgressBar("分析模型", "获取网格数据", 0.8f);

                // 第三部分：计算网格大小（完全按照TextureVRAM.Calc逻辑）
                Dictionary<Mesh, bool> meshes = new Dictionary<Mesh, bool>();
                var allMeshes = avatar.gameObject.GetComponentsInChildren<Renderer>(true)
                    .Select(r => r is SkinnedMeshRenderer ? (r as SkinnedMeshRenderer).sharedMesh :
                           r is MeshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null)
                    .Where(m => m != null);

                var activeMeshes = avatar.gameObject.GetComponentsInChildren<Renderer>(false)
                    .Select(r => r is SkinnedMeshRenderer ? (r as SkinnedMeshRenderer).sharedMesh :
                           r is MeshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null)
                    .Where(m => m != null);

                foreach (Mesh m in allMeshes)
                {
                    if (m == null) continue;
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

                Debug.Log($"[Combined Only Active] 网格总数: {meshes.Count}, 活动网格数: {meshes.Values.Count(v => v)}");

                // 计算活动网格大小
                int numMeshes = meshes.Keys.Count;
                int meshIdx = 1;
                long activeMeshSize = 0;
                foreach (KeyValuePair<Mesh, bool> m in meshes)
                {
                    EditorUtility.DisplayProgressBar("分析模型", $"计算网格大小: {m.Key.name}", 0.8f + 0.1f * (meshIdx / (float)numMeshes));
                    if (m.Value) // 只计算活动网格
                    {
                        long meshSize = CalculateMeshSizeUsingThryMethod(m.Key);
                        sizeActive += meshSize;
                        activeMeshSize += meshSize;
                    }
                    meshIdx++;
                }

                Debug.Log($"[Combined Only Active] 活动网格大小: {activeMeshSize / (1024f * 1024f):F2} MiB");
                Debug.Log($"[Combined Only Active] Combined (only active)总大小: {sizeActive / (1024f * 1024f):F2} MiB");

                return sizeActive;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Combined Only Active] 计算Combined (only active)大小时出错: {e.Message}");
                return 0;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // 完全按照画质压缩工具Combined (all)逻辑计算模型总大小
        private long CalculateCombinedAllSizeUsingThryLogic(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return 0;
            
            try
            {
                Debug.Log("[Combined All] 开始使用画质压缩工具完整算法计算Combined (all)大小");
                
                long sizeAllTextures = 0;
                long sizeAllMeshes = 0;
                
                // 第一部分：计算所有纹理大小（完全按照TextureVRAM.Calc逻辑）
                var materials = GetMaterialsFromAvatarThryLogic(avatar.gameObject);
                var activeMaterials = materials[0];
                var allMaterials = materials[1];
                
                // 获取所有纹理
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
                
                // 计算所有纹理大小
                foreach (KeyValuePair<Texture, bool> t in textures)
                {
                    var textureSize = CalculateTextureSizeUsingThryMethod(t.Key);
                    sizeAllTextures += textureSize;
                }
                
                Debug.Log($"[Combined All] 所有纹理大小: {sizeAllTextures / (1024f * 1024f):F2} MiB");
                
                // 第二部分：计算所有网格大小（完全按照TextureVRAM.Calc逻辑）
                Dictionary<Mesh, bool> meshes = new Dictionary<Mesh, bool>();
                var allMeshes = avatar.gameObject.GetComponentsInChildren<Renderer>(true)
                    .Select(r => r is SkinnedMeshRenderer ? (r as SkinnedMeshRenderer).sharedMesh : 
                           r is MeshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null)
                    .Where(m => m != null);
                    
                var activeMeshes = avatar.gameObject.GetComponentsInChildren<Renderer>(false)
                    .Select(r => r is SkinnedMeshRenderer ? (r as SkinnedMeshRenderer).sharedMesh : 
                           r is MeshRenderer ? r.GetComponent<MeshFilter>()?.sharedMesh : null)
                    .Where(m => m != null);
                
                foreach (Mesh m in allMeshes)
                {
                    if (m == null) continue;
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
                
                // 计算所有网格大小
                foreach (KeyValuePair<Mesh, bool> m in meshes)
                {
                    long meshSize = CalculateMeshSizeUsingThryMethod(m.Key);
                    sizeAllMeshes += meshSize;
                }
                
                Debug.Log($"[Combined All] 所有网格大小: {sizeAllMeshes / (1024f * 1024f):F2} MiB");
                
                // Combined (all) = 所有纹理 + 所有网格
                long sizeAll = sizeAllTextures + sizeAllMeshes;
                
                Debug.Log($"[Combined All] Combined (all)总大小: {sizeAll / (1024f * 1024f):F2} MiB");
                
                return sizeAll;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Combined All] 计算Combined (all)大小时出错: {e.Message}");
                return 0;
            }
        }
        
        // 使用画质压缩工具的纹理大小计算方法（完整迁移TextureVRAM.CalculateTextureSize逻辑）
        private long CalculateTextureSizeUsingThryMethod(Texture texture)
        {
            if (texture == null) return 0;

            try
            {
                // 纹理格式对应的BPP（每像素位数）映射表 - 从画质压缩工具完全迁移
                var BPP = new Dictionary<TextureFormat, float>
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
                    { TextureFormat.ETC2_RGB, 4 },
                    { TextureFormat.ETC2_RGBA1, 4 },
                    { TextureFormat.ETC2_RGBA8, 8 },
                    { TextureFormat.ASTC_4x4, 8 },
                    { TextureFormat.ASTC_5x5, 5 },
                    { TextureFormat.ASTC_6x6, 4 },
                    { TextureFormat.ASTC_8x8, 2 },
                    { TextureFormat.ASTC_10x10, 1 },
                    { TextureFormat.ASTC_12x12, 1 }
                };

                // RenderTexture格式对应的BPP映射表 - 从画质压缩工具完全迁移
                var RT_BPP = new Dictionary<RenderTextureFormat, float>
                {
                    { RenderTextureFormat.ARGB32, 32 },
                    { RenderTextureFormat.Depth, 16 },
                    { RenderTextureFormat.ARGBHalf, 64 },
                    { RenderTextureFormat.Shadowmap, 16 },
                    { RenderTextureFormat.RGB565, 16 },
                    { RenderTextureFormat.ARGB4444, 16 },
                    { RenderTextureFormat.ARGB1555, 16 },
                    { RenderTextureFormat.Default, 32 },
                    { RenderTextureFormat.ARGB2101010, 32 },
                    { RenderTextureFormat.DefaultHDR, 64 },
                    { RenderTextureFormat.ARGB64, 64 },
                    { RenderTextureFormat.ARGBFloat, 128 },
                    { RenderTextureFormat.RGFloat, 64 },
                    { RenderTextureFormat.RGHalf, 32 },
                    { RenderTextureFormat.RFloat, 32 },
                    { RenderTextureFormat.RHalf, 16 },
                    { RenderTextureFormat.R8, 8 },
                    { RenderTextureFormat.ARGBInt, 128 },
                    { RenderTextureFormat.RGInt, 64 },
                    { RenderTextureFormat.RInt, 32 },
                    { RenderTextureFormat.BGRA32, 32 },
                    { RenderTextureFormat.RGB111110Float, 32 },
                    { RenderTextureFormat.RG32, 32 },
                    { RenderTextureFormat.RGBAUShort, 64 },
                    { RenderTextureFormat.RG16, 16 },
                    { RenderTextureFormat.BGRA10101010_XR, 32 },
                    { RenderTextureFormat.BGR101010_XR, 32 },
                    { RenderTextureFormat.R16, 16 }
                };

                float bpp = 16; // 默认值
                long size = 0;

                if (texture is Texture2D texture2D)
                {
                    TextureFormat format = texture2D.format;
                    if (!BPP.TryGetValue(format, out bpp))
                        bpp = 16;

                    size = TextureToBytesUsingBPPExact(texture, bpp);
                }
                else if (texture is Texture2DArray texture2DArray)
                {
                    if (!BPP.TryGetValue(texture2DArray.format, out bpp))
                        bpp = 16;

                    size = TextureToBytesUsingBPPExact(texture, bpp) * texture2DArray.depth;
                }
                else if (texture is Cubemap cubemap)
                {
                    if (!BPP.TryGetValue(cubemap.format, out bpp))
                        bpp = 16;

                    size = TextureToBytesUsingBPPExact(texture, bpp);
                    if (cubemap.dimension == TextureDimension.Tex3D)
                    {
                        size *= 6;
                    }
                }
                else if (texture is RenderTexture renderTexture)
                {
                    if (!RT_BPP.TryGetValue(renderTexture.format, out bpp))
                        bpp = 16;

                    // 完全按照原始代码：BPP + depth
                    bpp += renderTexture.depth;
                    size = TextureToBytesUsingBPPExactForRenderTexture(renderTexture, bpp, RT_BPP);
                }
                else
                {
                    // 其他类型纹理，使用Profiler.GetRuntimeMemorySizeLong（与原工具一致）
                    size = Profiler.GetRuntimeMemorySizeLong(texture);
                }

                return size;
            }
            catch (Exception e)
            {
                Debug.LogError($"[纹理大小计算] 计算纹理 {texture.name} 大小时出错: {e.Message}");
                return 0;
            }
        }

        // 纹理大小计算辅助方法（完全按照画质压缩工具的TextureToBytesUsingBPP方法）
        private long TextureToBytesUsingBPPExact(Texture texture, float bpp, float resolutionScale = 1f)
        {
            int width = (int)(texture.width * resolutionScale);
            int height = (int)(texture.height * resolutionScale);
            long bytes = 0;

            if (texture is Texture2D || texture is Texture2DArray || texture is Cubemap)
            {
                // 完全按照原始代码的精确mipmap计算
                for (int index = 0; index < texture.mipmapCount; ++index)
                {
                    bytes += (long)Mathf.RoundToInt((float)((width * height) >> (2 * index)) * bpp / 8);
                }
            }
            else
            {
                bytes = Profiler.GetRuntimeMemorySizeLong(texture);
            }

            return bytes;
        }

        // RenderTexture专用计算方法（完全按照画质压缩工具逻辑）
        private long TextureToBytesUsingBPPExactForRenderTexture(RenderTexture renderTexture, float bpp, Dictionary<RenderTextureFormat, float> RT_BPP)
        {
            int width = renderTexture.width;
            int height = renderTexture.height;
            long bytes = 0;

            // 完全按照原始代码的RenderTexture计算
            double mipmaps = 1;
            for (int i = 0; i < renderTexture.mipmapCount; i++)
            {
                mipmaps += System.Math.Pow(0.25, i + 1);
            }
            bytes = (long)((RT_BPP[renderTexture.format] + renderTexture.depth) * width * height * (renderTexture.useMipMap ? mipmaps : 1) / 8);

            return bytes;
        }
        
        // 使用画质压缩工具的网格大小计算方法
        private long CalculateMeshSizeUsingThryMethod(Mesh mesh)
        {
            // 使用与画质压缩工具完全相同的网格计算方法
            return CalculateMeshSizeFromThryVRAM(mesh);
        }
        
        // 完全按照画质压缩工具TextureVRAM.CalculateMeshSize方法
        private long CalculateMeshSizeFromThryVRAM(Mesh mesh)
        {
            long bytes = 0;

            var vertexAttributes = mesh.GetVertexAttributes();
            long vertexAttributeVRAMSize = 0;
            foreach (var vertexAttribute in vertexAttributes)
            {
                int skinnedMeshPositionNormalTangentMultiplier = 1;
                // skinned meshes have 2x the amount of position, normal and tangent data since they store both the un-skinned and skinned data in VRAM
                if (mesh.HasVertexAttribute(VertexAttribute.BlendIndices) && mesh.HasVertexAttribute(VertexAttribute.BlendWeight) &&
                    (vertexAttribute.attribute == VertexAttribute.Position || vertexAttribute.attribute == VertexAttribute.Normal || vertexAttribute.attribute == VertexAttribute.Tangent))
                    skinnedMeshPositionNormalTangentMultiplier = 2;
                vertexAttributeVRAMSize += VertexAttributeByteSize[vertexAttribute.format] * vertexAttribute.dimension * skinnedMeshPositionNormalTangentMultiplier;
            }
            var deltaPositions = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            long blendShapeVRAMSize = 0;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                var blendShapeFrameCount = mesh.GetBlendShapeFrameCount(i);
                for (int j = 0; j < blendShapeFrameCount; j++)
                {
                    mesh.GetBlendShapeFrameVertices(i, j, deltaPositions, deltaNormals, deltaTangents);
                    for (int k = 0; k < deltaPositions.Length; k++)
                    {
                        if (deltaPositions[k] != Vector3.zero || deltaNormals[k] != Vector3.zero || deltaTangents[k] != Vector3.zero)
                        {
                            // every affected vertex has 1 uint for the index, 3 floats for the position, 3 floats for the normal and 3 floats for the tangent
                            // this is true even if all normals or tangents in all blend shapes are zero
                            blendShapeVRAMSize += 40;
                        }
                    }
                }
            }
            bytes = vertexAttributeVRAMSize * mesh.vertexCount + blendShapeVRAMSize;
            return bytes;
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
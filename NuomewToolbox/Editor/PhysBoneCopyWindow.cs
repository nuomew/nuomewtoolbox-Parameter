using UnityEngine;
using UnityEditor;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Collections.Generic;
using System.Linq;

namespace NyameauToolbox.Editor
{
    public class PhysBoneCopyWindow : EditorWindow
    {
        private GameObject sourceObject;
        private GameObject targetObject;
        private Vector2 scrollPosition;                  // 预览区域滚动位置
        private Vector2 mainScrollPosition;              // 主界面滚动位置
        private bool showAdvancedOptions = false;
        private bool copyPhysBones = true;
        private bool copyPhysBoneColliders = true;
        private bool overwriteExisting = false;
        private bool createMissingPaths = true; // 自动创建缺失路径
        private bool copyTransformData = true; // 是否复制Transform数据（位置、旋转、缩放）
        private bool copyMaterialData = false; // 是否复制材质数据（Skinned Mesh Renderer的材质球和着色器）
        
        // 样式
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle warningStyle;
        private GUIStyle successStyle;
        
        // 颜色
        private readonly Color primaryColor = new Color(0.4f, 0.6f, 1f, 1f);
        private readonly Color successColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        private readonly Color warningColor = new Color(1f, 0.6f, 0.2f, 1f);
        private readonly Color dangerColor = new Color(1f, 0.3f, 0.3f, 1f);
        
        [MenuItem("诺喵工具箱/物品同步器", false, 14)]
        public static void ShowWindow()
        {
            var window = GetWindow<PhysBoneCopyWindow>("物品同步器");
            window.minSize = new Vector2(400, 600);  // 设置最小窗口尺寸
            window.maxSize = new Vector2(600, 1200); // 设置最大窗口尺寸
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
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            
            buttonStyle = new GUIStyle("Button")
            {
                fontSize = 12,
                fixedHeight = 30,
                alignment = TextAnchor.MiddleCenter
            };
            
            warningStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = warningColor }
            };
            
            successStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = successColor }
            };
        }
        
        private void OnGUI()
        {
            mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
            
            DrawHeader();
            DrawObjectSelection();
            DrawOptions();
            DrawPreview();
            DrawCopyButton();
            DrawFooter();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            GUI.backgroundColor = primaryColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("🔄 物品同步器", headerStyle);
            GUILayout.Label("将物品A的数据同步到物品B上，包括动骨材质数据", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(15);
        }
        
        private void DrawObjectSelection()
        {
            EditorGUILayout.LabelField("📂 选择对象", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 源对象选择
            EditorGUILayout.LabelField("源对象 (复制来源):");
            sourceObject = (GameObject)EditorGUILayout.ObjectField(sourceObject, typeof(GameObject), true);
            
            if (sourceObject != null)
            {
                var sourcePhysBones = GetPhysBonesInObject(sourceObject);
                var sourceColliders = GetPhysBoneCollidersInObject(sourceObject);
                EditorGUILayout.LabelField($"检测到: {sourcePhysBones.Count} 个PhysBone, {sourceColliders.Count} 个PhysBoneCollider", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.Space(10);
            
            // 目标对象选择
            EditorGUILayout.LabelField("目标对象 (复制目标):");
            targetObject = (GameObject)EditorGUILayout.ObjectField(targetObject, typeof(GameObject), true);
            
            if (targetObject != null)
            {
                var targetPhysBones = GetPhysBonesInObject(targetObject);
                var targetColliders = GetPhysBoneCollidersInObject(targetObject);
                EditorGUILayout.LabelField($"当前有: {targetPhysBones.Count} 个PhysBone, {targetColliders.Count} 个PhysBoneCollider", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawOptions()
        {
            EditorGUILayout.LabelField("⚙️ 复制选项", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            copyPhysBones = EditorGUILayout.Toggle(copyPhysBones, GUILayout.Width(20));
            GUILayout.Space(10); // 增加间距
            EditorGUILayout.LabelField("复制 PhysBone 组件");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            copyPhysBoneColliders = EditorGUILayout.Toggle(copyPhysBoneColliders, GUILayout.Width(20));
            GUILayout.Space(10); // 增加间距
            EditorGUILayout.LabelField("复制 PhysBoneCollider 组件");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            copyMaterialData = EditorGUILayout.Toggle(copyMaterialData, GUILayout.Width(20));
            GUILayout.Space(10); // 增加间距
            EditorGUILayout.LabelField("复制 Skinned Mesh Renderer 材质");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "高级选项");
            if (showAdvancedOptions)
            {
                EditorGUILayout.BeginHorizontal();
                overwriteExisting = EditorGUILayout.Toggle(overwriteExisting, GUILayout.Width(20));
                GUILayout.Space(10); // 增加间距
                EditorGUILayout.LabelField("覆盖已存在的组件");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                createMissingPaths = EditorGUILayout.Toggle(createMissingPaths, GUILayout.Width(20));
                GUILayout.Space(10); // 增加间距
                EditorGUILayout.LabelField("自动创建缺失路径");
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                copyTransformData = EditorGUILayout.Toggle(copyTransformData, GUILayout.Width(20));
                GUILayout.Space(10); // 增加间距
                EditorGUILayout.LabelField("复制Transform数据");
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        
        private void DrawPreview()
        {
            if (sourceObject == null || targetObject == null) return;
            
            EditorGUILayout.LabelField("📋 预览", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox, GUILayout.Height(200));
            
            var matchingPaths = GetMatchingPaths();
            
            if (matchingPaths.Count == 0)
            {
                EditorGUILayout.LabelField("❌ 未找到匹配的路径", warningStyle);
                EditorGUILayout.LabelField("请确保两个对象具有相同的子对象结构", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"✅ 找到 {matchingPaths.Count} 个匹配的路径:", successStyle);
                EditorGUILayout.Space(5);
                
                foreach (var path in matchingPaths)
                {
                    EditorGUILayout.LabelField($"• {path}", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);
        }
        
        private void DrawCopyButton()
        {
            EditorGUI.BeginDisabledGroup(sourceObject == null || targetObject == null || (!copyPhysBones && !copyPhysBoneColliders && !copyMaterialData));
            
            GUI.backgroundColor = successColor;
            if (GUILayout.Button("🚀 开始复制", buttonStyle, GUILayout.Height(40)))
            {
                PerformCopy();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUI.EndDisabledGroup();
            
            if (sourceObject == null || targetObject == null)
            {
                EditorGUILayout.LabelField("请先选择源对象和目标对象", warningStyle);
            }
            else if (!copyPhysBones && !copyPhysBoneColliders && !copyMaterialData)
            {
                EditorGUILayout.LabelField("请至少选择一种组件类型进行复制", warningStyle);
            }
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            
            GUI.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("💡 使用说明:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. 选择包含动骨组件的源对象", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("2. 选择要复制到的目标对象", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("3. 工具会自动匹配相同路径的子对象", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("4. 点击开始复制按钮执行操作", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }
        
        private List<VRCPhysBone> GetPhysBonesInObject(GameObject obj)
        {
            if (obj == null) return new List<VRCPhysBone>();
            return obj.GetComponentsInChildren<VRCPhysBone>(true).ToList();
        }
        
        private List<VRCPhysBoneCollider> GetPhysBoneCollidersInObject(GameObject obj)
        {
            if (obj == null) return new List<VRCPhysBoneCollider>();
            return obj.GetComponentsInChildren<VRCPhysBoneCollider>(true).ToList();
        }
        
        private List<SkinnedMeshRenderer> GetSkinnedMeshRenderersInObject(GameObject obj)
        {
            if (obj == null) return new List<SkinnedMeshRenderer>();
            return obj.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();
        }
        
        private List<string> GetMatchingPaths()
        {
            var matchingPaths = new List<string>();
            
            if (sourceObject == null || targetObject == null) return matchingPaths;
            
            var sourcePhysBones = GetPhysBonesInObject(sourceObject);
            var sourceColliders = GetPhysBoneCollidersInObject(sourceObject);
            var sourceRenderers = GetSkinnedMeshRenderersInObject(sourceObject);
            
            // 收集所有有组件的路径
            var sourcePaths = new HashSet<string>();
            
            if (copyPhysBones)
            {
                foreach (var pb in sourcePhysBones)
                {
                    var path = GetRelativePath(sourceObject.transform, pb.transform);
                    sourcePaths.Add(path);
                }
            }
            
            if (copyPhysBoneColliders)
            {
                foreach (var pbc in sourceColliders)
                {
                    var path = GetRelativePath(sourceObject.transform, pbc.transform);
                    sourcePaths.Add(path);
                }
            }
            
            if (copyMaterialData)
            {
                foreach (var renderer in sourceRenderers)
                {
                    var path = GetRelativePath(sourceObject.transform, renderer.transform);
                    sourcePaths.Add(path);
                }
            }
            
            // 检查目标对象中是否存在相同路径
            foreach (var path in sourcePaths)
            {
                var targetTransform = FindChildByPath(targetObject.transform, path);
                if (targetTransform != null)
                {
                    matchingPaths.Add(path);
                }
            }
            
            return matchingPaths.OrderBy(p => p).ToList();
        }
        
        private string GetRelativePath(Transform root, Transform target)
        {
            if (target == root) return "";
            
            var path = target.name;
            var current = target.parent;
            
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
        
        private Transform FindChildByPath(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return root;
            
            var parts = path.Split('/');
            var current = root;
            
            foreach (var part in parts)
            {
                var found = false;
                for (int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    if (child.name == part)
                    {
                        current = child;
                        found = true;
                        break;
                    }
                }
                
                if (!found) return null;
            }
            
            return current;
        }
        
        // 创建缺失的路径，并复制对应源对象的Transform数据
        private Transform CreateOrFindChildByPath(Transform root, string path, Transform sourceRoot = null)
        {
            if (string.IsNullOrEmpty(path)) return root;
            
            var parts = path.Split('/');
            var current = root;
            var currentSourcePath = "";
            
            foreach (var part in parts)
            {
                var found = false;
                for (int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);
                    if (child.name == part)
                    {
                        current = child;
                        found = true;
                        break;
                    }
                }
                
                // 如果没找到，创建新的GameObject
                if (!found)
                {
                    var newGameObject = new GameObject(part);
                    Undo.RegisterCreatedObjectUndo(newGameObject, "创建缺失路径");
                    newGameObject.transform.SetParent(current);
                    
                    // 如果提供了源根节点且启用了Transform复制，则复制对应源对象的Transform数据
                    if (sourceRoot != null && copyTransformData)
                    {
                        currentSourcePath += (string.IsNullOrEmpty(currentSourcePath) ? "" : "/") + part;
                        var sourceTransform = FindChildByPath(sourceRoot, currentSourcePath);
                        if (sourceTransform != null)
                        {
                            newGameObject.transform.localPosition = sourceTransform.localPosition;
                            newGameObject.transform.localRotation = sourceTransform.localRotation;
                            newGameObject.transform.localScale = sourceTransform.localScale;
                        }
                        else
                        {
                            // 如果找不到对应的源对象，使用默认值
                            newGameObject.transform.localPosition = Vector3.zero;
                            newGameObject.transform.localRotation = Quaternion.identity;
                            newGameObject.transform.localScale = Vector3.one;
                        }
                    }
                    else
                    {
                        // 使用默认值
                        newGameObject.transform.localPosition = Vector3.zero;
                        newGameObject.transform.localRotation = Quaternion.identity;
                        newGameObject.transform.localScale = Vector3.one;
                    }
                    
                    current = newGameObject.transform;
                }
                else
                {
                    currentSourcePath += (string.IsNullOrEmpty(currentSourcePath) ? "" : "/") + part;
                }
            }
            
            return current;
        }
        
        private void PerformCopy()
        {
            if (sourceObject == null || targetObject == null) return;
            
            int copiedCount = 0;
            int skippedCount = 0;
            int createdPathsCount = 0;
            
            Undo.SetCurrentGroupName("复制动骨组件");
            int undoGroup = Undo.GetCurrentGroup();
            
            try
            {
                if (copyPhysBones)
                {
                    var sourcePhysBones = GetPhysBonesInObject(sourceObject);
                    foreach (var sourcePB in sourcePhysBones)
                    {
                        var relativePath = GetRelativePath(sourceObject.transform, sourcePB.transform);
                        Transform targetTransform;
                        
                        if (createMissingPaths)
                        {
                            var existingTransform = FindChildByPath(targetObject.transform, relativePath);
                            if (existingTransform == null)
                            {
                                createdPathsCount++;
                            }
                            targetTransform = CreateOrFindChildByPath(targetObject.transform, relativePath, sourceObject.transform);
                        }
                        else
                        {
                            targetTransform = FindChildByPath(targetObject.transform, relativePath);
                        }
                        
                        if (targetTransform != null)
                        {
                            var existingPB = targetTransform.GetComponent<VRCPhysBone>();
                            if (existingPB != null && !overwriteExisting)
                            {
                                skippedCount++;
                                continue;
                            }
                            
                            if (existingPB != null)
                            {
                                Undo.DestroyObjectImmediate(existingPB);
                            }
                            
                            var newPB = Undo.AddComponent<VRCPhysBone>(targetTransform.gameObject);
                            EditorUtility.CopySerialized(sourcePB, newPB);
                            
                            // 复制Transform数据（位置、旋转、缩放）
                            if (copyTransformData)
                            {
                                Undo.RecordObject(targetTransform, "复制Transform数据");
                                targetTransform.localPosition = sourcePB.transform.localPosition;
                                targetTransform.localRotation = sourcePB.transform.localRotation;
                                targetTransform.localScale = sourcePB.transform.localScale;
                            }
                            
                            copiedCount++;
                        }
                    }
                }
                
                if (copyPhysBoneColliders)
                {
                    var sourceColliders = GetPhysBoneCollidersInObject(sourceObject);
                    foreach (var sourcePBC in sourceColliders)
                    {
                        var relativePath = GetRelativePath(sourceObject.transform, sourcePBC.transform);
                        Transform targetTransform;
                        
                        if (createMissingPaths)
                        {
                            var existingTransform = FindChildByPath(targetObject.transform, relativePath);
                            if (existingTransform == null)
                            {
                                createdPathsCount++;
                            }
                            targetTransform = CreateOrFindChildByPath(targetObject.transform, relativePath, sourceObject.transform);
                        }
                        else
                        {
                            targetTransform = FindChildByPath(targetObject.transform, relativePath);
                        }
                        
                        if (targetTransform != null)
                        {
                            var existingPBC = targetTransform.GetComponent<VRCPhysBoneCollider>();
                            if (existingPBC != null && !overwriteExisting)
                            {
                                skippedCount++;
                                continue;
                            }
                            
                            if (existingPBC != null)
                            {
                                Undo.DestroyObjectImmediate(existingPBC);
                            }
                            
                            var newPBC = Undo.AddComponent<VRCPhysBoneCollider>(targetTransform.gameObject);
                            EditorUtility.CopySerialized(sourcePBC, newPBC);
                            
                            // 复制Transform数据（位置、旋转、缩放）
                            if (copyTransformData)
                            {
                                Undo.RecordObject(targetTransform, "复制Transform数据");
                                targetTransform.localPosition = sourcePBC.transform.localPosition;
                                targetTransform.localRotation = sourcePBC.transform.localRotation;
                                targetTransform.localScale = sourcePBC.transform.localScale;
                            }
                            
                            copiedCount++;
                        }
                    }
                }
                
                // 复制材质数据（Skinned Mesh Renderer）
                if (copyMaterialData)
                {
                    var sourceRenderers = GetSkinnedMeshRenderersInObject(sourceObject);
                    foreach (var sourceRenderer in sourceRenderers)
                    {
                        var relativePath = GetRelativePath(sourceObject.transform, sourceRenderer.transform);
                        Transform targetTransform;
                        
                        if (createMissingPaths)
                        {
                            var existingTransform = FindChildByPath(targetObject.transform, relativePath);
                            if (existingTransform == null)
                            {
                                createdPathsCount++;
                            }
                            targetTransform = CreateOrFindChildByPath(targetObject.transform, relativePath, sourceObject.transform);
                        }
                        else
                        {
                            targetTransform = FindChildByPath(targetObject.transform, relativePath);
                        }
                        
                        if (targetTransform != null)
                        {
                            var targetRenderer = targetTransform.GetComponent<SkinnedMeshRenderer>();
                            if (targetRenderer != null)
                            {
                                Undo.RecordObject(targetRenderer, "复制材质数据");
                                
                                // 复制材质数组
                                if (sourceRenderer.materials != null && sourceRenderer.materials.Length > 0)
                                {
                                    targetRenderer.materials = sourceRenderer.materials;
                                }
                                
                                // 复制其他渲染相关属性
                                targetRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                                targetRenderer.receiveShadows = sourceRenderer.receiveShadows;
                                targetRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                                targetRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
                                
                                copiedCount++;
                            }
                            else if (createMissingPaths && sourceRenderer != null)
                            {
                                // 如果目标没有SkinnedMeshRenderer但源有，则创建一个新的
                                var newRenderer = Undo.AddComponent<SkinnedMeshRenderer>(targetTransform.gameObject);
                                
                                // 只复制材质和渲染属性，不复制网格数据
                                newRenderer.materials = sourceRenderer.materials;
                                newRenderer.quality = sourceRenderer.quality;
                                newRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;
                                newRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
                                newRenderer.receiveShadows = sourceRenderer.receiveShadows;
                                newRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
                                newRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
                                
                                copiedCount++;
                            }
                        }
                    }
                }
                
                Undo.CollapseUndoOperations(undoGroup);
                
                string message = $"复制完成！\n成功复制: {copiedCount} 个组件";
                if (createdPathsCount > 0)
                {
                    message += $"\n创建路径: {createdPathsCount} 个";
                }
                if (skippedCount > 0)
                {
                    message += $"\n跳过: {skippedCount} 个组件 (已存在且未选择覆盖)";
                }
                
                EditorUtility.DisplayDialog("复制完成", message, "确定");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"复制动骨组件时发生错误: {e.Message}");
                EditorUtility.DisplayDialog("复制失败", $"复制过程中发生错误:\n{e.Message}", "确定");
            }
        }
    }
}
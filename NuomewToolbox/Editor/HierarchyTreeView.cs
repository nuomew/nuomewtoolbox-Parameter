/*
 * 层级窗口树状结构显示增强器
 * 功能：在Unity层级窗口中添加树状连接线和折叠图标
 * 作用：提供更清晰的层级结构可视化，方便观察文件层次关系
 * 性能优化：最高刷新5帧，避免过度绘制影响编辑器性能
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace NyameauToolbox.Editor
{
    [InitializeOnLoad]
    public static class HierarchyTreeView
    {
        // 功能开关状态
        private static bool isEnabled = true;
        
        // 性能优化：帧计数器
        private static int frameCounter = 0;
        private const int MAX_REFRESH_FRAMES = 5;
        
        // 缓存数据
        private static Dictionary<int, HierarchyItemData> hierarchyCache = new Dictionary<int, HierarchyItemData>();
        private static bool needsRefresh = true;
        
        // 样式配置
        private static readonly Color lineColor = new Color(1.0f, 0.4f, 0.7f, 0.8f); // 粉红色
        private const float lineWidth = 1f;
        private const float indentWidth = 14f;
        
        static HierarchyTreeView()
        {
            // 订阅层级窗口绘制事件
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            
            // 加载设置
            LoadSettings();
        }
        
        /// <summary>
        /// 层级窗口GUI绘制回调
        /// </summary>
        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (!isEnabled) return;
            
            // 性能优化：限制刷新频率
            frameCounter++;
            if (frameCounter > MAX_REFRESH_FRAMES)
            {
                frameCounter = 0;
                if (needsRefresh)
                {
                    RefreshHierarchyCache();
                    needsRefresh = false;
                }
            }
            
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null) return;
            
            DrawTreeStructure(gameObject, selectionRect);
        }
        
        /// <summary>
        /// 层级结构改变时的回调
        /// </summary>
        private static void OnHierarchyChanged()
        {
            needsRefresh = true;
            hierarchyCache.Clear();
        }
        
        /// <summary>
        /// 绘制树状结构
        /// </summary>
        private static void DrawTreeStructure(GameObject gameObject, Rect rect)
        {
            Transform transform = gameObject.transform;
            
            // 计算层级深度
            int depth = GetDepth(transform);
            if (depth == 0) return; // 根对象不绘制连接线
            
            // 获取或创建层级数据
            HierarchyItemData itemData = GetOrCreateItemData(gameObject.GetInstanceID(), transform);
            
            // 只绘制连接线，不绘制折叠图标（避免与Unity默认图标重叠）
            DrawConnectionLines(rect, itemData, depth);
        }
        
        /// <summary>
        /// 绘制连接线
        /// </summary>
        private static void DrawConnectionLines(Rect rect, HierarchyItemData itemData, int depth)
        {
            Handles.BeginGUI();
            Handles.color = lineColor;
            
            float centerY = rect.y + rect.height * 0.5f;
            
            // 只为叶子节点（没有子对象的节点）绘制连接线
            if (itemData.hasParent && !itemData.hasChildren)
            {
                // 计算直接父级的位置（只向左偏移一级）
                float parentX = rect.x - indentWidth;
                
                // 绘制水平连接线（只从直接父级到当前项）
                Vector3 horizontalStart = new Vector3(parentX + indentWidth * 0.5f, centerY, 0);
                Vector3 horizontalEnd = new Vector3(rect.x - 2, centerY, 0);
                Handles.DrawLine(horizontalStart, horizontalEnd);
                
                // 绘制垂直连接线（使用直接父级的位置）
                float verticalX = parentX + indentWidth * 0.5f;
                
                // 向上的连接线
                Vector3 verticalStart = new Vector3(verticalX, rect.y, 0);
                Vector3 verticalEnd = new Vector3(verticalX, centerY, 0);
                Handles.DrawLine(verticalStart, verticalEnd);
                
                // 向下的连接线（如果有下一个兄弟节点）
                if (itemData.hasNextSibling)
                {
                    Vector3 downStart = new Vector3(verticalX, centerY, 0);
                    Vector3 downEnd = new Vector3(verticalX, rect.y + rect.height, 0);
                    Handles.DrawLine(downStart, downEnd);
                }
            }
            
            // 不再需要绘制父级的垂直延续线，因为我们只关注直接父级
            // DrawParentLines(rect, itemData, depth);
            
            Handles.EndGUI();
        }
        
        /// <summary>
        /// 绘制父级的垂直延续线（只连接直接父级）
        /// </summary>
        private static void DrawParentLines(Rect rect, HierarchyItemData itemData, int depth)
        {
            // 只处理直接父级，不遍历更高层级
            Transform directParent = itemData.transform.parent;
            
            if (directParent != null && depth > 1)
            {
                // 检查直接父级是否还有下一个兄弟节点，且父级没有子对象（避免在折叠图标位置绘制）
                if (HasNextSibling(directParent) && directParent.childCount == 0)
                {
                    float lineX = rect.x - ((depth - 1) * indentWidth) + indentWidth * 0.5f;
                    Vector3 lineStart = new Vector3(lineX, rect.y, 0);
                    Vector3 lineEnd = new Vector3(lineX, rect.y + rect.height, 0);
                    Handles.DrawLine(lineStart, lineEnd);
                }
            }
        }
        

        
        /// <summary>
        /// 获取或创建层级项数据
        /// </summary>
        private static HierarchyItemData GetOrCreateItemData(int instanceID, Transform transform)
        {
            if (!hierarchyCache.TryGetValue(instanceID, out HierarchyItemData data))
            {
                data = new HierarchyItemData
                {
                    transform = transform,
                    hasParent = transform.parent != null,
                    hasChildren = transform.childCount > 0,
                    hasNextSibling = HasNextSibling(transform)
                };
                hierarchyCache[instanceID] = data;
            }
            
            return data;
        }
        
        /// <summary>
        /// 检查是否有下一个兄弟节点
        /// </summary>
        private static bool HasNextSibling(Transform transform)
        {
            if (transform.parent == null) return false;
            
            int siblingIndex = transform.GetSiblingIndex();
            return siblingIndex < transform.parent.childCount - 1;
        }
        

        
        /// <summary>
        /// 获取Transform的层级深度
        /// </summary>
        private static int GetDepth(Transform transform)
        {
            int depth = 0;
            Transform current = transform;
            
            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }
            
            return depth;
        }
        
        /// <summary>
        /// 刷新层级缓存
        /// </summary>
        private static void RefreshHierarchyCache()
        {
            hierarchyCache.Clear();
            
            // 重新构建缓存
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.transform.parent != null) // 只缓存非根对象
                {
                    GetOrCreateItemData(obj.GetInstanceID(), obj.transform);
                }
            }
        }
        
        /// <summary>
        /// 保存设置
        /// </summary>
        private static void SaveSettings()
        {
            EditorPrefs.SetBool("NyameauToolbox.HierarchyTreeView.Enabled", isEnabled);
        }
        
        /// <summary>
        /// 加载设置
        /// </summary>
        private static void LoadSettings()
        {
            isEnabled = EditorPrefs.GetBool("NyameauToolbox.HierarchyTreeView.Enabled", true);
        }
        
        /// <summary>
        /// 切换功能开关
        /// </summary>
        [MenuItem("诺喵工具箱/层级视觉增强/启用树状结构显示", false, 1)]
        public static void ToggleTreeView()
        {
            isEnabled = !isEnabled;
            SaveSettings();
            
            string status = isEnabled ? "已启用" : "已禁用";
            Debug.Log($"层级树状结构显示功能{status}");
            
            // 强制刷新层级窗口
            EditorApplication.RepaintHierarchyWindow();
        }
        
        /// <summary>
        /// 菜单项验证
        /// </summary>
        [MenuItem("诺喵工具箱/层级视觉增强/启用树状结构显示", true)]
        public static bool ToggleTreeViewValidate()
        {
            Menu.SetChecked("诺喵工具箱/层级视觉增强/启用树状结构显示", isEnabled);
            return true;
        }
    }
    
    /// <summary>
    /// 层级项数据结构
    /// </summary>
    public class HierarchyItemData
    {
        public Transform transform;
        public bool hasParent;
        public bool hasChildren;
        public bool hasNextSibling;
    }
}
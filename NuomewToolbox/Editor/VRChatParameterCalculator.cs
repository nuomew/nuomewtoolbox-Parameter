using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Profiling;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace NyameauToolbox.Editor
{
    /// <summary>
    /// VRChat参数精确计算器
    /// 提供更准确的模型参数计算方法
    /// </summary>
    public static class VRChatParameterCalculator
    {
        // VRChat官方限制
        public const int MAX_BITS = 256;
        public const int MAX_TEXTURE_MEMORY_MB = 500;
        public const int MAX_DYNAMIC_BONES = 256;  // 动骨限制改回256
        public const int MAX_UNCOMPRESSED_SIZE_MB = 500;
        public const int MAX_UPLOAD_SIZE_MB = 200;
        
        /// <summary>
        /// 计算Bits参数使用量 - 包含Modular Avatar Parameters同步参数
        /// 统计VRChat Expression Parameters + Modular Avatar Parameters组件中的同步参数
        /// </summary>
        public static float CalculateBitsUsage(VRCAvatarDescriptor avatar)
        {
            if (avatar == null) return 0f;
            
            int totalBits = 0;
            HashSet<string> processedParameters = new HashSet<string>(); // 防止重复计算
            
            Debug.Log("[Bits计算] 开始计算VRChat活动参数的Bits使用量...");
            Debug.Log("[Bits计算] 规则说明：只有勾选'networkSynced'(网络同步)的参数才占用bits，'saved'(本地保存)参数不占用网络带宽");
            
            // 1. 计算Expression Parameters中的Synced参数（VRChat原生参数）
            if (avatar.expressionParameters != null)
            {
                Debug.Log($"[Bits计算] 检查Expression Parameters: {avatar.expressionParameters.name}");
                
                foreach (var param in avatar.expressionParameters.parameters)
                {
                    // 只计算网络同步(networkSynced)的参数，saved参数不占用bits
                    if (!string.IsNullOrEmpty(param.name) && param.networkSynced)
                    {
                        if (!processedParameters.Contains(param.name))
            {
                            processedParameters.Add(param.name);
                            
                            switch (param.valueType)
                            {
                                case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool:
                                    totalBits += 1;
                                    Debug.Log($"[Bits计算] VRChat Bool参数 '{param.name}': +1 bit");
                                    break;
                                case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int:
                                    totalBits += 8;
                                    Debug.Log($"[Bits计算] VRChat Int参数 '{param.name}': +8 bits");
                                    break;
                                case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float:
                                    totalBits += 8;
                                    Debug.Log($"[Bits计算] VRChat Float参数 '{param.name}': +8 bits");
                                    break;
                            }
                        }
                        else
                        {
                            Debug.Log($"[Bits计算] 跳过重复参数 '{param.name}'");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(param.name))
                        {
                            Debug.Log($"[Bits计算] 跳过空名称参数");
                        }
                        else
                        {
                            Debug.Log($"[Bits计算] 跳过非网络同步参数 '{param.name}' (saved={param.saved}, networkSynced={param.networkSynced})");
                        }
                    }
                }
            }
            else
            {
                Debug.Log("[Bits计算] 未找到Expression Parameters");
            }
            
            // 2. 扫描Modular Avatar Parameters组件中的同步参数
            int modularAvatarBits = ScanModularAvatarParameters(avatar, processedParameters);
            totalBits += modularAvatarBits;
            
            Debug.Log($"[Bits计算] === 最终结果 ===");
            Debug.Log($"[Bits计算] VRChat原生参数: {totalBits - modularAvatarBits} bits");
            Debug.Log($"[Bits计算] Modular Avatar同步参数: {modularAvatarBits} bits");
            Debug.Log($"[Bits计算] 总Bits使用量: {totalBits} / {MAX_BITS}");
            Debug.Log($"[Bits计算] 处理的参数数量: {processedParameters.Count}");
            
            return totalBits;
        }
        
        /// <summary>
        /// 扫描Modular Avatar Parameters组件中的同步参数
        /// 只计算activeInHierarchy且enabled的MA Parameters组件中的同步参数
        /// </summary>
        private static int ScanModularAvatarParameters(VRCAvatarDescriptor avatar, HashSet<string> processedParameters)
        {
            if (avatar == null) return 0;
            
            int modularAvatarBits = 0;
            int totalMAParametersFound = 0;
            int activeMAParametersFound = 0;
            int syncedParametersFound = 0;
            
            try
            {
                Debug.Log("[Modular Avatar] 开始扫描MA Parameters组件中的同步参数...");
                
                // 获取所有组件，包括非激活的
                var allComponents = avatar.GetComponentsInChildren<Component>(true);
                
                foreach (var component in allComponents)
                {
                    if (component == null) continue;
                    
                    string typeName = component.GetType().Name;
                    string fullTypeName = component.GetType().FullName;
                    
                    // 检测是否为Modular Avatar Parameters组件
                    if (IsModularAvatarParametersComponent(typeName, fullTypeName))
                    {
                        totalMAParametersFound++;
                        
                        // 只处理活动且启用的组件
                        bool isActiveAndEnabled = component.gameObject.activeInHierarchy;
                        if (component is Behaviour behaviour)
                        {
                            isActiveAndEnabled = isActiveAndEnabled && behaviour.enabled;
                        }
                        
                        if (isActiveAndEnabled)
                        {
                            activeMAParametersFound++;
                            
                            // 扫描该组件中的同步参数
                            int componentBits = ScanParametersInComponent(component, processedParameters);
                            modularAvatarBits += componentBits;
                            syncedParametersFound += componentBits; // 假设每个bits对应一个参数（Bool=1, Int/Float=8）
                            
                            string componentPath = GetComponentPath(component.transform, avatar.transform);
                            Debug.Log($"[Modular Avatar] MA Parameters组件: {typeName} 在 {componentPath}, +{componentBits} bits");
                        }
                        else
                        {
                            string componentPath = GetComponentPath(component.transform, avatar.transform);
                            Debug.Log($"[Modular Avatar] 跳过非活动MA Parameters: {typeName} 在 {componentPath} (inactive or disabled)");
                        }
                    }
                }
                
                Debug.Log($"[Modular Avatar] MA Parameters扫描完成:");
                Debug.Log($"[Modular Avatar] 总计发现MA Parameters组件: {totalMAParametersFound}");
                Debug.Log($"[Modular Avatar] 活动MA Parameters组件: {activeMAParametersFound}");
                Debug.Log($"[Modular Avatar] 同步参数总bits: {modularAvatarBits}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Modular Avatar] 扫描MA Parameters时发生错误: {e.Message}");
            }
            
            return modularAvatarBits;
        }
        
        /// <summary>
        /// 判断是否为Modular Avatar Parameters组件
        /// </summary>
        private static bool IsModularAvatarParametersComponent(string typeName, string fullTypeName)
        {
            // 精确匹配MA Parameters组件
            if (typeName.Equals("ModularAvatarParameters", StringComparison.OrdinalIgnoreCase) ||
                fullTypeName.Contains("ModularAvatarParameters"))
            {
                return true;
            }
            
            // 检查是否包含MA命名空间且是Parameters相关组件
            bool hasMANamespace = fullTypeName.Contains("ModularAvatar") || 
                                 fullTypeName.Contains("modular_avatar") ||
                                 fullTypeName.Contains("nadena.dev.modular_avatar");
            
            bool isParametersComponent = typeName.Contains("Parameter") && 
                                       (typeName.Contains("MA") || typeName.Contains("Modular"));
            
            return hasMANamespace && isParametersComponent;
        }
        
        /// <summary>
        /// 扫描组件中的参数并计算bits占用
        /// </summary>
        private static int ScanParametersInComponent(Component component, HashSet<string> processedParameters)
        {
            int componentBits = 0;
            
            try
            {
                var componentType = component.GetType();
                
                // 尝试找到parameters字段
                var parametersField = componentType.GetField("parameters") ?? 
                                    componentType.GetField("Parameters") ??
                                    componentType.GetField("parameterList") ??
                                    componentType.GetField("_parameters");
                
                if (parametersField != null)
                {
                    var parametersValue = parametersField.GetValue(component);
                    if (parametersValue is IList parametersList)
                    {
                        foreach (var parameterObj in parametersList)
                        {
                            if (parameterObj == null) continue;
                            
                            int paramBits = AnalyzeMAParameter(parameterObj, processedParameters);
                            componentBits += paramBits;
                        }
                    }
                }
                else
                {
                    // 如果没有找到parameters字段，尝试其他可能的字段名
                    Debug.Log($"[Modular Avatar] 未在组件 {componentType.Name} 中找到parameters字段");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Modular Avatar] 分析组件参数时发生错误: {e.Message}");
            }
            
            return componentBits;
        }
        
        /// <summary>
        /// 分析单个MA参数并计算bits占用
        /// </summary>
        private static int AnalyzeMAParameter(object parameterObj, HashSet<string> processedParameters)
        {
            try
            {
                var paramType = parameterObj.GetType();
                
                // 获取参数名称 - 增强版检测
                var nameField = paramType.GetField("nameOrPrefix") ?? 
                              paramType.GetField("name") ??
                              paramType.GetField("parameterName") ??
                              paramType.GetField("_name") ??
                              paramType.GetField("paramName") ??
                              paramType.GetField("identifier") ??
                              paramType.GetField("_nameOrPrefix");
                
                string paramName = "";
                if (nameField != null)
                {
                    paramName = nameField.GetValue(parameterObj)?.ToString();
                    Debug.Log($"[Modular Avatar] 找到参数名称字段 '{nameField.Name}' = '{paramName}'");
                }
                else
                {
                    // 尝试通过属性获取参数名称
                    var nameProperty = paramType.GetProperty("nameOrPrefix") ??
                                     paramType.GetProperty("name") ??
                                     paramType.GetProperty("parameterName");
                    
                    if (nameProperty != null && nameProperty.CanRead)
                    {
                        paramName = nameProperty.GetValue(parameterObj)?.ToString();
                        Debug.Log($"[Modular Avatar] 找到参数名称属性 '{nameProperty.Name}' = '{paramName}'");
                    }
                    else
                    {
                        Debug.Log($"[Modular Avatar] 未找到参数名称字段，可用字段:");
                        var allFields = paramType.GetFields();
                        foreach (var field in allFields)
                        {
                            var fieldValue = field.GetValue(parameterObj);
                            Debug.Log($"  字段: {field.Name} = {fieldValue} ({field.FieldType.Name})");
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(paramName))
                {
                    Debug.Log($"[Modular Avatar] 参数名称为空，跳过此参数");
                    return 0; // 无效参数名
                }
                
                // 检查是否为网络同步参数 - 只计算网络同步，不计算本地保存
                var syncedField = paramType.GetField("networkSynced") ??
                                paramType.GetField("synced") ?? 
                                paramType.GetField("isSynced") ??
                                paramType.GetField("isNetworkSynced") ??
                                paramType.GetField("sync") ??
                                paramType.GetField("isSync") ??
                                paramType.GetField("_synced") ??
                                paramType.GetField("_networkSynced");
                
                bool isSynced = false;
                string syncFieldName = "";
                
                if (syncedField != null)
                {
                    syncFieldName = syncedField.Name;
                    var syncedValue = syncedField.GetValue(parameterObj);
                    isSynced = syncedValue is bool syncBool && syncBool;
                    Debug.Log($"[Modular Avatar] 参数 '{paramName}' 同步字段 '{syncFieldName}' = {syncedValue}");
                }
                else
                {
                    // 尝试通过属性访问（有些字段可能是属性而不是字段）
                    var syncedProperty = paramType.GetProperty("networkSynced") ??
                                       paramType.GetProperty("synced") ??
                                       paramType.GetProperty("isSynced") ??
                                       paramType.GetProperty("isNetworkSynced");
                    
                    if (syncedProperty != null && syncedProperty.CanRead)
                    {
                        syncFieldName = syncedProperty.Name + " (property)";
                        var syncedValue = syncedProperty.GetValue(parameterObj);
                        isSynced = syncedValue is bool syncBool && syncBool;
                        Debug.Log($"[Modular Avatar] 参数 '{paramName}' 同步属性 '{syncedProperty.Name}' = {syncedValue}");
                    }
                    else
                    {
                        // 如果既没有字段也没有属性，列出所有可用字段用于调试
                        var allFields = paramType.GetFields();
                        var allProperties = paramType.GetProperties();
                        
                        Debug.Log($"[Modular Avatar] 参数 '{paramName}' 未找到同步字段，可用字段:");
                        foreach (var field in allFields)
                        {
                            Debug.Log($"  字段: {field.Name} ({field.FieldType.Name})");
                        }
                        foreach (var prop in allProperties)
                        {
                            Debug.Log($"  属性: {prop.Name} ({prop.PropertyType.Name})");
                        }
                        
                        // 保守处理：如果找不到网络同步字段，跳过此参数（只计算网络同步参数）
                        Debug.Log($"[Modular Avatar] 未找到网络同步字段，跳过参数: '{paramName}'");
                        return 0;
                    }
                }
                
                if (!isSynced)
                {
                    Debug.Log($"[Modular Avatar] 跳过非网络同步MA参数: '{paramName}' ({syncFieldName}={isSynced})");
                    return 0;
                }
                
                // 避免重复计算
                if (processedParameters.Contains(paramName))
                {
                    Debug.Log($"[Modular Avatar] 跳过重复MA参数: '{paramName}'");
                    return 0;
                }
                
                processedParameters.Add(paramName);
                
                // 获取参数类型并计算bits - 增强版检测
                var typeField = paramType.GetField("valueType") ?? 
                              paramType.GetField("type") ??
                              paramType.GetField("parameterType") ??
                              paramType.GetField("paramType") ??
                              paramType.GetField("_valueType") ??
                              paramType.GetField("_type");
                
                if (typeField != null)
                {
                    var typeValue = typeField.GetValue(parameterObj);
                    Debug.Log($"[Modular Avatar] 参数 '{paramName}' 类型字段 '{typeField.Name}' = {typeValue} ({typeValue?.GetType().Name})");
                    int bits = CalculateParameterBits(typeValue, paramName);
                    Debug.Log($"[Modular Avatar] MA同步参数 '{paramName}' ({syncFieldName}): +{bits} bits");
                    return bits;
                }
                else
                {
                    // 尝试通过属性访问类型信息
                    var typeProperty = paramType.GetProperty("valueType") ??
                                     paramType.GetProperty("type") ??
                                     paramType.GetProperty("parameterType");
                    
                    if (typeProperty != null && typeProperty.CanRead)
                    {
                        var typeValue = typeProperty.GetValue(parameterObj);
                        Debug.Log($"[Modular Avatar] 参数 '{paramName}' 类型属性 '{typeProperty.Name}' = {typeValue} ({typeValue?.GetType().Name})");
                        int bits = CalculateParameterBits(typeValue, paramName);
                        Debug.Log($"[Modular Avatar] MA同步参数 '{paramName}' ({syncFieldName}): +{bits} bits");
                        return bits;
                    }
                    else
                    {
                        // 列出所有字段和属性用于调试
                        var allFields = paramType.GetFields();
                        var allProperties = paramType.GetProperties();
                        
                        Debug.Log($"[Modular Avatar] 参数 '{paramName}' 未找到类型字段，可用字段:");
                        foreach (var field in allFields)
                        {
                            var fieldValue = field.GetValue(parameterObj);
                            Debug.Log($"  字段: {field.Name} = {fieldValue} ({field.FieldType.Name})");
                        }
                        foreach (var prop in allProperties.Where(p => p.CanRead))
                        {
                            try
                            {
                                var propValue = prop.GetValue(parameterObj);
                                Debug.Log($"  属性: {prop.Name} = {propValue} ({prop.PropertyType.Name})");
                            }
                            catch (Exception ex)
                            {
                                Debug.Log($"  属性: {prop.Name} = [获取失败: {ex.Message}] ({prop.PropertyType.Name})");
                            }
                        }
                        
                        // 无法确定类型，根据参数名称推测
                        int bits = GuessParameterTypeFromName(paramName);
                        Debug.Log($"[Modular Avatar] MA同步参数 '{paramName}' ({syncFieldName}) [推测类型]: +{bits} bits");
                        return bits;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Modular Avatar] 分析MA参数时发生错误: {e.Message}");
                return 1; // 保守估计
            }
        }
        
        /// <summary>
        /// 根据参数类型计算bits占用
        /// </summary>
        private static int CalculateParameterBits(object typeValue, string paramName)
        {
            if (typeValue == null) return 1;
            
            string typeName = typeValue.ToString();
            
            // VRChat参数类型匹配
            if (typeName.Contains("Bool") || typeName.Equals("0")) // Bool通常是枚举值0
            {
                return 1;
            }
            else if (typeName.Contains("Int") || typeName.Equals("1")) // Int通常是枚举值1
            {
                return 8;
            }
            else if (typeName.Contains("Float") || typeName.Equals("2")) // Float通常是枚举值2
            {
                return 8;
            }
            else
            {
                // 根据名称推测
                return GuessParameterTypeFromName(paramName);
            }
        }
        
        /// <summary>
        /// 根据参数名称推测类型
        /// </summary>
        private static int GuessParameterTypeFromName(string paramName)
        {
            if (string.IsNullOrEmpty(paramName)) return 1;
            
            string lowerName = paramName.ToLower();
            
            // Bool类型的常见命名模式
            if (lowerName.Contains("isactive") || 
                lowerName.Contains("enable") || 
                lowerName.Contains("toggle") ||
                lowerName.Contains("visible") ||
                lowerName.Contains("show") ||
                lowerName.Contains("_is") ||
                lowerName.StartsWith("is"))
            {
                return 1; // Bool参数
            }
            
            // Float类型的常见命名模式
            if (lowerName.Contains("blend") ||
                lowerName.Contains("weight") ||
                lowerName.Contains("alpha") ||
                lowerName.Contains("scale") ||
                lowerName.Contains("intensity"))
            {
                return 8; // Float参数
            }
            
            // 默认假设为Bool参数（最保守的估计）
            return 1;
        }
        
        /// <summary>
        /// 获取组件在层级中的路径
        /// </summary>
        private static string GetComponentPath(Transform target, Transform root)
        {
            if (target == null || root == null) return "";
            
            var path = new List<string>();
            Transform current = target;
            
            while (current != null && current != root)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }
            
            if (current == root)
            {
                path.Insert(0, root.name);
                return string.Join("/", path);
            }
            
            return target.name;
        }
        
        /// <summary>
        /// 计算纹理显存占用 - 使用完整迁移的Thry's Avatar Tools算法
        /// </summary>
        public static TextureAnalysisResult CalculateTextureMemory(VRCAvatarDescriptor avatar)
        {
            if (avatar == null)
            {
                return new TextureAnalysisResult();
            }

            try
            {
                // 使用Thry's Avatar Tools的纹理获取逻辑
                Dictionary<Texture, bool> textures = GetTexturesFromAvatar(avatar.gameObject);
                
                var result = new TextureAnalysisResult
                {
                    textureInfos = new List<TextureInfo>(),
                    textureCount = textures.Count
                };

                long totalMemoryBytes = 0;

                foreach (var kvp in textures)
                {
                    Texture texture = kvp.Key;
                    if (texture == null) continue;

                    // 使用Thry's Avatar Tools的纹理大小计算逻辑
                    var textureSize = CalculateTextureSizeFromThry(texture);
                    totalMemoryBytes += textureSize.sizeBytes;

                    var textureInfo = new TextureInfo
                    {
                        name = texture.name,
                        width = texture.width,
                        height = texture.height,
                        format = textureSize.formatString,
                        sizeMB = textureSize.sizeMB,
                        mipmapCount = texture.mipmapCount
                    };
                    
                    result.textureInfos.Add(textureInfo);
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
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[诺喵工具箱] 计算纹理显存时出错: {e.Message}");
                return new TextureAnalysisResult();
            }
        }

        /// <summary>
        /// 获取GameObject中的所有纹理 - 完全按照Thry's Avatar Tools的逻辑
        /// </summary>
        private static Dictionary<Texture, bool> GetTexturesFromAvatar(GameObject avatar)
        {
            // 使用与AvatarEvaluator.GetMaterials完全相同的逻辑
            var materials = GetMaterialsFromAvatar(avatar);
            var activeMaterials = materials[0]; // 激活材质
            var allMaterials = materials[1];     // 所有材质

            Dictionary<Texture, bool> textures = new Dictionary<Texture, bool>();
            
            foreach (Material material in allMaterials)
            {
                if (material == null) continue;

                bool isActive = activeMaterials.Contains(material);
                int[] textureIds = material.GetTexturePropertyNameIDs();

                foreach (int id in textureIds)
                {
                    if (!material.HasProperty(id)) continue;
                    
                    Texture texture = material.GetTexture(id);
                    if (texture == null) continue;

                    if (textures.ContainsKey(texture))
                    {
                        // 如果纹理已存在且当前材质是激活的，则标记为激活
                        if (!textures[texture] && isActive)
                        {
                            textures[texture] = true;
                        }
                    }
                    else
                    {
                        textures.Add(texture, isActive);
                    }
                }
            }

            return textures;
        }

        /// <summary>
        /// 获取材质 - 完全按照AvatarEvaluator.GetMaterials的逻辑
        /// </summary>
        private static IEnumerable<Material>[] GetMaterialsFromAvatar(GameObject avatar)
        {
            // 过滤掉EditorOnly对象 - 这是关键差异！
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

            // 包含动画材质 - 另一个关键差异！
            var avatarDescriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor != null)
            {
                try
                {
                    var clips = avatarDescriptor.baseAnimationLayers
                        .Select(l => l.animatorController)
                        .Where(a => a != null)
                        .SelectMany(a => (a as UnityEditor.Animations.AnimatorController).animationClips)
                        .Distinct();

                    foreach (var clip in clips)
                    {
                        if (clip == null) continue;

                        var clipMaterials = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip)
                            .Where(b => b.isPPtrCurve && 
                                       b.type.IsSubclassOf(typeof(Renderer)) && 
                                       b.propertyName.StartsWith("m_Materials"))
                            .SelectMany(b => UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, b))
                            .Select(r => r.value as Material)
                            .Where(m => m != null);

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

        /// <summary>
        /// 计算单个纹理大小 - 完全按照Thry's Avatar Tools的CalculateTextureSize方法
        /// </summary>
        private static TextureSizeInfo CalculateTextureSizeFromThry(Texture texture)
        {
            var info = new TextureSizeInfo
            {
                texture = texture,
                formatString = "Unknown"
            };

            if (texture is Texture2D tex2D)
            {
                TextureFormat format = tex2D.format;
                if (!TextureFormatBPP.TryGetValue(format, out info.BPP))
                    info.BPP = 16;
                    
                info.formatString = format.ToString();
                info.sizeBytes = TextureToBytesUsingBPP(texture, info.BPP);

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
                if (!TextureFormatBPP.TryGetValue(tex2DArray.format, out info.BPP))
                    info.BPP = 16;
                info.formatString = tex2DArray.format.ToString();
                info.sizeBytes = TextureToBytesUsingBPP(texture, info.BPP) * tex2DArray.depth;
            }
            else if (texture is Cubemap cubemap)
            {
                if (!TextureFormatBPP.TryGetValue(cubemap.format, out info.BPP))
                    info.BPP = 16;
                info.formatString = cubemap.format.ToString();
                info.sizeBytes = TextureToBytesUsingBPP(texture, info.BPP);
                // 原始代码检查TextureDimension.Tex3D，但这个枚举值不存在
                // Cubemap默认就是6个面
                info.sizeBytes *= 6;
            }
            else if (texture is RenderTexture renderTexture)
            {
                if (!RenderTextureFormatBPP.TryGetValue(renderTexture.format, out info.BPP))
                    info.BPP = 16;
                info.BPP += renderTexture.depth;
                info.formatString = renderTexture.format.ToString();
                info.hasAlpha = renderTexture.format == RenderTextureFormat.ARGB32 || 
                               renderTexture.format == RenderTextureFormat.ARGBHalf || 
                               renderTexture.format == RenderTextureFormat.ARGBFloat;
                info.sizeBytes = TextureToBytesUsingBPP(texture, info.BPP);
            }
            else
            {
                info.sizeBytes = Profiler.GetRuntimeMemorySizeLong(texture);
            }

            info.sizeMB = info.sizeBytes / (1024f * 1024f);
            return info;
        }

        /// <summary>
        /// 根据BPP计算纹理字节大小 - 完全按照Thry's Avatar Tools的算法
        /// </summary>
        private static long TextureToBytesUsingBPP(Texture texture, float bpp, float resolutionScale = 1f)
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
                bytes = (long)((RenderTextureFormatBPP[renderTexture.format] + renderTexture.depth) * width * height * (renderTexture.useMipMap ? mipmaps : 1) / 8);
            }
            else
            {
                bytes = Profiler.GetRuntimeMemorySizeLong(texture);
            }

            return bytes;
        }

        // 纹理格式对应的每像素位数(BPP)字典 - 从Thry's Avatar Tools迁移
        private static readonly Dictionary<TextureFormat, float> TextureFormatBPP = new Dictionary<TextureFormat, float>()
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

        // RenderTexture格式对应的每像素位数字典 - 从Thry's Avatar Tools迁移
        private static readonly Dictionary<RenderTextureFormat, float> RenderTextureFormatBPP = new Dictionary<RenderTextureFormat, float>()
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

        /// <summary>
        /// 纹理大小信息 - 内部数据结构
        /// </summary>
        private struct TextureSizeInfo
        {
            public Texture texture;
            public long sizeBytes;
            public float sizeMB;
            public float BPP;
            public int minBPP;
            public string formatString;
            public bool hasAlpha;
        }
        
        // 旧的纹理分析方法已迁移到TextureMemoryCalculator类中
        
        /// <summary>
        /// 计算Dynamic Bone数量 - 全新的准确算法
        /// 直接使用Unity标准API确保准确性
        /// </summary>
        public static DynamicBoneCountResult CalculateDynamicBoneCount(VRCAvatarDescriptor avatar)
        {
            var result = new DynamicBoneCountResult();
            
            if (avatar == null)
            {
                Debug.LogWarning("[诺喵工具箱] Avatar为null");
                return result;
            }
            
            Debug.Log($"[诺喵工具箱] 🎯 开始准确计算动骨数量 - {avatar.name}");
            
            try
            {
                // 🎯 使用Unity标准API，确保搜索所有子对象（包括非激活的）
                var allPhysBones = avatar.GetComponentsInChildren<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone>(true);
                var allPhysBoneColliders = avatar.GetComponentsInChildren<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBoneCollider>(true);
                
                result.physBoneCount = allPhysBones != null ? allPhysBones.Length : 0;
                result.physBoneColliderCount = allPhysBoneColliders != null ? allPhysBoneColliders.Length : 0;
                
                Debug.Log($"[诺喵工具箱] ✅ Unity标准API检测结果:");
                Debug.Log($"  VRCPhysBone组件: {result.physBoneCount} 个");
                Debug.Log($"  VRCPhysBoneCollider组件: {result.physBoneColliderCount} 个");
                
                // 记录每个PhysBone组件的详细信息
                if (allPhysBones != null)
                {
                    for (int i = 0; i < allPhysBones.Length; i++)
                    {
                        var physBone = allPhysBones[i];
                        if (physBone != null)
                        {
                            string fullPath = GetFullTransformPath(physBone.transform, avatar.transform);
                            bool isActive = physBone.gameObject.activeInHierarchy;
                            
                            Debug.Log($"[诺喵工具箱] 📊 PhysBone {i+1}: '{physBone.name}' (活跃: {isActive})");
                            Debug.Log($"  路径: {fullPath}");
                            
                            result.detectedPhysBones.Add(new PhysBoneInfo
                            {
                                name = physBone.name,
                                gameObjectPath = fullPath,
                                rootTransform = physBone.rootTransform != null ? physBone.rootTransform.name : physBone.transform.name
                            });
                        }
                    }
                }
                
                // 记录每个PhysBoneCollider组件的详细信息
                if (allPhysBoneColliders != null)
                {
                    for (int i = 0; i < allPhysBoneColliders.Length; i++)
                    {
                        var collider = allPhysBoneColliders[i];
                        if (collider != null)
                        {
                            string fullPath = GetFullTransformPath(collider.transform, avatar.transform);
                            bool isActive = collider.gameObject.activeInHierarchy;
                            
                            Debug.Log($"[诺喵工具箱] 🔸 PhysBoneCollider {i+1}: '{collider.name}' (活跃: {isActive})");
                            Debug.Log($"  路径: {fullPath}");
                            
                            result.detectedColliders.Add(new PhysBoneColliderInfo
                            {
                                name = collider.name,
                                gameObjectPath = fullPath
                            });
                        }
                    }
                }
                
                // 🎯 关键：VRChat显示的就是PhysBone组件总数！
                result.totalCount = result.physBoneCount;
                result.affectedTransformsCount = result.physBoneCount; // 为了界面显示，实际上是组件数量
                
                Debug.Log($"[诺喵工具箱] === 🎉 最终结果（准确版本）===");
                Debug.Log($"🎯 动骨总数量: {result.totalCount} (应与VRChat 'Phys Bone Components' 完全一致)");
                Debug.Log($"PhysBone组件: {result.physBoneCount}");
                Debug.Log($"PhysBoneCollider组件: {result.physBoneColliderCount}");
                Debug.Log($"[诺喵工具箱] ✅ 动骨计算完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[诺喵工具箱] ❌ 动骨检测异常: {e.Message}");
                Debug.LogError($"异常详情: {e.StackTrace}");
                
                // 🆘 最终备用方法：手动搜索
                Debug.Log("[诺喵工具箱] 🆘 启动手动搜索备用方法...");
                try
                {
                    result = ManualSearchPhysBones(avatar);
                    Debug.Log($"[诺喵工具箱] 🆘 手动搜索结果: {result.totalCount} 个PhysBone组件");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[诺喵工具箱] ❌ 手动搜索也失败: {ex.Message}");
                    result.totalCount = 0;
                    result.affectedTransformsCount = 0;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 手动搜索PhysBone组件的备用方法
        /// </summary>
        private static DynamicBoneCountResult ManualSearchPhysBones(VRCAvatarDescriptor avatar)
        {
            var result = new DynamicBoneCountResult();
            var physBoneComponents = new List<Component>();
            var physBoneColliderComponents = new List<Component>();
            
            // 手动递归搜索所有Transform
            ManualRecursiveSearch(avatar.transform, physBoneComponents, physBoneColliderComponents);
            
            result.physBoneCount = physBoneComponents.Count;
            result.physBoneColliderCount = physBoneColliderComponents.Count;
            result.totalCount = result.physBoneCount;
            result.affectedTransformsCount = result.physBoneCount;
            
            // 填充详细信息列表
            foreach (var component in physBoneComponents)
            {
                if (component != null)
                {
                    string fullPath = GetFullTransformPath(component.transform, avatar.transform);
                    result.detectedPhysBones.Add(new PhysBoneInfo
                    {
                        name = component.name,
                        gameObjectPath = fullPath,
                        rootTransform = component.transform.name
                    });
                }
            }
            
            foreach (var component in physBoneColliderComponents)
            {
                if (component != null)
                {
                    string fullPath = GetFullTransformPath(component.transform, avatar.transform);
                    result.detectedColliders.Add(new PhysBoneColliderInfo
                    {
                        name = component.name,
                        gameObjectPath = fullPath
                    });
                }
            }
            
            Debug.Log($"[诺喵工具箱] 🆘 手动搜索发现:");
            Debug.Log($"  PhysBone组件: {result.physBoneCount} 个");
            Debug.Log($"  PhysBoneCollider组件: {result.physBoneColliderCount} 个");
            
            return result;
        }
        
        /// <summary>
        /// 手动递归搜索组件
        /// </summary>
        private static void ManualRecursiveSearch(Transform transform, List<Component> physBones, List<Component> colliders)
        {
            if (transform == null) return;
            
            // 检查当前节点的所有组件
            var allComponents = transform.GetComponents<Component>();
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                
                string typeName = component.GetType().Name;
                string fullTypeName = component.GetType().FullName;
                
                // 检查PhysBone组件
                if (typeName == "VRCPhysBone" || fullTypeName.Contains("VRCPhysBone"))
                {
                    physBones.Add(component);
                    Debug.Log($"[诺喵工具箱] 🆘 手动发现PhysBone: {component.name} 在 {transform.name}");
                }
                
                // 检查PhysBoneCollider组件
                if (typeName == "VRCPhysBoneCollider" || fullTypeName.Contains("VRCPhysBoneCollider"))
                {
                    colliders.Add(component);
                    Debug.Log($"[诺喵工具箱] 🆘 手动发现PhysBoneCollider: {component.name} 在 {transform.name}");
                }
            }
            
            // 递归搜索所有子节点
            for (int i = 0; i < transform.childCount; i++)
            {
                ManualRecursiveSearch(transform.GetChild(i), physBones, colliders);
            }
        }
        
        /// <summary>
        /// 获取从根节点到目标节点的完整路径
        /// </summary>
        private static string GetFullTransformPath(Transform target, Transform root)
        {
            if (target == null || root == null) return "";
            
            var path = new List<string>();
            Transform current = target;
            
            while (current != null && current != root)
            {
                path.Insert(0, current.name);
                current = current.parent;
            }
            
            if (current == root)
            {
                path.Insert(0, root.name);
                return string.Join("/", path);
            }
            
            return target.name; // 如果找不到路径，返回节点名
        }
        
        /// <summary>
        /// 计算模型大小 - 使用迁移的ModelSizeCalculator
        /// </summary>
        public static ModelSizeInfo CalculateModelSize(VRCAvatarDescriptor avatar)
        {
            if (avatar == null)
            {
                return new ModelSizeInfo();
            }

            // 使用新的模型大小计算器
            var meshResult = ModelSizeCalculator.CalculateMeshSize(avatar.gameObject);
            
            // 转换为原有的数据结构格式
            var result = new ModelSizeInfo
            {
                meshSizeMB = meshResult.totalSizeMB,
                vertexCount = meshResult.vertexCount,
                triangleCount = meshResult.triangleCount
            };
            
            return result;
        }
        
        // 旧的网格大小计算方法已迁移到ModelSizeCalculator类中
    }
    
    // 数据结构
    [System.Serializable]
    public class TextureAnalysisResult
    {
        public float totalMemoryMB;
        public int textureCount;
        public List<TextureInfo> textureInfos = new List<TextureInfo>();
    }
    
    [System.Serializable]
    public class TextureInfo
    {
        public string name;
        public int width;
        public int height;
        public string format;
        public int mipmapCount;
        public float sizeMB;
    }
    
    [System.Serializable]
    public class ModelSizeInfo
    {
        public float meshSizeMB;
        public int vertexCount;
        public int triangleCount;
    }
    
    // 新增：动骨计数结果
    [System.Serializable]
    public class DynamicBoneCountResult
    {
        public int physBoneCount;           // VRC PhysBone数量
        public int physBoneColliderCount;   // VRC PhysBoneCollider数量  
        public int totalCount;              // 总数
        
        // 详细信息
        public List<PhysBoneInfo> detectedPhysBones = new List<PhysBoneInfo>();
        public List<PhysBoneColliderInfo> detectedColliders = new List<PhysBoneColliderInfo>();
        
        public int affectedTransformsCount;  // 受影响变换总数
    }
    
    [System.Serializable]
    public class PhysBoneInfo
    {
        public string name;
        public string gameObjectPath;
        public string rootTransform;
    }
    
    [System.Serializable]
    public class PhysBoneColliderInfo
    {
        public string name;
        public string gameObjectPath;
    }
}
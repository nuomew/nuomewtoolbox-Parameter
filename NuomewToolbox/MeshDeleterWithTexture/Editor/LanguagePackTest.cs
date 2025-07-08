/*
 * 语言包加载测试脚本
 * 用于诊断和测试语言包加载问题
 */

using UnityEngine;
using UnityEditor;
using Gatosyocora.MeshDeleterWithTexture.Models;

namespace Gatosyocora.MeshDeleterWithTexture.Editor
{
    public static class LanguagePackTest
    {
        [MenuItem("Tools/MeshDeleter/Test Language Pack Loading")]
        public static void TestLanguagePackLoading()
        {
            Debug.Log("[语言包测试] 开始测试语言包加载...");
            
            try
            {
                // 测试AssetRepository.LoadLanguagePacks()
                var packs = AssetRepository.LoadLanguagePacks();
                
                if (packs == null)
                {
                    Debug.LogError("[语言包测试] AssetRepository.LoadLanguagePacks() 返回 null");
                    return;
                }
                
                Debug.Log($"[语言包测试] 加载了 {packs.Length} 个语言包");
                
                for (int i = 0; i < packs.Length; i++)
                {
                    var pack = packs[i];
                    if (pack == null)
                    {
                        Debug.LogError($"[语言包测试] 语言包 [{i}] 为 null");
                    }
                    else
                    {
                        Debug.Log($"[语言包测试] 语言包 [{i}]: {pack.language}, 渲染器文本: {pack.rendererLabelText}");
                    }
                }
                
                // 测试LocalizedText
                Debug.Log("[语言包测试] 测试 LocalizedText 初始化...");
                var localizedText = new LocalizedText();
                
                if (localizedText.Data == null)
                {
                    Debug.LogError("[语言包测试] LocalizedText.Data 为 null");
                }
                else
                {
                    Debug.Log($"[语言包测试] LocalizedText 加载成功，语言: {localizedText.SelectedLanguage}, 渲染器文本: {localizedText.Data.rendererLabelText}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[语言包测试] 发生异常: {ex.Message}\n{ex.StackTrace}");
            }
            
            Debug.Log("[语言包测试] 测试完成");
        }
    }
}
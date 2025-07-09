using System.Linq;
using UnityEngine;
using UnityEditor;
using System;

namespace Gatosyocora.MeshDeleterWithTexture.Models
{
    public class LocalizedText
    {
        public LanguagePack Data { get; private set; }

        public Language SelectedLanguage { get; private set; }

        private const string LOCAL_DATA_KEY = "mesh_deleter_language";

        public LocalizedText()
        {
            // 清理可能存在的无效语言设置
            CleanupInvalidLanguageSettings();
            SelectedLanguage = LoadLanguage();
            SetLanguage(SelectedLanguage);
        }

        public void SetLanguage(Language language)
        {
            var packs = AssetRepository.LoadLanguagePacks();
            if (packs == null || packs.Length == 0)
            {
                Debug.LogError("[网格删除器] 无法加载语言包资源");
                return;
            }
            
            var pack = packs.FirstOrDefault(p => p != null && p.language == language);
            if (pack == null)
            {
                Debug.LogWarning($"[网格删除器] 未找到语言 {language} 的语言包，使用默认中文语言包");
                pack = packs.FirstOrDefault(p => p != null && p.language == Language.CN);
            }
            
            if (pack == null)
            {
                Debug.LogError("[网格删除器] 无法找到任何有效的语言包");
                return;
            }
            
            Data = pack;
            SelectedLanguage = language;
            SaveLanguage();
        }

        private Language LoadLanguage()
        {
            var languageString = EditorUserSettings.GetConfigValue(LOCAL_DATA_KEY);
            if (string.IsNullOrEmpty(languageString))
            {
                return Language.CN; // 默认使用中文
            }

            try
            {
                if (Enum.TryParse<Language>(languageString, out Language result))
                {
                    return result;
                }
                else
                {
                    Debug.LogWarning($"[网格删除器] 无法解析语言设置 '{languageString}'，使用默认中文");
                    return Language.CN;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[网格删除器] 加载语言设置时发生错误: {ex.Message}");
                return Language.CN; // 默认使用中文
            }
        }

        private void SaveLanguage()
        {
            EditorUserSettings.SetConfigValue(LOCAL_DATA_KEY, SelectedLanguage.ToString());
        }

        /// <summary>
        /// 清理无效的语言设置，确保只使用支持的语言
        /// </summary>
        private void CleanupInvalidLanguageSettings()
        {
            var languageString = EditorUserSettings.GetConfigValue(LOCAL_DATA_KEY);
            if (!string.IsNullOrEmpty(languageString))
            {
                // 检查是否为支持的语言值
                if (!Enum.TryParse<Language>(languageString, out Language _))
                {
                    Debug.Log("[网格删除器] 插件加载完成");
                    EditorUserSettings.SetConfigValue(LOCAL_DATA_KEY, Language.CN.ToString());
                }
            }
        }
    }
}

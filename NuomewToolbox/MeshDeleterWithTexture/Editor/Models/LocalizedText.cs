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

            var obj = Enum.Parse(typeof(Language), languageString);
            if (obj != null)
            {
                return (Language)obj;
            } else
            {
                return Language.CN; // 默认使用中文
            }
        }

        private void SaveLanguage()
        {
            EditorUserSettings.SetConfigValue(LOCAL_DATA_KEY, SelectedLanguage.ToString());
        }
    }
}

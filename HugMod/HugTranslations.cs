using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace HugMod
{
    public static class HugTranslations
    {
        private class TranslationFile
        {
            public Dictionary<TextTranslation.Language, string> Prompt;
            public Dictionary<TextTranslation.Language, string> Ghost;
        }

        private static readonly TranslationFile _hugPromptTranslation;

        static HugTranslations()
        {
            var translationJson = File.ReadAllText(Path.Combine(HugMod.HugModInstance.ModHelper.Manifest.ModFolderPath, "Assets", "translation.json"));
            _hugPromptTranslation = JsonConvert.DeserializeObject<TranslationFile>(translationJson);
        }

        public static string GetHugPrompt(string name)
        {
            if (name == "Ghost")
            {
                _hugPromptTranslation.Ghost.TryGetValue(TextTranslation.Get().m_language, out name);
            }
            else
            {
                // If the name is not translated, default to the original string
                // Not using TextTranslation.Translate(name) because that will throw errors if it doesn't find it, but we don't mind using the default
                name = TextTranslation.Get().m_table.Get(name) ?? name;
            }

            if (_hugPromptTranslation.Prompt.TryGetValue(TextTranslation.Get().m_language, out var prompt))
            {
                return string.Format(prompt, name);
            }
            else
            {
                // This fallback should only occur if the player is using a modded language like Czech, Icelandic, or Andalusian
                return string.Format(_hugPromptTranslation.Prompt[TextTranslation.Language.ENGLISH], name);
            }
        }
    }
}

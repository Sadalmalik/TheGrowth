using System.Collections.Generic;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Scripting;
using TheGrowth.Core.Variables;
using UnityEngine;

namespace TheGrowth.Core.Content
{
    /// <summary>
    /// Файлы скриптов из реестра контента (система сессии). Файл разбирается при первом запросе и кешируется
    /// до конца сессии. Файл с ошибками разбора недоступен: ошибки логируются один раз, с файлом и строкой.
    /// ID конфигов — по реестру (<see cref="ContentScriptResolver"/>), переменные — по объявлениям
    /// (<see cref="VariableScriptResolver"/>).
    /// Требует в контейнере (обычно глобальном) <see cref="IContentRegistry"/>, <see cref="ScriptLanguage"/> и
    /// <see cref="VariableCatalog"/>.
    /// </summary>
    public sealed class ContentScriptLibrary : SharedObject, IScriptLibrary
    {
#pragma warning disable CS0649 // заполняется контейнером через [Inject]
        [Inject]
        private IContentRegistry m_Content;

        [Inject]
        private ScriptLanguage m_Language;

        [Inject]
        private VariableCatalog m_Variables;
#pragma warning restore CS0649

        /// <summary>Разобранные файлы по ID; null — файла нет или он с ошибками.</summary>
        private readonly Dictionary<string, ScriptFile> m_Files = new Dictionary<string, ScriptFile>();
        private ScriptParser m_Parser;

        /// <summary>Сколько файлов разобрано (для тестов кеша).</summary>
        internal int ParseCount { get; private set; }

        public override void Init()
        {
            m_Parser = new ScriptParser(m_Language.Registry, new ContentScriptResolver(m_Content), new VariableScriptResolver(m_Variables));
        }

        public override void Dispose()
        {
            m_Files.Clear();
        }

        public bool TryGetFile(string id, out ScriptFile file)
        {
            file = null;
            if (id == null)
                return false;

            if (m_Files.TryGetValue(id, out file))
                return file != null;

            file = Load(id);
            m_Files.Add(id, file);
            return file != null;
        }

        private ScriptFile Load(string id)
        {
            if (!m_Content.TryGetScript(id, out var entry))
                return null;

            if (entry.Text == null)
            {
                Debug.LogError($"Script file '{id}': text asset is missing in content registry");
                return null;
            }

            ParseCount++;
            var result = m_Parser.Parse(id, entry.Kind, entry.Text.text);
            if (!result.HasErrors)
                return result.File;

            Debug.LogError($"Script file '{entry}' has {result.Errors.Count} error(s) and is unavailable:\n{result}", entry.Text);
            return null;
        }
    }
}

using System;
using System.Collections.Generic;

namespace TheGrowth.Core.Scripting
{
    /// <summary>
    /// Сериализуемая позиция исполнения запуска (сохранение диалога, ждущего игрока).
    /// Sections[0] — корневая секция, далее — секции, вызванные через call, снизу вверх.
    /// </summary>
    [Serializable]
    public sealed class ScriptPosition
    {
        public List<ScriptSectionPosition> Sections = new List<ScriptSectionPosition>();
    }

    /// <summary>
    /// Позиция внутри одной секции. Path — [команда, блок, команда, блок, ..., команда]:
    /// пары «индекс управляющей команды, индекс её блока» по вложенным блокам и последним —
    /// индекс текущей команды. Для верхней секции это ожидающая команда; для нижних — команда
    /// после call, с которой исполнение продолжится после возврата.
    /// </summary>
    [Serializable]
    public sealed class ScriptSectionPosition
    {
        public string FileId;
        public string Section;
        public List<int> Path = new List<int>();

        public override string ToString()
        {
            return $"{FileId}.{Section}[{string.Join(",", Path)}]";
        }
    }
}

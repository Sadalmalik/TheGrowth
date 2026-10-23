using System;
using System.Collections.Generic;
using TheGrowth.Core.Tags;
using TheGrowth.Model.Graphs;

namespace TheGrowth.EditorTools.Graphs
{
    /// <summary>
    /// Целостность данных графа (для валидатора контента): пустые и повторяющиеся ID (общие для слотов, связей и сеток),
    /// связи с пропавшими слотами, петли и дубли связей, слоты пропавших сеток, пустые сетки, повторяющиеся и
    /// необъявленные теги слотов. Редактор поля этих ошибок не создаёт — они появляются при правке ассета руками или
    /// слиянии веток.
    /// </summary>
    public static class GraphChecks
    {
        /// <summary>Сообщения об ошибках; <paramref name="tags"/> null — теги не сверяются с реестром.</summary>
        public static List<string> Check(GraphModel graph, TagCatalog tags = null)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var grids = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var grid in graph.Grids)
            {
                CheckId(grid.Id, grid.ToString(), ids, errors);
                if (!string.IsNullOrEmpty(grid.Id))
                    grids[grid.Id] = 0;
            }

            foreach (var slot in graph.Slots)
            {
                CheckId(slot.Id, slot.ToString(), ids, errors);
                if (slot.IsInGrid)
                {
                    if (grids.TryGetValue(slot.GridId, out var count))
                        grids[slot.GridId] = count + 1;
                    else
                        errors.Add($"{slot}: grid '{slot.GridId}' is missing");
                }

                CheckTags(slot, tags, errors);
            }

            foreach (var pair in grids)
            {
                if (pair.Value == 0)
                    errors.Add($"grid '{pair.Key}' has no slots");
            }

            CheckLinks(graph, ids, errors);
            return errors;
        }

        private static void CheckLinks(GraphModel graph, HashSet<string> ids, List<string> errors)
        {
            var pairs = new HashSet<(string, string)>();
            foreach (var link in graph.Links)
            {
                CheckId(link.Id, link.ToString(), ids, errors);
                if (graph.FindSlot(link.SlotA) == null || graph.FindSlot(link.SlotB) == null)
                {
                    errors.Add($"{link}: slot is missing");
                    continue;
                }

                if (link.SlotA == link.SlotB)
                {
                    errors.Add($"{link}: links a slot to itself");
                    continue;
                }

                var pair = string.CompareOrdinal(link.SlotA, link.SlotB) < 0 ? (link.SlotA, link.SlotB) : (link.SlotB, link.SlotA);
                if (!pairs.Add(pair))
                    errors.Add($"{link}: slots are already linked");
            }
        }

        private static void CheckTags(GraphSlot slot, TagCatalog tags, List<string> errors)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tag in slot.Tags)
            {
                if (!seen.Add(tag))
                    errors.Add($"{slot}: tag '{tag}' is set twice");
                else if (tags != null && !tags.Contains(tag))
                    errors.Add($"{slot}: tag '{tag}' is not declared");
            }
        }

        private static void CheckId(string id, string element, HashSet<string> ids, List<string> errors)
        {
            if (string.IsNullOrEmpty(id))
                errors.Add($"{element}: empty id");
            else if (!ids.Add(id))
                errors.Add($"{element}: duplicate id '{id}'");
        }
    }
}

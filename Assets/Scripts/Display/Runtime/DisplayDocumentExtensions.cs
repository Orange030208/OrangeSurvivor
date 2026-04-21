using System;
using System.Collections.Generic;

/// <summary>
/// DisplayDocument 常用查询扩展。
/// UI 层可按自身支持的 Block 类型读取数据，而不依赖具体业务对象。
/// </summary>
public static class DisplayDocumentExtensions
{
    public static TBlock GetBlock<TBlock>(this DisplayDocument document) where TBlock : DisplayBlock
    {
        if (document == null || document.Blocks == null)
        {
            return null;
        }

        for (int i = 0; i < document.Blocks.Count; i++)
        {
            if (document.Blocks[i] is TBlock matchedBlock)
            {
                return matchedBlock;
            }
        }

        return null;
    }

    public static IReadOnlyList<TBlock> GetBlocks<TBlock>(this DisplayDocument document) where TBlock : DisplayBlock
    {
        if (document == null || document.Blocks == null || document.Blocks.Count == 0)
        {
            return Array.Empty<TBlock>();
        }

        List<TBlock> results = new();
        for (int i = 0; i < document.Blocks.Count; i++)
        {
            if (document.Blocks[i] is TBlock matchedBlock)
            {
                results.Add(matchedBlock);
            }
        }

        return results;
    }
}

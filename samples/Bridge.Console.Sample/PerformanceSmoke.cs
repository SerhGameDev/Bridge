using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bridge.Core;

static class PerformanceSmoke
{
    public static void Run()
    {
        foreach (int count in new[] { 1000, 10000, 50000 })
        {
            var doc = BridgeDocument.Create("performance");
            for (int i = 0; i < count; i++) doc.AddBlock(i.ToString("D5"), i % 2 == 0 ? "even" : "odd", BridgeValue.Parse("{\"value\":1,\"excluded\":2}"));
            var dictionary = doc.Blocks.ToDictionary(b => b.Id, StringComparer.Ordinal);
            // Replay the v0.1 Blocks getter algorithm in this process, with identical block instances.
            IReadOnlyList<BridgeBlock> Legacy() => Array.AsReadOnly(dictionary.Values.OrderBy(b => b.Id, StringComparer.Ordinal).ToArray());
            Measure("legacy-getter", count, () => Legacy().Count, count);
            Measure("cached-getter", count, () => doc.Blocks.Count, count);
            Measure("current-query", count, () => doc.Query().WhereTypes("even").SelectFields("value").Execute().Blocks.Count, count / 2);
        }
    }
    static void Measure(string label, int count, Func<int> operation, int expected)
    {
        for (int i = 0; i < 3; i++) if (operation() != expected) throw new Exception("Warm-up result mismatch.");
        const int repetitions = 10;
        long start = GC.GetAllocatedBytesForCurrentThread(); var watch = Stopwatch.StartNew();
        for (int i = 0; i < repetitions; i++) if (operation() != expected) throw new Exception("Result mismatch.");
        watch.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        Console.WriteLine($"PERF {count} {label}: {watch.Elapsed.TotalMilliseconds / repetitions:F4} ms/op; {allocated / repetitions} B/op; n={repetitions}");
    }
}

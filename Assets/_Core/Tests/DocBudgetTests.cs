using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Flame_n_Blame_Challenge.Tests
{
    /// The docs are the map every session reads, so a doc that grows past its budget is a doc whose content
    /// gets read less. The budgets started as rules on paper, and the docs grew ~250 lines a day under them
    /// (D-024), so the budget is asserted here: a red test says "compact this doc before writing code", not
    /// "raise the number". The table lives in docs/README.md §Budgets and is repeated in this file — changing
    /// one copy without the other is a doc that lies, and a lying doc is what this test exists to catch.
    /// It reads text files only, so it asserts nothing about Unity, and it is not a test on an Adapter.
    public class DocBudgetTests
    {
        private struct Budget
        {
            public string Path;
            public int MaxLines;
            public int MaxLineLength;
            public string EntryPrefix;
            public int MaxEntryLines;
        }

        private static readonly Budget[] Budgets =
        {
            new Budget { Path = "CLAUDE.md", MaxLines = 135, MaxLineLength = 110 },
            new Budget { Path = "AGENTS.md", MaxLines = 25, MaxLineLength = 110 },
            new Budget { Path = "README.md", MaxLines = 50, MaxLineLength = 110 },
            new Budget { Path = "docs/README.md", MaxLines = 60, MaxLineLength = 140 },
            new Budget { Path = "docs/DECISIONS.md", MaxLines = 400, MaxLineLength = 320, EntryPrefix = "## D-", MaxEntryLines = 10 },
            new Budget { Path = "docs/ARCHITECTURE.md", MaxLines = 200, MaxLineLength = 320 },
            new Budget { Path = "docs/TODO.md", MaxLines = 150, MaxLineLength = 120 },
            new Budget { Path = "docs/GOTCHAS.md", MaxLines = 160, MaxLineLength = 320, EntryPrefix = "## G-", MaxEntryLines = 4 },
            new Budget { Path = "docs/STYLE.md", MaxLines = 110, MaxLineLength = 120 },
        };

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        private static string[] ReadLines(string relativePath)
        {
            string fullPath = Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(fullPath), Is.True, $"{relativePath} is not in the repo root ({fullPath})");
            return File.ReadAllLines(fullPath, new UTF8Encoding(false));
        }

        [Test]
        public void DocFiles_StayWithinTheirLineBudgets()
        {
            List<string> violations = new List<string>();
            foreach (Budget budget in Budgets)
            {
                int lines = ReadLines(budget.Path).Length;
                if (lines > budget.MaxLines)
                    violations.Add($"{budget.Path}: {lines} lines, budget {budget.MaxLines}");
            }
            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        }

        [Test]
        public void DocLines_StayWithinTheirLengthBudgets()
        {
            List<string> violations = new List<string>();
            foreach (Budget budget in Budgets)
            {
                string[] lines = ReadLines(budget.Path);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Length > budget.MaxLineLength)
                        violations.Add($"{budget.Path}:{i + 1} is {lines[i].Length} chars, budget {budget.MaxLineLength}");
                }
            }
            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        }

        [Test]
        public void DocEntries_StayWithinTheirEntryBudgets()
        {
            List<string> violations = new List<string>();
            foreach (Budget budget in Budgets)
            {
                if (string.IsNullOrEmpty(budget.EntryPrefix))
                    continue;

                string[] lines = ReadLines(budget.Path);
                int start = -1;
                int size = 0;
                int entries = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith(budget.EntryPrefix))
                    {
                        if (start >= 0 && size > budget.MaxEntryLines)
                            violations.Add($"{budget.Path}:{start + 1} entry is {size} lines, budget {budget.MaxEntryLines}: {lines[start]}");
                        start = i;
                        size = 0;
                        entries++;
                    }
                    if (start >= 0 && lines[i].Trim().Length > 0)
                        size++;
                }

                if (start >= 0 && size > budget.MaxEntryLines)
                    violations.Add($"{budget.Path}:{start + 1} entry is {size} lines, budget {budget.MaxEntryLines}: {lines[start]}");
                Assert.That(entries, Is.Not.EqualTo(0), $"{budget.Path} has no {budget.EntryPrefix} entries — the log is gone");
            }
            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        }
    }
}

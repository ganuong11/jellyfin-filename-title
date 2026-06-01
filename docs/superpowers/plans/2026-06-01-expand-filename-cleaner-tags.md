# Expand FilenameCleanerService QualityTags Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand the hardcoded `QualityTags` array and the related site-name TLD / video-extension lists in `FilenameCleanerService.cs` so the plugin strips additional release tags, codecs, language markers, group prefixes, site-name TLDs, and full-disc image extensions from filenames.

**Architecture:** All cleaning logic is in the pure-function `FilenameCleanerService.Clean()` (`FilenameTitlePlugin/FilenameCleanerService.cs:36-63`). The cleaner already iterates `QualityTags` with case-insensitive word-boundary matches, so adding a token to the array is the only change needed to strip a new tag. Two adjacent fields are also touched: `SiteNameRegex` (broader TLD alternation) and `VideoExtensions` (add `.iso`/`.img`). The cleaner's tag loop is reordered to run BEFORE the dot/underscore replacement (Task 2 step 3), so every tag matches against the original (dot-preserved) filename. The cleaner's whitespace-trim step is replaced with a regex that also strips leading/trailing punctuation left over after tag stripping (Task 5 step 5). Behavior is verified by appending `[InlineData]` rows to the existing `Clean_ReturnsExpectedTitle` `[Theory]` and adding one `[Fact]` regression guard.

**Tech Stack:** .NET 8, C# 12, xUnit 2.9

**User constraints honored:**
- Per the user-level `AGENTS.md` rule, **do not run any `git commit` commands**. User commits manually. Tasks end after the test run.
- The `FINAL` release-flag token is **excluded** from the new additions (user request).

---

## Revision 1 (2026-06-01)

Plan-review pass found 4 issues that would have produced failing tests. The user constraint is that the test rows in the plan must NOT be edited (they are the verification spec). Fixes are applied to the source files only; the rest of the plan is unchanged.

### Issue 1: Tag ordering — `DTS` strips from `DTS-HD` / `DTSHD` / `DTS-X` / `DTSX`
The existing `DTS` tag in `QualityTags` line 14 is processed BEFORE the new compound tags appended at the end of the array. The regex `\bDTS\b` matches `DTS` in `DTS-HD` (the `-` provides the trailing word boundary), stripping it and leaving `-HD` or `-X`. The compound tag never gets a chance to match because `DTS` is gone. This affects all 4 DTS compound test rows in Task 2.

**Fix:** Reorder the `QualityTags` array so the DTS compound forms are processed before the bare `DTS`. Concretely: insert a new line of DTS-compound tags BEFORE the existing audio line; the bare `DTS` stays where it is. (Even if the cleaner is also reordered to strip tags before dot replacement — see Issue 2 — the tag order issue remains, because `\bDTS\b` matches `DTS` inside `DTS-HD` regardless of whether the dot is still in the input.)

### Issue 2: Dots in tags — `DD5.1` / `DD7.1` are dead code
The cleaner replaces `.` with space in step 3 (`FilenameCleanerService.cs:48`), BEFORE the tag loop in step 4. By the time the tag loop runs, `DD5.1` is already `DD5 1` (with a space). The regex `\bDD5\.1\b` cannot match `DD5 1`. The tag is dead code. This affects the `DD5.1` test row in Task 2.

**Fix:** Reorder the cleaner so the tag loop runs BEFORE the dot/underscore replacement step. Concretely: move the existing `foreach (var tag in QualityTags)` block (lines 51-54) above the existing `name = name.Replace('.', ' ').Replace('_', ' ');` line (line 48). After this change, every tag matches against the original (dot-preserved) filename, so `DD5.1` and `DD7.1` work as-is, and any future tag containing a dot or hyphen will also work.

### Issue 3: Trailing dashes from release-group tags
The format `Title.2024.1080p.BluRay.x264-GROUP` has a hyphen between `x264` and the group name. The cleaner replaces dots and underscores with spaces (`FilenameCleanerService.cs:48`) but does not touch hyphens. After `x264` and `GROUP` are stripped, the hyphen remains. The cleaner does not strip punctuation. Result: `Movie 2024 -` (with trailing dash) instead of `Movie 2024`. Affects all 22 release-group test rows in Task 5.

**Fix:** Replace the existing `Trim()`-of-whitespace step in the cleaner with a regex-based step that strips leading/trailing whitespace AND punctuation (`-._/`) in one pass. The new regex `^[\s\-._/]+|[\s\-._/]+$` is anchored to start/end, so internal punctuation (e.g. the dash in `Spider-Man`) is preserved. The whitespace behavior of the old `Trim()` is preserved (the `\s` class matches the same characters), and punctuation stripping is added on top. Inserted at `FilenameCleanerService.cs:60` (replaces the existing single line). It does not affect any existing test because no current test has leading or trailing punctuation in the cleaned name.

### Issue 4: Plan arithmetic
The summary line in the original plan claimed `65 [InlineData]` rows + 6 `[Fact]` methods = 71 passing tests` and `4 original `[Fact]` + 1 new `[Fact]` = 71`. Both operands are wrong:
- The Theory actually accumulates 71 `[InlineData]` rows after Tasks 1-8 (7 pre-existing + 64 new: Task 1 = 4, Task 2 = 11, Task 3 = 11, Task 4 = 10, Task 5 = 22, Task 7 = 4, Task 8 = 2). The "65" was the running count up to Task 5 only, not after all tasks.
- There are 4 pre-existing `[Fact]`s + 1 new `[Fact]` from Task 6 = 5 `[Fact]`s, not 6. (The plan also double-counted by saying "plus the original `[Fact]`s listed in the test file" in the same parenthetical.)

**Fix:** Correct the numbers throughout: 71 `[InlineData]` rows + 5 `[Fact]` methods = **76** total passing tests. Updated in Task 6 step 4, Task 9 step 2, and the verification matrix.

---

## File Structure

### Files modified
- `FilenameTitlePlugin/FilenameCleanerService.cs`
  - `QualityTags` array (lines 7-18) — append ~89 new tokens grouped by category (4 video codec in Task 1 + 13 audio in Task 2 + 5 bit depth + 8 release flags in Task 3 + 28 language markers in Task 4 + 31 group prefixes in Task 5)
  - `SiteNameRegex` (lines 23-25) — expand TLD alternation
  - `VideoExtensions` (lines 20-21) — add `.iso`, `.img`
  - `Clean()` method — reorder: tag loop (currently lines 51-54) moved BEFORE the dot/underscore replacement (currently line 48). Tag stripping now runs against the original (dot-preserved) filename. Done in Task 2 step 3.
  - `LeadingTrailingPunctRegex` (new field, Task 5 step 5) — regex that strips leading/trailing whitespace and punctuation, replaces the existing `Trim()` call in `Clean()`
- `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs`
  - `Clean_ReturnsExpectedTitle` `[Theory]` (lines 10-21) — append new `[InlineData]` rows
  - Add one new `[Fact]` method for the negative regression case

### Files created
- None

### Files NOT touched
- `Plugin.cs` — no behavior change; the safety rule at line 43 still gates any rewrite
- `TitleUpdaterTask.cs` — no behavior change; same safety rule at line 60
- `PluginServiceRegistrator.cs` — no DI change
- `PluginConfiguration.cs` — no config change
- `install.sh` — out of scope
- `README.md` — token table there is representative but not exhaustive; not updated in this pass
- `AGENTS.md` — guidance remains accurate; no need to amend

---

## TDD convention for every task

Each task below follows the same TDD cycle (steps 1-4). The "Commit" step from the standard template is intentionally omitted per the user-level rule.

1. **Edit the test file** to add the new `[InlineData]` row(s) or `[Fact]`.
2. **Run the test** and verify it fails (for new rows) or passes (for the negative `[Fact]` that guards existing behavior).
3. **Edit the source file** to add the new tag(s) / regex / extension(s).
4. **Re-run the test** and verify it passes.

If a row in the new test data fails for an unexpected reason (different from "tag not in the list"), **stop and investigate** before continuing — that is a sign the cleaner has a behavior the new tag collides with.

---

### Task 1: Add video codec tags

**Files:**
- Modify: `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs:17` (after the last existing `[InlineData]`)
- Modify: `FilenameTitlePlugin/FilenameCleanerService.cs:13` (after the codec line)

- [ ] **Step 1: Add failing test row**

In `FilenameCleanerServiceTests.cs`, immediately after line 17 (`[InlineData("The.Dark.Knight", "The Dark Knight")]`), insert:

```csharp
[InlineData("Movie.2024.1080p.BluRay.XviD.avi", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DivX.avi", "Movie 2024")]
[InlineData("Movie.2024.2160p.BluRay.AV1.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.VP9.webm", "Movie 2024")]
```

(`.webm` is already in `VideoExtensions` so the extension will be stripped; the cleaner test does not assert on extension stripping, only the cleaned name.)

- [ ] **Step 2: Run the new rows and verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: 4 new failures with messages like `Expected: "Movie 2024" / Actual: "Movie 2024 1080p BluRay XviD"`. Existing 7 rows pass.

- [ ] **Step 3: Add the tags**

In `FilenameCleanerService.cs`, line 13 currently reads:

```csharp
"x264", "x265", "H264", "H265", "HEVC", "AVC",
```

Append a new line after it (still inside the array) with the four new tags:

```csharp
        "XviD", "DivX", "AV1", "VP9",
```

Resulting block (lines 13-14):

```csharp
        "x264", "x265", "H264", "H265", "HEVC", "AVC",
        "XviD", "DivX", "AV1", "VP9",
```

- [ ] **Step 4: Re-run and verify pass**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: all 11 rows pass.

---

### Task 2: Add audio codec tags

**Files:**
- Modify: `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs` (append after Task 1's rows)
- Modify: `FilenameTitlePlugin/FilenameCleanerService.cs:14` (reorder DTS compound forms + add new audio tags)
- Modify: `FilenameTitlePlugin/FilenameCleanerService.cs:48,51-54` (reorder cleaner: tag loop before dot/underscore replacement)

- [ ] **Step 1: Add failing test rows**

In `FilenameCleanerServiceTests.cs`, append after the rows added in Task 1:

```csharp
[InlineData("Movie.2024.1080p.BluRay.Opus.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.FLAC.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.EAC3.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DDP.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DD.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DD5.1.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DTS-HD.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DTSHD.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DTS-X.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.LPCM.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.PCM.mkv", "Movie 2024")]
```

> **Test rows are unchanged from the original plan** (per user constraint: the tests are the verification spec). 11 rows total: 5 simple audio (Opus, FLAC, EAC3, DDP, DD), 1 dot-in-tag (DD5.1), 3 DTS compound (DTS-HD, DTSHD, DTS-X), 2 lossless (LPCM, PCM). The `DD5.1` row exercises Issue 2; the 3 DTS compound rows exercise Issue 1.

- [ ] **Step 2: Run the new rows and verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: 11 new failures. The 5 simple audio rows fail because the tags are not in the list. The `DD5.1` row fails because the dot is replaced with a space before the tag loop runs (Issue 2). The 3 DTS compound rows fail because `\bDTS\b` strips `DTS` from `DTS-HD` first (Issue 1). The 2 lossless rows fail because the tags are not in the list.

- [ ] **Step 3: Reorder the cleaner and add the new audio tags**

Two source changes are required: (a) reorder the cleaner so the tag loop runs before the dot/underscore replacement (fixes Issue 2), and (b) reorder `QualityTags` so the DTS compound forms come before the bare `DTS` and add the new audio tags (fixes Issue 1).

**Change (a) — reorder the cleaner in `Clean()`:**

The current method body is:

```csharp
        // Step 3: Replace dots and underscores with spaces
        name = name.Replace('.', ' ').Replace('_', ' ');

        // Step 4: Remove quality/codec tags
        foreach (var tag in QualityTags)
        {
            name = Regex.Replace(name, $@"\b{Regex.Escape(tag)}\b", " ", RegexOptions.IgnoreCase);
        }
```

Move the tag-loop block (the `foreach`) ABOVE the dot/underscore replacement. After the move, the body should read:

```csharp
        // Step 3: Remove quality/codec tags
        foreach (var tag in QualityTags)
        {
            name = Regex.Replace(name, $@"\b{Regex.Escape(tag)}\b", " ", RegexOptions.IgnoreCase);
        }

        // Step 4: Replace dots and underscores with spaces
        name = name.Replace('.', ' ').Replace('_', ' ');
```

The comments can stay; only the order of the two blocks changes. The tag loop now runs against the original (dot-preserved) filename, so `DD5.1`, `DD7.1`, and any future tag containing a `.` or `-` will work.

**Change (b) — reorder DTS compound forms and add new audio tags in `QualityTags`:**

The DTS compound forms must be processed BEFORE the bare `DTS` so the `\bDTS\b` regex does not strip `DTS` from `DTS-HD` first. Insert a new line BEFORE the existing audio line, and a new line AFTER.

In `FilenameCleanerService.cs`, INSERT a new line BEFORE line 14:

```csharp
        "DTSHD", "DTS-HD", "DTSX", "DTS-X",
```

Resulting block (lines 14-15):

```csharp
        "DTSHD", "DTS-HD", "DTSX", "DTS-X",
        "AAC", "AC3", "DTS", "MP3", "TrueHD", "Atmos",
```

Then APPEND a new line AFTER the existing audio line (after the new line 15):

```csharp
        "Opus", "FLAC", "EAC3", "DDP", "DD", "DD5.1", "DD7.1", "LPCM", "PCM",
```

The cleaner already escapes tag chars via `Regex.Escape`, so the dashes in `DTS-HD` and `DTS-X` are safe. The `Regex.Replace` in `Clean()` (now at line 53 after the reorder) builds `\bDTS\-HD\b` which matches the literal `DTS-HD` and the word boundary before the next char. With the compound forms listed first, they are stripped before the bare `DTS` is ever evaluated; the bare `DTS` then has no `DTS` substring left to match in compound tokens.

- [ ] **Step 4: Re-run and verify pass**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: all 22 rows pass (11 from Task 1 + 11 from Task 2). Verify that:
- `DD5.1` is stripped (Issue 2 fixed by cleaner reorder).
- `DTS-HD`, `DTSHD`, `DTS-X` are stripped without leaving `-HD`, `HD`, or `-X` behind (Issue 1 fixed by QualityTags reorder).

---

### Task 3: Add bit depth, source variants, and release/edition flags

**Files:**
- Modify: `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs` (append)
- Modify: `FilenameTitlePlugin/FilenameCleanerService.cs` (append after line 16)

- [ ] **Step 1: Add failing test rows**

Append after the rows added in Task 2:

```csharp
[InlineData("Movie.2024.1080p.BluRay.x264.10bit.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264.8bit.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264.Hi10P.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.REMUX.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.HDLight.mkv", "Movie 2024")]
[InlineData("Movie.1972.1080p.BluRay.REMASTERED.mkv", "Movie 1972")]
[InlineData("Movie.1972.1080p.BluRay.REMASTER.mkv", "Movie 1972")]
[InlineData("Movie.2024.1080p.BluRay.RESTORED.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.UNCUT.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.LIMITED.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.IMAX.mkv", "Movie 2024")]
```

`FINAL` is intentionally **excluded** per user request.

- [ ] **Step 2: Run the new rows and verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: 11 new failures.

- [ ] **Step 3: Add the tags**

In `FilenameCleanerService.cs`, line 16 currently reads:

```csharp
"PROPER", "REPACK", "EXTENDED", "THEATRICAL", "UNRATED",
```

Append a new line after it (still inside the array):

```csharp
        "REMUX", "HDLight", "REMASTERED", "REMASTER", "RESTORED", "UNCUT", "LIMITED", "IMAX",
```

Then modify line 13 (video codec area) to add the bit-depth tags. The line currently reads:

```csharp
"x264", "x265", "H264", "H265", "HEVC", "AVC",
"XviD", "DivX", "AV1", "VP9",
```

Add a new line after Task 1's additions:

```csharp
        "10bit", "8bit", "Hi10P", "Hi10", "Hi444",
```

Resulting codec block (lines 13-15):

```csharp
        "x264", "x265", "H264", "H265", "HEVC", "AVC",
        "XviD", "DivX", "AV1", "VP9",
        "10bit", "8bit", "Hi10P", "Hi10", "Hi444",
```

- [ ] **Step 4: Re-run and verify pass**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: all 33 rows pass.

---

### Task 4: Add language / source markers

**Files:**
- Modify: `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs` (append)
- Modify: `FilenameTitlePlugin/FilenameCleanerService.cs:17` (append after the release-flag line)

- [ ] **Step 1: Add failing test rows**

Append after the rows added in Task 3:

```csharp
[InlineData("Movie.2024.1080p.BluRay.MULTi.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DUAL.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.VOSTFR.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.FRENCH.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.GERMAN.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.NORDiC.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.SUBBED.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.DUBBED.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.LATIN.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.JAPANESE.mkv", "Movie 2024")]
```

- [ ] **Step 2: Run the new rows and verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: 10 new failures.

- [ ] **Step 3: Add the tags**

In `FilenameCleanerService.cs`, line 17 currently reads:

```csharp
"COMPLETE", "INTERNAL"
```

Replace it with the existing two tokens followed by a new line of language markers (still inside the array):

```csharp
        "COMPLETE", "INTERNAL",
        "MULTi", "MULTiLANG", "DUAL", "DUALAUDIO", "SUBBED", "DUBBED", "DUB", "VOSTFR",
        "FRENCH", "GERMAN", "ITALIAN", "SPANISH", "LATIN", "JAPANESE", "KOREAN", "CHINESE",
        "RUSSIAN", "HINDI", "TAMIL", "TELUGU", "ARABIC", "PORTUGUESE", "BRAZILIAN",
        "POLISH", "TURKISH", "DUTCH", "NORDiC", "NORDIC"
```

The `MULTi` and `NORDiC` tokens use mixed case as that is the canonical scene-release form. The `Regex.Replace` (line 53) is case-insensitive, so `multi` and `Multi` will also match. No need to add `MULTI` separately.

- [ ] **Step 4: Re-run and verify pass**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: all 43 rows pass.

---

### Task 5: Add release-group prefixes

**Files:**
- Modify: `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs` (append)
- Modify: `FilenameTitlePlugin/FilenameCleanerService.cs` (append final line of tags)

- [ ] **Step 1: Add failing test rows**

Append after the rows added in Task 4:

```csharp
[InlineData("Movie.2024.1080p.BluRay.x264-YIFY.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-RARBG.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-NTb.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-FLUX.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-ION10.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-EDITH.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-AMIABLE.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-EVO.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-PSA.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-GALAXY.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-MONKEE.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-DEFLATE.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-FoV.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-WAFFLES.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-SiRiUs.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-ETHEL.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-DUST.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-AIDA.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-ROVERS.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-NOSCREENS.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-SbR.mkv", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.x264-NOGRP.mkv", "Movie 2024")]
```

**Heuristic note:** some of these group names (e.g. `DUST`, `GHOUL`, `ROVERS`) are also real English words and could appear in a movie title. The cleaner cannot distinguish a group tag from a title token; this is the same trade-off the existing list already accepts with `PROPER`, `REPACK`, `INTERNAL`, etc. Real users who hit a false positive can edit the array.

- [ ] **Step 2: Run the new rows and verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: 22 new failures (the group names appear in the expected output as `-YIFY`, `-RARBG`, etc., and will not be stripped until added to the array).

- [ ] **Step 3: Add the group tags**

In `FilenameCleanerService.cs`, the array currently ends at line 18 with `];`. Modify line 17 to add the group-prefix tokens as a final line inside the array. The end of the array should read:

```csharp
        "COMPLETE", "INTERNAL",
        "MULTi", "MULTiLANG", "DUAL", "DUALAUDIO", "SUBBED", "DUBBED", "DUB", "VOSTFR",
        "FRENCH", "GERMAN", "ITALIAN", "SPANISH", "LATIN", "JAPANESE", "KOREAN", "CHINESE",
        "RUSSIAN", "HINDI", "TAMIL", "TELUGU", "ARABIC", "PORTUGUESE", "BRAZILIAN",
        "POLISH", "TURKISH", "DUTCH", "NORDiC", "NORDIC",
        "YIFY", "YTS", "RARBG", "PSA", "NTb", "FLUX", "ION10", "EDITH", "AMIABLE",
        "DEFLATE", "GALAXY", "NOGRP", "MONKEE", "FoV", "WAFFLES", "MIRCrew", "SiRiUs",
        "Rets", "PublicHD", "EVO", "ETHEL", "DUST", "AIDA", "SHORTBRE", "GNOME", "GHOUL",
        "ROVERS", "SbR", "VETO", "NOSCREENS", "FLAR"
    ];
```

- [ ] **Step 4: Re-run and verify partial pass (group tags stripped, trailing dash remains)**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: the 22 new rows STILL FAIL, but for a different reason than step 2. The group tag is now stripped, but the hyphen between `x264` and the group name remains. The actual output is `Movie 2024 -` (with a trailing dash), not the expected `Movie 2024`. The original 43 rows from Tasks 1-4 still pass. **If any of the 22 rows still show the group tag in the actual output, stop — the tag was not added correctly.**

- [ ] **Step 5: Strip leading/trailing punctuation from the cleaner output**

The cleaner (`FilenameCleanerService.cs:60`) collapses multiple spaces and trims, but it does not strip leading or trailing non-word characters. The hyphen between `x264` and the group name is preserved, leaving `Movie 2024 -` in the final result.

Replace the single line in `Clean()` that currently reads:

```csharp
        name = ExtraSpacesRegex.Replace(name, " ").Trim();
```

with two lines (the `Trim()` is moved inside the new regex, which also strips whitespace, so it does the work of both the old `Trim()` and the new punctuation strip in one pass):

```csharp
        name = ExtraSpacesRegex.Replace(name, " ");
        name = LeadingTrailingPunctRegex.Replace(name, string.Empty);
```

and add a new `Regex` field next to the existing ones (after `ExtraSpacesRegex`, before the `Clean` method):

```csharp
    // Strips leading and trailing whitespace + punctuation (hyphen, period, underscore, slash) left over after tag stripping
    private static readonly Regex LeadingTrailingPunctRegex = new(
        @"^[\s\-._/]+|[\s\-._/]+$",
        RegexOptions.Compiled);
```

The character class `[\s\-._/]` matches whitespace (`\s`) plus `-._/`. Anchored to start (`^...+`) or end (`...+$`) so only leading and trailing runs are removed — internal punctuation (e.g. the dash in `Spider-Man`) is preserved.

**Why this is safe:** the existing tests do not produce any leading or trailing punctuation in the cleaned name, so this change does not alter any existing passing test. The behavior of the old `Trim()` (which removed leading/trailing whitespace) is preserved by including `\s` in the new regex's character class, and punctuation stripping is added on top. The new behavior is exercised by all 22 release-group test rows added in step 1 of this task.

- [ ] **Step 6: Re-run and verify all pass**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: all 65 rows pass (43 from Tasks 1-4 + 22 from Task 5).

---

### Task 6: Add a regression-guard `[Fact]` for real English words in titles

**Files:**
- Modify: `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs` (append new `[Fact]` before the closing brace of the class)

- [ ] **Step 1: Add the new `[Fact]`**

In `FilenameCleanerServiceTests.cs`, after the `Clean_MultipleSpacesCollapsed` `[Fact]` (which ends at line 45) and before the class's closing `}` (line 46), insert:

```csharp

    [Fact]
    public void Clean_RealEnglishWordsInTitle_NotStripped()
    {
        Assert.Equal("The King 2022", _sut.Clean("The.King.2022.1080p.BluRay.x264.mkv"));
    }
```

- [ ] **Step 2: Run the new `[Fact]` and verify it passes**

```bash
dotnet test --filter "FullyQualifiedName~Clean_RealEnglishWordsInTitle_NotStripped"
```

Expected: PASS. `KING` is not in the new tag list, so it must survive. This guard will fail in the future if someone adds `KING` (or any other real title word) to the tag list.

- [ ] **Step 3: (no source change)**

This task is test-only. No edits to `FilenameCleanerService.cs`.

- [ ] **Step 4: Re-run full test suite to confirm nothing regressed**

```bash
dotnet test --filter "FullyQualifiedName~Clean_"
```

Expected: 71 `[InlineData]` rows on `Clean_ReturnsExpectedTitle` (7 pre-existing + 64 new from Tasks 1-8) + 5 `[Fact]` methods (4 pre-existing + 1 new from Task 6) = **76 passing tests**.

---

### Task 7: Expand site-name TLD set

**Files:**
- Modify: `FilenameTitlePlugin/FilenameCleanerService.cs:23-25`
- Modify: `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs` (append new `[InlineData]` rows)

- [ ] **Step 1: Add failing test rows**

In `FilenameCleanerServiceTests.cs`, append after the rows added in Task 5:

```csharp
[InlineData("torrentleech.org_Movie.2024.1080p.BluRay.mkv", "Movie 2024")]
[InlineData("www.spa-tracker.be_Movie.2024.1080p.BluRay.mkv", "Movie 2024")]
[InlineData("privatehd.to_Movie.2024.1080p.BluRay.mkv", "Movie 2024")]
[InlineData("rargb.to_Movie.2024.1080p.BluRay.mkv", "Movie 2024")]
```

`rargb.to` and `privatehd.to` would already match the current `.to` TLD; they are included as positive controls to confirm the existing behavior. The first two rows exercise the new TLDs and should fail until the regex is updated.

- [ ] **Step 2: Run the new rows and verify the new-TLD rows fail**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: at least the first two new rows fail (the `.be` and `.org` rows); the `.to` rows may already pass.

- [ ] **Step 3: Update the `SiteNameRegex`**

In `FilenameCleanerService.cs`, line 23-25 currently reads:

```csharp
private static readonly Regex SiteNameRegex = new(
    @"(?:www\.)?(?:\w+\.)*\w+\.(to|com|net|org|io|tv)(?=[_.\s]|$)",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);
```

Replace the alternation group with the broader set:

```csharp
private static readonly Regex SiteNameRegex = new(
    @"(?:www\.)?(?:\w+\.)*\w+\.(to|com|net|org|io|tv|me|ws|nu|de|nl|es|it|fr|pl|ru|se|no|dk|fi|is|cz|sk|hu|ro|bg|gr|tr|ua|jp|kr|cn|hk|tw|in|id|th|my|ph|vn|mx|br|ar|cl|co|pe|ve|uy|be)(?=[_.\s]|$)",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);
```

**Caution:** the regex still matches any `word.word.tld` sequence followed by `_ . space` or end-of-string. A filename like `My.File.Name.2024.1080p.mkv` does **not** trigger it because the year is followed by `.1080p` and the regex requires the TLD to be at a boundary; the `1080p` chunk is itself `\w+` so the engine will not stop at `2024` as if it were a TLD. (Verified via manual trace.) If a TLD-listed word appears as the second-to-last dot-separated token of the title and is followed by the year, it can over-match — e.g. `My.movie.de.2024.1080p.mkv` would be read as site `movie.de` followed by `2024`. This is an existing limitation, not a regression.

- [ ] **Step 4: Re-run and verify pass**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: all rows pass, including `My.movie.de.2024.1080p.mkv` → `My movie 2024 1080p` if present (it is not added in this task; mentioned only as a caveat).

---

### Task 8: Add `.iso` and `.img` to known video extensions

**Files:**
- Modify: `FilenameTitlePlugin/FilenameCleanerService.cs:20-21`
- Modify: `FilenameTitlePlugin.Tests/FilenameCleanerServiceTests.cs` (append new `[InlineData]` rows)

- [ ] **Step 1: Add failing test rows**

In `FilenameCleanerServiceTests.cs`, append after the rows added in Task 7:

```csharp
[InlineData("Movie.2024.1080p.BluRay.iso", "Movie 2024")]
[InlineData("Movie.2024.1080p.BluRay.img", "Movie 2024")]
```

- [ ] **Step 2: Run the new rows and verify they fail**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: 2 new failures — `.iso` and `.img` are not in `VideoExtensions`, so the cleaner falls through to `Path.GetFileName(filename)` (line 42) and the result still contains the extension.

- [ ] **Step 3: Update `VideoExtensions`**

In `FilenameCleanerService.cs`, line 20-21 currently reads:

```csharp
private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".m4v", ".ts", ".m2ts", ".webm", ".mpg", ".mpeg" };
```

Replace with:

```csharp
private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    { ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".m4v", ".ts", ".m2ts", ".webm", ".mpg", ".mpeg", ".iso", ".img" };
```

**Side effect to be aware of:** the safety rule in `Plugin.cs:43` and `TitleUpdaterTask.cs:60` uses `Path.GetFileNameWithoutExtension(item.Path)` to compute the "raw name" to compare against `item.Name`. With `.iso`/`.img` now recognized, the safety check works identically — items whose title is still the bare filename (e.g. `Movie.2024.1080p.BluRay`) will be eligible for cleaning even on `.iso`/`.img` files. This is the intended behavior; no code change needed outside this task.

- [ ] **Step 4: Re-run and verify pass**

```bash
dotnet test --filter "FullyQualifiedName~Clean_ReturnsExpectedTitle"
```

Expected: all rows pass.

---

### Task 9: Final full build + full test run

**Files:** None modified.

- [ ] **Step 1: Build the solution**

```bash
dotnet build
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` (warnings from the Jellyfin package references are expected and unrelated).

- [ ] **Step 2: Run the full test suite**

```bash
dotnet test
```

Expected: all **76** tests pass (71 `[InlineData]` rows on `Clean_ReturnsExpectedTitle` — 7 pre-existing + 64 new from Tasks 1-8 — plus 5 `[Fact]` methods: `Clean_ExtensionOnly_ReturnsEmpty`, `Clean_YearInBrackets_UnwrappedNotRemoved`, `Clean_YearInSquareBrackets_UnwrappedNotRemoved`, `Clean_MultipleSpacesCollapsed`, `Clean_RealEnglishWordsInTitle_NotStripped`). The test runner's summary line should read something like `Passed!  - Failed: 0, Passed: 76, Skipped: 0, Total: 76`.

- [ ] **Step 3: (Stop here — do not commit)**

Per the user-level `AGENTS.md` rule, **do not run `git add` or `git commit`**. Stop after the test run. The user will inspect the diff and commit manually.

---

## Verification matrix (at a glance)

After all 9 tasks complete, the following should be true:

| Check | Command | Expected |
|---|---|---|
| Solution builds | `dotnet build` | 0 errors |
| All tests pass | `dotnet test` | 76 passed, 0 failed |
| `QualityTags` contains new tokens | `grep` for `YIFY`, `Opus`, `10bit`, `MULTi`, `REMUX`, `REMASTERED` in `FilenameCleanerService.cs` | matches found |
| `VideoExtensions` contains `.iso`/`.img` | `grep` for `".iso"`, `".img"` in `FilenameCleanerService.cs` | matches found |
| `SiteNameRegex` TLD set expanded | `grep` for `.be` in `FilenameCleanerService.cs` | match found |
| Negative guard exists | `grep` for `Clean_RealEnglishWordsInTitle_NotStripped` in `FilenameCleanerServiceTests.cs` | match found |
| No stray `FINAL` in new tags | `grep` for `"FINAL"` in `FilenameCleanerService.cs` | no matches inside `QualityTags` |

## Out of scope (intentionally not changed)

- Multi-episode handling (`S03E05-E08`, `S03E05.06.07`) — requires logic, not just a tag list edit.
- Trailer/sample/featurette suffixes (`-Trailer`, `-Sample`) — heuristic; out of scope.
- File extension tokens inside the name (e.g. `the.mkv.movie.2024.1080p.mkv`) — current `\b` regex does not match `mkv` adjacent to `.movie`; requires separate code change.
- `install.sh` — out of scope; the `1.0.0.0` vs `1.1.0.0` version mismatch noted in `AGENTS.md` is pre-existing.
- README token table — representative, not exhaustive; not updated in this pass.

## Self-review

- **Spec coverage:** All 5 deliverables from the prior plan are covered (video codecs → Task 1, audio codecs → Task 2, bit depth + source variants + release flags → Task 3, language markers → Task 4, group prefixes → Task 5). TLD expansion → Task 7. `.iso`/`.img` extensions → Task 8. Negative test → Task 6. Final verification → Task 9.
- **Placeholder scan:** No `TBD`, `TODO`, "implement later", "add appropriate error handling", "similar to Task N" (each task repeats the exact test/code rather than referencing), or empty code blocks.
- **Type consistency:** All test method names use the `Clean_*` prefix established in the existing file. All tag additions go into the same `QualityTags` array declared in `FilenameCleanerService.cs:7`. The TLD change stays on the same `SiteNameRegex` field declared on `FilenameCleanerService.cs:23`. Extension additions go into `VideoExtensions` declared on `FilenameCleanerService.cs:20`. The new `LeadingTrailingPunctRegex` (added in Task 5 step 5) is a new field declared next to the existing `ExtraSpacesRegex` and follows the same pattern (private static readonly `Regex` with `RegexOptions.Compiled`). The `Clean()` method body is reordered in Task 2 step 3 (tag loop moved above dot/underscore replacement) but no method signature or field declaration changes. No renames; no method-signature drift.
- **Excluded `FINAL`:** Confirmed not present in any new code block.

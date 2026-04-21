---
title: "feat: Smart CSV Detection Backend"
type: feat
status: active
date: 2026-04-21
origin: docs/tasks/031-smart-csv-detection-backend.md
---

# feat: Smart CSV Detection Backend

## Overview

Replace the current hard-coded English column mapping in `CsvImporter` with a two-layer detection system. Layer 1 attempts fast rule-based pattern matching (no AI cost). If confidence falls below 85%, Layer 2 sends a 5-line CSV sample to Azure OpenAI for structure analysis. Detection results drive delimiter, culture, and column name resolution during parsing.

## Problem Frame

`CsvImporter.cs` assumes comma-delimited files with English column headers and `InvariantCulture` number/date parsing. This fails immediately for international bank formats (Portuguese semicolons, German pipes, non-ASCII column names, comma-vs-dot decimal separators). The budget tracker needs to support any bank CSV format without per-bank configuration.

## Requirements Trace

- R1. Detect column delimiter automatically (comma, semicolon, pipe, tab)
- R2. Detect culture/locale for number and date parsing (e.g., `pt-PT`, `de-DE`)
- R3. Map column names for Date, Description, and Amount regardless of language
- R4. Rule-based detection handles standard English formats without an AI call
- R5. AI detection handles unknown/international formats as fallback
- R6. Confidence scoring: reject with `400 BadRequest` if score < 85
- R7. Import result includes detection method and confidence for transparency

## Scope Boundaries

- Frontend detection UI (`032-smart-csv-detection-ui.md`) is out of scope for this plan
- No changes to the enhance (`/import/enhance`) endpoint
- No new database migrations required

## Context & Research

### Relevant Code and Patterns

- `src/BudgetTracker.Api/Features/Transactions/Import/Processing/CsvImporter.cs` — current parser, 4-argument signature to keep backward-compatible
- `src/BudgetTracker.Api/Features/Transactions/Import/ImportApi.cs` — minimal API entry point, injection via method parameters
- `src/BudgetTracker.Api/Features/Transactions/Import/ImportResult.cs` — DTO to extend with detection fields
- `src/BudgetTracker.Api/Features/Transactions/Import/Enhancement/TransactionEnhancer.cs` — existing `IChatClient` injection pattern to replicate in `CsvAnalyzer`
- `src/BudgetTracker.Api/Program.cs` — `IChatClient` already registered as singleton; add three new scoped registrations here

### Institutional Learnings

- AI response text may wrap JSON in ` ```json ``` ` blocks — a shared extension method is needed to strip the wrapper before `JsonDocument.Parse()`

## Key Technical Decisions

- **Overload, don't replace**: Keep the 4-argument `ParseCsvAsync` signature and add a 5-argument overload accepting `CsvStructureDetectionResult?`. Null means fall back to English defaults — preserves any callers that don't go through detection.
- **Detection before stream consumption**: `DetectStructureAsync` reads the stream, then `ImportApi` resets `stream.Position = 0` before passing it to the importer.
- **Confidence threshold is 85**: Matches the spec exactly. Below this, return `BadRequest` regardless of detection method.
- **`StringExtensions` goes in `Infrastructure/Extensions/`**: This utility is shared — it will also be used by the future image importer (`033`).
- **`ICsvAnalyzer` lives in `Processing/`, not `Detection/`**: It wraps the raw AI call without parsing; `CsvDetector` (in `Detection/`) owns the JSON interpretation layer.

## Open Questions

### Resolved During Planning

- **Where does `ICsvAnalyzer` live?** In `Processing/` per the task spec — it's a raw-IO concern, parallel to `CsvImporter`, not a detection-strategy concern.
- **Should the 4-argument overload call the 5-argument one?** Yes — forwards with `null` so no duplication.

### Deferred to Implementation

- **AI prompt tuning**: The prompt in the task spec is a starting point. Exact wording may need adjustment after testing against real bank files.
- **Tab-delimiter handling**: The spec notes `"\\t"` must be converted to `"\t"` in `CsvDetector`. Verify CsvHelper accepts a string delimiter (vs. char) at runtime.

## Output Structure

    src/BudgetTracker.Api/
    ├── Features/Transactions/Import/
    │   ├── Detection/
    │   │   ├── CsvStructureDetectionResult.cs   (new)
    │   │   ├── ColumnMappingDictionary.cs        (new)
    │   │   ├── ICsvStructureDetector.cs          (new)
    │   │   ├── ICsvDetector.cs                   (new)
    │   │   ├── CsvDetector.cs                    (new)
    │   │   └── CsvStructureDetector.cs           (new)
    │   └── Processing/
    │       ├── ICsvAnalyzer.cs                   (new)
    │       └── CsvAnalyzer.cs                    (new)
    └── Infrastructure/Extensions/
        └── StringExtensions.cs                   (new)

## High-Level Technical Design

> *This illustrates the intended approach and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce.*

```
ImportApi.ImportAsync
  │
  ├─ ICsvStructureDetector.DetectStructureAsync(stream)
  │     │
  │     ├─ TrySimpleParsing(stream)
  │     │     ├─ Read header line, split on ","
  │     │     ├─ Match against ColumnMappingDictionary
  │     │     ├─ Parse 3 sample rows (date + amount)
  │     │     └─ ConfidenceScore = successRate * 100
  │     │
  │     ├─ if confidence >= 85 → return RuleBased result
  │     │
  │     └─ else → ICsvDetector.AnalyzeCsvStructureAsync(stream)
  │                 │
  │                 └─ ICsvAnalyzer.AnalyzeCsvStructureAsync(5-line sample)
  │                       └─ IChatClient → JSON → CsvStructureDetectionResult
  │
  ├─ if confidence < 85 → BadRequest
  │
  ├─ stream.Position = 0
  └─ CsvImporter.ParseCsvAsync(stream, ..., detectionResult)
        ├─ CsvConfiguration.Delimiter = detectionResult.Delimiter
        ├─ column lookup via ColumnMappings + English fallbacks
        ├─ TryParseDate uses detectionResult.CultureCode
        └─ TryParseAmount uses detectionResult.CultureCode
```

## Implementation Units

- [ ] **Unit 1: Detection result types**

  **Goal:** Define the shared data contract for all detection services.

  **Requirements:** R1, R2, R3, R6, R7

  **Dependencies:** None

  **Files:**
  - Create: `src/BudgetTracker.Api/Features/Transactions/Import/Detection/CsvStructureDetectionResult.cs`

  **Approach:**
  - `CsvStructureDetectionResult` with properties: `Delimiter` (default `","`), `ColumnMappings` (`Dictionary<string, string>`), `CultureCode` (default `"en-US"`), `ConfidenceScore` (`double`), `DetectionMethod` (`DetectionMethod` enum)
  - `DetectionMethod` enum: `RuleBased`, `AI`

  **Test scenarios:**
  - Test expectation: none — pure data container, no behavior

  **Verification:** File compiles; both detection classes can instantiate and populate this type.

---

- [ ] **Unit 2: Column mapping dictionary**

  **Goal:** Centralize English column-name patterns for rule-based detection.

  **Requirements:** R3, R4

  **Dependencies:** Unit 1

  **Files:**
  - Create: `src/BudgetTracker.Api/Features/Transactions/Import/Detection/ColumnMappingDictionary.cs`

  **Approach:**
  - Static class with three `string[]` fields: `DateColumns`, `DescriptionColumns`, `AmountColumns`
  - Values from the task spec: e.g. `["Date", "Transaction Date", "Posting Date", "Value Date", "Txn Date"]`

  **Test scenarios:**
  - Test expectation: none — static lookup table, no behavior

  **Verification:** Referenced successfully by `CsvStructureDetector`.

---

- [ ] **Unit 3: Detector interfaces**

  **Goal:** Define the interface contracts for the orchestrator and the AI detector.

  **Requirements:** R4, R5

  **Dependencies:** Unit 1

  **Files:**
  - Create: `src/BudgetTracker.Api/Features/Transactions/Import/Detection/ICsvStructureDetector.cs`
  - Create: `src/BudgetTracker.Api/Features/Transactions/Import/Detection/ICsvDetector.cs`

  **Approach:**
  - `ICsvStructureDetector`: `Task<CsvStructureDetectionResult> DetectStructureAsync(Stream csvStream)`
  - `ICsvDetector`: `Task<CsvStructureDetectionResult> AnalyzeCsvStructureAsync(Stream csvStream)`

  **Test scenarios:**
  - Test expectation: none — interfaces only

  **Verification:** Both implemented by concrete classes without compilation errors.

---

- [ ] **Unit 4: AI analyzer service**

  **Goal:** Wrap the raw Azure OpenAI call for CSV structure analysis.

  **Requirements:** R1, R2, R3, R5

  **Dependencies:** Unit 1

  **Files:**
  - Create: `src/BudgetTracker.Api/Features/Transactions/Import/Processing/ICsvAnalyzer.cs`
  - Create: `src/BudgetTracker.Api/Features/Transactions/Import/Processing/CsvAnalyzer.cs`

  **Approach:**
  - `ICsvAnalyzer`: `Task<string> AnalyzeCsvStructureAsync(string csvContent)`
  - `CsvAnalyzer` injects `IChatClient` (already registered as singleton)
  - System prompt: "You are a CSV structure analysis expert…"
  - User prompt lists the 7 things to identify and specifies the exact JSON response shape (from task spec)
  - Returns raw response text — JSON parsing happens in `CsvDetector`

  **Patterns to follow:**
  - `src/BudgetTracker.Api/Features/Transactions/Import/Enhancement/TransactionEnhancer.cs` — `IChatClient.GetResponseAsync` call pattern

  **Test scenarios:**
  - Happy path: given 5 CSV lines with semicolons and Portuguese headers → returns non-empty string containing JSON
  - Error path: `IChatClient` throws → surface the exception (caller handles it)

  **Verification:** `CsvAnalyzer` can be constructed in tests with a substituted `IChatClient`.

---

- [ ] **Unit 5: JSON extraction extension**

  **Goal:** Strip ` ```json ``` ` wrappers from AI responses before JSON parsing.

  **Requirements:** R5 (defensive AI response handling)

  **Dependencies:** None

  **Files:**
  - Create: `src/BudgetTracker.Api/Infrastructure/Extensions/StringExtensions.cs`

  **Approach:**
  - `public static string ExtractJsonFromCodeBlock(this string input)`
  - If input does not contain ` ```json ` → return input unchanged
  - Extract via regex ```` ```json\s*([\s\S]*?)\s*``` ````
  - Throw `FormatException` only if ` ```json ` is present but regex fails to match

  **Test scenarios:**
  - Happy path: input with ` ```json {...} ``` ` wrapper → returns the inner JSON string
  - Happy path: input with no code block wrapper → returns input unchanged
  - Error path: input starts with ` ```json ` but has no closing ` ``` ` → throws `FormatException`

  **Verification:** Unit tests pass for all three scenarios.

---

- [ ] **Unit 6: AI detection service (`CsvDetector`)**

  **Goal:** Orchestrate the AI call and parse the JSON response into a detection result.

  **Requirements:** R1, R2, R3, R5

  **Dependencies:** Units 1, 4, 5

  **Files:**
  - Create: `src/BudgetTracker.Api/Features/Transactions/Import/Detection/CsvDetector.cs`

  **Approach:**
  - Reads first 5 non-empty lines from stream
  - Calls `ICsvAnalyzer.AnalyzeCsvStructureAsync(csvContent)`
  - Calls `ExtractJsonFromCodeBlock()` on the response
  - Parses JSON: `columnSeparator`, `cultureCode`, `dateColumn`, `descriptionColumn`, `amountColumn`, `confidenceScore`
  - Handles `"\\t"` → `"\t"` for tab delimiter
  - On any exception → log warning, return `ConfidenceScore = 0, DetectionMethod = AI`
  - Always sets `DetectionMethod = AI`

  **Patterns to follow:**
  - `src/BudgetTracker.Api/Features/Transactions/Import/Enhancement/TransactionEnhancer.cs` — logging + fallback pattern

  **Test scenarios:**
  - Happy path: AI returns valid JSON with semicolon delimiter, `pt-PT`, known column names → result has correct values
  - Edge case: AI wraps JSON in ` ```json ``` ` block → still parses correctly via `ExtractJsonFromCodeBlock`
  - Edge case: `columnSeparator` is `"\\t"` → `Delimiter` resolves to `"\t"`
  - Error path: `ICsvAnalyzer` throws → returns `ConfidenceScore = 0`
  - Error path: AI returns malformed JSON → returns `ConfidenceScore = 0`
  - Edge case: empty CSV stream → returns `ConfidenceScore = 0`

  **Verification:** Substituted `ICsvAnalyzer` in unit tests; all scenarios pass.

---

- [ ] **Unit 7: Rule-based + orchestrator (`CsvStructureDetector`)**

  **Goal:** Implement the two-layer detection orchestrator.

  **Requirements:** R1, R2, R3, R4, R5, R6

  **Dependencies:** Units 1, 2, 3, 6

  **Files:**
  - Create: `src/BudgetTracker.Api/Features/Transactions/Import/Detection/CsvStructureDetector.cs`

  **Approach:**
  - `TrySimpleParsing(stream)`:
    - Read up to 100 lines
    - Split header on `","`, match each column against `ColumnMappingDictionary` (case-insensitive)
    - If Date, Description, or Amount not found → `ConfidenceScore = 0`
    - Parse up to 3 data rows: try `DateTime.TryParse` (InvariantCulture) + `decimal.TryParse` (InvariantCulture after stripping `$`)
    - `ConfidenceScore = (successfulParses / totalSamples) * 100`; if no data rows found but columns matched → 85
  - `DetectStructureAsync`: call `TrySimpleParsing`; if `>= 85` return it; else reset stream, delegate to `ICsvDetector`

  **Test scenarios:**
  - Happy path: standard English CSV (`Date,Description,Amount,Balance`) with 3 valid rows → `RuleBased`, confidence 100
  - Happy path: English headers found but no data rows (header-only file) → `RuleBased`, confidence 85
  - Edge case: only 1 of 3 required columns found → `ConfidenceScore = 0`, falls through to AI
  - Edge case: date column found but all 3 sample rows have unparseable dates → low confidence, AI fallback
  - Integration: Portuguese CSV (semicolons, non-English headers) → rule-based returns 0, AI detector called

  **Verification:** With substituted `ICsvDetector`, rule-based path returns correct result; AI path is called when rule confidence < 85.

---

- [ ] **Unit 8: Culture-aware `CsvImporter` overload**

  **Goal:** Make the importer accept and use detection results for flexible parsing.

  **Requirements:** R1, R2, R3

  **Dependencies:** Unit 1

  **Files:**
  - Modify: `src/BudgetTracker.Api/Features/Transactions/Import/Processing/CsvImporter.cs`

  **Approach:**
  - Keep existing 4-argument signature; it calls the new 5-argument overload with `null`
  - New 5-argument overload: `ParseCsvAsync(..., CsvStructureDetectionResult? detectionResult)`
  - `CsvConfiguration.Delimiter = detectionResult?.Delimiter ?? ","`
  - Add `GetColumnValueWithDetection()`: check `detectionResult.ColumnMappings[key]` first, then fall back to English default list
  - Update `TryParseDate()`: build `CultureInfo` from `detectionResult?.CultureCode`; try culture-specific parse, then `InvariantCulture` fallback
  - Update `TryParseAmount()` → `TryParseAmountWithCulture()`: use culture for `decimal.TryParse` with `NumberStyles.Currency`

  **Test scenarios:**
  - Happy path: `null` detectionResult → behaves identically to current implementation
  - Happy path: detection result with `Delimiter = ";"`, `CultureCode = "pt-PT"`, mapped columns → Portuguese CSV rows parse correctly
  - Happy path: detection result with `CultureCode = "de-DE"` → German decimal format (`23,45`) parses to `23.45`
  - Edge case: `detectionResult.ColumnMappings` has Date but not Amount → falls back to English Amount fallbacks
  - Edge case: invalid `CultureCode` (e.g., `"xx-XX"`) → silently falls back to `InvariantCulture`

  **Verification:** Existing English CSVs continue to import correctly; Portuguese and German test CSVs parse amounts and dates accurately.

---

- [ ] **Unit 9: Extend `ImportResult` with detection fields**

  **Goal:** Surface detection metadata in the API response.

  **Requirements:** R7

  **Dependencies:** Unit 1

  **Files:**
  - Modify: `src/BudgetTracker.Api/Features/Transactions/Import/ImportResult.cs`

  **Approach:**
  - Add to `ImportResult`:
    - `public string? DetectionMethod { get; set; }` — `"RuleBased"` or `"AI"`
    - `public double DetectionConfidence { get; set; }` — `0–100`

  **Test scenarios:**
  - Test expectation: none — pure DTO addition; verified via integration through Unit 10

  **Verification:** JSON serialization includes both new fields in API response.

---

- [ ] **Unit 10: Wire detection into `ImportApi`**

  **Goal:** Run structure detection before parsing and populate detection fields in the result.

  **Requirements:** R6, R7

  **Dependencies:** Units 3, 7, 8, 9

  **Files:**
  - Modify: `src/BudgetTracker.Api/Features/Transactions/Import/ImportApi.cs`

  **Approach:**
  - Add `ICsvStructureDetector detectionService` parameter to `ImportAsync`
  - After file validation, open stream and call `detectionService.DetectStructureAsync(stream)`
  - If `ConfidenceScore < 85`:
    - AI method → `"Unable to automatically detect CSV structure using AI analysis. Please ensure your CSV contains Date, Description, and Amount columns with recognizable headers."`
    - Rule-based method → `"Unable to automatically detect CSV structure. Please ensure your CSV file follows a standard banking format."`
    - Return `TypedResults.BadRequest(errorMessage)`
  - Reset `stream.Position = 0`
  - Pass `detectionResult` to `csvImporter.ParseCsvAsync(..., detectionResult)`
  - After import: set `result.DetectionMethod = detectionResult.DetectionMethod.ToString()` and `result.DetectionConfidence = detectionResult.ConfidenceScore`

  **Test scenarios:**
  - Happy path: standard English CSV → 200 OK, result includes `DetectionMethod: "RuleBased"`, `DetectionConfidence >= 85`
  - Happy path: Portuguese CSV → 200 OK, result includes `DetectionMethod: "AI"`
  - Error path: unrecognizable CSV → 400 BadRequest with detection error message
  - Edge case: rule-based confidence < 85, AI confidence >= 85 → import succeeds with `DetectionMethod: "AI"`

  **Verification:** Upload `samples/chase-bank-sample.csv` via Swagger → `DetectionMethod: "RuleBased"`. Upload `samples/activobank-pt-sample.csv` → `DetectionMethod: "AI"`.

---

- [ ] **Unit 11: Register detection services in DI**

  **Goal:** Register all new services with the correct lifetimes.

  **Requirements:** Structural prerequisite for all prior units

  **Dependencies:** Units 3, 4, 6, 7

  **Files:**
  - Modify: `src/BudgetTracker.Api/Program.cs`

  **Approach:**
  - Add after the existing `CsvImporter` / `ITransactionEnhancer` registrations:
    ```
    builder.Services.AddScoped<ICsvStructureDetector, CsvStructureDetector>();
    builder.Services.AddScoped<ICsvDetector, CsvDetector>();
    builder.Services.AddScoped<ICsvAnalyzer, CsvAnalyzer>();
    ```
  - `IChatClient` is already singleton — no change needed

  **Test scenarios:**
  - Test expectation: none — DI wiring; verified by application startup and integration tests

  **Verification:** `dotnet build` passes. Application starts without `InvalidOperationException` for missing service registrations.

---

## System-Wide Impact

- **Interaction graph:** Only `ImportApi.ImportAsync` is affected — `EnhanceImportAsync` is unchanged
- **Error propagation:** Detection failures return `BadRequest` before any database write occurs — no partial state
- **Stream lifecycle:** Stream position must be reset to 0 between detection and parsing; the API owns this reset
- **Unchanged invariants:** The `/import/enhance` endpoint, transaction enhancement flow, and all auth/antiforgery middleware are unmodified
- **Integration coverage:** Rule-based path must be verified with the actual `samples/chase-bank-sample.csv`; AI path with `samples/activobank-pt-sample.csv` — mocks alone won't prove culture-aware parsing

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| AI returns JSON wrapped in code block | `StringExtensions.ExtractJsonFromCodeBlock` handles this; `CsvDetector` always calls it |
| AI returns low confidence for a valid international file | Confidence threshold is AI-reported (0–100); prompt asks AI to self-assess — acceptable tradeoff per spec |
| Tab delimiter string vs. char in CsvHelper | Test at runtime; if CsvHelper requires char, convert `string` delimiter to `char[0]` |
| `stream.Position = 0` not supported (non-seekable stream) | `IFormFile.OpenReadStream()` returns a seekable `MemoryStream` for small files; confirm at runtime |

## Sources & References

- **Origin document:** [docs/tasks/031-smart-csv-detection-backend.md](docs/tasks/031-smart-csv-detection-backend.md)
- Sample CSVs: `samples/activobank-pt-sample.csv`, `samples/chase-bank-sample.csv`
- Existing AI pattern: `src/BudgetTracker.Api/Features/Transactions/Import/Enhancement/TransactionEnhancer.cs`
- CsvHelper: existing usage in `src/BudgetTracker.Api/Features/Transactions/Import/Processing/CsvImporter.cs`
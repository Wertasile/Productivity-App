# Date Parse and Byte Serialization Error

## Filename note

The requested filename `Date Parse | Byte Serialization Error` is not valid on Windows because `|` is a reserved character. This report is stored as `Date Parse - Byte Serialization Error.md`.

## Summary of changes made

### 1. Multi-day calendar task rendering

Files changed:
- `Pages/Calendar.razor`

Problem:
- `TaskItem` has `Start` and `End` values, but the calendar originally rendered a task only on the single day returned directly by the API entry for that date.
- Tasks that spanned multiple calendar days did not appear across the full date range.

Cause:
- The calendar entry builder only used the current day bucket from `calendarResponse` and did not expand task visibility across the inclusive range between `Start.Date` and `End.Date`.

Resolution:
- Added month-level task collection and de-duplication.
- Added task range helpers so a task is included on each day where:

```csharp
day.Date >= start.Date && day.Date <= end.Date
```

- Updated both the month grid and monthly feed to use the same day-aware entry generation.

Result:
- Tasks now appear on all covered days instead of only one day.

## Date parsing issues fixed

Files changed:
- `Dialog/CreateCalendarItemDialog.razor`
- `Dialog/EditTaskDialog.razor`
- `Dialog/ProjectDialog.razor`

### Error symptoms

Observed errors included:
- `Task start date and time is required.`
- `Task end date and time is required.`
- `Invalid start date and time format.`
- `Invalid end date and time format.`

These appeared even after valid values were entered in `datetime-local` inputs.

### Root causes

There were three separate but related causes.

#### Cause 1: overly strict parsing format

The original parser only accepted:

```csharp
yyyy-MM-ddTHH:mm
```

But browser `datetime-local` values can arrive as:
- `yyyy-MM-ddTHH:mm`
- `yyyy-MM-ddTHH:mm:ss`
- `yyyy-MM-ddTHH:mm:ss.fff`

When seconds or milliseconds were present, `DateTime.TryParseExact(...)` failed and validation treated the field as missing.

#### Cause 2: `ChangeEventArgs.Value` may be a `DateTime`, not a string

The original handlers used:

```csharp
e.Value?.ToString()
```

In Blazor, especially with `input type="datetime-local"`, `e.Value` can arrive as a `DateTime` object instead of a raw string.

When that happened, `.ToString()` used the current culture, producing values such as:

```text
6/3/2026 9:00:00 AM
```

That string does not match the HTML datetime-local format, so exact parsing failed.

#### Cause 3: `default(DateTime)` produced browser-invalid values

The original formatting logic returned:

```text
0001-01-01T00:00
```

for `default(DateTime)`.

Browsers can treat that as out-of-range or invalid for the input and display an empty field. That created a mismatch between what Blazor thought the value was and what the browser actually kept in the control.

### Resolution applied

#### 1. Introduced `FormatDateTimeLocalValue(object? value)`

This helper now normalizes the `@onchange` value before storing it:

```csharp
private static string FormatDateTimeLocalValue(object? value)
{
    if (value is DateTime dt)
        return dt == default ? string.Empty : dt.ToString("yyyy-MM-ddTHH:mm");

    var s = value?.ToString();
    return string.IsNullOrWhiteSpace(s) ? string.Empty : s;
}
```

This fixes the case where `e.Value` is already a `DateTime`.

#### 2. Expanded accepted exact formats

All dialogs now use a shared format set in each file:

```csharp
private static readonly string[] DateTimeLocalFormats =
{
    "yyyy-MM-ddTHH:mm",
    "yyyy-MM-ddTHH:mm:ss",
    "yyyy-MM-ddTHH:mm:ss.fff",
};
```

#### 3. Added a final `DateTime.TryParse(...)` fallback

If the exact browser-oriented formats still do not match, parsing falls back to:

```csharp
DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
```

That gives a last chance for other browser/runtime formatting variants.

#### 4. Prevented invalid default formatting

Formatting back into the input now uses:

```csharp
private static string ToDateTimeLocalValue(DateTime value)
    => value == default ? string.Empty : value.ToString("yyyy-MM-ddTHH:mm");
```

So empty or unset dates stay empty instead of rendering a bogus year 0001 value.

## Current date parsing system

### How the input fields work

These dialogs use HTML inputs with:

```html
<input type="datetime-local" ... />
```

Current pattern used in the dialogs:

```razor
<input type="datetime-local"
       value="@taskStartInput"
       @onchange="e => taskStartInput = FormatDateTimeLocalValue(e.Value)" />
```

The same pattern is used for:
- task start
- task end
- task completion
- reminder date/time
- project start
- project end

### Why parsing is needed

The `datetime-local` input ultimately gives the UI a text/date value representing local time. The app does not send that raw UI value directly into the model without validation because it needs to:

- confirm a required field is actually present
- convert the input into a `DateTime`
- validate business rules such as `end > start`
- normalize browser/runtime variations before sending data to the API

### How the parsing works now

#### Step 1: UI change is normalized

`FormatDateTimeLocalValue` takes the event value and stores a string in a predictable format.

#### Step 2: submit validates and parses

On submit, the dialog calls:

```csharp
TryParseDateTimeLocal(taskStartInput, out var start)
TryParseDateTimeLocal(taskEndInput, out var end)
```

That parser now does this:

1. Reject empty or whitespace values.
2. Try exact parsing with the accepted HTML datetime-local formats.
3. If exact parsing fails, fall back to invariant `DateTime.TryParse`.

#### Step 3: parsed `DateTime` is assigned to the model

Examples:

```csharp
taskModel.Start = start;
taskModel.End = end;
editModel.Start = start;
editModel.End = end;
```

### How values are sent to the API

After parsing, the dialogs pass strongly-typed models into the service layer:

- `ItemService.CreateItemAsync(taskModel)`
- `ItemService.UpdateItemAsync(editModel)`
- `ProjectService.UpdateProject(editModel)`

For `ItemService`, the request payload is built as an anonymous object and sent using:

```csharp
PostAsJsonAsync(...)
PutAsJsonAsync(...)
```

That means the `DateTime` values are serialized by `System.Text.Json` as JSON strings in ISO 8601 style, for example:

```json
{
  "Start": "2026-06-03T09:00:00",
  "End": "2026-06-05T17:00:00"
}
```

### Why this format is used

ISO 8601 JSON datetime strings are the standard format used by `System.Text.Json` because they are:

- language-neutral
- unambiguous for APIs
- natively supported by .NET serialization/deserialization
- consistent across the frontend and backend

Important detail:
- The UI input format and the API JSON format are related but not identical concerns.
- The UI works with HTML `datetime-local` values.
- The API works with serialized .NET `DateTime` values in JSON.
- Parsing is the step that bridges those two layers safely.

## Byte serialization error report

### Actual error observed

```text
DeserializationMustSpecifyTypeDiscriminator, BaseItem Path: $ | LineNumber: 0 | BytePositionInLine: 6.
```

### What this error actually means

Despite the wording, the problem was not the request body byte serialization for sending data.

The failure happened when the frontend tried to deserialize the API response back into `BaseItem`.

`BaseItem` is being treated as a polymorphic type. `System.Text.Json` requires a discriminator to know which concrete subtype to create, such as:
- `TaskItem`
- `Reminder`

If the backend response omits the required discriminator metadata, deserialization into `BaseItem` fails even though the API call itself succeeded.

### Where it happened

Originally this was handled incorrectly in `ItemService.CreateItemAsync`:

```csharp
return response.IsSuccessStatusCode
    ? await response.Content.ReadFromJsonAsync<BaseItem>()
    : null;
```

That meant a successful create response could still throw an exception in the client if the backend returned valid JSON without the type discriminator.

`UpdateItemAsync` had already been adjusted to catch this. `CreateItemAsync` needed the same fallback.

### Resolution

`CreateItemAsync` now behaves like `UpdateItemAsync`:

1. If the HTTP response is not successful, return `null`.
2. If the HTTP response is successful, try to deserialize `BaseItem`.
3. If deserialization fails with `JsonException` or `NotSupportedException`, return the original item that was sent.

This is safe here because:
- the server already accepted the request
- the UI reloads calendar data after save
- the caller only needs a success path, not necessarily a perfectly typed response body

### Why this resolved the issue

The frontend no longer treats a successful API response as a failure just because the response body lacks the polymorphic discriminator required by `System.Text.Json`.

## Practical outcome

After these changes:

- task create/edit datetime inputs accept real browser values reliably
- project start/end inputs parse correctly
- default/unset dates no longer poison datetime-local inputs
- multi-day tasks render across their full date range in the calendar
- successful create/update item API calls no longer fail on missing `BaseItem` discriminator metadata in the response

## Files involved

- `Pages/Calendar.razor`
- `Dialog/CreateCalendarItemDialog.razor`
- `Dialog/EditTaskDialog.razor`
- `Dialog/ProjectDialog.razor`
- `Services/Api/ItemService.cs`
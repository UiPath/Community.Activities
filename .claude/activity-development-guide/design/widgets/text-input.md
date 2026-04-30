# Text Input Widgets: TextComposer, RichTextComposer, PromptComposer

## When to read this

Read this document when you need to:
- Display a single-line or multi-line text input for a string property
- Provide a rich text (HTML) editor
- Add a prompt composer with `@` variable insertion for AI-related activities

All three widgets are used with `DesignInArgument<string>` properties.

---

## TextComposer

The `TextComposer` widget provides a text input that supports expressions. It defaults to multi-line mode.

### Multi-line (default)

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.TextComposer };
```

### Single-line

Set the `IsSingleLineFormat` metadata to restrict to a single line. Use this when the property represents a short value like a name, subject line, or identifier.

```csharp
Property.Widget = new DefaultWidget
{
    Type = ViewModelWidgetType.TextComposer,
    Metadata = new() { { TextComposerMetadata.IsSingleLineFormat, true.ToString() } }
};
```

### When to use TextComposer vs default

The default widget for `DesignInArgument<string>` is a plain expression editor. Use `TextComposer` when:
- Users are more likely to enter plain text than expressions
- The input benefits from multi-line editing (e.g., email body, message content)
- You want a richer text editing experience than the default expression box

---

## RichTextComposer

The `RichTextComposer` widget provides an HTML/rich text editor with formatting controls (bold, italic, lists, etc.).

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.RichTextComposer };
```

Use `RichTextComposer` when the property stores formatted content such as:
- Email body with HTML formatting
- Notification messages with rich text
- Document content that needs inline styling

---

## PromptComposer

The `PromptComposer` widget provides a text editor with `@` variable insertion support. When the user types `@`, a dropdown of available workflow variables appears for inline insertion.

```csharp
Property.Widget = new DefaultWidget { Type = ViewModelWidgetType.PromptComposer };
```

Use `PromptComposer` for AI/LLM-related activities where the user constructs a prompt that references workflow variables. The `@` insertion makes it easy to interpolate variables into natural language prompts without manually writing expressions.

---

## Troubleshooting

**TextComposer shows multi-line when single-line is expected.**
Verify that the metadata key is `TextComposerMetadata.IsSingleLineFormat` (not a raw string), and the value is `true.ToString()` (the string `"True"`).

**RichTextComposer content not rendering HTML at runtime.**
The `RichTextComposer` stores content as HTML. Ensure the runtime activity processes the value as HTML, not plain text. If the activity sends the value to an API, check that the API accepts HTML content.

**PromptComposer `@` dropdown is empty.**
The `@` dropdown populates from workflow variables available in the current scope. If no variables are defined in the workflow, the dropdown will be empty. This is expected behavior -- the user must first create variables in the workflow.

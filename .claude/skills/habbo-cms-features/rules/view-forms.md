---
title: Form Patterns
impact: MEDIUM
impactDescription: Incorrect form patterns cause validation failures and poor UX
tags: [views, forms, tag-helpers, validation]
---

# Form Patterns

Razor views use ASP.NET Core Tag Helpers for form generation.

## Standard Form Template

```cshtml
@model KeplerCMS.Data.Models.MyFeature

@{
    ViewBag.Title = "Create Feature";
}

<h2>Create Feature</h2>

<form asp-action="Create" method="post">
    <div class="form-group">
        <label asp-for="Name"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>

    <div class="form-group">
        <label asp-for="Description"></label>
        <textarea asp-for="Description" class="form-control" rows="5"></textarea>
        <span asp-validation-for="Description" class="text-danger"></span>
    </div>

    <div class="form-group">
        <label asp-for="IsActive"></label>
        <input asp-for="IsActive" type="checkbox" />
    </div>

    <button type="submit" class="btn btn-success">Create</button>
    <a href="@Url.Action("Index")" class="btn btn-secondary">Cancel</a>
</form>
```

## Dropdown Select

```cshtml
<div class="form-group">
    <label>Category</label>
    <select asp-for="CategoryId" class="form-control">
        <option value="">-- Select --</option>
        @foreach (var cat in Model.Categories)
        {
            <option value="@cat.Id">@cat.Name</option>
        }
    </select>
</div>
```

## Habbo-Style Form (Classic Design)

Based on the original screenshots, Habbo forms used:
- Labels on the left side, inputs on the right
- Simple HTML `<input>` and `<select>` elements
- "Choose a common message" dropdown for pre-filled text
- Action buttons at the bottom: `Ban`, `Kick`, `Alert`, `Search`
- Inline confirmation text after submission

```cshtml
<table>
    <tr>
        <td><strong>The recipient</strong></td>
        <td><input asp-for="Username" /></td>
    </tr>
    <tr>
        <td><strong>Message</strong></td>
        <td>
            <select id="commonMessage" onchange="document.getElementById('message').value = this.value">
                <option value="">Choose a common message</option>
                <option value="Please follow the Habbo Way">Please follow the Habbo Way</option>
            </select>
        </td>
    </tr>
    <tr>
        <td></td>
        <td><input asp-for="Message" id="message" /></td>
    </tr>
    <tr>
        <td></td>
        <td><button type="submit">Alert</button></td>
    </tr>
</table>
```

## Rules

1. Use `asp-for` tag helpers for model binding
2. Include `asp-validation-for` spans for client-side validation display
3. Use `method="post"` explicitly on forms
4. Provide Cancel/Back links to the Index page
5. For rich text fields, use TinyMCE (include via `@section scripts`)

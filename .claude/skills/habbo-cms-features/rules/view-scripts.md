---
title: Page-Specific JavaScript Patterns
impact: LOW
impactDescription: JavaScript placed incorrectly can cause loading order issues or layout breaks
tags: [views, javascript, scripts, frontend]
---

# Page-Specific JavaScript Patterns

Views can include page-specific scripts via the `scripts` section.

## Usage

```cshtml
@section scripts {
    <script>
        // Page-specific JavaScript here
        $(document).ready(function() {
            // jQuery code
        });
    </script>
}
```

## Common Libraries Available

The Housekeeping layout includes these libraries globally:
- **jQuery** — DOM manipulation, AJAX
- **SweetAlert2** — Confirmation dialogs and alerts
- **TinyMCE** — Rich text editing (loaded per-page when needed)
- **jQuery UI** — Datepicker and other widgets

## SweetAlert2 Confirmation Pattern

```cshtml
@section scripts {
    <script>
        function confirmAction(url, message) {
            Swal.fire({
                title: 'Are you sure?',
                text: message || "This action cannot be undone!",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Yes, do it!'
            }).then((result) => {
                if (result.isConfirmed) {
                    window.location.href = url;
                }
            });
        }
    </script>
}
```

## TinyMCE Rich Text Editor

```cshtml
@section scripts {
    <script src="/lib/tinymce/tinymce.min.js"></script>
    <script>
        tinymce.init({
            selector: '#Description',
            height: 400,
            plugins: 'link image code table',
            toolbar: 'undo redo | formatselect | bold italic | alignleft aligncenter alignright | bullist numlist | link image | code'
        });
    </script>
}
```

## AJAX Form Submission

```cshtml
@section scripts {
    <script>
        async function submitForm(event) {
            event.preventDefault();
            const form = event.target;
            const data = Object.fromEntries(new FormData(form));
            
            const response = await fetch(form.action, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });
            
            if (response.ok) {
                const result = await response.json();
                Swal.fire('Success', 'Operation completed', 'success');
            } else {
                const error = await response.json();
                Swal.fire('Error', error.error || 'Something went wrong', 'error');
            }
        }
    </script>
}
```

## Rules

1. Always wrap page scripts in `@section scripts { }` (renders at bottom of layout)
2. Use `Swal.fire()` instead of `confirm()` or `alert()` for better UX
3. Use `fetch()` for AJAX calls (not `$.ajax()` unless maintaining legacy code)
4. TinyMCE scripts should only be loaded on pages that need rich text editing
5. Avoid inline `onclick` handlers for complex logic — use event listeners in the scripts section

---
title: Data Table Patterns
impact: MEDIUM
impactDescription: Inconsistent table patterns reduce usability for admin users
tags: [views, tables, lists, pagination]
---

# Data Table Patterns

List views use HTML tables with consistent styling.

## Standard Table Template

```cshtml
@model IEnumerable<KeplerCMS.Data.Models.MyFeature>

@{
    ViewBag.Title = "My Features";
}

@if (!string.IsNullOrEmpty(Context.Request.Query["message"]))
{
    <div class="alert alert-success">@Context.Request.Query["message"]</div>
}

<h2>My Features</h2>
<a href="@Url.Action("Create")" class="btn btn-primary">Create New</a>

<table class="table table-striped mt-3">
    <thead>
        <tr>
            <th>ID</th>
            <th>Name</th>
            <th>Created</th>
            <th>Active</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model)
        {
            <tr>
                <td>@item.Id</td>
                <td>@item.Name</td>
                <td>@item.CreatedAt.ToString("yyyy-MM-dd HH:mm")</td>
                <td>@(item.IsActive ? "Yes" : "No")</td>
                <td>
                    <a href="@Url.Action("Update", new { id = item.Id })">Edit</a> |
                    <a href="#" onclick="confirmDelete(@item.Id)">Delete</a>
                </td>
            </tr>
        }
    </tbody>
</table>
```

## Classic Habbo Table Style

The original Habbo Housekeeping used yellow/gold highlighted tables:

```cshtml
<table class="habboTable" cellpadding="3" cellspacing="1" width="100%">
    <tr style="background-color: #FFD700; font-weight: bold;">
        <td>Username</td>
        <td>Email</td>
        <td>Last IP</td>
        <td>Last access</td>
        <td>Actions</td>
    </tr>
    @foreach (var user in Model.Users)
    {
        <tr style="background-color: #FFFFCC;">
            <td><a href="@Url.Action("Manage", new { id = user.Id })">@user.Username</a></td>
            <td>@user.Email</td>
            <td>@user.LastIp</td>
            <td>@user.LastAccess?.ToString("yyyy-MM-dd HH:mm")</td>
            <td><a href="@Url.Action("Manage", new { id = user.Id })">View</a></td>
        </tr>
    }
</table>
```

## Pagination

```cshtml
@if (Model.TotalPages > 1)
{
    <nav>
        <ul class="pagination">
            @for (int i = 1; i <= Model.TotalPages; i++)
            {
                <li class="page-item @(i == Model.CurrentPage ? "active" : "")">
                    <a class="page-link" 
                       href="@Url.Action("Index", new { currentPage = i, search = Model.Search })">@i</a>
                </li>
            }
        </ul>
    </nav>
}
```

## Alphabetical Filter (Habbo Style)

```cshtml
<div class="letter-filter">
    <a href="@Url.Action("Index", new { letter = "" })">ALL</a>
    @foreach (var c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
    {
        <a href="@Url.Action("Index", new { letter = c.ToString() })" 
           class="@(Model.Letter == c.ToString() ? "active" : "")">@c</a>
    }
</div>
```

## Delete Confirmation with SweetAlert2

```cshtml
@section scripts {
    <script>
        function confirmDelete(id) {
            Swal.fire({
                title: 'Are you sure?',
                text: "This action cannot be undone!",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#d33',
                confirmButtonText: 'Yes, delete it!'
            }).then((result) => {
                if (result.isConfirmed) {
                    window.location.href = '@Url.Action("Remove")/' + id;
                }
            });
        }
    </script>
}
```

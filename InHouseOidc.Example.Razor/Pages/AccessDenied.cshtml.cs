// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InHouseOidc.Example.Razor.Pages
{
    public class AccessDenied : PageModel
    {
        [BindProperty]
        public string? ReturnUrl { get; set; }

        public void OnGet(string returnUrl)
        {
            this.ReturnUrl = returnUrl;
        }
    }
}

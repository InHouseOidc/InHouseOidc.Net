// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InHouseOidc.Certify.Provider.Pages
{
    public class Error : PageModel
    {
        [BindProperty]
        public string? ErrorDescription { get; set; }

        public void OnGet(string errorDescription)
        {
            this.ErrorDescription = errorDescription;
        }
    }
}

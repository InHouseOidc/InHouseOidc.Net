// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Mvc;

namespace InHouseOidc.Example.Mvc.Controllers
{
    public class AccessDeniedController : Controller
    {
        public IActionResult Index(string returnUrl)
        {
            return this.View(new Models.AccessDeniedModel { ReturnUrl = returnUrl });
        }
    }
}

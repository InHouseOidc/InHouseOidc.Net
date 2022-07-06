// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace InHouseOidc.Test.Common
{
    public class TestServiceCollection : List<ServiceDescriptor>, IServiceCollection
    {
        public void AssertContains(ServiceLifetime serviceLifetime, System.Type serviceType)
        {
            Assert.IsNotNull(
                this.FirstOrDefault(sc => sc.Lifetime == serviceLifetime && sc.ServiceType == serviceType),
                $"ServiceCollection missing {serviceLifetime} {serviceType.Name}"
            );
        }
    }
}

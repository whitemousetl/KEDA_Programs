using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTBridge.Test.Integrations;
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
    // 空即可
}
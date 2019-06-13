using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    public class NullCSharpEntityTypeConfigurationGenerator : ICSharpEntityTypeConfigurationGenerator
    {
        public string WriteCode(IEntityType entityType, string @namespace, bool useDataAnnotations)
        {
            return string.Empty;
        }
    }
}

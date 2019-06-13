using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    public interface ICSharpEntityTypeConfigurationGenerator
    {

        string WriteCode(IEntityType entityType, string @namespace, bool useDataAnnotations);
    }
}

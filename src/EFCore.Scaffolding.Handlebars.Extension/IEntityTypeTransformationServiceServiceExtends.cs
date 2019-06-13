using EntityFrameworkCore.Scaffolding.Handlebars;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    public static class IEntityTypeTransformationServiceServiceExtends
    {
        public static string TransformEntityTypeConfigurationName(this IEntityTypeTransformationService service, string entityName,string mapSuffix)
        {
            return service.TransformEntityName(entityName) + mapSuffix;
        }

        public static string TransformEntityTypeConfigurationFileName(this IEntityTypeTransformationService service, string entityFileName, string mapSuffix)
        {
            return service.TransformEntityFileName(entityFileName) + mapSuffix;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    public static class ConstantsExtends
    {
        //
        // 摘要:
        //     Entity type configuration template folder.
        public const string EntityTypeConfigurationDirectory = "CodeTemplates/CSharpEntityTypeConfiguration";

        //
        // 摘要:
        //     Entity type configuration partial templates folder.
        public const string EntityTypeConfigurationPartialsDirectory = "CodeTemplates/CSharpEntityTypeConfiguration/Partials";

        //
        // 摘要:
        //     Entity type configuration template.
        public const string EntityTypeConfigurationTemplate = "Configuration";

        //
        // 摘要:
        //     Entity type configuration imports template.
        public const string EntityTypeConfigurationImportTemplate = "CfgImports";
    }
}

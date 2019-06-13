using EFCore.Scaffolding.Handlebars.Extension.Options;
using EntityFrameworkCore.Scaffolding.Handlebars;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Newtonsoft.Json;
using System;
using System.IO;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    /// <summary>
    /// 增强HbsCSharpModelGenerator
    /// </summary>
    public class HbsCSharpModelGeneratorEnhance : HbsCSharpModelGenerator
    {
        private const string FileExtension = ".cs";
        public virtual HandlebarsScaffoldingOptions HandlebarsScaffoldingOptions { get; set; }

        /// <summary>
        /// Entity type configuration template service.
        /// </summary>
        public virtual IEntityTypeConfigurationTemplateService EntityTypeConfigurationTemplateService
        {
            get;
        }

        /// <summary>
        /// Entity type configuration generator.
        /// </summary>
        public virtual ICSharpEntityTypeConfigurationGenerator CSharpEntityTypeConfigurationGenerator
        {
            get;
        }

        /// <summary>
        /// Constructor for the HbsCSharpModelGenerator.
        /// </summary>
        /// <param name="dependencies">Service dependencies parameter class for HbsCSharpModelGenerator.</param>
        /// <param name="handlebarsHelperService">Handlebars helper service.</param>
        /// <param name="dbContextTemplateService">Template service for DbContext generator.</param>
        /// <param name="entityTypeTemplateService">Template service for the entity types generator.</param>
        /// <param name="entityTypeTransformationService">Service for transforming entity definitions.</param>
        /// <param name="cSharpDbContextGenerator">DbContext generator.</param>
        /// <param name="cSharpEntityTypeGenerator">Entity type generator.</param>
        public HbsCSharpModelGeneratorEnhance(ModelCodeGeneratorDependencies dependencies,
            IHbsHelperService handlebarsHelperService,
            IDbContextTemplateService dbContextTemplateService,
            IEntityTypeTemplateService entityTypeTemplateService,
            IEntityTypeConfigurationTemplateService entityTypeConfigurationTemplateService,
            IEntityTypeTransformationService entityTypeTransformationService,
            ICSharpDbContextGenerator cSharpDbContextGenerator,
            ICSharpEntityTypeGenerator cSharpEntityTypeGenerator,
            ICSharpEntityTypeConfigurationGenerator cSharpEntityTypeConfigurationGenerator,
            HandlebarsScaffoldingOptions handlebarsScaffoldingOptions) : base(dependencies,
             handlebarsHelperService,
             dbContextTemplateService,
             entityTypeTemplateService,
             entityTypeTransformationService,
             cSharpDbContextGenerator,
             cSharpEntityTypeGenerator)
        {
            EntityTypeConfigurationTemplateService = entityTypeConfigurationTemplateService;
            CSharpEntityTypeConfigurationGenerator = cSharpEntityTypeConfigurationGenerator;
            HandlebarsScaffoldingOptions = handlebarsScaffoldingOptions ?? HandlebarsScaffoldingOptions.Default;
        }

        public override ScaffoldedModel GenerateModel(IModel model,
            string @namespace,
            string contextDir,
            string contextName,
            string connectionString,
            ModelCodeGenerationOptions options)
        {
            EntityTypeConfigurationTemplateService.RegisterPartialTemplates();

            var resultingFiles = base.GenerateModel(model, @namespace, contextDir, contextName, connectionString, options);

            string generatedCode;
            if (!(CSharpEntityTypeConfigurationGenerator is NullCSharpEntityTypeConfigurationGenerator))
            {
                foreach (var entityType in model.GetEntityTypes())
                {
                    generatedCode = CSharpEntityTypeConfigurationGenerator.WriteCode(entityType, @namespace, options.UseDataAnnotations);

                    var transformedFileName = EntityTypeTransformationService.TransformEntityTypeConfigurationFileName(entityType.DisplayName(), HandlebarsScaffoldingOptions.EntityTypeConfigurationFileNameSuffix);
                    var entityTypeCfgFileName = transformedFileName + FileExtension;
                    resultingFiles.AdditionalFiles.Add(new ScaffoldedFile { Path = Path.Combine(contextDir, contextName + HandlebarsScaffoldingOptions.EntityTypeConfigurationDirSuffix, entityTypeCfgFileName), Code = generatedCode });
                }
            }

            return resultingFiles;
        }
    }
}

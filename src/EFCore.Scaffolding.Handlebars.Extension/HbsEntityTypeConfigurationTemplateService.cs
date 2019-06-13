using EntityFrameworkCore.Scaffolding.Handlebars;
using EntityFrameworkCore.Scaffolding.Handlebars.Helpers;
using System;
using System.Collections.Generic;
using HandlebarsLib = HandlebarsDotNet.Handlebars;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    /// <summary>
    /// Provide services required to generate entity type classes using Handlebars templates.
    /// </summary>
    public class HbsEntityTypeConfigurationTemplateService : HbsTemplateService, IEntityTypeConfigurationTemplateService
    {
        /// <summary>
        /// Entity type template.
        /// </summary>
        public Func<object, string> EntityTypeConfigurationTemplate { get; private set; }

        /// <summary>
        /// Constructor for entity type template service.
        /// </summary>
        /// <param name="fileService">Template file service.</param>
        public HbsEntityTypeConfigurationTemplateService(ITemplateFileService fileService) : base(fileService)
        {
        }

        /// <summary>
        /// Generate entity type configuration class.
        /// </summary>
        /// <param name="data">Data used to generate entity type configuration class.</param>
        /// <returns>Generated entity type class.</returns>
        public virtual string GenerateEntityTypeConfiguration(object data)
        {
            if (EntityTypeConfigurationTemplate == null)
            {
                EntityTypeConfigurationTemplate = CompileEntityTypeConfigurationTemplate();
            }
            string entityType = EntityTypeConfigurationTemplate(data);
            return entityType;
        }

        /// <summary>
        /// Compile entity type configuration template.
        /// </summary>
        /// <returns>Entity type configuration template.</returns>
        protected virtual Func<object, string> CompileEntityTypeConfigurationTemplate()
        {
            var template = FileService.RetrieveTemplateFileContents(
                ConstantsExtends.EntityTypeConfigurationDirectory,
                ConstantsExtends.EntityTypeConfigurationTemplate + Constants.TemplateExtension);
            var cfgTemplate = HandlebarsLib.Compile(template);
            return cfgTemplate;
        }

        /// <summary>
        /// Get partial templates.
        /// </summary>
        /// <returns>Partial templates.</returns>
        protected override IDictionary<string, string> GetPartialTemplates()
        {
            var importTemplate = FileService.RetrieveTemplateFileContents(
                ConstantsExtends.EntityTypeConfigurationPartialsDirectory,
                ConstantsExtends.EntityTypeConfigurationImportTemplate + Constants.TemplateExtension);

            var templates = new Dictionary<string, string>
            {
                {
                    ConstantsExtends.EntityTypeConfigurationImportTemplate.ToLower(),
                    importTemplate
                },
            };
            return templates;
        }
    }
}

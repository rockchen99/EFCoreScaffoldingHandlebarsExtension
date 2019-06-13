using System;
using System.Collections.Generic;
using EntityFrameworkCore.Scaffolding.Handlebars;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    public interface IEntityTypeConfigurationTemplateService : IHbsTemplateService
    {
        /// <summary>
        ///  Entity type configuration template.
        /// </summary>
        Func<object, string> EntityTypeConfigurationTemplate
        {
            get;
        }

        /// <summary>
        /// Generate entity type configuration class.
        /// </summary>
        /// <param name="data"> Data used to generate entity type configuration class.</param>
        /// <returns> Generated entity type class.</returns>
        string GenerateEntityTypeConfiguration(object data);
    }
}

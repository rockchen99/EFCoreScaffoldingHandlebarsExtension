using EFCore.Scaffolding.Handlebars.Extension;
using EFCore.Scaffolding.Handlebars.Extension.Options;
using EntityFrameworkCore.Scaffolding.Handlebars;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;

namespace RefineFluteAPI
{
    public class ScaffoldingDesignTimeServices : IDesignTimeServices
    {
        public void ConfigureDesignTimeServices(IServiceCollection services)
        {
            var options = HandlebarsScaffoldingOptions.Default;
            //options.Tables.Add("dbo.Category");
            services.AddHandlebarsScaffolding(options);

            // Register Handlebars helper
            var myHelper = (helperName: "my-helper", helperFunction: (Action<TextWriter, Dictionary<string, object>, object[]>)MyHbsHelper);

            // Add optional Handlebars helpers
            services.AddHandlebarsHelpers(myHelper);

            // Add Handlebars transformer for Country property
            services.AddHandlebarsTransformers(
                propertyTransformer: e =>
                    e.PropertyName == "Country"
                        ? new EntityPropertyInfo("Country", e.PropertyName)
                        : new EntityPropertyInfo(e.PropertyType, e.PropertyName));

            // Add optional Handlebars transformers
            //services.AddHandlebarsTransformers(
            //    entityNameTransformer: n => n + "Foo",
            //    entityFileNameTransformer: n => n + "Foo",
            //    constructorTransformer: e => new EntityPropertyInfo(e.PropertyType + "Foo", e.PropertyName + "Foo"),
            //    propertyTransformer: e => new EntityPropertyInfo(e.PropertyType, e.PropertyName + "Foo"),
            //    navPropertyTransformer: e => new EntityPropertyInfo(e.PropertyType + "Foo", e.PropertyName + "Foo"));
        }

        // Sample Handlebars helper
        void MyHbsHelper(TextWriter writer, Dictionary<string, object> context, object[] parameters)
        {
            writer.Write("// My Handlebars Helper");
        }
    }
}
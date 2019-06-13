using EFCore.Scaffolding.Handlebars.Extension.Options;
using EntityFrameworkCore.Scaffolding.Handlebars;
using EntityFrameworkCore.Scaffolding.Handlebars.Helpers;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    public static  class ServiceCollectionExtensions
    {
        /// <summary>
        ///     <para>
        ///        将Handlebars脚手架生成器注册为<see cref =“IServiceCollection”/>中的服务。
        ///        这允许您通过修改CodeTemplates文件夹中的Handlebars模板来自定义生成的DbContext和实体类型类。
        ///     </para>
        ///     <para>
        ///        有<paramref name =“options”/>，它允许您选择是仅生成DbContext类，
        ///        仅生成实体类型类，还是生成DbContext和实体类型类（默认值）。
        ///        这在将模型类放入a时非常有用。 单独的类库。
        ///     </para>
        /// </summary>
        /// <param name="services"> The <see cref="IServiceCollection" /> to add services to. </param>
        /// <param name="options">Options for reverse engineering classes from an existing database.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        public static IServiceCollection AddHandlebarsScaffolding(this IServiceCollection services, HandlebarsScaffoldingOptions options=null)
        {
            if(options==null)
                options = HandlebarsScaffoldingOptions.Default;

            services.AddSingleton<HandlebarsScaffoldingOptions>(options);
            AddSingletonForCSharpDbContextGenerator(services, options);
            AddSingletonForCSharpEntityTypeConfigurationGenerator(services, options);
            AddSingletonForCSharpEntityTypeGenerator(services, options);

            services.AddSingleton<ITemplateFileService, FileSystemTemplateFileService>();
            services.AddSingleton<IDbContextTemplateService, HbsDbContextTemplateService>();
            services.AddSingleton<IEntityTypeTemplateService, HbsEntityTypeTemplateService>();
            services.AddSingleton<IEntityTypeConfigurationTemplateService, HbsEntityTypeConfigurationTemplateService>();
            services.AddSingleton<IModelCodeGenerator, HbsCSharpModelGeneratorEnhance>();
            services.AddSingleton<IReverseEngineerScaffolder, HbsReverseEngineerScaffolderEnhance>();
            services.AddSingleton<IEntityTypeTransformationService, HbsEntityTypeTransformationService>();
            services.AddSingleton<IHbsHelperService, HbsHelperService>(provider =>
            {
                var helpers = new Dictionary<string, Action<TextWriter, Dictionary<string, object>, object[]>>
                {
                    {Constants.SpacesHelper, HandlebarsHelpers.SpacesHelper}
                };
                return new HbsHelperService(helpers);
            });
            return services;
        }

        private static void AddSingletonForCSharpDbContextGenerator(IServiceCollection services, HandlebarsScaffoldingOptions options)
        {
            Type dbContextGeneratorImpl;
            var dbContextGeneratorType = typeof(ICSharpDbContextGenerator);
            if (options.ReverseEngineerOptions == ReverseEngineerOptions.DbContextOnly
                || options.ReverseEngineerOptions == ReverseEngineerOptions.DbContextAndEntities)
            {
                dbContextGeneratorImpl =  typeof(HbsCSharpDbContextGeneratorEnhance) ;
            }
            else
            {
                dbContextGeneratorImpl = typeof(NullCSharpDbContextGenerator);
            }
            services.AddSingleton(dbContextGeneratorType, dbContextGeneratorImpl);
        }

        private static void AddSingletonForCSharpEntityTypeConfigurationGenerator(IServiceCollection services, HandlebarsScaffoldingOptions options)
        {
            Type entityTypeCfgGeneratorImpl=null;
            var entityTypeCfgGeneratorType = typeof(ICSharpEntityTypeConfigurationGenerator);
            if (options.ReverseEngineerOptions == ReverseEngineerOptions.DbContextOnly
                || options.ReverseEngineerOptions == ReverseEngineerOptions.DbContextAndEntities)
            {
                if (options.RefineFluteAPI)
                {
                    entityTypeCfgGeneratorImpl = typeof(HbsCSharpEntityTypeConfigurationGenerator);
                }   
            }

            if(entityTypeCfgGeneratorImpl==null)
            {
                entityTypeCfgGeneratorImpl = typeof(NullCSharpEntityTypeConfigurationGenerator);
            }

            services.AddSingleton(entityTypeCfgGeneratorType, entityTypeCfgGeneratorImpl);
        }

        private static void AddSingletonForCSharpEntityTypeGenerator(IServiceCollection services, HandlebarsScaffoldingOptions options)
        {
            Type entityGeneratorImpl;
            var entityGeneratorType = typeof(ICSharpEntityTypeGenerator);
            if (options.ReverseEngineerOptions == ReverseEngineerOptions.EntitiesOnly
                || options.ReverseEngineerOptions == ReverseEngineerOptions.DbContextAndEntities)
            {
                entityGeneratorImpl = typeof(HbsCSharpEntityTypeGenerator);
            }
            else
            {
                entityGeneratorImpl = typeof(NullCSharpEntityTypeGenerator);
            }
            services.AddSingleton(entityGeneratorType, entityGeneratorImpl);
        }
    }
}

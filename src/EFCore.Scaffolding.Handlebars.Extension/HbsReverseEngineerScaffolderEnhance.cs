using EntityFrameworkCore.Scaffolding.Handlebars;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.EntityFrameworkCore.Internal;
using System.Globalization;
using EFCore.Scaffolding.Handlebars.Extension.Options;
using System;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    /// <summary>
    /// 增强HbsReverseEngineerScaffolder
    /// </summary>
    public class HbsReverseEngineerScaffolderEnhance : HbsReverseEngineerScaffolder
    {
        public virtual HandlebarsScaffoldingOptions HandlebarsScaffoldingOptions { get; set; }

        /// <summary>
        /// Constructor for the HbsCSharpModelGenerator.
        /// </summary>
        /// <param name="databaseModelFactory">Service to reverse engineer a database into a database model.</param>
        /// <param name="scaffoldingModelFactory">Factory to create a scaffolding model.</param>
        /// <param name="modelCodeGeneratorSelector">Selects a model code generator service for a given programming language.</param>
        /// <param name="cSharpUtilities">C# utilities.</param>
        /// <param name="cSharpHelper">C# helper.</param>
        /// <param name="connectionStringResolver">Connection string resolver.</param>
        public HbsReverseEngineerScaffolderEnhance(
            HandlebarsScaffoldingOptions handlebarsScaffoldingOptions,
            IDatabaseModelFactory databaseModelFactory,
            IScaffoldingModelFactory scaffoldingModelFactory,
            IModelCodeGeneratorSelector modelCodeGeneratorSelector,
            ICSharpUtilities cSharpUtilities,
            ICSharpHelper cSharpHelper,
            INamedConnectionStringResolver connectionStringResolver) : base(databaseModelFactory,
                scaffoldingModelFactory,
                modelCodeGeneratorSelector,
                cSharpUtilities,
                cSharpHelper,
                connectionStringResolver)
        {
            HandlebarsScaffoldingOptions = handlebarsScaffoldingOptions ?? HandlebarsScaffoldingOptions.Default;
        }

        //
        // 摘要:
        //     /// This API supports the Entity Framework Core infrastructure and is not intended
        //     to be used /// directly from your code. This API may change or be removed in
        //     future releases. ///
        public override ScaffoldedModel ScaffoldModel(string connectionString, 
            IEnumerable<string> tables, 
            IEnumerable<string> schemas, 
            string @namespace, 
            string language, 
            string contextDir, 
            string contextName, 
            ModelReverseEngineerOptions modelOptions, 
            ModelCodeGenerationOptions codeOptions)
        {
            if(tables==null || !tables.Any())
            {
                tables = HandlebarsScaffoldingOptions.Tables ?? new List<string>();
            }
            if (schemas == null || !schemas.Any())
            {
                schemas = HandlebarsScaffoldingOptions.Schemas ?? new List<string>();
            }
            return base.ScaffoldModel(connectionString, tables, schemas, @namespace, language, contextDir, contextName, modelOptions, codeOptions);
        }


        /// <summary>
        /// Persist generated DbContext and entity type classes. 
        /// </summary>
        /// <param name="scaffoldedModel">Represents a scaffolded model.</param>
        /// <param name="outputDir">Output directory.</param>
        /// <param name="overwriteFiles">True to overwrite existing files.</param>
        /// <returns></returns>
        public override SavedModelFiles Save(ScaffoldedModel scaffoldedModel, string outputDir, bool overwriteFiles)
        {
           
            CheckOutputFiles(scaffoldedModel, outputDir, overwriteFiles);

            Directory.CreateDirectory(outputDir);

            var contextPath = string.Empty;
            if (scaffoldedModel.ContextFile != null
                && !string.IsNullOrWhiteSpace(scaffoldedModel.ContextFile.Path))
            {
                contextPath = Path.GetFullPath(Path.Combine(outputDir, scaffoldedModel.ContextFile.Path));
                Directory.CreateDirectory(Path.GetDirectoryName(contextPath));
                File.WriteAllText(contextPath, scaffoldedModel.ContextFile.Code, Encoding.UTF8);
            }

            var additionalFiles = new List<string>();
            foreach (var entityTypeFile in scaffoldedModel.AdditionalFiles)
            {
                // 修复
                var additionalFilePath = Path.GetFullPath(Path.Combine(outputDir, entityTypeFile.Path));
                Directory.CreateDirectory(Path.GetDirectoryName(additionalFilePath));

                File.WriteAllText(additionalFilePath, entityTypeFile.Code, Encoding.UTF8);
                additionalFiles.Add(additionalFilePath);
            }

            return new SavedModelFiles(contextPath, additionalFiles);
        }

        private static void CheckOutputFiles(
            ScaffoldedModel scaffoldedModel,
            string outputDir,
            bool overwriteFiles)
        {
            var paths = scaffoldedModel.AdditionalFiles.Select(f => f.Path).ToList();
            if (scaffoldedModel.ContextFile != null
                && !string.IsNullOrWhiteSpace(scaffoldedModel.ContextFile.Path))
                paths.Insert(0, scaffoldedModel.ContextFile.Path);

            var existingFiles = new List<string>();
            var readOnlyFiles = new List<string>();
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(outputDir, path);

                if (File.Exists(fullPath))
                {
                    existingFiles.Add(path);

                    if (File.GetAttributes(fullPath).HasFlag(FileAttributes.ReadOnly))
                    {
                        readOnlyFiles.Add(path);
                    }
                }
            }

            if (!overwriteFiles && existingFiles.Count != 0)
            {
                throw new OperationException(
                    DesignStrings.ExistingFiles(
                        outputDir,
                        string.Join(CultureInfo.CurrentCulture.TextInfo.ListSeparator, existingFiles)));
            }
            if (readOnlyFiles.Count != 0)
            {
                throw new OperationException(
                    DesignStrings.ReadOnlyFiles(
                        outputDir,
                        string.Join(CultureInfo.CurrentCulture.TextInfo.ListSeparator, readOnlyFiles)));
            }
        }
    }
}

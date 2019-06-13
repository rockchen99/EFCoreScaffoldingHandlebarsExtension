﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using EntityFrameworkCore.Scaffolding.Handlebars.Internal;
using EntityFrameworkCore.Scaffolding.Handlebars;
using EFCore.Scaffolding.Handlebars.Extension.Options;
using EFCore.Scaffolding.Handlebars.Extension.Utils;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    /// <summary>
    /// 增强HbsCSharpDbContextGenerator
    /// </summary>
    public class HbsCSharpDbContextGeneratorEnhance : ICSharpDbContextGenerator
    {
        private const string EntityLambdaIdentifier = "entity";
        private const string Language = "CSharp";

        /// <summary>
        /// CSharp helper.
        /// </summary>
        protected ICSharpHelper CSharpHelper { get; }

#pragma warning disable 618
        /// <summary>
        /// Legacy provider code generator.
        /// </summary>
        protected IScaffoldingProviderCodeGenerator LegacyProviderCodeGenerator { get; }
#pragma warning restore 618

        /// <summary>
        /// Generator for scaffolding provider.
        /// </summary>
        protected IProviderConfigurationCodeGenerator ProviderConfigurationCodeGenerator { get; }

        /// <summary>
        /// Annotation code generator.
        /// </summary>
        protected IAnnotationCodeGenerator CodeGenerator { get; }

        /// <summary>
        /// Flag for initialization of entity type builder.
        /// </summary>
        protected bool EntityTypeBuilderInitialized { get; set; }

        /// <summary>
        /// Handlebars template data.
        /// </summary>
        protected Dictionary<string, object> TemplateData { get; set; }

        /// <summary>
        /// DbContext template service.
        /// </summary>
        public virtual IDbContextTemplateService DbContextTemplateService { get; }

        /// <summary>
        /// Service for transforming entity definitions.
        /// </summary>
        public virtual IEntityTypeTransformationService EntityTypeTransformationService { get; }

        public virtual HandlebarsScaffoldingOptions HandlebarsScaffoldingOptions { get; set; }

        /// <summary>
        /// Constructor for the Handlebars DbContext generator.
        /// </summary>
        /// <param name="handlebarsScaffoldingOptions">Handlebars Scaffolding Options.</param>
        /// <param name="legacyProviderCodeGenerators">Legacy provider code generators.</param>
        /// <param name="providerCodeGenerators">Generator for scaffolding provider.</param>
        /// <param name="annotationCodeGenerator">Annotation code generator.</param>
        /// <param name="dbContextTemplateService">Template service for DbContext generator.</param>
        /// <param name="entityTypeTransformationService">Service for transforming entity definitions.</param>
        /// <param name="cSharpHelper">CSharp helper.</param>
        public HbsCSharpDbContextGeneratorEnhance(
#pragma warning disable CS0618 // Type or member is obsolete
            IEnumerable<IScaffoldingProviderCodeGenerator> legacyProviderCodeGenerators,
#pragma warning restore CS0618 // Type or member is obsolete
            IEnumerable<IProviderConfigurationCodeGenerator> providerCodeGenerators,
            IAnnotationCodeGenerator annotationCodeGenerator,
            IDbContextTemplateService dbContextTemplateService,
            IEntityTypeTransformationService entityTypeTransformationService,
            ICSharpHelper cSharpHelper,
            HandlebarsScaffoldingOptions handlebarsScaffoldingOptions)
        {
            if (!legacyProviderCodeGenerators.Any() && !providerCodeGenerators.Any())
            {
                throw new ArgumentException(AbstractionsStrings.CollectionArgumentIsEmpty(nameof(providerCodeGenerators)));
            }

            HandlebarsScaffoldingOptions = handlebarsScaffoldingOptions ?? HandlebarsScaffoldingOptions.Default;
            LegacyProviderCodeGenerator = legacyProviderCodeGenerators.LastOrDefault();
            ProviderConfigurationCodeGenerator = providerCodeGenerators.LastOrDefault();
            CodeGenerator = annotationCodeGenerator ?? throw new ArgumentNullException(nameof(annotationCodeGenerator));
            DbContextTemplateService = dbContextTemplateService ?? throw new ArgumentNullException(nameof(dbContextTemplateService));
            EntityTypeTransformationService = entityTypeTransformationService ?? throw new ArgumentNullException(nameof(entityTypeTransformationService));
            CSharpHelper = cSharpHelper ?? throw new ArgumentNullException(nameof(cSharpHelper));
        }

        /// <summary>
        /// Generate the DbContext class.
        /// </summary>
        /// <param name="model">Metadata about the shape of entities, the relationships between them, and how they map to the database.</param>
        /// <param name="namespace">DbContext namespace.</param>
        /// <param name="contextName">Name of DbContext class.</param>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="useDataAnnotations">If false use fluent modeling API.</param>
        /// <param name="suppressConnectionStringWarning">Suppress connection string warning.</param>
        /// <returns></returns>
        public virtual string WriteCode(
            IModel model,
            string @namespace,
            string contextName,
            string connectionString,
            bool useDataAnnotations,
            bool suppressConnectionStringWarning)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            TemplateData = new Dictionary<string, object>();
            TemplateData.Add("namespace", @namespace);

            GenerateClass(model, contextName, connectionString, useDataAnnotations, suppressConnectionStringWarning);

            string output = DbContextTemplateService.GenerateDbContext(TemplateData);
            return output;
        }

        /// <summary>
        /// Generate the DbContext class.
        /// </summary>
        /// <param name="model">Metadata about the shape of entities, the relationships between them, and how they map to the database.</param>
        /// <param name="contextName">Name of DbContext class.</param>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="useDataAnnotations">Use fluent modeling API if false.</param>
        /// <param name="suppressConnectionStringWarning">Suppress connection string warning.</param>
        protected virtual void GenerateClass(
            IModel model,
            string contextName,
            string connectionString,
            bool useDataAnnotations,
            bool suppressConnectionStringWarning)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (contextName == null) throw new ArgumentNullException(nameof(contextName));
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

            TemplateData.Add("class", contextName);

            GenerateDbSets(model);
            GenerateEntityTypeErrors(model);
            GenerateOnConfiguring(connectionString, suppressConnectionStringWarning);
            GenerateOnModelCreating(model, useDataAnnotations);
        }

        private void GenerateDbSets(IModel model)
        {
            var dbSets = new List<Dictionary<string, object>>();

            foreach (var entityType in model.GetEntityTypes())
            {
                var transformedEntityName = EntityTypeTransformationService.TransformEntityName(entityType.Name);
                dbSets.Add(new Dictionary<string, object>
                {
                    { "set-property-type", transformedEntityName },
                    { "set-property-name", entityType.Scaffolding().DbSetName },
                });
            }

            TemplateData.Add("dbsets", dbSets);
        }

        private void GenerateEntityTypeErrors(IModel model)
        {
            var entityTypeErrors = new List<Dictionary<string, object>>();

            foreach (var entityTypeError in model.Scaffolding().EntityTypeErrors)
            {
                entityTypeErrors.Add(new Dictionary<string, object>
                {
                    { "entity-type-error", $"// {entityTypeError.Value} Please see the warning messages." },
                });
            }

            TemplateData.Add("entity-type-errors", entityTypeErrors);
        }

        /// <summary>
        /// Generate OnConfiguring method.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="suppressConnectionStringWarning">Suppress connection string warning.</param>
        protected virtual void GenerateOnConfiguring(
            string connectionString,
            bool suppressConnectionStringWarning)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

            var sb = new IndentedStringBuilder();

            using (sb.Indent())
            {
                sb.IncrementIndent();
                sb.AppendLine("protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)");
                sb.AppendLine("{");

                using (sb.Indent())
                {
                    sb.AppendLine("if (!optionsBuilder.IsConfigured)");
                    sb.AppendLine("{");

                    if (HandlebarsScaffoldingOptions.IncludeConnectionString)
                    {
                        using (sb.Indent())
                        {
                            if (!suppressConnectionStringWarning)
                            {
                                sb.DecrementIndent()
                                    .DecrementIndent()
                                    .DecrementIndent()
                                    .DecrementIndent()
                                    .AppendLine("#warning " + DesignStrings.SensitiveInformationWarning)
                                    .IncrementIndent()
                                    .IncrementIndent()
                                    .IncrementIndent()
                                    .IncrementIndent();
                            }

                            sb.Append("optionsBuilder")
                                .Append(
                                    ProviderConfigurationCodeGenerator != null
                                        ? CSharpHelper.Fragment(
                                            ProviderConfigurationCodeGenerator.GenerateUseProvider(connectionString, null))
#pragma warning disable CS0618 // Type or member is obsolete
                                    : LegacyProviderCodeGenerator.GenerateUseProvider(connectionString, Language))
#pragma warning restore CS0618 // Type or member is obsolete
                            .AppendLine(";");
                        }
                    }

                    sb.AppendLine("}");
                }
                sb.AppendLine("}");

                var onConfiguring = sb.ToString();
                TemplateData.Add("on-configuring", onConfiguring);
            }
        }

        /// <summary>
        /// Generate OnModelBuilding method.
        /// </summary>
        /// <param name="model">Metadata about the shape of entities, the relationships between them, and how they map to the database.</param>
        /// <param name="useDataAnnotations">Use fluent modeling API if false.</param>
        protected virtual void GenerateOnModelCreating(IModel model, bool useDataAnnotations)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var sb = new IndentedStringBuilder();

            using (sb.Indent())
            {
                sb.IncrementIndent();
                sb.AppendLine("protected override void OnModelCreating(ModelBuilder modelBuilder)");
                sb.Append("{");

                var annotations = model.GetAnnotations().ToList();
                RemoveAnnotation(ref annotations, ScaffoldingAnnotationNames.DatabaseName);
                RemoveAnnotation(ref annotations, ScaffoldingAnnotationNames.EntityTypeErrors);

                var annotationsToRemove = new List<IAnnotation>();
                annotationsToRemove.AddRange(annotations.Where(a => a.Name.StartsWith(RelationalAnnotationNames.SequencePrefix, StringComparison.Ordinal)));

                var lines = new List<string>();

                foreach (var annotation in annotations)
                {
                    if (CodeGenerator.IsHandledByConvention(model, annotation))
                    {
                        annotationsToRemove.Add(annotation);
                    }
                    else
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        var line = CodeGenerator.GenerateFluentApi(model, annotation, Language);
#pragma warning restore CS0618 // Type or member is obsolete

                        if (line != null)
                        {
                            lines.Add(line);
                            annotationsToRemove.Add(annotation);
                        }
                    }
                }

                lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)));

                if (lines.Count > 0)
                {
                    using (sb.Indent())
                    {
                        sb.AppendLine();
                        sb.Append("modelBuilder" + lines[0]);

                        using (sb.Indent())
                        {
                            foreach (var line in lines.Skip(1))
                            {
                                sb.AppendLine();
                                sb.Append(line);
                            }
                        }

                        sb.AppendLine(";");
                    }
                }

                using (sb.Indent())
                {
                    if (HandlebarsScaffoldingOptions.RefineFluteAPI)
                    {
                        AllGenerateEntityTypeConfigurations(model, sb);
                    }
                    else
                    {
                        AllGenerateEntityTypes(model, useDataAnnotations, sb);
                    }

                    AllGenerateSequences(model, sb);

                    sb.AppendLine();
                    sb.AppendLine("OnModelCreatingExt(modelBuilder);");
                }

                sb.AppendLine("}");
                sb.AppendLine();
                sb.Append("partial void OnModelCreatingExt(ModelBuilder modelBuilder);");

                var onModelCreating = sb.ToString();
                TemplateData.Add("on-model-creating", onModelCreating);
            }
        }

        private void AllGenerateEntityTypes(IModel model, bool useDataAnnotations, IndentedStringBuilder sb)
        {
            foreach (var entityType in model.GetEntityTypes())
            {
                EntityTypeBuilderInitialized = false;

                GenerateEntityType(entityType, useDataAnnotations, sb);

                if (EntityTypeBuilderInitialized)
                {
                    sb.AppendLine("});");
                }
            }
        }

        private void AllGenerateEntityTypeConfigurations(IModel model, IndentedStringBuilder sb)
        {
            foreach (var entityType in model.GetEntityTypes())
            {
                var entityTypeCfgName = EntityTypeTransformationService.TransformEntityTypeConfigurationName(entityType.Name, HandlebarsScaffoldingOptions.EntityTypeConfigurationFileNameSuffix);
                sb.AppendLine($"modelBuilder.ApplyConfiguration(new {entityTypeCfgName}());");
            }
        }

        private void AllGenerateSequences(IModel model, IndentedStringBuilder sb)
        {
            foreach (var sequence in model.Relational().Sequences)
            {
                GenerateSequence(sequence, sb);
            }
        }

        private void InitializeEntityTypeBuilder(IEntityType entityType,
            IndentedStringBuilder sb)
        {
            if (!EntityTypeBuilderInitialized)
            {
                var transformedEntityName = EntityTypeTransformationService.TransformEntityName(entityType.Name);
                sb.AppendLine();
                sb.AppendLine($"modelBuilder.Entity<{transformedEntityName}>({EntityLambdaIdentifier} =>");
                sb.Append("{");
            }

            EntityTypeBuilderInitialized = true;
        }

        private void GenerateEntityType(IEntityType entityType, bool useDataAnnotations,
            IndentedStringBuilder sb)
        {
            GenerateKey(entityType.FindPrimaryKey(), useDataAnnotations, sb);

            var annotations = entityType.GetAnnotations().ToList();
            RemoveAnnotation(ref annotations, RelationalAnnotationNames.TableName);
            RemoveAnnotation(ref annotations, RelationalAnnotationNames.Schema);
            RemoveAnnotation(ref annotations, ScaffoldingAnnotationNames.DbSetName);

            if (!useDataAnnotations)
            {
                GenerateTableName(entityType, sb);
            }

            var annotationsToRemove = new List<IAnnotation>();
            var lines = new List<string>();

            foreach (var annotation in annotations)
            {
                if (CodeGenerator.IsHandledByConvention(entityType, annotation))
                {
                    annotationsToRemove.Add(annotation);
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var line = CodeGenerator.GenerateFluentApi(entityType, annotation, Language);
#pragma warning restore CS0618 // Type or member is obsolete

                    if (line != null)
                    {
                        lines.Add(line);
                        annotationsToRemove.Add(annotation);
                    }
                }
            }

            lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)));

            AppendMultiLineFluentApi(entityType, lines, sb);

            foreach (var index in entityType.GetIndexes())
            {
                GenerateIndex(index, sb);
            }

            foreach (var property in entityType.GetProperties())
            {
                var transformedPropName = EntityTypeTransformationService.TransformPropertyName(property.Name);
                GenerateProperty(property, transformedPropName, useDataAnnotations, sb);
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var transformedDepPropName = EntityTypeTransformationService.TransformPropertyName(foreignKey.DependentToPrincipal.Name);
                var transformedPrincipalPropName = EntityTypeTransformationService.TransformPropertyName(foreignKey.PrincipalToDependent.Name);
                GenerateRelationship(foreignKey, transformedDepPropName, transformedPrincipalPropName, useDataAnnotations, sb);
            }
        }

        private void AppendMultiLineFluentApi(IEntityType entityType, IList<string> lines,
            IndentedStringBuilder sb)
        {
            if (lines.Count <= 0)
            {
                return;
            }

            InitializeEntityTypeBuilder(entityType, sb);

            using (sb.Indent())
            {
                sb.AppendLine();

                sb.Append(EntityLambdaIdentifier + lines[0]);

                using (sb.Indent())
                {
                    foreach (var line in lines.Skip(1))
                    {
                        sb.AppendLine();
                        sb.Append(line);
                    }
                }

                sb.AppendLine(";");
            }
        }

        private void GenerateKey(IKey key, bool useDataAnnotations, IndentedStringBuilder sb)
        {
            if (key == null)
            {
                return;
            }

            var annotations = key.GetAnnotations().ToList();

            var explicitName = key.Relational().Name != ConstraintNamer.GetDefaultName(key);
            RemoveAnnotation(ref annotations, RelationalAnnotationNames.Name);

            if (key.Properties.Count == 1)
            {
                if (key is Key concreteKey
                    && key.Properties.SequenceEqual(new KeyDiscoveryConvention(null).DiscoverKeyProperties(concreteKey.DeclaringEntityType, concreteKey.DeclaringEntityType.GetProperties().ToList())))
                {
                    return;
                }

                if (!explicitName
                    && useDataAnnotations)
                {
                    return;
                }
            }

            var lines = new List<string>
            {
                $".{nameof(EntityTypeBuilder.HasKey)}(e => {GenerateLambdaToKey(key.Properties, "e")})"
            };

            if (explicitName)
            {
                lines.Add($".{nameof(RelationalKeyBuilderExtensions.HasName)}" +
                          $"({CSharpHelper.Literal(key.Relational().Name)})");
            }

            var annotationsToRemove = new List<IAnnotation>();

            foreach (var annotation in annotations)
            {
                if (CodeGenerator.IsHandledByConvention(key, annotation))
                {
                    annotationsToRemove.Add(annotation);
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var line = CodeGenerator.GenerateFluentApi(key, annotation, Language);
#pragma warning restore CS0618 // Type or member is obsolete

                    if (line != null)
                    {
                        lines.Add(line);
                        annotationsToRemove.Add(annotation);
                    }
                }
            }

            lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)));

            AppendMultiLineFluentApi(key.DeclaringEntityType, lines, sb);
        }

        private void GenerateTableName(IEntityType entityType, IndentedStringBuilder sb)
        {
            var tableName = entityType.Relational().TableName;
            var schema = entityType.Relational().Schema;
            var defaultSchema = entityType.Model.Relational().DefaultSchema;

            var explicitSchema = schema != null && schema != defaultSchema;
            var explicitTable = explicitSchema || tableName != null && tableName != entityType.Scaffolding().DbSetName;

            if (explicitTable)
            {
                var parameterString = CSharpHelper.Literal(tableName);
                if (explicitSchema)
                {
                    parameterString += ", " + CSharpHelper.Literal(schema);
                }

                var lines = new List<string>
                {
                    $".{nameof(RelationalEntityTypeBuilderExtensions.ToTable)}({parameterString})"
                };

                AppendMultiLineFluentApi(entityType, lines, sb);
            }
        }

        private void GenerateIndex(IIndex index, IndentedStringBuilder sb)
        {
            var lines = new List<string>
            {
                $".{nameof(EntityTypeBuilder.HasIndex)}(e => {GenerateLambdaToKey(index.Properties, "e")})"
            };

            var annotations = index.GetAnnotations().ToList();

            if (!string.IsNullOrEmpty((string)index[RelationalAnnotationNames.Name]))
            {
                lines.Add(
                    $".{nameof(RelationalIndexBuilderExtensions.HasName)}" +
                    $"({CSharpHelper.Literal(index.Relational().Name)})");
                RemoveAnnotation(ref annotations, RelationalAnnotationNames.Name);
            }

            if (index.IsUnique)
            {
                lines.Add($".{nameof(IndexBuilder.IsUnique)}()");
            }

            if (index.Relational().Filter != null)
            {
                lines.Add(
                    $".{nameof(RelationalIndexBuilderExtensions.HasFilter)}" +
                    $"({CSharpHelper.Literal(index.Relational().Filter)})");
                RemoveAnnotation(ref annotations, RelationalAnnotationNames.Filter);
            }

            var annotationsToRemove = new List<IAnnotation>();

            foreach (var annotation in annotations)
            {
                if (CodeGenerator.IsHandledByConvention(index, annotation))
                {
                    annotationsToRemove.Add(annotation);
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var line = CodeGenerator.GenerateFluentApi(index, annotation, Language);
#pragma warning restore CS0618 // Type or member is obsolete

                    if (line != null)
                    {
                        lines.Add(line);
                        annotationsToRemove.Add(annotation);
                    }
                }
            }

            lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)));

            AppendMultiLineFluentApi(index.DeclaringEntityType, lines, sb);
        }

        private void GenerateProperty(IProperty property, string propertyName, bool useDataAnnotations,
            IndentedStringBuilder sb)
        {
            //var transformedPropertyName = EntityTypeTransformationService.TransformProperties(entityType.Name);
            var lines = new List<string>
            {
                $".{nameof(EntityTypeBuilder.Property)}(e => e.{propertyName})"
            };

            var annotations = property.GetAnnotations().ToList();

            RemoveAnnotation(ref annotations, RelationalAnnotationNames.ColumnName);
            RemoveAnnotation(ref annotations, RelationalAnnotationNames.ColumnType);
            RemoveAnnotation(ref annotations, CoreAnnotationNames.MaxLengthAnnotation);
            RemoveAnnotation(ref annotations, CoreAnnotationNames.UnicodeAnnotation);
            RemoveAnnotation(ref annotations, RelationalAnnotationNames.DefaultValue);
            RemoveAnnotation(ref annotations, RelationalAnnotationNames.DefaultValueSql);
            RemoveAnnotation(ref annotations, RelationalAnnotationNames.ComputedColumnSql);
            RemoveAnnotation(ref annotations, ScaffoldingAnnotationNames.ColumnOrdinal);

            if (!useDataAnnotations)
            {
                if (!property.IsNullable
                    && property.ClrType.IsNullableType()
                    && !property.IsPrimaryKey())
                {
                    lines.Add($".{nameof(PropertyBuilder.IsRequired)}()");
                }

                var columnName = property.Relational().ColumnName;

                if (columnName != null
                    && columnName != property.Name)
                {
                    lines.Add(
                        $".{nameof(RelationalPropertyBuilderExtensions.HasColumnName)}" +
                        $"({CSharpHelper.Literal(columnName)})");
                }

                var columnType = property.GetConfiguredColumnType();

                if (columnType != null)
                {
                    lines.Add(
                        $".{nameof(RelationalPropertyBuilderExtensions.HasColumnType)}" +
                        $"({CSharpHelper.Literal(columnType)})");
                }

                var maxLength = property.GetMaxLength();

                if (maxLength.HasValue)
                {
                    lines.Add(
                        $".{nameof(PropertyBuilder.HasMaxLength)}" +
                        $"({CSharpHelper.Literal(maxLength.Value)})");
                }
            }

            if (property.IsUnicode() != null)
            {
                lines.Add(
                    $".{nameof(PropertyBuilder.IsUnicode)}" +
                    $"({(property.IsUnicode() == false ? "false" : "")})");
            }

            if (property.Relational().IsFixedLength)
            {
                lines.Add(
                    $".{nameof(RelationalPropertyBuilderExtensions.IsFixedLength)}()");
            }

            if (property.Relational().DefaultValue != null)
            {
                lines.Add(
                    $".{nameof(RelationalPropertyBuilderExtensions.HasDefaultValue)}" +
                    $"({CSharpHelper.UnknownLiteral(property.Relational().DefaultValue)})");
            }

            if (property.Relational().DefaultValueSql != null)
            {
                lines.Add(
                    $".{nameof(RelationalPropertyBuilderExtensions.HasDefaultValueSql)}" +
                    $"({CSharpHelper.Literal(property.Relational().DefaultValueSql)})");
            }

            if (property.Relational().ComputedColumnSql != null)
            {
                lines.Add(
                    $".{nameof(RelationalPropertyBuilderExtensions.HasComputedColumnSql)}" +
                    $"({CSharpHelper.Literal(property.Relational().ComputedColumnSql)})");
            }

            var valueGenerated = property.ValueGenerated;
            var isRowVersion = false;
            if (((Property)property).GetValueGeneratedConfigurationSource().HasValue
                && new RelationalValueGeneratorConvention().GetValueGenerated((Property)property) != valueGenerated)
            {
                string methodName;
                switch (valueGenerated)
                {
                    case ValueGenerated.OnAdd:
                        methodName = nameof(PropertyBuilder.ValueGeneratedOnAdd);
                        break;

                    case ValueGenerated.OnAddOrUpdate:
                        isRowVersion = property.IsConcurrencyToken;
                        methodName = isRowVersion
                            ? nameof(PropertyBuilder.IsRowVersion)
                            : nameof(PropertyBuilder.ValueGeneratedOnAddOrUpdate);
                        break;

                    case ValueGenerated.Never:
                        methodName = nameof(PropertyBuilder.ValueGeneratedNever);
                        break;

                    default:
                        methodName = "";
                        break;
                }

                lines.Add($".{methodName}()");
            }

            if (property.IsConcurrencyToken
                && !isRowVersion)
            {
                lines.Add($".{nameof(PropertyBuilder.IsConcurrencyToken)}()");
            }

            var annotationsToRemove = new List<IAnnotation>();

            foreach (var annotation in annotations)
            {
                if (CodeGenerator.IsHandledByConvention(property, annotation))
                {
                    annotationsToRemove.Add(annotation);
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var line = CodeGenerator.GenerateFluentApi(property, annotation, Language);
#pragma warning restore CS0618 // Type or member is obsolete

                    if (line != null)
                    {
                        lines.Add(line);
                        annotationsToRemove.Add(annotation);
                    }
                }
            }

            lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)));

            switch (lines.Count)
            {
                case 1:
                    return;
                case 2:
                    lines = new List<string>
                    {
                        lines[0] + lines[1]
                    };
                    break;
            }

            AppendMultiLineFluentApi(property.DeclaringEntityType, lines, sb);
        }

        private void GenerateRelationship(IForeignKey foreignKey, string dependentPropName, string principalPropName,
            bool useDataAnnotations, IndentedStringBuilder sb)
        {
            var canUseDataAnnotations = true;
            var annotations = foreignKey.GetAnnotations().ToList();

            var lines = new List<string>
            {
                $".{nameof(EntityTypeBuilder.HasOne)}(d => d.{dependentPropName})",
                $".{(foreignKey.IsUnique ? nameof(ReferenceNavigationBuilder.WithOne) : nameof(ReferenceNavigationBuilder.WithMany))}"
                + $"(p => p.{principalPropName})"
            };

            if (!foreignKey.PrincipalKey.IsPrimaryKey())
            {
                canUseDataAnnotations = false;
                var principlePropDisplayName = EntityTypeTransformationService
                    .TransformPropertyName(foreignKey.PrincipalEntityType.DisplayName());
                lines.Add(
                    $".{nameof(ReferenceReferenceBuilder.HasPrincipalKey)}"
                    + $"{(foreignKey.IsUnique ? $"<{principlePropDisplayName}>" : "")}"
                    + $"(p => {GenerateLambdaToKey(foreignKey.PrincipalKey.Properties, "p")})");
            }

            var dependentPropDisplayName = EntityTypeTransformationService
                .TransformPropertyName(foreignKey.DeclaringEntityType.DisplayName());
            lines.Add(
                $".{nameof(ReferenceReferenceBuilder.HasForeignKey)}"
                + $"{(foreignKey.IsUnique ? $"<{dependentPropDisplayName}>" : "")}"
                + $"(d => {GenerateLambdaToKey(foreignKey.Properties, "d")})");

            var defaultOnDeleteAction = foreignKey.IsRequired
                ? DeleteBehavior.Cascade
                : DeleteBehavior.ClientSetNull;

            if (foreignKey.DeleteBehavior != defaultOnDeleteAction)
            {
                canUseDataAnnotations = false;
                lines.Add(
                    $".{nameof(ReferenceReferenceBuilder.OnDelete)}" +
                    $"({CSharpHelper.Literal(foreignKey.DeleteBehavior)})");
            }

            if (!string.IsNullOrEmpty((string)foreignKey[RelationalAnnotationNames.Name]))
            {
                canUseDataAnnotations = false;
                lines.Add(
                    $".{nameof(RelationalReferenceReferenceBuilderExtensions.HasConstraintName)}" +
                    $"({CSharpHelper.Literal(foreignKey.Relational().Name)})");
                RemoveAnnotation(ref annotations, RelationalAnnotationNames.Name);
            }

            var annotationsToRemove = new List<IAnnotation>();

            foreach (var annotation in annotations)
            {
                if (CodeGenerator.IsHandledByConvention(foreignKey, annotation))
                {
                    annotationsToRemove.Add(annotation);
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var line = CodeGenerator.GenerateFluentApi(foreignKey, annotation, Language);
#pragma warning restore CS0618 // Type or member is obsolete

                    if (line != null)
                    {
                        canUseDataAnnotations = false;
                        lines.Add(line);
                        annotationsToRemove.Add(annotation);
                    }
                }
            }

            lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)));

            if (!useDataAnnotations
                || !canUseDataAnnotations)
            {
                AppendMultiLineFluentApi(foreignKey.DeclaringEntityType, lines, sb);
            }
        }

        private void GenerateSequence(ISequence sequence, IndentedStringBuilder sb)
        {
            var methodName = nameof(RelationalModelBuilderExtensions.HasSequence);

            if (sequence.ClrType != Sequence.DefaultClrType)
            {
                methodName += $"<{CSharpHelper.Reference(sequence.ClrType)}>";
            }

            var parameters = CSharpHelper.Literal(sequence.Name);

            if (string.IsNullOrEmpty(sequence.Schema)
                && sequence.Model.Relational().DefaultSchema != sequence.Schema)
            {
                parameters += $", {CSharpHelper.Literal(sequence.Schema)}";
            }

            var lines = new List<string>
            {
                $"modelBuilder.{methodName}({parameters})"
            };

            if (sequence.StartValue != Sequence.DefaultStartValue)
            {
                lines.Add($".{nameof(SequenceBuilder.StartsAt)}({sequence.StartValue})");
            }

            if (sequence.IncrementBy != Sequence.DefaultIncrementBy)
            {
                lines.Add($".{nameof(SequenceBuilder.IncrementsBy)}({sequence.IncrementBy})");
            }

            if (sequence.MinValue != Sequence.DefaultMinValue)
            {
                lines.Add($".{nameof(SequenceBuilder.HasMin)}({sequence.MinValue})");
            }

            if (sequence.MaxValue != Sequence.DefaultMaxValue)
            {
                lines.Add($".{nameof(SequenceBuilder.HasMax)}({sequence.MaxValue})");
            }

            if (sequence.IsCyclic != Sequence.DefaultIsCyclic)
            {
                lines.Add($".{nameof(SequenceBuilder.IsCyclic)}()");
            }

            if (lines.Count == 2)
            {
                lines = new List<string>
                {
                    lines[0] + lines[1]
                };
            }

            sb.AppendLine();
            sb.Append(lines[0]);

            using (sb.Indent())
            {
                foreach (var line in lines.Skip(1))
                {
                    sb.AppendLine();
                    sb.Append(line);
                }
            }

            sb.AppendLine(";");
        }

        private string GenerateLambdaToKey(
            IReadOnlyList<IProperty> properties,
            string lambdaIdentifier)
        {
            if (properties.Count <= 0)
            {
                return "";
            }

            return properties.Count == 1
                ? $"{lambdaIdentifier}.{EntityTypeTransformationService.TransformPropertyName(properties[0].Name)}"
                : $"new {{ {string.Join(", ", properties.Select(p => lambdaIdentifier + "." + EntityTypeTransformationService.TransformPropertyName(p.Name)))} }}";
        }

        private void RemoveAnnotation(ref List<IAnnotation> annotations, string annotationName)
            => annotations.Remove(annotations.SingleOrDefault(a => a.Name == annotationName));

        private IList<string> GenerateAnnotations(IEnumerable<IAnnotation> annotations)
            => annotations.Select(GenerateAnnotation).ToList();

        private string GenerateAnnotation(IAnnotation annotation)
            => $".HasAnnotation({CSharpHelper.Literal(annotation.Name)}, " +
               $"{CSharpHelper.UnknownLiteral(annotation.Value)})";
    }
}

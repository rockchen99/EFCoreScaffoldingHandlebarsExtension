using System;
using System.Collections.Generic;
using System.Linq;
using EFCore.Scaffolding.Handlebars.Extension.Utils;
using EntityFrameworkCore.Scaffolding.Handlebars;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    public class HbsCSharpEntityTypeConfigurationGenerator : ICSharpEntityTypeConfigurationGenerator
    {
        private const string EntityLambdaIdentifier = "builder";
        private const string Language = "CSharp";

        /// <summary>
        /// CSharp helper.
        /// </summary>
        protected ICSharpHelper CSharpHelper { get; }

        /// <summary>
        /// Annotation code generator.
        /// </summary>
        protected IAnnotationCodeGenerator CodeGenerator { get; }

        /// <summary>
        /// Handlebars template data.
        /// </summary>
        protected Dictionary<string, object> TemplateData { get; private set; }

        /// <summary>
        /// Template service for the entity types generator.
        /// </summary>
        public virtual IEntityTypeConfigurationTemplateService EntityTypeConfigurationTemplateService { get; }

        /// <summary>
        /// Service for transforming entity definitions.
        /// </summary>
        public virtual IEntityTypeTransformationService EntityTypeTransformationService { get; }

        /// <summary>
        /// Constructor for the Handlebars entity types generator.
        /// </summary>
        /// <param name="entityTypeTemplateService">Template service for the entity types generator.</param>
        /// <param name="entityTypeTransformationService">Service for transforming entity definitions.</param>
        /// <param name="cSharpHelper">CSharp helper.</param>
        public HbsCSharpEntityTypeConfigurationGenerator(
            IAnnotationCodeGenerator annotationCodeGenerator,
            IEntityTypeConfigurationTemplateService entityTypeConfigurationTemplateService,
            IEntityTypeTransformationService entityTypeTransformationService,
            ICSharpHelper cSharpHelper)
        {
            CSharpHelper = cSharpHelper ?? throw new ArgumentNullException(nameof(cSharpHelper));
            CodeGenerator = annotationCodeGenerator ?? throw new ArgumentNullException(nameof(annotationCodeGenerator));
            EntityTypeConfigurationTemplateService = entityTypeConfigurationTemplateService ?? throw new ArgumentNullException(nameof(entityTypeConfigurationTemplateService));
            EntityTypeTransformationService = entityTypeTransformationService ?? throw new ArgumentNullException(nameof(entityTypeTransformationService));
        }

        /// <summary>
        /// Generate entity type class.
        /// </summary>
        /// <param name="entityType">Represents an entity type in an <see cref="T:Microsoft.EntityFrameworkCore.Metadata.IModel" />.</param>
        /// <param name="namespace">Entity type namespace.</param>
        /// <param name="useDataAnnotations">If true use data annotations.</param>
        /// <returns>Generated entity type.</returns>
        public virtual string WriteCode(IEntityType entityType, string @namespace, bool useDataAnnotations)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));
            if (@namespace == null) throw new ArgumentNullException(nameof(@namespace));

            TemplateData = new Dictionary<string, object>();

            GenerateImports(entityType);

            TemplateData.Add("namespace", @namespace);

            GenerateClassAndMap(entityType, useDataAnnotations);

            string output = EntityTypeConfigurationTemplateService.GenerateEntityTypeConfiguration(TemplateData);
            return output;
        }

        /// <summary>
        /// Generate entity type imports.
        /// </summary>
        /// <param name="entityType">Represents an entity type in an <see cref="T:Microsoft.EntityFrameworkCore.Metadata.IModel" />.</param>
        protected virtual void GenerateImports(IEntityType entityType)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            var imports = new List<Dictionary<string, object>>();

            foreach (var ns in entityType.GetProperties()
                .SelectMany(p => p.ClrType.GetNamespaces())
                .Where(ns => ns != "System" && ns != "System.Collections.Generic")
                .Distinct())
            {
                imports.Add(new Dictionary<string, object> { { "import", ns } });
            }

            TemplateData.Add("imports", imports);
        }

        /// <summary>
        /// Generate entity type class.
        /// </summary>
        /// <param name="entityType">Represents an entity type in an <see cref="T:Microsoft.EntityFrameworkCore.Metadata.IModel" />.</param>
        protected virtual void GenerateClassAndMap(IEntityType entityType, bool useDataAnnotations)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            var transformedEntityName = EntityTypeTransformationService.TransformEntityName(entityType.Name);

            TemplateData.Add("class", transformedEntityName);
            TemplateData.Add("class-map", transformedEntityName + "Map");

            GenerateConfiguration(entityType, useDataAnnotations);
        }

        /// <summary>
        /// Generate OnModelBuilding method.
        /// </summary>
        /// <param name="model">Metadata about the shape of entities, the relationships between them, and how they map to the database.</param>
        /// <param name="useDataAnnotations">Use fluent modeling API if false.</param>
        protected virtual void GenerateConfiguration(IEntityType entityType, bool useDataAnnotations)
        {
            if (entityType == null) throw new ArgumentNullException(nameof(entityType));

            var transformedEntityName = EntityTypeTransformationService.TransformEntityName(entityType.DisplayName());
            var sb = new IndentedStringBuilder();

            using (sb.Indent())
            {
                sb.IncrementIndent();
                sb.AppendLine($"public void Configure(EntityTypeBuilder<{transformedEntityName}> builder)");
                sb.Append("{");

                using (sb.Indent())
                {

                    GenerateEntityType(entityType, useDataAnnotations, sb);

                    //GenerateSequence(sequence, sb);

                    sb.AppendLine();
                }

                sb.Append("}");

                var configuration = sb.ToString();
                TemplateData.Add("configuration", configuration);
            }
        }

        private void GenerateEntityType(IEntityType entityType, bool useDataAnnotations, IndentedStringBuilder sb)
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

        private void AppendMultiLineFluentApi(IEntityType entityType, IList<string> lines, IndentedStringBuilder sb)
        {
            if (lines.Count <= 0)
            {
                return;
            }

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

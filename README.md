# 实体框架核心脚手架与把手的扩展(EFCoreScaffoldingHandlebarsExtension)
目的：基于Entity Framework Core Scaffolding with Handlebars扩展，使Flute API 配置与DbContext分离，避免因数据表太多造成生成的数据上下文类型文件过大

扩展：
- 扩展CSharpEntityTypeConfiguration模板
 
## 先决条件

- [Visual Studio 2017](https://www.visualstudio.com/downloads/) 15.9 or greater.
- The .[NET Core 2.2 SDK](https://www.microsoft.com/net/download/core) (version 2.2.100 or greater).
- The .[EntityFrameworkCore.Scaffolding.Handlebars v1.7.2](https://github.com/TrackableEntities/EntityFrameworkCore.Scaffolding.Handlebars) ( v1.7.2).

## EntityFrameworkCore.Scaffolding.Handlebars介绍

1. GitHub：https://github.com/TrackableEntities/EntityFrameworkCore.Scaffolding.Handlebars
2. 作者博客：https://blog.tonysneed.com/2018/05/27/customize-ef-core-scaffolding-with-handlebars-templates/

## 扩展的配置

```csharp
        /// <summary>原有配置</summary>
        public ReverseEngineerOptions ReverseEngineerOptions { get; set; }
        /// <summary> 是否分离FluteAPI</summary>
        public bool RefineFluteAPI { get; set; }
        /// <summary> 是否包含连接字符串 </summary>
        public bool IncludeConnectionString { get; set; }
        /// <summary> 分离的FluteAPI 配置目录后缀 </summary>
        public string EntityTypeConfigurationDirSuffix { get; set; }
        /// <summary> 分离的FluteAPI 配置类型文件名称缀 </summary>
        public string EntityTypeConfigurationFileNameSuffix { get; set; }
        /// <summary> 配置对应的数据表（schema.tablename） </summary>
        public ICollection<string> Tables { get; set; }
        /// <summary> 配置对应的架构（schema） </summary>
        public ICollection<string> Schemas { get; set; }
```

## 用法
1. 在目标项目中新建ScaffoldingDesignTimeServices类型，实现IDesignTimeServices接口
2. 创建HandlebarsScaffoldingOptions配置对象
3. 指定数据表AddHandlebarsScaffolding.Tables（注意：不指定表示所有数据表，也可以使用命令的-t 选项指定数据表，指定数据表时最好加上schema，如：dbo.Category）
4. 指定数据库架构（如果指定了schema将会表示所有同schema的数据表，所以Tables将失效）
5. 注册服务:AddHandlebarsScaffolding(this IServiceCollection services, HandlebarsScaffoldingOptions options=null)
6. 编写其他扩展
```csharp
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
```

## 示例/samples
- 数据库脚本：EFCore.mdf
- Powershell
```csharp
dotnet ef dbcontext scaffold "Data Source=.;Initial Catalog=EFCoreDb;Persist Security Info=True;User ID=sa;Password=**********;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c NorthwindSlimContext --context-dir Contexts -f -d
```


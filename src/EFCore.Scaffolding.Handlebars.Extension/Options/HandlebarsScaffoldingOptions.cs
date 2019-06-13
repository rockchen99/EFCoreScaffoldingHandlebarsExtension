using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EFCore.Scaffolding.Handlebars.Extension.Options
{
   public class HandlebarsScaffoldingOptions
    {
        public static HandlebarsScaffoldingOptions Default = new HandlebarsScaffoldingOptions
        {
            ReverseEngineerOptions = ReverseEngineerOptions.DbContextAndEntities,
            RefineFluteAPI = true,
            IncludeConnectionString = false,
            EntityTypeConfigurationDirSuffix = "Maps",
            EntityTypeConfigurationFileNameSuffix = "Map",
            //Schemas = new List<string> { "dbo"}
        };

        public ReverseEngineerOptions ReverseEngineerOptions { get; set; }

        /// <summary> 是否分离FluteAPI</summary>
        public bool RefineFluteAPI { get; set; }

        /// <summary> 是否包含连接字符串 </summary>
        public bool IncludeConnectionString { get; set; }

        /// <summary> 分离的FluteAPI 配置目录后缀 </summary>
        public string EntityTypeConfigurationDirSuffix { get; set; }

        /// <summary> 分离的FluteAPI 配置类型文件名称缀 </summary>
        public string EntityTypeConfigurationFileNameSuffix { get; set; }

        public ICollection<string> Tables { get; set; }
        public ICollection<string> Schemas { get; set; }

        public HandlebarsScaffoldingOptions()
        {
            Tables = new List<string>();
            Schemas= new List<string>();
        }
    }
}

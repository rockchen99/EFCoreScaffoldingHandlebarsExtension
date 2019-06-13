using EFCore.Scaffolding.Handlebars.Extension.Options;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Scaffolding.Handlebars.Extension
{
    public class ScaffoldingDesignTimeServices : IDesignTimeServices
    {
        public void ConfigureDesignTimeServices(IServiceCollection services)
        {
            var options = HandlebarsScaffoldingOptions.Default;
            services.AddHandlebarsScaffolding(options);
        }
    }
}
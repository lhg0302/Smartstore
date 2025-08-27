global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Smartstore.Domain;
global using Smartstore.Engine;
global using EfState = Microsoft.EntityFrameworkCore.EntityState;
global using LogLevel = Smartstore.Core.Logging.LogLevel;
global using EntityState = Smartstore.Data.EntityState;
using System.Text;
using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Smartstore.Bootstrapping;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.JsonConverters;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using Smartstore.Engine.Builders;
using Smartstore.Templating;
using Smartstore.Templating.Liquid;

namespace Smartstore.Core.Bootstrapping
{
    internal class CoreStarter : StarterBase
    {
        public override int Order => (int)StarterOrdering.Early;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            var config = appContext.AppConfiguration;
            
            // Type converters
            RegisterTypeConverters();

            // Default Json serializer settings
            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = SmartContractResolver.Instance,
                    TypeNameHandling = TypeNameHandling.None,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Auto,
                    NullValueHandling = NullValueHandling.Ignore,
                    MaxDepth = 32
                };

                settings.Converters.Add(new UTCDateTimeConverter(new IsoDateTimeConverter()));
                settings.Converters.Add(new StringEnumConverter());

                return settings;
            };

            // CodePages dependency required by ExcelDataReader to avoid NotSupportedException "No data is available for encoding 1252."
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            services.AddDisplayControl();
            services.AddWkHtmlToPdf();

            services.AddSingleton<ISqlSugarClient>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                return new SqlSugarClient(new ConnectionConfig
                {
                    ConnectionString = connectionString,
                    DbType = DbType.SqlServer,
                    IsAutoCloseConnection = true
                });
            });
        }

        internal static void RegisterTypeConverters()
        {
            TypeConverterFactory.Providers.Insert(0, new ProductBundleItemOrderDataConverterProvider());
            TypeConverterFactory.Providers.Insert(0, new ShippingOptionConverterProvider());
            TypeConverterFactory.Providers.Insert(0, new GiftCardCouponCodeConverterProvider());
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<LiquidTemplateEngine>().As<ITemplateEngine>().SingleInstance();

            builder.RegisterModule(new LoggingModule());
            builder.RegisterModule(new PackagingModule());
            builder.RegisterModule(new CommonServicesModule());
            builder.RegisterModule(new DbHooksModule(appContext));
            builder.RegisterModule(new DbQuerySettingsModule());
            builder.RegisterModule(new StoresModule());
        }
    }
}

﻿using System;
using System.Linq;
using System.Web.Http;
using System.Net.Http;
using System.Collections.Generic;
using System.Web.Http.Routing;
using Newtonsoft.Json;

namespace Swashbuckle.Application
{
    public static class HttpConfigurationExtensions
    {
        private static readonly string DefaultRouteTemplate = "swagger/docs/{*documentName}";

        public static SwaggerEnabledConfiguration EnableSwagger(
            this HttpConfiguration httpConfig,
            Action<SwaggerDocsConfig> configure = null)
        {
            return EnableSwagger(httpConfig, DefaultRouteTemplate, configure);
        }

        public static SwaggerEnabledConfiguration EnableSwagger(
            this HttpConfiguration httpConfig,
            string routeTemplate,
            Action<SwaggerDocsConfig> configure = null)
        {
            var config = new SwaggerDocsConfig();
			configure?.Invoke(config);

			httpConfig.Routes.MapHttpRoute(
                name: "swagger_docs" + routeTemplate,
                routeTemplate: routeTemplate,
                defaults: null,
                constraints: new { documentName = @".+" },
                handler: new SwaggerDocsHandler(config)
            );

            return new SwaggerEnabledConfiguration(
                httpConfig,
                config.GetRootUrl,
                config.GetSwaggerDocs().Select(doc => routeTemplate.Replace("{*documentName}", doc.Key)));
        }

        internal static JsonSerializerSettings SerializerSettingsOrDefault(this HttpConfiguration httpConfig)
        {
            var formatter = httpConfig.Formatters.JsonFormatter;
            if (formatter != null)
                return formatter.SerializerSettings;

            return new JsonSerializerSettings();
        }
    }

    public class SwaggerEnabledConfiguration
    {
        private static readonly string DefaultRouteTemplate = "swagger/ui/{*assetPath}";

        private readonly HttpConfiguration _httpConfig;
        private readonly Func<HttpRequestMessage, string> _rootUrlResolver;
        private readonly IEnumerable<string> _discoveryPaths;

        public SwaggerEnabledConfiguration(
            HttpConfiguration httpConfig,
            Func<HttpRequestMessage, string> rootUrlResolver,
            IEnumerable<string> discoveryPaths)
        {
            _httpConfig = httpConfig;
            _rootUrlResolver = rootUrlResolver;
            _discoveryPaths = discoveryPaths;
        }

        public void EnableSwaggerUi(Action<SwaggerUiConfig> configure = null)
        {
            EnableSwaggerUi(DefaultRouteTemplate, configure);
        }

        public void EnableSwaggerUi(
            string routeTemplate,
            Action<SwaggerUiConfig> configure = null)
        {
            var config = new SwaggerUiConfig(_discoveryPaths, _rootUrlResolver);
			configure?.Invoke(config);

			_httpConfig.Routes.MapHttpRoute(
                name: "swagger_ui" + routeTemplate,
                routeTemplate: routeTemplate,
                defaults: null,
                constraints: new { assetPath = @".+" },
                handler: new SwaggerUiHandler(config)
            );

            if (routeTemplate == DefaultRouteTemplate)
            {
                _httpConfig.Routes.MapHttpRoute(
                    name: "swagger_ui_shortcut",
                    routeTemplate: "swagger",
                    defaults: null,
                    constraints: new { uriResolution = new HttpRouteDirectionConstraint(HttpRouteDirection.UriResolution) },
                    handler: new RedirectHandler(_rootUrlResolver, "swagger/ui/index"));
            }
        }
    }
}
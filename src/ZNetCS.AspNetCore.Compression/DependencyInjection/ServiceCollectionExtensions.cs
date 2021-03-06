﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Marcin Smółka zNET Computer Solutions">
//   Copyright (c) Marcin Smółka zNET Computer Solutions. All rights reserved.
// </copyright>
// <summary>
//   The service collection extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ZNetCS.AspNetCore.Compression.DependencyInjection
{
    #region Usings

    using System;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    using ZNetCS.AspNetCore.Compression.Infrastructure;

    #endregion

    /// <summary>
    /// The service collection extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Public Methods

        /// <summary>
        /// Adds compression executor services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> to add services to.
        /// </param>
        /// <returns>
        /// The <see cref="IServiceCollection"/> so that additional calls can be chained.
        /// </returns>
        public static IServiceCollection AddCompression(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            services.TryAddSingleton<CompressionExecutor>();
            services.TryAddSingleton<DecompressionExecutor>();

            return services;
        }

        #endregion
    }
}
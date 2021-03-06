﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompressionMiddleware.cs" company="Marcin Smółka zNET Computer Solutions">
//   Copyright (c) Marcin Smółka zNET Computer Solutions. All rights reserved.
// </copyright>
// <summary>
//   The compression middleware.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ZNetCS.AspNetCore.Compression
{
    #region Usings

    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using ZNetCS.AspNetCore.Compression.Infrastructure;

    #endregion

    /// <summary>
    /// The compression middleware.
    /// </summary>
    public class CompressionMiddleware
    {
        #region Fields

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// The next.
        /// </summary>
        private readonly RequestDelegate next;

        /// <summary>
        /// The options.
        /// </summary>
        private readonly CompressionOptions options;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressionMiddleware"/> class.
        /// </summary>
        /// <param name="next">
        /// The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.
        /// </param>
        /// <param name="loggerFactory">
        /// The logger factory.
        /// </param>
        /// <param name="options">
        /// The <see cref="CompressionOptions"/> representing the options for the <see cref="CompressionMiddleware"/>.
        /// </param>
        public CompressionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<CompressionOptions> options)
        {
            this.next = next;
            this.logger = loggerFactory.CreateLogger<CompressionMiddleware>();
            this.options = options.Value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Invokes middleware.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/> context.
        /// </param>
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CancellationToken cancellationToken = context.RequestAborted;
            var decompressionExecutor = context.RequestServices.GetRequiredService<DecompressionExecutor>();

            // first decompress incoming request
            this.logger.LogInformation("Checking request for decompression: " + context.Request.Path);
            if (decompressionExecutor.CanDecompress(context, this.options.Decompressors))
            {
                await decompressionExecutor.ExecuteAsync(context, this.options.Decompressors, cancellationToken);
            }

            this.logger.LogInformation("Checking response for compression: " + context.Request.Path);
            var compressionExecutor = context.RequestServices.GetRequiredService<CompressionExecutor>();

            // check we are supporting accepted encodings and request path is not ignored
            if (compressionExecutor.CanCompress(context, this.options.IgnoredPaths) && compressionExecutor.CanCompress(context, this.options.Compressors))
            {
                using (var bufferedStream = new MemoryStream())
                {
                    Stream bodyStream = context.Response.Body;
                    context.Response.Body = bufferedStream;

                    await this.next.Invoke(context);

                    context.Response.Body = bodyStream;
                    bufferedStream.Seek(0, SeekOrigin.Begin);

                    // skip compression for small requests, and not allowed media types
                    if ((bufferedStream.Length < this.options.MinimumCompressionThreshold) || !compressionExecutor.CanCompress(context, this.options.AllowedMediaTypes))
                    {
                        // simply copy buffed value to output stream
                        await bufferedStream.CopyToAsync(context.Response.Body, Consts.DefaultBufferSize, cancellationToken);
                    }
                    else
                    {
                        // compress buffered stream directly to output body
                        await compressionExecutor.ExecuteAsync(context, bufferedStream, this.options.Compressors, cancellationToken);
                    }
                }
            }
            else
            {
                this.logger.LogInformation("Continue response without compression");
                await this.next.Invoke(context);
            }

            this.logger.LogInformation("Finished handling request.");
        }

        #endregion
    }
}
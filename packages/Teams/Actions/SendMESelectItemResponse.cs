﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Teams.Actions
{
    /// <summary>
    /// Send a messaging extension 'message' response.
    /// </summary>
    public class SendMESelectItemResponse : BaseTeamsCacheInfoResponseDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendMESelectItemResponse"/> class.
        /// </summary>
        /// <param name="callerPath">Optional, source file full path.</param>
        /// <param name="callerLine">Optional, line number in source file.</param>
        [JsonConstructor]
        public SendMESelectItemResponse([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Teams.SendMESelectItemResponse";

        /// <summary>
        /// Gets or sets template for the expression containing a Hero Card or Adaptive Card to send.
        /// </summary>
        /// <value>
        /// Template for the card.
        /// </value>
        [JsonProperty("card")]
        public ITemplate<Activity> Card { get; set; }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (Disabled != null && Disabled.GetValue(dc.State))
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            if (Card == null)
            {
                throw new ArgumentException($"A valid card is required for {Kind}.");
            }

            var activity = await Card.BindAsync(dc, dc.State).ConfigureAwait(false);
            if (activity?.Attachments?.Any() != true)
            {
                throw new InvalidOperationException($"Invalid activity. An attachment is required for {Kind}.");
            }
            
            var attachment = activity.Attachments[0];
            var extensionAttachment = new MessagingExtensionAttachment(attachment.ContentType, null, attachment.Content);

            var response = new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = MEResultResponseType.result.ToString(),
                    AttachmentLayout = MEAttachmentLayoutResponseType.list.ToString(), // TODO: enum this
                    Attachments = new List<MessagingExtensionAttachment> { extensionAttachment }
                },
                CacheInfo = GetCacheInfo(dc),
            };

            var invokeResponse = CreateInvokeResponseActivity(response);
            ResourceResponse sendResponse = await dc.Context.SendActivityAsync(invokeResponse, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await dc.EndDialogAsync(sendResponse, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override string OnComputeId()
        {
            return $"{GetType().Name}[{Card?.ToString() ?? string.Empty}]";
        }
    }
}

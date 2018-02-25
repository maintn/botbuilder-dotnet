﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Middleware;

namespace Microsoft.Bot.Builder
{
    public class BotContext : IBotContext
    {
        private readonly BotAdapter _adapter;
        private readonly Activity _request;
        private readonly ConversationReference _conversationReference;
        private IList<Activity> _responses = new List<Activity>();
        private Dictionary<string, object> _services = new Dictionary<string, object>();

        public BotContext(BotAdapter adapter, Activity request)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _request = request ?? throw new ArgumentNullException(nameof(request));

            _conversationReference = new ConversationReference()
            {
                ActivityId = request.Id,
                User = request.From,
                Bot = request.Recipient,
                Conversation = request.Conversation,
                ChannelId = request.ChannelId,
                ServiceUrl = request.ServiceUrl
            };
        }

        public BotContext(BotAdapter bot, ConversationReference conversationReference)
        {
            _adapter = bot ?? throw new ArgumentNullException(nameof(bot));
            _conversationReference = conversationReference ?? throw new ArgumentNullException(nameof(conversationReference));
        }
        public BotAdapter Adapter => _adapter;

        public Activity Request => _request;

        public IList<Activity> Responses { get => _responses; set => this._responses = value; }

        public ConversationReference ConversationReference { get => _conversationReference; }

        public IBotContext Reply(string text, string speak = null)
        {
            var reply = this.ConversationReference.GetPostToUserMessage();
            reply.Text = text;
            if (!string.IsNullOrWhiteSpace(speak))
            {
                // Developer included SSML to attach to the message.
                reply.Speak = speak;
            }
            this.Responses.Add(reply);
            return this;
        }

        public IBotContext Reply(IActivity activity)
        {
            BotAssert.ActivityNotNull(activity);
            this.Responses.Add((Activity)activity);
            return this;
        }

        public void Set(string serviceId, object service)
        {
            if (String.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentNullException(nameof(serviceId));
            lock (_services)
            {
                this._services[serviceId] = service;
            }
        }

        public object Get(string serviceId)
        {
            if (String.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentNullException(nameof(serviceId));
            object service = null;
            lock (_services)
            {
                this._services.TryGetValue(serviceId, out service);
            }
            return service;
        }
    }

    /// <summary>
    /// Utility class to allow you to create custom BotContext wrapper
    /// </summary>
    public class BotContextWrapper : IBotContext
    {
        private IBotContext _innerContext;

        public BotContextWrapper(IBotContext context)
        {
            this._innerContext = context;
        }

        public BotAdapter Adapter => this._innerContext.Adapter; 

        public Activity Request => this._innerContext.Request; 

        public IList<Activity> Responses { get => this._innerContext.Responses; set => this._innerContext.Responses = value; }

        public ConversationReference ConversationReference => this._innerContext.ConversationReference;

        public object Get(string serviceId)
        {
            return this._innerContext.Get(serviceId);
        }

        public IBotContext Reply(string text, string speak = null)
        {
            this._innerContext.Reply(text, speak);
            return this;
        }

        public IBotContext Reply(IActivity activity)
        {
            this._innerContext.Reply(activity);
            return this;
        }

        public void Set(string serviceId, object service)
        {
            this._innerContext.Set(serviceId, service);
        }
    }
}

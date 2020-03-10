﻿#if NET45 || NET451 || NET452 ||NET461
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Concurrent;

namespace CodeArts.SignalR
{
    /// <summary>
    /// 信箱模块
    /// </summary>
    public class MailPipelineModule : HubPipelineModule
    {
        private readonly IMail mail;
        private static readonly ConcurrentDictionary<Type, HubDescriptor> hubConnections = new ConcurrentDictionary<Type, HubDescriptor>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public MailPipelineModule(IMail mail)
        {
            this.mail = mail ?? throw new ArgumentNullException(nameof(mail));
        }

        /// <summary>
        /// 认证链接前。
        /// </summary>
        /// <param name="hubDescriptor">消息中心信息</param>
        /// <param name="request">请求</param>
        /// <returns></returns>
        protected override bool OnBeforeAuthorizeConnect(HubDescriptor hubDescriptor, IRequest request)
        {
            return base.OnBeforeAuthorizeConnect(hubConnections.GetOrAdd(hubDescriptor.HubType, hubDescriptor), request);
        }

        /// <summary>
        /// 连接成功后做一些事情。
        /// </summary>
        /// <param name="hub">消息中心</param>
        protected override void OnAfterConnect(IHub hub)
        {
            var request = hub.Context.Request;

            if ((request.User?.Identity?.IsAuthenticated ?? false) && hubConnections.TryGetValue(hub.GetType(), out HubDescriptor descriptor))
            {
                string userId = request.User.Identity.Name;

                mail.Send($"hu-{descriptor.Name}.{userId}", massage =>
                {
                    var userProxy = (UserProxy)hub.Clients.User(userId);

                    return userProxy.Invoke(massage.Method, massage.Args);
                });
            }

            base.OnAfterConnect(hub);
        }

        /// <summary>
        /// 连接成功后做一些事情。
        /// </summary>
        /// <param name="hub">消息中心</param>
        protected override void OnAfterReconnect(IHub hub)
        {
            var request = hub.Context.Request;

            if (hubConnections.TryGetValue(hub.GetType(), out HubDescriptor descriptor))
            {
                if (request.User?.Identity?.IsAuthenticated ?? false)
                {
                    string userId = request.User.Identity.Name;

                    mail.Send($"hu-{descriptor.Name}.{userId}", massage =>
                    {
                        var userProxy = (UserProxy)hub.Clients.User(userId);

                        return userProxy.Invoke(massage.Method, massage.Args);
                    });
                }
                else
                {
                    string connectionId = hub.Context.ConnectionId;

                    mail.Send($"hc-{descriptor.Name}.{connectionId}", massage =>
                    {
                        var clientProxy = (ConnectionIdProxy)hub.Clients.Client(connectionId);

                        return clientProxy.Invoke(massage.Method, massage.Args);
                    });
                }
            }

            base.OnAfterReconnect(hub);
        }
    }
}
#endif
﻿// -----------------------------------------------------------------------
// <copyright file="ControllerRouteHandler.cs" company="Andrew Forrest">©2013 Andrew Forrest</copyright>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may
// not use this file except in compliance with the License. Copy of
// license at: http://www.apache.org/licenses/LICENSE-2.0
//
// This software is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES 
// OR CONDITIONS. See License for specific permissions and limitations.
// -----------------------------------------------------------------------
namespace Dysphoria.Net.UrlRouting.Handlers
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Web.Mvc;
	using System.Web.Mvc.Async;
	using System.Web.Routing;
	using System.Web.SessionState;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class ControllerRouteHandler<C> : AbstractRouteHandler
		where C : ControllerBase
	{
		private static readonly ConcurrentDictionary<Type, SessionStateBehavior> _sessionStateCache = new ConcurrentDictionary<Type, SessionStateBehavior>();
		private readonly AbstractRequestPattern pattern;
		private readonly string controllerName, actionName;
		private readonly Type controllerType;

        public ControllerRouteHandler(AbstractRequestPattern pattern, string controllerName, string actionName)
		{
			this.pattern = pattern;
			this.controllerName = controllerName;
			this.actionName = actionName;
			this.controllerType = typeof(C);
		}

		protected override AbstractUrlPattern UrlPattern
		{
			get { return this.pattern.Url; }
		}

		protected override SessionStateBehavior GetSessionStateBehavior(RequestContext requestContext)
		{
			return GetControllerSessionBehavior(requestContext, this.controllerType);
		}

		protected override void ProcessRequest(RequestContext context)
		{
			var controller = Activator.CreateInstance<C>();
			var disposable = controller as IDisposable;
			try
			{
				var fullController = controller as Controller;
                //if (fullController != null)
                //{
					fullController.ActionInvoker = new Invoker(this);
					this.SetUpRouteData(context.RouteData);
					var asyncController = (IAsyncController)fullController;
					var handle = asyncController.BeginExecute(
						context,
						(asyncResult) => { asyncController.EndExecute(asyncResult); },
						null);
                //}
                //else
                //{
                //    ProcessRequestMinimally(new ControllerContext(context, controller), controller);
                //}
			}
			finally
			{
				if (disposable != null) disposable.Dispose();
			}
		}

		/// <summary>
		/// Processes request, but without involving any filters. Therefore authentication
		/// will probably not work. This is only practically any use for testing.
		/// </summary>
        //private void ProcessRequestMinimally(ControllerContext controllerContext, C controller)
        //{
        //    this.SetUpRouteData(controllerContext.RouteData);
        //    controller.ControllerContext = controllerContext;
        //    var result = this.handler.Invoke(controller, controllerContext, null);

        //    result.ExecuteResult(controllerContext);
        //}

		private void SetUpRouteData(RouteData routeData)
		{
			routeData.Values["controller"] = this.controllerName;
			routeData.Values["action"] = this.actionName;
		}

		protected internal virtual SessionStateBehavior GetControllerSessionBehavior(RequestContext requestContext, Type controllerType)
		{
			return _sessionStateCache.GetOrAdd(
				controllerType,
				type =>
				{
					var attr = type.GetCustomAttributes(typeof(SessionStateAttribute), inherit: true)
						.OfType<SessionStateAttribute>()
						.FirstOrDefault();

					return (attr != null) ? attr.Behavior : SessionStateBehavior.Default;
				});
		}

		private class Invoker : ControllerActionInvoker
		{
			private readonly ControllerRouteHandler<C> outer;

			public Invoker(ControllerRouteHandler<C> outer)
			{
				this.outer = outer;
			}

            protected override IDictionary<string, object> GetParameterValues(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
            {
                Dictionary<string, object> parametersDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                ParameterDescriptor[] parameterDescriptors = actionDescriptor.GetParameters();
                AbstractUrlPattern p = this.outer.UrlPattern;
                

                for (int i = 0; i < parameterDescriptors.Length; i++)
                {
                    ParameterDescriptor parameterDescriptor = parameterDescriptors[i];
                    String actionParameterName = parameterDescriptor.ParameterName;
                    parametersDict[actionParameterName] = GetParameterValue(controllerContext, 
                        i < p.Arity ? new PathParameterDescriptor(parameterDescriptor, p.ParameterName(i)) : parameterDescriptor);
                }
                return parametersDict;
            }
		}

        private class PathParameterDescriptor : ParameterDescriptor
        {
            private readonly ParameterDescriptor original;
            private readonly string _ParameterName;

            public PathParameterDescriptor(ParameterDescriptor original, string parameterName)
            {
                this.original = original;
                this._ParameterName = parameterName;
            }

            public override ActionDescriptor ActionDescriptor
            {
                get { return original.ActionDescriptor; }
            }

            public override string ParameterName
            {
                get { return this._ParameterName; }
            }

            public override Type ParameterType
            {
                get { return original.ParameterType; }
            }
        }
	}
}
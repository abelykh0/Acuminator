﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Walkers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Acuminator.Analyzers.StaticAnalysis.LongOperationStart
{
	public class StartLongOperationDelegateWalker : DelegatesWalkerBase
	{
		private readonly HashSet<SyntaxNode> _delegates = new HashSet<SyntaxNode>();
		private readonly PXContext _pxContext;

		public ImmutableArray<SyntaxNode> Delegates => _delegates.ToImmutableArray();

		public StartLongOperationDelegateWalker(PXContext pxContext, Compilation compilation, CancellationToken cancellation)
			: base(compilation, cancellation, pxContext.CodeAnalysisSettings)
		{
			_pxContext = pxContext.CheckIfNull(nameof(pxContext));
		}

		public override void VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			ThrowIfCancellationRequested();

			IMethodSymbol? methodSymbol = GetSymbol<IMethodSymbol>(node);

			if (methodSymbol == null || !_pxContext.StartOperation.Contains(methodSymbol))
			{
				base.VisitInvocationExpression(node);
			}

			var delegateExists = node.ArgumentList?.Arguments.Count > 1;

			if (!delegateExists)
			{
				return;
			}

			var firstArgument = node.ArgumentList!.Arguments[1].Expression;
			if (firstArgument == null)
			{
				return;
			}

			var delegateBody = GetDelegateNode(firstArgument);
			if (delegateBody == null)
			{
				return;
			}

			_delegates.Add(delegateBody);
		}
	}
}
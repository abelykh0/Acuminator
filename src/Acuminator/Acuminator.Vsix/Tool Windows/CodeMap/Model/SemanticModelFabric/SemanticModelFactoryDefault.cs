﻿using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Semantic.Dac;
using Acuminator.Utilities.Roslyn.Semantic.PXGraph;
using Acuminator.Utilities.Common;


namespace Acuminator.Vsix.ToolWindows.CodeMap
{
	/// <summary>
	/// Default implementation for <see cref="ISemanticModelFactory"/> interface.
	/// </summary>
	public class SemanticModelFactoryDefault : ISemanticModelFactory
	{
		/// <summary>
		/// Try to infer semantic model for <paramref name="typeSymbol"/>. 
		/// If semantic model can't be inferred the <paramref name="semanticModel"/> is null and the method returns false.
		/// </summary>
		/// <param name="typeSymbol">The type symbol.</param>
		/// <param name="context">The context.</param>
		/// <param name="semanticModel">[out] The inferred semantic model.</param>
		/// <param name="cancellationToken">(Optional) A token that allows processing to be cancelled.</param>
		/// <returns>
		/// True if it succeeds, false if it fails.
		/// </returns>
		public virtual bool TryToInferSemanticModel(INamedTypeSymbol typeSymbol, PXContext context, out ISemanticModel semanticModel, 
													CancellationToken cancellationToken = default)
		{
			typeSymbol.ThrowOnNull(nameof(typeSymbol));
			context.ThrowOnNull(nameof(context));
			cancellationToken.ThrowIfCancellationRequested();

			if (typeSymbol.IsPXGraphOrExtension(context))
			{
				return TryToInferGraphOrGraphExtensionSemanticModel(typeSymbol, context, out semanticModel, cancellationToken);
			}
			else if (typeSymbol.IsDacOrExtension(context))
			{
				return TryToInferDacOrDacExtensionSemanticModel(typeSymbol, context, out semanticModel, cancellationToken);
			}

			semanticModel = null;
			return false;
		}

		protected virtual bool TryToInferGraphOrGraphExtensionSemanticModel(INamedTypeSymbol graphSymbol, PXContext context, 
																		    out ISemanticModel graphSemanticModel,
																			CancellationToken cancellationToken = default)
		{
			var graphSimpleModel = PXGraphEventSemanticModel.InferModels(context, graphSymbol, cancellationToken).FirstOrDefault();

			if (graphSimpleModel == null || graphSimpleModel.Type == GraphType.None)
			{
				graphSemanticModel = null;
				return false;
			}

			graphSemanticModel = new GraphSemanticModelForCodeMap(graphSimpleModel, context);
			return true;
		}

		protected virtual bool TryToInferDacOrDacExtensionSemanticModel(INamedTypeSymbol dacSymbol, PXContext context,
																		out ISemanticModel dacSemanticModel,
																		CancellationToken cancellationToken = default)
		{
			dacSemanticModel = DacSemanticModel.InferModel(context, dacSymbol, cancellationToken);
			return dacSemanticModel != null;
		}
	}
}

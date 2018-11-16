﻿using Acuminator.Analyzers.StaticAnalysis.AnalyzersAggregator;
using Acuminator.Utilities;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Semantic.PXGraph;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Acuminator.Analyzers.StaticAnalysis.PXGraph
{
    public interface IPXGraphAnalyzer : ISymbolAnalyzer
    {
        void Analyze(SymbolAnalysisContext context, PXContext pxContext, CodeAnalysisSettings settings, PXGraphSemanticModel pxGraph);

		/// <summary>
		/// Determine if the analyzer should run on graph.
		/// </summary>
		/// <param name="pxContext">Context.</param>
		/// <param name="settings">Options for controlling the analysis.</param>
		/// <param name="pxGraph">The graph semantic model.</param>
		/// <returns/>
		bool ShouldAnalyze(PXContext pxContext, CodeAnalysisSettings settings, PXGraphSemanticModel graph);
	}
}

﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Acuminator.Utilities;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.DiagnosticSuppression;
using Acuminator.Utilities.Roslyn.Constants;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Semantic.Dac;
using Acuminator.Utilities.Roslyn.Syntax;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Acuminator.Analyzers.StaticAnalysis.PublicClassXmlComment
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PublicClassXmlCommentAnalyzer : PXDiagnosticAnalyzer
	{
		internal enum FixOption
		{
			NoXmlComment,
			NoSummaryTag,
			EmptySummaryTag
		}

		public const string XmlCommentExcludeTag = "exclude";
		public static readonly string XmlCommentSummaryTag = SyntaxFactory.XmlSummaryElement().StartTag.Name.ToFullString();
		internal const string FixOptionKey = nameof(FixOption);

		private static readonly string[] _xmlCommentSummarySeparators = { SyntaxFactory.DocumentationComment().ToFullString() };

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Descriptors.PX1007_PublicClassXmlComment);

		public PublicClassXmlCommentAnalyzer(CodeAnalysisSettings codeAnalysisSettings) :
										base(codeAnalysisSettings)
		{
		}

		public PublicClassXmlCommentAnalyzer() : 
										this(null)
		{
		}

		protected override bool ShouldAnalyze(PXContext pxContext) =>
			pxContext.CodeAnalysisSettings.PX1007DocumentationDiagnosticEnabled &&
			base.ShouldAnalyze(pxContext);
			
		internal override void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext, PXContext pxContext)
		{
			compilationStartContext.RegisterSyntaxNodeAction(context => AnalyzeCompilationUnit(context, pxContext),
															 SyntaxKind.CompilationUnit);
		}

		private void AnalyzeCompilationUnit(SyntaxNodeAnalysisContext syntaxContext, PXContext pxContext)
		{
			syntaxContext.CancellationToken.ThrowIfCancellationRequested();

			if (!(syntaxContext.Node is CompilationUnitSyntax compilationUnitSyntax))
				return;

			var commentsWalker = new XmlCommentsWalker(syntaxContext, pxContext, CodeAnalysisSettings);
			compilationUnitSyntax.Accept(commentsWalker);
		}
	}
}

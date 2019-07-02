﻿using Acuminator.Analyzers.StaticAnalysis;
using Acuminator.Analyzers.StaticAnalysis.DacDeclaration;
using Acuminator.Analyzers.StaticAnalysis.EventHandlers;
using Acuminator.Tests.Helpers;
using Acuminator.Tests.Verification;
using Acuminator.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Acuminator.Tests.Tests.StaticAnalysis.DacDeclaration
{
	public class DacForbiddenFieldsNonISVTests : CodeFixVerifier
	{

		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() =>
			new DacDeclarationAnalyzer(CodeAnalysisSettings.Default
				.WithIsvSpecificAnalyzersDisabled());

		protected override CodeFixProvider GetCSharpCodeFixProvider() =>
			new ForbiddenFieldsInDacFix();

		[Theory]
		[EmbeddedFileData("DacForbiddenFields.cs")]
		public virtual void TestDacWithForbiddenFields(string source) =>
			VerifyCSharpDiagnostic(source,
				Descriptors.PX1027_ForbiddenFieldsInDacDeclaration.CreateFor(13, 25, "companyId"),
				Descriptors.PX1027_ForbiddenFieldsInDacDeclaration.CreateFor(17, 17, "CompanyID"),
				Descriptors.PX1027_ForbiddenFieldsInDacDeclaration_NonISV.CreateFor(27, 25, "deletedDatabaseRecord"),
				Descriptors.PX1027_ForbiddenFieldsInDacDeclaration_NonISV.CreateFor(30, 17, "DeletedDatabaseRecord"),
				Descriptors.PX1027_ForbiddenFieldsInDacDeclaration.CreateFor(39, 25, "companyMask"),
				Descriptors.PX1027_ForbiddenFieldsInDacDeclaration.CreateFor(42, 17, "CompanyMask"));
	}
}

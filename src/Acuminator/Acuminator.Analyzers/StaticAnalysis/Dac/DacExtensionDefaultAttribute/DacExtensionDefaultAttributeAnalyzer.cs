﻿using Acuminator.Utilities.DiagnosticSuppression;
using Acuminator.Utilities.Roslyn.PXFieldAttributes;
using Acuminator.Utilities.Roslyn.Semantic;
using Acuminator.Utilities.Roslyn.Semantic.Dac;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Acuminator.Utilities.Roslyn.Constants;
using Acuminator.Analyzers.StaticAnalysis.Dac;
using System.Collections.Generic;

namespace Acuminator.Analyzers.StaticAnalysis.DacExtensionDefaultAttribute
{

	public enum PXPersistingCheckValues
	{
		Null = 0,
		NullOrBlank = 1,
		Nothing = 2
	}

	public class DacExtensionDefaultAttributeAnalyzer : DacAggregatedAnalyzerBase
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(
                Descriptors.PX1030_DefaultAttibuteToExistingRecordsOnDAC,
                Descriptors.PX1030_DefaultAttibuteToExistingRecordsError,
                Descriptors.PX1030_DefaultAttibuteToExistingRecordsWarning);

		public override void Analyze(SymbolAnalysisContext context, PXContext pxContext, DacSemanticModel dacOrExtension)
		{
			var attributeInformation = new AttributeInformation(pxContext);

			foreach (DacPropertyInfo property in dacOrExtension.AllDeclaredProperties)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				AnalyzeProperty(context, pxContext, attributeInformation, dacOrExtension, property);
			}
		}

        private static void AnalyzeProperty(SymbolAnalysisContext symbolContext, PXContext pxContext, AttributeInformation attributeInformation,
											DacSemanticModel dacOrExtension, DacPropertyInfo property)
        {         
            var attributes = property.Symbol.GetAttributes();
            var boundType = GetPropertyBoundType(dacOrExtension, property, attributeInformation);

            switch (boundType)
            {
                case BoundType.Unbound:
                    AnalyzeUnboundProperty(symbolContext, pxContext, dacOrExtension, attributeInformation, attributes);
                    return;
                case BoundType.DbBound
                when dacOrExtension.DacType == DacType.DacExtension: // Analyze bound property only for extensions
                    AnalyzeBoundPropertyAttributes(symbolContext, pxContext, dacOrExtension, attributeInformation, property, attributes);
					return;
            }
        }

        private static void AnalyzeUnboundProperty(SymbolAnalysisContext symbolContext, PXContext pxContext,
												   DacSemanticModel dacOrExtension, AttributeInformation attributeInformation,
												   ImmutableArray<AttributeData> attributes)
        {
            var (pxDefaultAttribute, hasPersistingCheckNothing) = GetPXDefaultInfo(pxContext, attributeInformation, attributes);

            if (pxDefaultAttribute == null || hasPersistingCheckNothing)
                return;

            var attributeLocation = GetAttributeLocation(pxDefaultAttribute, symbolContext.CancellationToken);
            if (attributeLocation == null)
            {
                return;
            }

            var diagnosticProperties = ImmutableDictionary<string, string>.Empty
																		  .Add(DiagnosticProperty.IsBoundField, bool.FalseString);
            var descriptor = dacOrExtension.DacType == DacType.Dac
				? Descriptors.PX1030_DefaultAttibuteToExistingRecordsOnDAC 
				: Descriptors.PX1030_DefaultAttibuteToExistingRecordsError;
            var diagnostic = Diagnostic.Create(descriptor, attributeLocation, diagnosticProperties);

            symbolContext.ReportDiagnosticWithSuppressionCheck(diagnostic, pxContext.CodeAnalysisSettings);
        }

        private static void AnalyzeBoundPropertyAttributes(SymbolAnalysisContext symbolContext, PXContext pxContext,
														   DacSemanticModel dacExtension, AttributeInformation attributeInformation,
														   DacPropertyInfo property, ImmutableArray<AttributeData> attributes)
        {
            var pxDefaultAttribute = GetInvalidPXDefaultAttributeFromBoundProperty(pxContext, property, attributeInformation, attributes);

            if (pxDefaultAttribute == null)
                return;

            var attributeLocation = GetAttributeLocation(pxDefaultAttribute, symbolContext.CancellationToken);
            if (attributeLocation == null)
                return;

            var diagnosticProperties = ImmutableDictionary<string, string>.Empty
																		  .Add(DiagnosticProperty.IsBoundField, bool.TrueString);
            var diagnostic = Diagnostic.Create(Descriptors.PX1030_DefaultAttibuteToExistingRecordsWarning, attributeLocation, diagnosticProperties);

            symbolContext.ReportDiagnosticWithSuppressionCheck(diagnostic, pxContext.CodeAnalysisSettings);
        }

        private static AttributeData GetInvalidPXDefaultAttributeFromBoundProperty(PXContext pxContext, DacPropertyInfo property,
																				   AttributeInformation attributeInformation, 
																				   ImmutableArray<AttributeData> attributes)
        {
            var (pxDefaultAttribute, hasPersistingCheckNothing) = GetPXDefaultInfo(pxContext, attributeInformation, attributes);

            if (pxDefaultAttribute == null || hasPersistingCheckNothing)
                return null;

            // If Dac extension contains PXDefaultAttribute without PersistingCheck.Nothing
            // we need to look to attribute wich it overrides:
            // if base attribute is also doesn't contain PersistingCheck.Nothing it is legitimately
            foreach (DacPropertyInfo overridenProperties in property.JustOverridenItems())
            {
				ImmutableArray<AttributeData> attributesBase = overridenProperties.Symbol.GetAttributes();
                var (pxDefaultAttributeBase, hasPersistingCheckNothingBase) = GetPXDefaultInfo(pxContext, attributeInformation, attributesBase);

                if (pxDefaultAttributeBase == null)
                    continue;      

                return hasPersistingCheckNothingBase	//The values from the latest appropriate override should be used
					? pxDefaultAttribute
					: null;
            }

            return pxDefaultAttribute;
        }

        private static (AttributeData PXDefaultAttribute, bool HasPersistingCheckNothing) GetPXDefaultInfo(PXContext pxContext,
																								AttributeInformation attributeInformation,
																								ImmutableArray<AttributeData> attributes)
        {
            var pxDefaultAttribute = GetPXDefaultAttributeFromAttributes(attributes, pxContext, attributeInformation);
            if (pxDefaultAttribute == null)
            {
                return (null, false);
            }

			var hasPersistingCheckNothing = (from arg in pxDefaultAttribute.NamedArguments
											 where TypeNames.PersistingCheck.Equals(arg.Key, StringComparison.Ordinal)
											 select arg.Value.Value)
											.Any(value => value is int persistingCheck && persistingCheck == (int)PXPersistingCheckValues.Nothing);

            return (pxDefaultAttribute, hasPersistingCheckNothing);
        }

        private static AttributeData GetPXDefaultAttributeFromAttributes(ImmutableArray<AttributeData> attributes, PXContext pxContext, 
																		 AttributeInformation attributeInformation)
        {
			var pxDefaultAttribute = pxContext.AttributeTypes.PXDefaultAttribute;
			var pxUnboundDefaultAttribute = pxContext.AttributeTypes.PXUnboundDefaultAttribute;

			return attributes.FirstOrDefault(attribute => IsDefaultAttribute(attribute.AttributeClass));

			bool IsDefaultAttribute(INamedTypeSymbol attributeType) =>
				attributeInformation.IsAttributeDerivedFromClass(attributeType, pxDefaultAttribute) &&
				!attributeInformation.IsAttributeDerivedFromClass(attributeType, pxUnboundDefaultAttribute);
		}

        private static BoundType GetPropertyBoundType(DacSemanticModel dacOrExtension, DacPropertyInfo property,
													  AttributeInformation attributeInformation)
        {
            if (dacOrExtension.DacType == DacType.Dac)
            {
                return GetBoundTypeFromDeclaredProperty(property, attributeInformation);
            }

            // We need to analyze base Dac extensions and Dac to find a first occurrence of bound/unbound attribute
            // because extension can be PXNonInstantiatedExtension
            foreach (DacPropertyInfo propertyInOverridesChain in property.ThisAndOverridenItems())
            {			         
                var boundType = GetBoundTypeFromDeclaredProperty(propertyInOverridesChain, attributeInformation);

                if (boundType == BoundType.DbBound || boundType == BoundType.Unbound)
                    return boundType;
            }

            return BoundType.NotDefined;
        }

        private static BoundType GetBoundTypeFromDeclaredProperty(DacPropertyInfo property, AttributeInformation attributeInformation)
        {
            var propertyAttributes = property.Symbol.GetAttributes();

            if (attributeInformation.ContainsBoundAttributes(propertyAttributes))
            {
                return BoundType.DbBound;
            }
            else if (attributeInformation.ContainsUnboundAttributes(propertyAttributes))
            {
                return BoundType.Unbound;
            }
			else
			{
				return BoundType.NotDefined;
			}           
        }

        public static Location GetAttributeLocation(AttributeData attribute, CancellationToken cancellationToken) =>
			attribute.ApplicationSyntaxReference
					?.GetSyntax(cancellationToken)
					?.GetLocation();
	}
}

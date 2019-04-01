﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Syntax;
using Acuminator.Vsix.Utilities;



namespace Acuminator.Vsix.ChangesClassification
{
	/// <summary>
	/// A base class for document changes classification.
	/// </summary>
	public class DocumentChangesClassifier
	{
		/// <summary>
		/// Values that represent containment mode changes for a <see cref="TextSpan"/> which was containing another <see cref="TextSpan"/> before changes.
		/// </summary>
		protected enum ContainmentModeChange : byte
		{
			StillContaining,
			NotContaining,		
		} 

		public async Task<ChangeLocation> GetChangesLocationAsync(Document oldDocument, SyntaxNode oldRoot, Document newDocument, 
																  CancellationToken cancellationToken = default)
		{
			oldDocument.ThrowOnNull(nameof(oldDocument));
			oldRoot.ThrowOnNull(nameof(oldRoot));
			newDocument.ThrowOnNull(nameof(newDocument));

			IEnumerable<TextChange> textChanges = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken)
																   .ConfigureAwait(false);
			if (textChanges.IsNullOrEmpty())
				return ChangeLocation.None;

			return GetChangesLocationImplAsync(oldDocument, oldRoot, newDocument, textChanges, cancellationToken);
		}

		protected virtual ChangeLocation GetChangesLocationImplAsync(Document oldDocument, SyntaxNode oldRoot, Document newDocument,
																	 IEnumerable<TextChange> textChanges, 
																	 CancellationToken cancellationToken = default)
		{
			ChangeLocation accumulatedChangeLocation = ChangeLocation.None;

			foreach (TextChange change in textChanges)
			{
				ChangeLocation changeLocation = GetTextChangeLocation(change, oldRoot);
				accumulatedChangeLocation = accumulatedChangeLocation | changeLocation;

				cancellationToken.ThrowIfCancellationRequested();
			}

			return accumulatedChangeLocation;
		}

		protected virtual ChangeLocation GetTextChangeLocation(in TextChange textChange, SyntaxNode oldRoot)
		{
			var containingNode = oldRoot.FindNode(textChange.Span);

			if (containingNode == null)
				return ChangeLocation.None;

			while (!containingNode.IsKind(SyntaxKind.CompilationUnit))
			{
				ContainmentModeChange containingModeChange = GetContainingSpanNewContainmentModeForTextChange(textChange, containingNode.Span);
				ChangeLocation? changesLocation = null;

				switch (containingNode)
				{
					case StatementSyntax statementNode:
						changesLocation = GetChangeLocationFromStatementNode(statementNode, textChange, containingModeChange);
						break;
					case MemberDeclarationSyntax memberDeclaration:
						changesLocation = GetChangeLocationFromTypeMemberNode(memberDeclaration, textChange, containingModeChange);
						break;			
				}
				
				if (changesLocation.HasValue)
					return changesLocation.Value;

				containingNode = containingNode.Parent;
			}

			return ChangeLocation.Namespace;
		}

		protected virtual ChangeLocation? GetChangeLocationFromStatementNode(StatementSyntax statementNode, in TextChange textChange,
																			 ContainmentModeChange containingModeChange) =>
			containingModeChange == ContainmentModeChange.StillContaining
				? ChangeLocation.StatementsBlock
				: (ChangeLocation?)null;

		protected virtual ChangeLocation? GetChangeLocationFromTypeMemberNode(MemberDeclarationSyntax memberDeclaration,
																			  in TextChange textChange, ContainmentModeChange containingModeChange)
		{
			ChangeLocation? changeLocation = GetChangeLocationFromNodeTrivia(memberDeclaration, textChange);

			if (changeLocation.HasValue)
				return changeLocation;

			switch (memberDeclaration)
			{
				case BaseMethodDeclarationSyntax baseMethodNode:
					return GetChangeLocationFromMethodBaseSyntaxNode(baseMethodNode, textChange, containingModeChange);

				case BasePropertyDeclarationSyntax basePropertyNode:
					return GetChangeLocationFromPropertyBaseSyntaxNode(basePropertyNode, textChange, containingModeChange);

				default:
					return ChangeLocation.Class;
			}
		}

		protected virtual ChangeLocation? GetChangeLocationFromMethodBaseSyntaxNode(BaseMethodDeclarationSyntax methodNodeBase,
																					in TextChange textChange, ContainmentModeChange containingModeChange)
		{
			TextSpan spanToCheck = new TextSpan(textChange.Span.Start, textChange.NewText.Length);

			if (methodNodeBase.AttributeLists.Span.Contains(spanToCheck))
			{
				return ChangeLocation.Attributes;
			}

			TextSpan? methodBodySpan = methodNodeBase.Body?.Span;

			if (methodBodySpan == null && methodNodeBase is MethodDeclarationSyntax methodNode)
			{
				methodBodySpan = methodNode.ExpressionBody?.Span;
			}
		
			return methodBodySpan == null
					? ChangeLocation.Class
					: methodBodySpan.Value.Contains(spanToCheck)
						? ChangeLocation.StatementsBlock
						: ChangeLocation.Class;
		}

		protected virtual ChangeLocation? GetChangeLocationFromPropertyBaseSyntaxNode(BasePropertyDeclarationSyntax propertyNodeBase, 
																					  in TextChange textChange, ContainmentModeChange containingModeChange)
		{
			TextSpan spanToCheck = new TextSpan(textChange.Span.Start, textChange.NewText.Length);
			
			if (propertyNodeBase.AttributeLists.Span.Contains(spanToCheck))
			{
				return ChangeLocation.Attributes;
			}

			TextSpan? bodySpan = propertyNodeBase.AccessorList?.Span;

			if (bodySpan == null)
			{
				switch (propertyNodeBase)
				{
					case PropertyDeclarationSyntax property:
						bodySpan = property.ExpressionBody?.Span;
						break;
					case IndexerDeclarationSyntax indexer:
						bodySpan = indexer.ExpressionBody?.Span;
						break;
				}
			}

			return bodySpan == null
				? ChangeLocation.Class
				: bodySpan.Value.Contains(spanToCheck)
					? ChangeLocation.StatementsBlock
					: ChangeLocation.Class;
		}

		protected ContainmentModeChange GetContainingSpanNewContainmentModeForTextChange(in TextChange textChange, in TextSpan existingContainingSpan)
		{
			var lengthFromChangeStart = existingContainingSpan.End - textChange.Span.Start;

			return textChange.NewText.Length == lengthFromChangeStart
				? ContainmentModeChange.StillContaining
				: lengthFromChangeStart < textChange.NewText.Length
						? ContainmentModeChange.NotContaining
						: ContainmentModeChange.StillContaining;
		}	

		protected virtual ChangeLocation? GetChangeLocationFromNodeTrivia(SyntaxNode syntaxNode, in TextChange textChange)
		{
			if (!IsNewLineOrWhitespaceChange(textChange))
				return null;

			ChangeLocation? changeLocation = GetNewLineOrWhitespaceChangeLocationFromTriviaList(syntaxNode.GetLeadingTrivia(), textChange);

			if (changeLocation.HasValue)
				return changeLocation;

			return GetNewLineOrWhitespaceChangeLocationFromTriviaList(syntaxNode.GetTrailingTrivia(), textChange);
		}

		protected static bool IsNewLineOrWhitespaceChange(in TextChange textChange) =>
			textChange.Span.IsEmpty && (textChange.NewText == Environment.NewLine || textChange.NewText.IsNullOrWhiteSpace());

		protected static ChangeLocation? GetNewLineOrWhitespaceChangeLocationFromTriviaList(in SyntaxTriviaList triviaList, 
																							in TextChange newLineOrWhitespaceChange) =>
			triviaList.Span.Contains(newLineOrWhitespaceChange.Span.Start)
				? ChangeLocation.Trivia
				: (ChangeLocation?)null;
	}
}

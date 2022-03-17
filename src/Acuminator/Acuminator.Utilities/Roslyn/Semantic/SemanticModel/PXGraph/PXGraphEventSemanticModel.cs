﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Semantic.SharedInfo;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Acuminator.Utilities.Roslyn.Semantic.PXGraph
{
	public partial class PXGraphEventSemanticModel : ISemanticModel
	{
		private enum GraphEventCategory
		{
			None,
			Row,
			Field
		};

		private readonly CancellationToken _cancellation;

		private PXContext PXContext => BaseGraphModel.PXContext;

		public PXGraphSemanticModel BaseGraphModel { get; }

		#region Base Model
		public GraphSemanticModelCreationOptions ModelCreationOptions => BaseGraphModel.ModelCreationOptions;

		public bool IsProcessing => BaseGraphModel.IsProcessing;

		public GraphType Type => BaseGraphModel.Type;

		public INamedTypeSymbol Symbol => BaseGraphModel.Symbol;

		/// <summary>
		/// The graph symbol. For the graph is the same as <see cref="Symbol"/>. For graph extensions is the extension's base graph.
		/// </summary>
		public ITypeSymbol GraphSymbol => BaseGraphModel.GraphSymbol;

		public ImmutableArray<StaticConstructorInfo> StaticConstructors => BaseGraphModel.StaticConstructors;
		public ImmutableArray<GraphInitializerInfo> Initializers => BaseGraphModel.Initializers;

		public ImmutableDictionary<string, DataViewInfo> ViewsByNames => BaseGraphModel.ViewsByNames;
		public IEnumerable<DataViewInfo> Views => BaseGraphModel.Views;

		public ImmutableDictionary<string, DataViewDelegateInfo> ViewDelegatesByNames => BaseGraphModel.ViewDelegatesByNames;
		public IEnumerable<DataViewDelegateInfo> ViewDelegates => BaseGraphModel.ViewDelegates;

		public ImmutableDictionary<string, ActionInfo> ActionsByNames => BaseGraphModel.ActionsByNames;
		public IEnumerable<ActionInfo> Actions => BaseGraphModel.Actions;

		public ImmutableDictionary<string, ActionHandlerInfo> ActionHandlersByNames => BaseGraphModel.ActionHandlersByNames;
		public IEnumerable<ActionHandlerInfo> ActionHandlers => BaseGraphModel.ActionHandlers;

		/// <summary>
		/// Gets the info about IsActive method for graph extensions. Can be <c>null</c>. Always <c>null</c> for graphs.
		/// </summary>
		/// <value>
		/// The info about IsActive method.
		/// </value>
		public IsActiveMethodInfo IsActiveMethodInfo => BaseGraphModel.IsActiveMethodInfo;
		#endregion

		#region Events
		public ImmutableDictionary<string, GraphRowEventInfo> RowSelectingByName { get; }
		public IEnumerable<GraphRowEventInfo> RowSelectingEvents => RowSelectingByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowSelectedByName { get; }
		public IEnumerable<GraphRowEventInfo> RowSelectedEvents => RowSelectedByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowInsertingByName { get; }
		public IEnumerable<GraphRowEventInfo> RowInsertingEvents => RowInsertingByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowInsertedByName { get; }
		public IEnumerable<GraphRowEventInfo> RowInsertedEvents => RowInsertedByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowUpdatingByName { get; }
		public IEnumerable<GraphRowEventInfo> RowUpdatingEvents => RowUpdatingByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowUpdatedByName { get; }
		public IEnumerable<GraphRowEventInfo> RowUpdatedEvents => RowUpdatedByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowDeletingByName { get; }
		public IEnumerable<GraphRowEventInfo> RowDeletingEvents => RowDeletingByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowDeletedByName { get; }
		public IEnumerable<GraphRowEventInfo> RowDeletedEvents => RowDeletedByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowPersistingByName { get; }
		public IEnumerable<GraphRowEventInfo> RowPersistingEvents => RowPersistingByName.Values;

		public ImmutableDictionary<string, GraphRowEventInfo> RowPersistedByName { get; }
		public IEnumerable<GraphRowEventInfo> RowPersistedEvents => RowPersistedByName.Values;

		public ImmutableDictionary<string, GraphFieldEventInfo> FieldSelectingByName { get; }
		public IEnumerable<GraphFieldEventInfo> FieldSelectingEvents => FieldSelectingByName.Values;

		public ImmutableDictionary<string, GraphFieldEventInfo> FieldDefaultingByName { get; }
		public IEnumerable<GraphFieldEventInfo> FieldDefaultingEvents => FieldDefaultingByName.Values;

		public ImmutableDictionary<string, GraphFieldEventInfo> FieldVerifyingByName { get; }
		public IEnumerable<GraphFieldEventInfo> FieldVerifyingEvents => FieldVerifyingByName.Values;

		public ImmutableDictionary<string, GraphFieldEventInfo> FieldUpdatingByName { get; }
		public IEnumerable<GraphFieldEventInfo> FieldUpdatingEvents => FieldUpdatingByName.Values;

		public ImmutableDictionary<string, GraphFieldEventInfo> FieldUpdatedByName { get; }
		public IEnumerable<GraphFieldEventInfo> FieldUpdatedEvents => FieldUpdatedByName.Values;

		public ImmutableDictionary<string, GraphFieldEventInfo> CacheAttachedByName { get; }
		public IEnumerable<GraphFieldEventInfo> CacheAttachedEvents => CacheAttachedByName.Values;

		public ImmutableDictionary<string, GraphFieldEventInfo> CommandPreparingByName { get; }
		public IEnumerable<GraphFieldEventInfo> CommandPreparingEvents => CommandPreparingByName.Values;

		public ImmutableDictionary<string, GraphFieldEventInfo> ExceptionHandlingByName { get; }
		public IEnumerable<GraphFieldEventInfo> ExceptionHandlingEvents => ExceptionHandlingByName.Values;
		#endregion

		private PXGraphEventSemanticModel(PXGraphSemanticModel baseGraphModel, CancellationToken cancellation = default)
		{
			_cancellation = cancellation;
			BaseGraphModel = baseGraphModel;

			if (BaseGraphModel.Type != GraphType.None)
			{
				var eventsCollector = InitializeEvents();
			
				RowSelectingByName = GetRowEvents(eventsCollector, EventType.RowSelecting);
				RowSelectedByName = GetRowEvents(eventsCollector, EventType.RowSelected);

				RowInsertingByName = GetRowEvents(eventsCollector, EventType.RowInserting);
				RowInsertedByName = GetRowEvents(eventsCollector, EventType.RowInserted);

				RowUpdatingByName = GetRowEvents(eventsCollector, EventType.RowUpdating);
				RowUpdatedByName = GetRowEvents(eventsCollector, EventType.RowUpdated);

				RowDeletingByName = GetRowEvents(eventsCollector, EventType.RowDeleting);
				RowDeletedByName = GetRowEvents(eventsCollector, EventType.RowDeleted);

				RowPersistingByName = GetRowEvents(eventsCollector, EventType.RowPersisting);
				RowPersistedByName = GetRowEvents(eventsCollector, EventType.RowPersisted);

				FieldSelectingByName = GetFieldEvents(eventsCollector, EventType.FieldSelecting);
				FieldDefaultingByName = GetFieldEvents(eventsCollector, EventType.FieldDefaulting);
				FieldVerifyingByName = GetFieldEvents(eventsCollector, EventType.FieldVerifying);
				FieldUpdatingByName = GetFieldEvents(eventsCollector, EventType.FieldUpdating);
				FieldUpdatedByName = GetFieldEvents(eventsCollector, EventType.FieldUpdated);

				CacheAttachedByName = GetFieldEvents(eventsCollector, EventType.CacheAttached);
				CommandPreparingByName = GetFieldEvents(eventsCollector, EventType.CommandPreparing);
				ExceptionHandlingByName = GetFieldEvents(eventsCollector, EventType.ExceptionHandling);
			}
			else
			{
				RowSelectingByName      = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowSelectedByName       = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowInsertingByName      = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowInsertedByName       = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowUpdatingByName       = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowUpdatedByName        = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowDeletingByName       = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowDeletedByName        = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowPersistingByName     = ImmutableDictionary<string, GraphRowEventInfo>.Empty;
				RowPersistedByName      = ImmutableDictionary<string, GraphRowEventInfo>.Empty;

				FieldSelectingByName    = ImmutableDictionary<string, GraphFieldEventInfo>.Empty;
				FieldDefaultingByName   = ImmutableDictionary<string, GraphFieldEventInfo>.Empty;
				FieldVerifyingByName    = ImmutableDictionary<string, GraphFieldEventInfo>.Empty;
				FieldUpdatingByName     = ImmutableDictionary<string, GraphFieldEventInfo>.Empty;
				FieldUpdatedByName      = ImmutableDictionary<string, GraphFieldEventInfo>.Empty;

				CacheAttachedByName     = ImmutableDictionary<string, GraphFieldEventInfo>.Empty;
				CommandPreparingByName  = ImmutableDictionary<string, GraphFieldEventInfo>.Empty;
				ExceptionHandlingByName = ImmutableDictionary<string, GraphFieldEventInfo>.Empty;
			}
		}

		public static PXGraphEventSemanticModel EnrichGraphModelWithEvents(PXGraphSemanticModel baseGraphModel, 
																		   CancellationToken cancellationToken = default) =>
			new PXGraphEventSemanticModel(baseGraphModel.CheckIfNull(nameof(baseGraphModel)), 
										  cancellationToken);

		public static IEnumerable<PXGraphEventSemanticModel> InferModels(PXContext pxContext, INamedTypeSymbol typeSymbol, 
																		 GraphSemanticModelCreationOptions modelCreationOptions,
																		 CancellationToken cancellation = default)
		{	
			var baseGraphModels = PXGraphSemanticModel.InferModels(pxContext, typeSymbol, modelCreationOptions, cancellation);
			var eventsGraphModels = baseGraphModels.Select(graph => new PXGraphEventSemanticModel(graph, cancellation))
												   .ToList();
			return eventsGraphModels;
		}

		public IEnumerable<GraphEventInfoBase> GetEventsByEventType(EventType eventType) => eventType switch
		{
			EventType.RowSelecting      => RowSelectingEvents,
			EventType.RowSelected       => RowSelectedEvents,
			EventType.RowInserting      => RowInsertingEvents,
			EventType.RowInserted       => RowInsertedEvents,
			EventType.RowUpdating       => RowUpdatingEvents,
			EventType.RowUpdated        => RowUpdatedEvents,
			EventType.RowDeleting       => RowDeletingEvents,
			EventType.RowDeleted        => RowDeletedEvents,
			EventType.RowPersisting     => RowPersistingEvents,
			EventType.RowPersisted      => RowPersistedEvents,

			EventType.FieldSelecting    => FieldSelectingEvents,
			EventType.FieldDefaulting   => FieldDefaultingEvents,
			EventType.FieldVerifying    => FieldVerifyingEvents,
			EventType.FieldUpdating     => FieldUpdatingEvents,
			EventType.FieldUpdated      => FieldUpdatedEvents,

			EventType.CacheAttached     => CacheAttachedEvents,
			EventType.CommandPreparing  => CommandPreparingEvents,
			EventType.ExceptionHandling => ExceptionHandlingEvents,
			_                           => Enumerable.Empty<GraphEventInfoBase>()
		};

		public IEnumerable<GraphEventInfoBase> GetAllEvents()
		{
			IEnumerable<GraphEventInfoBase>? allEvents = RowSelectingByName.Values;

			AppendRowEvents(RowSelectedByName);
			AppendRowEvents(RowInsertingByName);
			AppendRowEvents(RowInsertedByName);
			AppendRowEvents(RowUpdatingByName);
			AppendRowEvents(RowUpdatedByName);
			AppendRowEvents(RowDeletingByName);
			AppendRowEvents(RowDeletedByName);
			AppendRowEvents(RowPersistingByName);
			AppendRowEvents(RowPersistedByName);

			AppendFieldEvents(FieldSelectingByName);
			AppendFieldEvents(FieldDefaultingByName);
			AppendFieldEvents(FieldVerifyingByName);
			AppendFieldEvents(FieldUpdatingByName);
			AppendFieldEvents(FieldUpdatedByName);

			AppendFieldEvents(CacheAttachedByName);
			AppendFieldEvents(CommandPreparingByName);
			AppendFieldEvents(ExceptionHandlingByName);

			return allEvents;

			//------------------------------------Local Function----------------------------------------------
			void AppendRowEvents(ImmutableDictionary<string, GraphRowEventInfo> rowEvents)
			{
				if (rowEvents.Count > 0)
					allEvents = allEvents.Concat(rowEvents.Values);
			}

			void AppendFieldEvents(ImmutableDictionary<string, GraphFieldEventInfo> fieldEvents)
			{
				if (fieldEvents.Count > 0)
					allEvents = allEvents.Concat(fieldEvents.Values);
			}
		}

		private EventsCollector InitializeEvents()
		{
			_cancellation.ThrowIfCancellationRequested();
			var methods = GetAllGraphMethodsFromBaseToDerived();

			var eventsCollector = new EventsCollector(this, PXContext);
			int declarationOrder = 0;

			foreach (IMethodSymbol method in methods)
			{
				_cancellation.ThrowIfCancellationRequested();

				var eventInfo = method.GetEventHandlerInfo(PXContext);

				if (eventInfo.SignatureType == EventHandlerSignatureType.None || eventInfo.Type == EventType.None)
					continue;

				GraphEventCategory eventCategory = GetEventCategoryByEventType(eventInfo.Type);

				if (!IsValidGraphEvent(method, eventInfo.SignatureType, eventCategory))
					continue;

				if (eventCategory == GraphEventCategory.Row)
				{
					eventsCollector.AddEvent(eventInfo.SignatureType, eventInfo.Type, method, declarationOrder, _cancellation);
				}
				else if (eventCategory == GraphEventCategory.Field)
				{
					eventsCollector.AddFieldEvent(eventInfo.SignatureType, eventInfo.Type, method, declarationOrder, _cancellation);
				}
				
				declarationOrder++;
			}

			return eventsCollector;
		}

		private IEnumerable<IMethodSymbol> GetAllGraphMethodsFromBaseToDerived()
		{
			IEnumerable<ITypeSymbol> baseTypes = BaseGraphModel.GraphSymbol
															   .GetGraphWithBaseTypes()
															   .Reverse();

			if (BaseGraphModel.Type == GraphType.PXGraphExtension)
			{
				baseTypes = baseTypes.Concat(
										BaseGraphModel.Symbol.GetGraphExtensionWithBaseExtensions(PXContext, 
																								  SortDirection.Ascending,
																								  includeGraph: false));
			}

			return baseTypes.SelectMany(t => t.GetMembers().OfType<IMethodSymbol>());
		}

		private ImmutableDictionary<string, GraphRowEventInfo> GetRowEvents(EventsCollector eventsCollector, EventType eventType)
		{
			if (Type == GraphType.None)
				return ImmutableDictionary.Create<string, GraphRowEventInfo>();

			OverridableItemsCollection<GraphRowEventInfo> rawCollection = eventsCollector.GetRowEvents(eventType);
			return rawCollection?.ToImmutableDictionary() ?? ImmutableDictionary.Create<string, GraphRowEventInfo>();
		}

		private ImmutableDictionary<string, GraphFieldEventInfo> GetFieldEvents(EventsCollector eventsCollector, EventType eventType)
		{
			if (Type == GraphType.None)
				return ImmutableDictionary.Create<string, GraphFieldEventInfo>();

			OverridableItemsCollection<GraphFieldEventInfo> rawCollection = eventsCollector.GetFieldEvents(eventType);
			return rawCollection.ToImmutableDictionary() ?? ImmutableDictionary.Create<string, GraphFieldEventInfo>();
		}

		private GraphEventCategory GetEventCategoryByEventType(EventType eventType) =>
			eventType.IsDacRowEvent()
				? GraphEventCategory.Row
				: eventType.IsDacFieldEvent()
					? GraphEventCategory.Field
					: GraphEventCategory.None;

		/// <summary>
		/// <see cref="CodeResolvingUtils.GetEventHandlerInfo"/> helper allows not only graph events but also helper methods with appropriate signature. 
		/// However, for graph events semantic model we are interested only in graph events, so we need to rule out helper methods by checking their signature.
		/// </summary>
		/// <param name="eventCandidate">The event candidate.</param>
		/// <param name="signatureType">Type of the signature.</param>
		/// <param name="eventCategory">Category the event belongs to.</param>
		/// <returns/>
		private bool IsValidGraphEvent(IMethodSymbol eventCandidate, EventHandlerSignatureType signatureType, GraphEventCategory eventCategory)
		{
			if (eventCandidate.IsStatic || eventCandidate.Parameters.Length > 2 || eventCategory == GraphEventCategory.None)
				return false;
			else if (signatureType != EventHandlerSignatureType.Default)
				return true;

			const char underscore = '_';

			if (eventCandidate.Name[0] == underscore || eventCandidate.Name[eventCandidate.Name.Length - 1] == underscore)
				return false;

			int underscoresCount = eventCandidate.Name.Count(c => c == underscore);

			return eventCategory switch
			{
				GraphEventCategory.Row => underscoresCount == 1,
				GraphEventCategory.Field => underscoresCount == 2,
				_ => false,
			};
		}
	}
}

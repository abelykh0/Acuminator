﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Acuminator.Utilities.Common;
using Acuminator.Utilities.Roslyn.Semantic.PXGraph;
using Acuminator.Vsix.Utilities;



namespace Acuminator.Vsix.ToolWindows.CodeMap
{
	public class DacFieldEventsGroupingNodeViewModel : TreeNodeViewModel
	{
		public GraphEventCategoryNodeViewModel GraphEventsCategoryVM => DacVM.GraphEventsCategoryVM;

		public DacEventsGroupingNodeViewModel DacVM { get; }

		public string DacFieldName { get; }

		public override string Name
		{
			get => DacFieldName;
			protected set { }
		}

		protected DacFieldEventsGroupingNodeViewModel(DacEventsGroupingNodeViewModel dacVM, string dacFieldName, bool isExpanded) :
												 base(dacVM?.Tree, isExpanded)
		{
			dacFieldName.ThrowOnNullOrWhiteSpace(nameof(dacFieldName));

			DacVM = dacVM;
			DacFieldName = dacFieldName;
		}

		public static DacFieldEventsGroupingNodeViewModel Create(DacEventsGroupingNodeViewModel dacVM, string dacFieldName, 
																 IEnumerable<GraphEventInfo> dacFieldEvents,
																 bool isExpanded = false)
		{
			if (dacFieldEvents.IsNullOrEmpty() || dacFieldName.IsNullOrWhiteSpace())
				return null;

			var dacFieldVM = new DacFieldEventsGroupingNodeViewModel(dacVM, dacFieldName, isExpanded);
			var dacFieldEventVMs = dacFieldVM.GetDacFieldNodeChildren(dacFieldEvents, isExpanded);
			dacFieldVM.Children.AddRange(dacFieldEventVMs);
			return dacFieldVM;
		}

		protected virtual IEnumerable<GraphMemberNodeViewModel> GetDacFieldNodeChildren(IEnumerable<GraphEventInfo> dacFieldEvents,
																						bool isExpanded)
		{
			return dacFieldEvents.Select(eventInfo => GraphEventsCategoryVM.CreateNewEventVM(this, eventInfo, isExpanded))
								 .Where(graphMemberVM => graphMemberVM != null && !graphMemberVM.Name.IsNullOrEmpty())
								 .OrderBy(graphMemberVM => graphMemberVM.Name);
		}
	}
}
﻿using System;
using PX.Data;
using PX.Common;

namespace Acuminator.Tests.Tests.StaticAnalysis.CallsToInternalAPI.Sources
{
	[PXInternalUseOnly]
	public class InternalService
	{
		public bool IsActive;

		public bool SomeFlag { get; }

		public void ProvideService()
		{

		}
	}


	public class AccessChecker
	{
		[PXInternalUseOnly]
		public bool IsActive;

		[PXInternalUseOnly]
		public bool ShouldCheck { get; }

		[PXInternalUseOnly]
		public void CheckAccess()
		{

		}
	}


	public class ServiceProvider
	{
		public InternalService Service = new InternalService();

		public AccessChecker AccessChecker = new AccessChecker();
	}
}

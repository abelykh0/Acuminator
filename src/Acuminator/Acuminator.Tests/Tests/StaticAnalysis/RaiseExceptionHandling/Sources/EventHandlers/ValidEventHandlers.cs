﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;

namespace PX.Objects
{
	public class SOInvoiceEntry : PXGraph<SOInvoiceEntry, SOInvoice>
	{
		protected virtual void _(Events.FieldVerifying<SOInvoice.refNbr> e)
		{
			cache.RaiseExceptionHandling<SOInvoice.refNbr>(null, null, new PXSetPropertyException("Something bad happened"));
		}
		
		protected virtual void _(Events.RowUpdating<SOInvoice> e)
		{
			cache.RaiseExceptionHandling<SOInvoice.refNbr>(null, null, new PXSetPropertyException("Something bad happened"));
		}

		protected virtual void _(Events.RowDeleting<SOInvoice> e)
		{
			cache.RaiseExceptionHandling<SOInvoice.refNbr>(null, null, new PXSetPropertyException("Something bad happened"));
		}

		protected virtual void _(Events.RowInserted<SOInvoice> e)
		{
			cache.RaiseExceptionHandling<SOInvoice.refNbr>(null, null, new PXSetPropertyException("Something bad happened"));
		}

		protected virtual void _(Events.RowUpdated<SOInvoice> e)
		{
			cache.RaiseExceptionHandling<SOInvoice.refNbr>(null, null, new PXSetPropertyException("Something bad happened"));
		}

		protected virtual void _(Events.RowDeleted<SOInvoice> e)
		{
			cache.RaiseExceptionHandling<SOInvoice.refNbr>(null, null, new PXSetPropertyException("Something bad happened"));
		}

		protected virtual void _(Events.RowPersisting<SOInvoice> e)
		{
			cache.RaiseExceptionHandling<SOInvoice.refNbr>(null, null, new PXSetPropertyException("Something bad happened"));
		}
		
		protected virtual void _(Events.RowSelected<SOInvoice> e)
		{
			cache.RaiseExceptionHandling<SOInvoice.refNbr>(null, null, new PXSetPropertyException("Something bad happened"));
		}
	}

	public class SOInvoice : IBqlTable
	{
		#region RefNbr
		[PXDBString(8, IsKey = true, InputMask = "")]
		public string RefNbr { get; set; }
		public abstract class refNbr : IBqlField { }
		#endregion	
	}
}
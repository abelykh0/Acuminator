﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;

namespace PX.Objects
{
	public class CustomerService
	{
		public void UpdateCustomer(int acuCustomerId)
		{
			var existingGraph = new PXGraph();
			var graph = new PXGraph();

			graph.Customers.Current = PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
				.Select(graph, acuCustomerId);
		}
	}
}
//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;


namespace Microsoft.Data.ConnectionUI
{
	public interface IDataConnectionProperties
	{
		void Add(string propertyName);
		bool Contains(string propertyName);
		bool IsComplete { get; }
		bool IsExtensible { get; }
		void Parse(string s);
		event EventHandler PropertyChanged;
		void Remove(string propertyName);
		void Reset();
		void Reset(string propertyName);
		void Test();
		object this[string propertyName] { get; set; }
		string ToDisplayString();
		string ToFullString();
	}
}

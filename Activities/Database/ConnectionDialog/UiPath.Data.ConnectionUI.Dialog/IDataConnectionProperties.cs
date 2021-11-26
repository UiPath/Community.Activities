using System;

namespace UiPath.Data.ConnectionUI.Dialog
{
	public interface IDataConnectionProperties
	{
		event EventHandler PropertyChanged;
		void Add(string propertyName);
		bool Contains(string propertyName);
		bool IsComplete { get; }
		bool IsExtensible { get; }
		void Parse(string s);
		void Remove(string propertyName);
		void Reset();
		void Reset(string propertyName);
		void Test();
		object this[string propertyName] { get; set; }
		string ToDisplayString();
		string ToFullString();
	}
}

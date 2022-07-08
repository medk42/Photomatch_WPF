using System;
using System.IO;
using System.Text;

namespace PhotomatchCore.Interfaces
{
	/// <summary>
	/// Interface for implementing object serialization. 
	/// 
	/// Usage: class ExampleClass : ISafeSerializable<ExampleClass> { ... }
	/// </summary>
	/// <typeparam name="T">Type to be de/serialized, has to implement ISafeSerializable<T> and have constructor without parameters.</typeparam>
	public interface ISafeSerializable<T> where T : ISafeSerializable<T>, new()
	{
		/// <summary>
		/// Serialize object to binary writer.
		/// </summary>
		/// <param name="writer">Writer to serialize object to.</param>
		void Serialize(BinaryWriter writer);

		/// <summary>
		/// De-serialize object from binary reader to current instance.
		/// </summary>
		/// <param name="reader">Reader to de-serialize from.</param>
		void Deserialize(BinaryReader reader);

		/// <summary>
		/// Create de-serialized object from binary reader using new() constructor and self-deserialization.
		/// </summary>
		/// <param name="reader">Reader to de-serialize from.</param>
		/// <returns>New object that is de-serialized from binary reader.</returns>
		static T CreateDeserialize(BinaryReader reader)
		{
			T newT = new T();
			newT.Deserialize(reader);
			return newT;
		}
	}
}

using System;
using System.IO;
using System.Text;

namespace Photomatch_ProofOfConcept_WPF.Utilities
{
	public interface ISafeSerializable<T> where T : ISafeSerializable<T>, new()
	{
		void Serialize(BinaryWriter writer);
		void Deserialize(BinaryReader reader);

		static T CreateDeserialize(BinaryReader reader)
		{
			T newT = new T();
			newT.Deserialize(reader);
			return newT;
		}
	}
}

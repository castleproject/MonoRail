namespace Castle.MonoRail.Tests
{
	using System;
	using System.IO;
	using Serialization;

	class StubModelSerializerResolver : IModelSerializerResolver
	{
		public bool HasCustomSerializer(Type model, string mediaType)
		{
			return true;
		}

		public void Register<a>(string mediaType, Type serializer)
		{
			
		}

		public IModelSerializer<a> CreateSerializer<a>(string mediaType)
		{
			return new StubSerializer<a>();
		}

		public IModelSerializer CreateSerializer(Type modelType, string mediaType)
		{
			var typeIns = typeof (StubSerializer<>).MakeGenericType(modelType);
			return (IModelSerializer) Activator.CreateInstance(typeIns);
		}

		class StubSerializer<a> : IModelSerializer<a>, IModelSerializer
		{
			public void Serialize(a model, string contentType, TextWriter writer, ModelMetadataProvider metadataProvider)
			{
			}

			public a Deserialize(string prefix, string contentType, ModelSerializationContext context, ModelMetadataProvider metadataProvider)
			{
				var instance = Activator.CreateInstance(typeof (a));

				return (a) instance;
			}

			public void Serialize(object model, string contentType, TextWriter writer, ModelMetadataProvider metadataProvider)
			{
			}

			object IModelSerializer.Deserialize(string prefix, string contentType, ModelSerializationContext context, ModelMetadataProvider metadataProvider)
			{
				return Deserialize(prefix, contentType, context, metadataProvider);
			}
		}
	}
}

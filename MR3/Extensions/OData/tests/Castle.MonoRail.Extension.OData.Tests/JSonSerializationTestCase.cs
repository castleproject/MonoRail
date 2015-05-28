using Castle.MonoRail.Tests;

namespace Castle.MonoRail.Extension.OData.Tests
{
	using System.Collections.Generic;
	using System.ComponentModel.DataAnnotations;
	using System.IO;
	using System.Linq;
	using System.Text;
	using FluentAssertions;
	using NUnit.Framework;
	using Serialization;

	public static class SerializationModel
	{
		public class Revision
		{
			[Key]
			public int Id { get; set; }
			public string FileName { get; set; }
			public int UserId { get; set; }
		}

		public class Branch
		{
			public Branch()
			{
				Revisions = new List<Revision>();
			}

			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public Repository Owner { get; set; }
			public IList<Revision> Revisions { get; set; }
            public int? Parent { get; set; }
		}

		public class Repository
		{
			public Repository()
			{
				Branches = new List<Branch>();
				Info = new RepoInfo();
			}

			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
			public IList<Branch> Branches { get; set; }
			public RepoInfo Info { get; set; }
		}

		public class RepoInfo
		{
			public RepoInfo ()
			{
				Ip = "123.123.123.123";
			}
			public string Ip { get; set; }
		}

	}

	[TestFixture]
	public class JSonSerializationTestCase
	{
		private IQueryable<SerializationModel.Repository> reposSet;
		private StubModel _model;

		[SetUp]
		public void Init()
		{
			reposSet = new List<SerializationModel.Repository>() { }.AsQueryable();

			_model = new StubModel(
				m =>
				{
					m.EntitySet("repositories", reposSet);
				});
			var services = new StubServiceRegistry();
			_model.Initialize(services);
		}

		[Test]
		public void Deserialize_StandardJson_SimpleObj()
		{
			var rt = _model.GetResourceType("Repository").Value;

			var reader = new StringReader(@"
{ Id: 1, Name: ""NewName"" }
");

			var result = JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);

			result.Should().NotBeNull();
			var repo = (SerializationModel.Repository) result;
			repo.Id.Should().Be(1);
			repo.Name.Should().Be("NewName");
		}

        [Test]
        public void Deserialize_StandardJson_NullableTypes_ValueSet()
        {
            var rt = _model.GetResourceType("Branch").Value;

            var reader = new StringReader(@"{ Id: 1, Name: ""NewName"", Parent: 0  } ");

            var result = JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);

            result.Should().NotBeNull();
            var repo = (SerializationModel.Branch)result;
            repo.Parent.Should().Be(0);
        }
        
        [Test]
        public void Deserialize_StandardJson_NullableTypes_ValueNotSet()
        {
            var rt = _model.GetResourceType("Branch").Value;

            var reader = new StringReader(@"{ Id: 1, Name: ""NewName""  } ");

            var result = JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);

            result.Should().NotBeNull();
            var repo = (SerializationModel.Branch)result;
            repo.Parent.Should().Be(null);
        }

		[Test]
		public void Deserialize_VerboseJson_SimpleObj()
		{
			var rt = _model.GetResourceType("Repository").Value;

			var reader = new StringReader(@"
{ d: { Id: 1, Name: ""NewName"" } }
");

			var result = JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);

			result.Should().NotBeNull();
			var repo = (SerializationModel.Repository)result;
			repo.Id.Should().Be(1);
			repo.Name.Should().Be("NewName");
		}

		[Test]
		public void Deserialize_StandardJson_InnerComplexType()
		{
			var rt = _model.GetResourceType("Repository").Value;

			var reader = new StringReader(@"
{ Id: 1, Name: ""NewName"", Info: { Ip: ""127.1.1.1"" } }
");

			var result = JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);

			result.Should().NotBeNull();
			var repo = (SerializationModel.Repository)result;
			repo.Id.Should().Be(1);
			repo.Name.Should().Be("NewName");
			repo.Info.Ip.Should().Be("127.1.1.1");
			repo.Branches.Should().HaveCount(0);
		}

		[Test]
		public void Deserialize_StandardJson_InnerResourceRef()
		{
			var rt = _model.GetResourceType("Repository").Value;

			var reader = new StringReader(@"
{ Id: 1, Name: ""NewName"", Branches: [ { Id: 2, Name: ""Branch1"", Owner: { Id : 3 } } ] }
");

			var result = JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);

			result.Should().NotBeNull();
			var repo = (SerializationModel.Repository)result;
			repo.Id.Should().Be(1);
			repo.Name.Should().Be("NewName");
			repo.Branches.Should().HaveCount(1);
			repo.Branches.ElementAt(0).Id.Should().Be(2);
			repo.Branches.ElementAt(0).Name.Should().Be("Branch1");
			repo.Branches.ElementAt(0).Owner.Should().NotBeNull();
		}

		[Test]
		public void Deserialize_StandardJson_InnerResourceSet()
		{
			var rt = _model.GetResourceType("Repository").Value;

			var reader = new StringReader(@"
{ Id: 1, Name: ""NewName"", Branches: [ { Id: 2, Name: ""Branch1"" }, { Id: 3, Name: ""Branch2"" } ] }
");

			var result = JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);

			result.Should().NotBeNull();
			var repo = (SerializationModel.Repository)result;
			repo.Id.Should().Be(1);
			repo.Name.Should().Be("NewName");
			repo.Branches.Should().HaveCount(2);
			repo.Branches.ElementAt(0).Id.Should().Be(2);
			repo.Branches.ElementAt(1).Id.Should().Be(3);
			repo.Branches.ElementAt(0).Name.Should().Be("Branch1");
			repo.Branches.ElementAt(1).Name.Should().Be("Branch2");
		}

		[Test, ExpectedException(ExpectedMessage = "Property not found on model Branch: Revision")]
		public void Deserialize_ReferenceToNonExistentProperty()
		{
			var rt = _model.GetResourceType("Repository").Value;

			var reader = new StringReader(@"
{ 
	Id: 1, 
	Name: ""NewName"", 
	Branches: [ 
		{ 
			Id: 2, 
			Name: ""Branch1"", 
			Revision: [ 
				{ Id :100, FileName : ""Test1"" }, 
				{ Id :101, FileName : ""Test2"" } 
			] 
		}, 
		{ 
			Id: 3, 
			Name: ""Branch2"", 
			Revision: [ 
				{ Id :200, FileName : ""1Test1"" } 
			] 
		} 
	] 
}
");

			JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);
		}

		[Test]
		public void Deserialize_StandardJson_InnerResourceSet_TwoLevels()
		{
			var rt = _model.GetResourceType("Repository").Value;

			var reader = new StringReader(@"
{ 
	Id: 1, 
	Name: ""NewName"", 
	Branches: [ 
		{ 
			Id: 2, 
			Name: ""Branch1"", 
			Revisions: [ 
				{ Id :100, FileName : ""Test1"" }, 
				{ Id :101, FileName : ""Test2"" } 
			] 
		}, 
		{ 
			Id: 3, 
			Name: ""Branch2"", 
			Revisions: [ 
				{ Id :200, FileName : ""1Test1"" } 
			] 
		} 
	] 
}
");

			var result = JSonSerialization.DeserializerInstance.DeserializeSingle(rt, reader, Encoding.UTF8, null);

			result.Should().NotBeNull();
			var repo = (SerializationModel.Repository)result;
			repo.Id.Should().Be(1);
			repo.Name.Should().Be("NewName");
 			repo.Branches.Should().HaveCount(2);

			var branch1 = repo.Branches.ElementAt(0);
			var branch2 = repo.Branches.ElementAt(1);
			branch1.Id.Should().Be(2);
			branch2.Id.Should().Be(3);
			branch1.Name.Should().Be("Branch1");
			branch2.Name.Should().Be("Branch2");

			branch1.Revisions.Should().HaveCount(2);
			branch2.Revisions.Should().HaveCount(1);

			branch1.Revisions.ElementAt(0).Id.Should().Be(100);
			branch1.Revisions.ElementAt(1).Id.Should().Be(101);
			branch1.Revisions.ElementAt(0).FileName.Should().Be("Test1");
			branch1.Revisions.ElementAt(1).FileName.Should().Be("Test2");

			branch2.Revisions.ElementAt(0).Id.Should().Be(200);
			branch2.Revisions.ElementAt(0).FileName.Should().Be("1Test1");
		}
	}
}

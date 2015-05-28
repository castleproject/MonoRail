namespace Castle.MonoRail.Extension.OData.Integration.Tests
{
	using System;
	using System.IO;
	using System.Net;
	using System.Text;
	using FluentAssertions;
	using MonoRail.Integration.Tests;
	using NUnit.Framework;

	/// <summary>
	/// The server side model has no direct relation to the resource navigation 
	/// </summary>
	[TestFixture, Category("Integration")]
	public class ModelWithRefsTestCase : BaseServerTest
	{
		public ModelWithRefsTestCase()
		{
			this.WebSiteFolder = "ODataTestWebSite";
		}

		[Test]
		public void FetchingRepositories()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/models/Repo/Repositories")));
			req.Accept = "application/json";
			req.Method = "GET";
			var reply = (HttpWebResponse)req.GetResponse();
			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			reply.ContentType.Should().Be("application/json; charset=utf-8");
			var replyContent = new StreamReader(reply.GetResponseStream()).ReadToEnd();
			replyContent.Should().BeEquivalentTo(@"{
	""d"": [
		{
			""__metadata"": {
				""uri"": ""http://localhost:1302/models/Repo/Repositories(1)"",
				""type"": ""ns.Repository""
			},
			""Id"": 1,
			""Name"": ""repo1"",
			""Branches"": {
				""__deferred"": {
					""uri"": ""http://localhost:1302/models/Repo/Repositories(1)/Branches""
				}
			}
		}
	]
}");
		}

		[Test]
		public void FetchingRepositories_ExpandingOnBranches()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/models/Repo/Repositories?$expand=Branches&$format=simplejson")));
			req.Accept = "application/json";
			req.Method = "GET";
			var reply = (HttpWebResponse)req.GetResponse();
			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			reply.ContentType.Should().Be("application/json; charset=utf-8");
			var replyContent = new StreamReader(reply.GetResponseStream()).ReadToEnd();
			// Console.WriteLine(replyContent);

			replyContent.Should().BeEquivalentTo(@"[
	{
		""Id"": 1,
		""Name"": ""repo1"",
		""Branches"": [
			{
				""Id"": 100,
				""Name"": ""Initial Spike"",
				""Revisions"": {}
			},
			{
				""Id"": 101,
				""Name"": ""develop"",
				""Revisions"": {}
			}
		]
	}
]");
		}

		[Test]
		public void FetchingRepositories_ExpandingOnBranchesAndRevisions()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/models/Repo/Repositories?$expand=Branches/Revisions&$format=simplejson")));
			req.Accept = "application/json";
			req.Method = "GET";
			var reply = (HttpWebResponse)req.GetResponse();
			reply.StatusCode.Should().Be(HttpStatusCode.OK);
			reply.ContentType.Should().Be("application/json; charset=utf-8");
			var replyContent = new StreamReader(reply.GetResponseStream()).ReadToEnd();

			replyContent.Should().BeEquivalentTo(@"[
	{
		""Id"": 1,
		""Name"": ""repo1"",
		""Branches"": [
			{
				""Id"": 100,
				""Name"": ""Initial Spike"",
				""Revisions"": [
					{
						""Id"": 3000,
						""FileName"": ""File1"",
						""UserId"": 102
					},
					{
						""Id"": 3001,
						""FileName"": ""File2"",
						""UserId"": 102
					},
					{
						""Id"": 3002,
						""FileName"": ""File1"",
						""UserId"": 101
					}
				]
			},
			{
				""Id"": 101,
				""Name"": ""develop"",
				""Revisions"": [
					{
						""Id"": 4000,
						""FileName"": ""File31"",
						""UserId"": 102
					},
					{
						""Id"": 4001,
						""FileName"": ""File21"",
						""UserId"": 102
					},
					{
						""Id"": 4002,
						""FileName"": ""File11"",
						""UserId"": 101
					}
				]
			}
		]
	}
]");
		}

		[Test]
		public void Create_Repository()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/models/Repo/Repositories")));

			req.Accept = "application/json";
			req.ContentType = "application/json";
			req.Method = "POST";
			var reqWriter = new StreamWriter(req.GetRequestStream());

			reqWriter.Write(
@"{
  ""d"": {
    ""Name"": ""Repo 2""
  }
}");
			reqWriter.Flush();

			var reply = (HttpWebResponse)req.GetResponse();
			reply.StatusCode.Should().Be(HttpStatusCode.Created);
			reply.ContentType.Should().Be("application/json; charset=utf-8");
			// var replyContent = new StreamReader(reply.GetResponseStream()).ReadToEnd();
			// Console.WriteLine(replyContent);
		}

		[Test]
		public void Update_Repository()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/models/Repo/Repositories(1)")));

			req.Accept = "application/json";
			req.ContentType = "application/json";
			req.Method = "PUT";
			var reqWriter = new StreamWriter(req.GetRequestStream());

			reqWriter.Write(
@"{
  ""d"": {
    ""Name"": ""New name""
  }
}");
			reqWriter.Flush();

			var reply = (HttpWebResponse)req.GetResponse();
			reply.StatusCode.Should().Be(HttpStatusCode.NoContent);
		}

		[Test]
		public void Create_Branch()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/models/Repo/Repositories(1)/Branches")));

			req.Accept = "application/json";
			req.ContentType = "application/json";
			req.Method = "POST";
			var reqWriter = new StreamWriter(req.GetRequestStream());

			reqWriter.Write(
@"{
  ""d"": {
    ""Name"": ""New name""
  }
}");
			reqWriter.Flush();

			var reply = (HttpWebResponse)req.GetResponse();
			reply.StatusCode.Should().Be(HttpStatusCode.Created);
		}

		[Test]
		public void Update_Branch()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/models/Repo/Repositories(1)/Branches(100)")));

			req.Accept = "application/json";
			req.ContentType = "application/json";
			req.Method = "PUT";
			var reqWriter = new StreamWriter(req.GetRequestStream());

			reqWriter.Write(
@"{
  ""d"": {
    ""Name"": ""New name""
  }
}");
			reqWriter.Flush();

			var reply = (HttpWebResponse)req.GetResponse();
			reply.StatusCode.Should().Be(HttpStatusCode.NoContent);
		}

		[Test, Description("Since this model uses indirection types, the metadata should not reflect those")]
		public void CorrectMetadataGenerated()
		{
			var req = (HttpWebRequest)WebRequest.CreateDefault(new Uri(BuildUrl("/models/Repo/$metadata")));

			var rep = req.GetResponse();
			var reader = new StreamReader(rep.GetResponseStream(), Encoding.UTF8);
			reader.ReadToEnd().Should().BeEquivalentTo(@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
<edmx:Edmx Version=""1.0"" xmlns:edmx=""http://schemas.microsoft.com/ado/2007/06/edmx"">
  <edmx:DataServices xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" m:DataServiceVersion=""2.0"">
    <Schema Namespace=""ns"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://schemas.microsoft.com/ado/2008/09/edm"">
      <EntityType Name=""Repository"">
        <Key>
          <PropertyRef Name=""Id"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""Name"" Type=""Edm.String"" Nullable=""true"" />
        <NavigationProperty Name=""Branches"" Relationship=""ns.Repository_Branches"" FromRole=""Repository"" ToRole=""Branches"" />
      </EntityType>
      <EntityType Name=""Branch"">
        <Key>
          <PropertyRef Name=""Id"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""Name"" Type=""Edm.String"" Nullable=""true"" />
        <NavigationProperty Name=""Revisions"" Relationship=""ns.Branch_Revisions"" FromRole=""Branch"" ToRole=""Revisions"" />
      </EntityType>
      <EntityType Name=""Revision"">
        <Key>
          <PropertyRef Name=""Id"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""FileName"" Type=""Edm.String"" Nullable=""true"" />
        <Property Name=""UserId"" Type=""Edm.Int32"" Nullable=""false"" />
      </EntityType>
      <Association Name=""Repository_Branches"">
        <End Role=""Repository"" Type=""ns.Repository"" Multiplicity=""*"" />
        <End Role=""Branches"" Type=""ns.Branch"" Multiplicity=""*"" />
      </Association>
      <Association Name=""Branch_Revisions"">
        <End Role=""Branch"" Type=""ns.Branch"" Multiplicity=""*"" />
        <End Role=""Revisions"" Type=""ns.Revision"" Multiplicity=""*"" />
      </Association>
      <EntityContainer Name=""container"" m:IsDefaultEntityContainer=""true"">
        <EntitySet Name=""Repositories"" EntityType=""ns.Repository"" />
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>");
		}


	}
}

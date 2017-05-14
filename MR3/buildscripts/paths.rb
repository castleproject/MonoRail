
BASE_DIR = File.expand_path("#{File.dirname(__FILE__)}/..")

Names = {

}

CoreFolders = {
	:root  => BASE_DIR,
	:src   => File.join(BASE_DIR, "src"),
	:out   => File.join(BASE_DIR, "build"),
	:tests => File.join(BASE_DIR, "build", "tests")
}

Folders = CoreFolders.merge( {

	:tools 	        => File.join(CoreFolders[:root], "tools"),
	:nunit          => File.join(CoreFolders[:root], "tools", "NUnit"),
	:nuget			=> File.join(CoreFolders[:root], "tools")
})

Commands = {
	:nunit          => File.join(Folders[:nunit], "nunit-console.exe"),
	:nuget          => File.join(Folders[:tools], "NuGet.exe")
}

Files = {
	:solution => File.join(Folders[:root], "Castle.MonoRail.sln"),
	:test_out => File.join(Folders[:tests], "test-out.log"),
	:test_out_xml => File.join(Folders[:tests], "test-out.xml"),
}



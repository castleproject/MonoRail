$: << './'
require 'rubygems'
require 'fileutils'
require 'albacore'
require 'rake/clean'
require 'buildscripts/paths'
require 'buildscripts/asm_list'

PRODUCT_NAME = 'MonoRail3'
VERSION = '0.0.1.' + (ENV['BUILD_NUMBER'] || '0')
IS_TEAMCITY = !(ENV['BUILD_NUMBER'].nil?)

Albacore.configure do |config|
	config.log_level = :verbose
	config.nunit.command = Commands[:nunit]
	config.msbuild.use :net4
end

desc "Clean, create shared assembly info, compile and test."
task :default => [:clean, :compile, :test, :nuget]

#Add the folders that should be cleaned as part of the clean task
CLEAN.clear #core is included by clean.rb by default.
#CLEAN.include(FileList["**/bin/Release"])
#CLEAN.include(FileList["**/bin/Debug"])
#CLEAN.include(FileList["**/obj"])
#CLEAN.include(FileList["**/bin"])
CLEAN.include(Folders[:out])
CLEAN.include(Folders[:tests])

desc "Compile solution file"
msbuild :compile do |msb|
	Dir.mkdir Folders[:out] unless File.exists? Folders[:out]
	msb.solution = Files[:solution]
	msb.targets :Clean, :Build
	msb.properties :Configuration => :Debug # , :OutDir => "#{Folders[:out]}/"
	msb.parameters "/nologo" 
	msb.verbosity = "minimal"
	msb.loggermodule = "JetBrains.BuildServer.MSBuildLoggers.MSBuildLogger,#{ENV['MSBuildLogger']}"  if IS_TEAMCITY
end

task :test => [:run_tests_real, :test_publish_artifacts]

desc "Runs nunit for every testing assembly"
task :run_tests do |t|

end

task :test_publish_artifacts => :run_tests do
	# puts "##teamcity[importData type='nunit' path='#{Files[:test_out_xml]}']"
	# puts "##teamcity[publishArtifacts '#{Files[:test_out]}']"
end

task :nuget => [:nuget_core, :nuget_windsor, :nuget_odata]

task :nuget_core do |t| 
	nugetpack do |nuget|
	   nuget.command     = "#{Commands[:nuget]}"
	   nuget.nuspec      = "nuspecs/monorail.nuspec"
	   nuget.base_folder = "#{Folders[:out]}/"
	   nuget.output      = "#{Folders[:out]}/"
	end
end

task :nuget_windsor do |t| 
end

task :nuget_odata do |t| 
end

nunit :run_tests_real do |nunit|
	Dir.mkdir "#{Folders[:tests]}/" unless File.exists? Folders[:tests]
	nunit.command = Commands[:nunit]
	nunit.options '/framework v4.0'   #, "/out #{Files[:test_out]}", "/xml #{Files[:test_out_xml]}"
	nunit.assemblies FileList.new("#{Folders[:out]}/*.Tests.dll")
	
	puts "##teamcity[importData type='nunit' path='#{Files[:test_out_xml]}']"
	puts "##teamcity[publishArtifacts '#{Files[:test_out]}']"
end


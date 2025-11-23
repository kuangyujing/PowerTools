up:
	dotnet run --project PowerTools.Server

cache-packages:
	dotnet restore -r linux-x64 --packages ./packages
	dotnet restore -r osx-arm64 --packages ./packages
	dotnet restore -r win-x64 --packages ./packages

restore:
	NUGET_PACKAGES=./packages dotnet restore --source ./packages --no-http-cache

dotnet restore
dotnet build --configuration Release PunishLib/ECommons/ECommons/ECommons.csproj
dotnet build --configuration Release PunishLib/PunishLib/PunishLib.csproj

dotnet build --configuration Debug PunishLib/ECommons/ECommons/ECommons.csproj
dotnet build --configuration Debug PunishLib/PunishLib/PunishLib.csproj

dotnet build --configuration Release
dotnet build --configuration Debug
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ExpenseIntelligence.sln ./
COPY src/ExpenseIntelligence.Domain ./src/ExpenseIntelligence.Domain
COPY src/ExpenseIntelligence.Infrastructure ./src/ExpenseIntelligence.Infrastructure
COPY src/ExpenseIntelligence.Api ./src/ExpenseIntelligence.Api
COPY seed ./seed
RUN dotnet restore src/ExpenseIntelligence.Api/ExpenseIntelligence.Api.csproj
RUN dotnet publish src/ExpenseIntelligence.Api/ExpenseIntelligence.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/publish .
COPY seed ./seed
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpenseIntelligence.Api.dll"]

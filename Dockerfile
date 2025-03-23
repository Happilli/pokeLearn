FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["pokesharpC.csproj", "./"]
RUN dotnet restore "pokesharpC.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "pokesharpC.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "pokesharpC.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pokesharpC.dll"]

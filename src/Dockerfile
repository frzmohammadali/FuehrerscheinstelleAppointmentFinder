FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/", "src/"]
RUN dotnet restore "FuehrerscheinstelleAppointmentFinder.csproj"
RUN dotnet build "FuehrerscheinstelleAppointmentFinder.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src
RUN dotnet publish "FuehrerscheinstelleAppointmentFinder.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FuehrerscheinstelleAppointmentFinder.dll"]
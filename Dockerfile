# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "GambiarraBem Feita.csproj"
RUN dotnet publish "GambiarraBem Feita.csproj" -c Release -o /app

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

EXPOSE 10000
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000}
CMD ["dotnet", "GambiarraBem_Feita.dll"]
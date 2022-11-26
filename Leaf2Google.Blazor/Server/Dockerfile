#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Leaf2Google.Blazor/Server/Leaf2Google.Blazor.Server.csproj", "Leaf2Google.Blazor/Server/"]
COPY ["Leaf2Google.Models/Leaf2Google.Models.csproj", "Leaf2Google.Models/"]
COPY ["Leaf2Google.Blazor/Client/Leaf2Google.Blazor.Client.csproj", "Leaf2Google.Blazor/Client/"]
RUN apt-get update -y
RUN apt-get install -y python3
RUN dotnet workload install wasm-tools
RUN dotnet restore "Leaf2Google.Blazor/Server/Leaf2Google.Blazor.Server.csproj"
COPY . .
WORKDIR "/src/Leaf2Google.Blazor/Server"
RUN dotnet build "Leaf2Google.Blazor.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Leaf2Google.Blazor.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Leaf2Google.Blazor.Server.dll"]
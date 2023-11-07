#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Llama/LlamaApi/LlamaApi.csproj", "Llama/LlamaApi/"]
COPY ["Logging/LoggingApiClient/LoggingApiClient.csproj", "Logging/LoggingApiClient/"]
COPY ["Logging/Logging.Shared/Logging.Shared.csproj", "Logging/Logging.Shared/"]
COPY ["Llama/Llama.Core/Llama.Core.csproj", "Llama/Llama.Core/"]
COPY ["Utils/Ai.Utils/Ai.Utils.csproj", "Utils/Ai.Utils/"]
COPY ["Llama/Llama.Data/Llama.Data.csproj", "Llama/Llama.Data/"]
COPY ["Llama/Llama.Extensions/Llama.Extensions.csproj", "Llama/Llama.Extensions/"]
COPY ["Llama/Llama.Native/Llama.Native.csproj", "Llama/Llama.Native/"]
COPY ["Llama/LlamaApi.Shared/LlamaApi.Shared.csproj", "Llama/LlamaApi.Shared/"]
RUN dotnet restore "Llama/LlamaApi/LlamaApi.csproj"
COPY . .
WORKDIR "/src/Llama/LlamaApi"
RUN dotnet build "LlamaApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LlamaApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install Python
RUN apt-get update 
RUN apt-get install -y python3 
RUN apt-get install -y python3-pip
RUN ln -s /usr/bin/python3 /usr/bin/python
RUN rm -rf /var/lib/apt/lists/*

RUN pip install runpod

CMD python -u /app/handler.py
# Use the NVIDIA base image
FROM nvidia/cuda:12.2.2-runtime-ubuntu22.04 AS base

# Update packages
run apt-get update
RUN apt-get install -y python3 python3-pip aspnetcore-runtime-6.0
RUN ln -s /usr/bin/python3 /usr/bin/python 
RUN pip3 install runpod 

# Set the working directory and expose the necessary ports
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET SDK to build the project
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
RUN dotnet build "LlamaApi.csproj" --v:q -c Release -o /app/build    

FROM nvidia/cuda:12.2.2-devel-ubuntu22.04 AS nativebuild
WORKDIR /src
COPY Llama/llama.cpp .
RUN apt-get update
RUN apt-get install -y cmake
RUN mkdir build && \
    cd build && \
    cmake .. -DBUILD_SHARED_LIBS=ON -DLLAMA_CUBLAS=ON -DLLAMA_AVX2=ON -DLLAMA_AVX=ON -DLLAMA_CUDA_F16=ON -DCMAKE_CXX_FLAGS="-w" -DCMAKE_C_FLAGS="-w" && \
    cmake --build . --verbose --config Release

# Publish the project
FROM build AS publish
RUN dotnet publish "LlamaApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Set up the final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#COPY --from=nativebuild  /src/build/libggml_shared.so .
#COPY --from=nativebuild  /src/build/libggml_static.a .
#COPY --from=nativebuild  /src/build/libllama.so .
COPY --from=nativebuild  /src/build .
RUN rm -rf /var/lib/apt/lists/*

# Set the command to run the application
CMD python -u /app/handler.py

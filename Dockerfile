## Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.
#
## Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#USER $APP_UID
#WORKDIR /app
#EXPOSE 8080
#EXPOSE 8081
#
#
## Esta fase é usada para compilar o projeto de serviço
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#COPY ["ZapScrap.API/ZapScrap.API.csproj", "ZapScrap.API/"]
#RUN dotnet restore "./ZapScrap.API/ZapScrap.API.csproj"
#COPY . .
#WORKDIR "/src/ZapScrap.API"
#RUN dotnet build "./ZapScrap.API.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
## Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
#FROM build AS publish
#ARG BUILD_CONFIGURATION=Release
#RUN dotnet publish "./ZapScrap.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
## Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "ZapScrap.API.dll"]

# Etapa base com .NET e Chrome instalado
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas o csproj para cache do restore
COPY ["ZapScrap.API/ZapScrap.API.csproj", "ZapScrap.API/"]
RUN dotnet restore "ZapScrap.API/ZapScrap.API.csproj"

# Copia todo o restante do código
COPY . .

WORKDIR "/src/ZapScrap.API"
RUN dotnet build "ZapScrap.API.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "ZapScrap.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Variável para Render
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ZapScrap.API.dll"]

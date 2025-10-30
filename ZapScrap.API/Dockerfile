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
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Instala o Google Chrome e dependências
RUN apt-get update && apt-get install -y wget gnupg unzip \
    && wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list' \
    && apt-get update && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

# Etapa de build do projeto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ZapScrap.API.csproj", "./"]
RUN dotnet restore "./ZapScrap.API.csproj"
COPY . .
WORKDIR "/app"
RUN dotnet build "./ZapScrap.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Fase final (produção)
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Variável obrigatória para o Render
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ZapScrap.API.dll"]

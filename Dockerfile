FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ZapScrap.API/ZapScrap.API.csproj", "ZapScrap.API/"]
RUN dotnet restore "ZapScrap.API/ZapScrap.API.csproj"

COPY . .
WORKDIR "/src/ZapScrap.API"
RUN dotnet build "ZapScrap.API.csproj" -c Release -o /app/build
RUN dotnet publish "ZapScrap.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instala dependências
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    unzip \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Instala Chrome e ChromeDriver usando versões específicas conhecidas
RUN wget -q https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb \
    && apt-get update \
    && apt-get install -y ./google-chrome-stable_current_amd64.deb \
    && rm google-chrome-stable_current_amd64.deb \
    && rm -rf /var/lib/apt/lists/*

# Instala ChromeDriver versão 130 (mais estável)
RUN wget -q https://storage.googleapis.com/chrome-for-testing-public/130.0.6723.116/linux64/chromedriver-linux64.zip \
    && unzip chromedriver-linux64.zip \
    && mv chromedriver-linux64/chromedriver /usr/local/bin/ \
    && chmod +x /usr/local/bin/chromedriver \
    && rm -rf chromedriver-linux64.zip chromedriver-linux64

# Verifica se tudo foi instalado
RUN which google-chrome && which chromedriver \
    && google-chrome --version \
    && chromedriver --version

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ZapScrap.API.dll"]
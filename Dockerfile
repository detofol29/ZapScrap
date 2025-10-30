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

# Instala Chrome versão específica 131 (compatível com ChromeDriver 131)
RUN wget -q https://storage.googleapis.com/chrome-for-testing-public/131.0.6778.204/linux64/chrome-linux64.zip \
    && unzip chrome-linux64.zip \
    && mv chrome-linux64 /opt/chrome \
    && ln -s /opt/chrome/chrome /usr/bin/google-chrome \
    && rm chrome-linux64.zip

# Instala ChromeDriver versão 131 (mesma versão do Chrome)
RUN wget -q https://storage.googleapis.com/chrome-for-testing-public/131.0.6778.204/linux64/chromedriver-linux64.zip \
    && unzip chromedriver-linux64.zip \
    && mv chromedriver-linux64/chromedriver /usr/local/bin/ \
    && chmod +x /usr/local/bin/chromedriver \
    && rm -rf chromedriver-linux64.zip chromedriver-linux64

# Instala dependências do Chrome
RUN apt-get update && apt-get install -y \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libatspi2.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libwayland-client0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxkbcommon0 \
    libxrandr2 \
    xdg-utils \
    libu2f-udev \
    libvulkan1 \
    && rm -rf /var/lib/apt/lists/*

# Verifica as versões instaladas
RUN google-chrome --version && chromedriver --version

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ZapScrap.API.dll"]
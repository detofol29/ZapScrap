using System.Diagnostics;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using ZapScrap.API.Enuns;
using ZapScrap.API.Modelos;

namespace ZapImoveisWebScraper
{
    public class ZapImoveisService : IDisposable
    {
        private IWebDriver _driver;
        private readonly bool _modoHeadless;

        public ZapImoveisService(bool modoHeadless = true)
        {
            _modoHeadless = modoHeadless;
            InicializarDriver();
        }

        //private void InicializarDriver()
        //{
        //    try
        //    {
        //        Console.WriteLine("Verificando e baixando ChromeDriver compatível...");
        //        new DriverManager().SetUpDriver(new ChromeConfig(), "141.0.7390.108");
        //        Console.WriteLine("✓ ChromeDriver configurado!");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"⚠️ Aviso ao configurar WebDriverManager: {ex.Message}");
        //        Console.WriteLine("Tentando usar ChromeDriver local...");
        //    }

        //    var options = new ChromeOptions();

        //    if (_modoHeadless)
        //    {
        //        options.AddArgument("--headless=new"); // modo headless atualizado
        //    }

        //    options.AddArgument("--no-sandbox");
        //    options.AddArgument("--disable-dev-shm-usage");
        //    options.AddArgument("--disable-gpu");
        //    options.AddArgument("--window-size=1920,1080");
        //    options.AddArgument("--disable-blink-features=AutomationControlled");
        //    options.AddExcludedArgument("enable-automation");
        //    options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        //    options.AddAdditionalOption("useAutomationExtension", false);
        //    options.AddUserProfilePreference("credentials_enable_service", false);
        //    options.AddUserProfilePreference("profile.password_manager_enabled", false);

        //    // Caminho do ChromeDriver (Render roda em Linux)
        //    var chromeDriverService = ChromeDriverService.CreateDefaultService();
        //    chromeDriverService.HideCommandPromptWindow = true;

        //    // >>> MUITO IMPORTANTE <<<
        //    chromeDriverService.EnableVerboseLogging = false;

        //    // Use o path correto para o Chrome no Linux container
        //    options.BinaryLocation = "/usr/bin/google-chrome";

        //    try
        //    {
        //        _driver = new ChromeDriver(chromeDriverService, options);
        //        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        //        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        //        Console.WriteLine("✓ ChromeDriver inicializado com sucesso!");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Erro ao inicializar ChromeDriver: {ex.Message}");
        //        Console.WriteLine("\nCertifique-se de que:");
        //        Console.WriteLine("1. O Chrome está instalado");
        //        Console.WriteLine("2. O ChromeDriver está instalado (dotnet add package Selenium.WebDriver.ChromeDriver)");
        //        Console.WriteLine("3. As versões do Chrome e ChromeDriver são compatíveis");
        //        throw;
        //    }
        //}


        private void InicializarDriver()
        {
            var options = new ChromeOptions();

            if (_modoHeadless)
            {
                options.AddArgument("--headless=new");
            }

            // Argumentos essenciais para container Docker
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-setuid-sandbox");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            options.AddAdditionalOption("useAutomationExtension", false);
            options.AddUserProfilePreference("credentials_enable_service", false);
            options.AddUserProfilePreference("profile.password_manager_enabled", false);

            // Caminho do Chrome no container (ajustado para a instalação manual)
            options.BinaryLocation = "/usr/bin/google-chrome";

            try
            {
                Console.WriteLine("Inicializando ChromeDriver...");
                _driver = new ChromeDriver(options);
                _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                Console.WriteLine("✓ ChromeDriver inicializado com sucesso!");
                //Console.WriteLine($"Chrome version: {_driver.Capabilities.GetCapability("browserVersion")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao inicializar ChromeDriver: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<Imovel>> BuscarImoveis(ZapScrapRequest filtros)
        {
            var todosImoveis = new List<Imovel>();

            for (int pagina = 1; pagina <= filtros.Paginas; pagina++)
            {

                    var url = GerarUrl(filtros);
                    Console.WriteLine($"\nBuscando página {pagina}: {url}");


                    _driver.Navigate().GoToUrl(url);

                    Console.WriteLine("Aguardando carregamento da página...");
                    await Task.Delay(3000);

                    RolarPagina();

                    await Task.Delay(5000);

                    var html = _driver.PageSource;
  

                    // Extrai imóveis
                var imoveis = ExtrairImoveis(html, _driver);

                if (imoveis.Count == 0)
                    {
                        Console.WriteLine("⚠️  Nenhum imóvel encontrado nesta página.");
                        Console.WriteLine("Possíveis causas:");
                        Console.WriteLine("- Não há imóveis para estes filtros");
                        Console.WriteLine("- O site mudou sua estrutura HTML");
                        Console.WriteLine("- Está sendo exibido um CAPTCHA");

                        // Tenta detectar CAPTCHA ou bloqueio
                        if (html.ToLower().Contains("captcha") || html.ToLower().Contains("verificação"))
                        {
                            Console.WriteLine("\n❌ CAPTCHA detectado! O site está bloqueando.");
                            Console.WriteLine("Aguardando 10 segundos... Resolva o CAPTCHA manualmente se a janela estiver visível.");
                            await Task.Delay(10000);
                            html = _driver.PageSource;
                            imoveis = ExtrairImoveis(html, _driver);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"✓ Encontrados {imoveis.Count} imóveis na página {pagina}");
                    }

                    todosImoveis.AddRange(imoveis);

                    // Aguarda entre páginas
                    if (pagina < filtros.Paginas)
                    {
                        var delay = 3000 + new Random().Next(1000, 3000);
                        Console.WriteLine($"Aguardando {delay / 1000}s antes da próxima página...");
                        await Task.Delay(delay);
                    }
            }

            return todosImoveis;
        }

        private string GerarUrl(ZapScrapRequest filtros) 
        {
            var tipoNegocio = filtros.TipoAnuncio == TipoDeAnuncio.Aluguel 
                ? "aluguel" 
                : "venda";

            var url = $"https://www.zapimoveis.com.br/{tipoNegocio}/";

            var cidade = filtros.Cidade.ToLower();
            var estado = filtros.Estado.ToLower();
            var bairro = filtros.Bairro?.Trim().ToLower().Replace(" ", "-");

            var localizacao = $"{estado}+{cidade}";

            if (!string.IsNullOrEmpty(bairro))
                localizacao += $"++{bairro}";

            if (filtros.TipoImovel is not null)
            {
                var TipoImovel = filtros.TipoImovel == TipoDeImovel.Casa
                    ? "casas"
                    : "apartamentos";

                url += $"{TipoImovel}/";
            }
            else
            {
                url += $"imoveis/";
            }

            url += $"{localizacao}/?transacao={tipoNegocio}";

            if (filtros.ValorMaximo is not null && filtros.ValorMaximo != decimal.Zero)
            {
                url += $"&precoMaximo={filtros.ValorMaximo.ToString()}";
            }

            if (filtros.QuantidadeMinimaDeQuartos is not null && filtros.QuantidadeMinimaDeQuartos != decimal.Zero)
            {
                var quartosMinimos = (int)filtros.QuantidadeMinimaDeQuartos;
                var listaQuartos = new List<int>();

                // adiciona todos os valores a partir do mínimo até 4 (máximo suportado pelo filtro do Zap)
                for (int i = quartosMinimos; i <= 4; i++)
                {
                    listaQuartos.Add(i);
                }

                // monta a string no formato exigido pela URL (ex: "2%2C3%2C4")
                var quartosParam = string.Join("%2C", listaQuartos);

                url += $"&quartos={quartosParam}";
            }

            if (filtros.QuantidadeMinimaDeBanheiros is not null && filtros.QuantidadeMinimaDeBanheiros != decimal.Zero)
            {
                var banheirosMinimos = (int)filtros.QuantidadeMinimaDeBanheiros;
                var listaBanheiros = new List<int>();

                for (int i = banheirosMinimos; i <= 4; i++)
                {
                    listaBanheiros.Add(i);
                }

                var banheirosParam = string.Join("%2C", listaBanheiros);

                url += $"&banheiros={banheirosParam}";
            }

            if (filtros.QuantidadeMinimaDeVagasNaGaragem is not null && filtros.QuantidadeMinimaDeVagasNaGaragem != decimal.Zero)
            {
                var vagasMinimos = (int)filtros.QuantidadeMinimaDeVagasNaGaragem;
                var listaVagas = new List<int>();

                for (int i = vagasMinimos; i <= 4; i++)
                {
                    listaVagas.Add(i);
                }

                var vagasParam = string.Join("%2C", listaVagas);

                url += $"&vagas={vagasParam}";
            }

            if (filtros.AreaMinima is not null && filtros.AreaMinima != decimal.Zero)
            {

                url += $"&areaMinima={filtros.AreaMinima}";
            }

            return url;
        }

        private void RolarPagina()
        {
            try
            {
                var js = (IJavaScriptExecutor)_driver;

                // Rola até o meio da página
                js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight / 2);");
                Thread.Sleep(2000);

                // Rola até o final
                js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
                Thread.Sleep(2000);

                // Volta para o topo
                js.ExecuteScript("window.scrollTo(0, 0);");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso ao rolar página: {ex.Message}");
            }
        }

        private List<Imovel> ExtrairImoveis(string html, IWebDriver driver)
        {
            var imoveis = new List<Imovel>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Tenta encontrar os cards de imóveis com múltiplos seletores
            var possiveisSeletores = new[]
            {
                "//div[@data-type='property']",
                "//div[contains(@class, 'result-card')]",
                "//article[contains(@class, 'card')]",
                "//div[contains(@class, 'ListingCard')]",
                "//div[contains(@data-testid, 'listing-card')]",
                "//a[contains(@href, '/imovel/')]",
                "//div[contains(@class, 'listing')]",
                "//div[contains(@class, 'CardContainer')]"
            };

            var nenhumEncontrado = doc.DocumentNode.SelectNodes("//h2[contains(@title, 'Não encontramos imóveis com correspondência exata')]");

            if(nenhumEncontrado != null)
            {
                throw new Exception("Nenhum imóvel encontrado com os filtros informados.");
            }

            HtmlNodeCollection cards = null;

            foreach (var seletor in possiveisSeletores)
            {
                cards = doc.DocumentNode.SelectNodes(seletor);
                if (cards != null && cards.Count > 0)
                {
                    Console.WriteLine($"✓ Usando seletor: {seletor} ({cards.Count} cards)");
                    var nenhumValorEncontrado = doc.DocumentNode.InnerHtml.Contains("Não encontramos imóveis com correspondência exata\r\n");
                    break;
                }
            }

            // Se não encontrou, tenta com Selenium direto
            if (cards == null || cards.Count == 0)
            {
                Console.WriteLine("Tentando buscar elementos com Selenium...");
                try
                {
                    var elementos = driver.FindElements(By.CssSelector("a[href*='/imovel/']"));
                    if (elementos.Count > 0)
                    {
                        Console.WriteLine($"✓ Encontrados {elementos.Count} links de imóveis com Selenium");
                        return ExtrairImoveisSelenium(elementos);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao buscar com Selenium: {ex.Message}");
                }

                return imoveis;
            }

            // Processa cada card
            foreach (var card in cards)
            {
                try
                {
                    var imovel = ExtrairDadosCard(card);

                    if (!string.IsNullOrEmpty(imovel.Titulo) ||
                        !string.IsNullOrEmpty(imovel.Link) ||
                        !string.IsNullOrEmpty(imovel.Preco))
                    {
                        imoveis.Add(imovel);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar card: {ex.Message}");
                }
            }

            return imoveis;
        }

        private Imovel ExtrairDadosCard(HtmlNode card)
        {
            var imovel = new Imovel();

            // Título
            var tituloNode = card.SelectSingleNode(".//h2") ??
                            card.SelectSingleNode(".//h3") ??
                            card.SelectSingleNode(".//*[@class='card-title']") ??
                            card.SelectSingleNode(".//a[@title]");

            imovel.Titulo = LimparTexto(tituloNode?.InnerText);
            if (string.IsNullOrEmpty(imovel.Titulo) && tituloNode != null)
                imovel.Titulo = tituloNode.GetAttributeValue("title", "");

            // Link

            card.InnerHtml = card.OuterHtml;
            var linkNode = card.SelectSingleNode(".//a[@href]");
            if (linkNode != null)
            {
                var link = linkNode.GetAttributeValue("href", "");
                imovel.Link = link.StartsWith("http") ? link : $"https://www.zapimoveis.com.br{link}";
            }

            // Preço
            var precoNodes = card.SelectNodes(".//*[contains(text(), 'R$')]");
            if (precoNodes != null && precoNodes.Count > 0)
            {
                foreach (var node in precoNodes)
                {
                    var texto = LimparTexto(node.InnerText);
                    if (texto.Contains("R$") && !texto.ToLower().Contains("cond"))
                    {
                        imovel.Preco = texto;
                        break;
                    }
                }

                foreach (var node in precoNodes)
                {
                    var texto = LimparTexto(node.InnerText);
                    if (texto.Contains("R$") && texto.ToLower().Contains("cond"))
                    {
                        imovel.PrecoCondominio = texto.Split('•').First().Replace("Cond.", string.Empty).Trim();
                        imovel.Iptu = texto.Split('•').Last().Replace("IPTU", string.Empty).Trim();
                        if (imovel.Iptu.Contains("Cond"))
                        {
                            imovel.Iptu = "Não informado";
                        }
                        break;
                    }
                }


            }

            // Endereço
            var enderecoNode = card.SelectSingleNode(".//*[contains(@class, 'address')]") ??
                              card.SelectSingleNode(".//*[contains(@data-cy, 'street')]");
            imovel.Endereco = LimparTexto(enderecoNode?.InnerText);

            // Área
            var areaNode = card.SelectSingleNode(".//*[contains(@data-cy, 'propertyArea')]");
            var areaNumerica = Regex.Replace(areaNode?.InnerText ?? string.Empty, "[^0-9]", "");
            imovel.AreaTotal = areaNumerica + " m2";

            // Características
            ExtrairCaracteristicas(card, imovel);

            // Imagens
            ExtrairImagens(card, imovel);

            var tipoImovelNode = card.SelectSingleNode(".//*[contains(@class, 'address')]") ??
                  card.SelectSingleNode(".//*[contains(@data-cy, 'location')]");

            imovel.TipoImovel = tipoImovelNode.InnerText.Contains("Apartamento")
                ? "Apartamento"
                : tipoImovelNode.InnerText.Contains("Casa")
                    ? "Casa"
                    : imovel.Link.Contains("apartamento")
                        ? "Apartamento"
                        : "casa";



            imovel.Cidade = tipoImovelNode.InnerText.Split("em").Last().Split(",").Last().Trim();
            imovel.Bairro = tipoImovelNode.InnerText.Split("em").Last().Split(",").First().Trim();

            if (string.IsNullOrEmpty(imovel.Titulo))
            {
                var vendaAluguel = imovel.Link.Contains("venda")
                    ? "para venda"
                    : "para alugar";
                imovel.Titulo = $"{imovel.TipoImovel} {vendaAluguel} no {imovel.Bairro} com {imovel.Quartos} quartos, {imovel.Banheiros} banheiros e {imovel.Vagas} vagas de garagem";
            }
            return imovel;
        }

        private List<Imovel> ExtrairImoveisSelenium(IReadOnlyCollection<IWebElement> elementos)
        {
            var imoveis = new List<Imovel>();
            var linksProcessados = new HashSet<string>();

            foreach (var elemento in elementos)
            {
                try
                {
                    var link = elemento.GetAttribute("href");

                    if (string.IsNullOrEmpty(link) || linksProcessados.Contains(link))
                        continue;

                    linksProcessados.Add(link);

                    var imovel = new Imovel
                    {
                        Link = link,
                        Titulo = elemento.Text
                    };

                    // Tenta pegar mais informações do elemento pai
                    try
                    {
                        var pai = elemento.FindElement(By.XPath("./ancestor::div[contains(@class, 'card') or contains(@class, 'listing')][1]"));
                        imovel.Titulo = pai.Text.Split('\n')[0];
                    }
                    catch { }

                    imoveis.Add(imovel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar elemento Selenium: {ex.Message}");
                }
            }

            return imoveis;
        }

        private void ExtrairCaracteristicas(HtmlNode card, Imovel imovel)
        {
            var textoCompleto = card.InnerText.ToLower();

            // Quartos
            var padroes = new[] { @"(\d+)\s*quarto", @"(\d+)\s*dorm" };
            foreach (var padrao in padroes)
            {
                var match = System.Text.RegularExpressions.Regex.Match(textoCompleto, padrao);
                if (match.Success)
                {
                    imovel.Quartos = int.Parse(match.Groups[1].Value);
                    break;
                }

                var quartosNode = card.SelectSingleNode(".//*[contains(@data-cy, 'bedroomQuantity')]");
                var quantidade = int.Parse(quartosNode?.InnerText ?? "0");
                imovel.Quartos = quantidade;
            }

            // Banheiros
            var match2 = System.Text.RegularExpressions.Regex.Match(textoCompleto, @"(\d+)\s*banh");
            imovel.Banheiros = match2.Success
                    ? int.Parse(match2.Groups[1].Value ?? "0")
                    : int.Parse(card.SelectSingleNode(".//*[contains(@data-cy, 'bathroomQuantity')]")?.InnerText ?? "0");

            // Vagas
            var match3 = System.Text.RegularExpressions.Regex.Match(textoCompleto, @"(\d+)\s*(vaga|garagem)");
            imovel.Vagas = match3.Success
                    ? int.Parse(match3.Groups[1].Value)
                    : int.Parse(card.SelectSingleNode(".//*[contains(@data-cy, 'parkingSpacesQuantity')]")?.InnerText ?? "0");
        }

        private void ExtrairImagens(HtmlNode card, Imovel imovel)
        {
            var imagens = card.SelectNodes(".//img");

            if (imagens != null)
            {
                foreach (var img in imagens)
                {
                    var src = img.GetAttributeValue("src", "");
                    var dataSrc = img.GetAttributeValue("data-src", "");

                    var url = !string.IsNullOrEmpty(dataSrc) ? dataSrc : src;

                    if (!string.IsNullOrEmpty(url) &&
                        url.StartsWith("http") &&
                        !url.Contains("placeholder"))
                    {
                        imovel.Imagens.Add(url);
                    }
                }
            }
        }

        private string LimparTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            return System.Text.RegularExpressions.Regex.Replace(texto.Trim(), @"\s+", " ");
        }

        public void Dispose()
        {
            try
            {
                _driver?.Quit();
                _driver?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fechar driver: {ex.Message}");
            }
        }
    }
}
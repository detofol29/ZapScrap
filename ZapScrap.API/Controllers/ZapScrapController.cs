//using Microsoft.AspNetCore.Mvc;
//using System.Diagnostics;
//using ZapImoveisWebScraper;
//using ZapScrap.API.Enuns;
//using ZapScrap.API.Modelos;

//namespace ZapScrap.API.Controllers
//{
//    [ApiController]
//    [Route("[controller]/")]
//    public class ZapScrapController : ControllerBase
//    {
//        [HttpGet("ObterAnuncios")]
//        public async Task<IActionResult> ObterAnuncios(ZapScrapRequest request)
//        {
//            try
//            {
//                var scraper = new ZapImoveisService();

//                var imoveis = await scraper.BuscarImoveis(request);


//                var objetoRetorno = ObterObjetoRetorno(imoveis, (TipoDeAnuncio)request.TipoAnuncio);
//                return Ok(objetoRetorno);
//            }
//            catch (Exception ex)
//            {
//                return Ok(new { ex.Message });
//            }
//        }

//        private object ObterObjetoRetorno(List<Imovel> imoveis, TipoDeAnuncio tipoDeAnuncio)
//        {
//            return new
//            {
//                TotalDeImoveis = imoveis.Count,
//                Imoveis = imoveis.Select(imovel => new
//                {
//                    Titulo = imovel.Titulo,
//                    Valor = imovel.Preco,
//                    Condominio = imovel.PrecoCondominio,
//                    Iptu = imovel.Iptu,
//                    Area = imovel.AreaTotal,
//                    Quartos = imovel.Quartos,
//                    Banheiros = imovel.Banheiros,
//                    VagasNaGaragem = imovel.Vagas,
//                    Endereco = imovel.Endereco,
//                    TipoDeImovel = imovel.TipoImovel,
//                    TipoDeAnuncio = tipoDeAnuncio == TipoDeAnuncio.Aluguel
//                        ? "Aluguel"
//                        : "Venda",
//                    LinkDoAnuncio = imovel.Link,
//                    imagens = imovel.Imagens
//                }).ToList()
//            };
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using ZapImoveisWebScraper;
using ZapScrap.API.Enuns;
using ZapScrap.API.Modelos;

namespace ZapScrap.API.Controllers
{
    [ApiController]
    [Route("[controller]/")]
    public class ZapScrapController : ControllerBase
    {
        private readonly ILogger<ZapScrapController> _logger;

        public ZapScrapController(ILogger<ZapScrapController> logger)
        {
            _logger = logger;
        }

        [HttpGet("ObterAnuncios")]
        public async Task<IActionResult> ObterAnuncios([FromQuery] ZapScrapRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            ZapImoveisService? scraper = null;

            try
            {
                _logger.LogInformation("Iniciando busca de anúncios. Tipo: {TipoAnuncio}", request.TipoAnuncio);

                // Timeout de 90 segundos (ajuste conforme necessário)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

                scraper = new ZapImoveisService();

                // Executa busca com timeout
                var imoveis = await Task.Run(async () =>
                    await scraper.BuscarImoveis(request), cts.Token);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Busca concluída com sucesso. Total: {Total} imóveis em {Tempo}ms",
                    imoveis.Count,
                    stopwatch.ElapsedMilliseconds
                );

                var objetoRetorno = ObterObjetoRetorno(imoveis, (TipoDeAnuncio)request.TipoAnuncio);

                return Ok(new
                {
                    Sucesso = true,
                    TempoDeExecucao = $"{stopwatch.ElapsedMilliseconds}ms",
                    Dados = objetoRetorno
                });
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogWarning("Timeout na busca de anúncios após {Tempo}ms", stopwatch.ElapsedMilliseconds);

                return StatusCode(504, new
                {
                    Sucesso = false,
                    Mensagem = "A busca demorou muito tempo e foi cancelada. Tente reduzir os filtros ou usar menos páginas.",
                    TempoDeExecucao = $"{stopwatch.ElapsedMilliseconds}ms"
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Erro ao buscar anúncios. Tempo até falha: {Tempo}ms", stopwatch.ElapsedMilliseconds);

                return StatusCode(500, new
                {
                    Sucesso = false,
                    Mensagem = "Erro ao buscar anúncios",
                    Erro = ex.Message,
                    TempoDeExecucao = $"{stopwatch.ElapsedMilliseconds}ms"
                });
            }
            finally
            {
                // Garante que o driver seja fechado mesmo em caso de erro
                try
                {
                    scraper?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao liberar recursos do scraper");
                }
            }
        }

        [HttpGet("Health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Versao = "1.0.0"
            });
        }

        [HttpGet("TestarScraper")]
        public async Task<IActionResult> TestarScraper()
        {
            ZapImoveisService? scraper = null;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Testando scraper...");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                scraper = new ZapImoveisService();

                // Teste simples: busca apenas 1 página
                var requestTeste = new ZapScrapRequest
                {
                    TipoAnuncio = TipoDeAnuncio.Venda, // Venda
                    Cidade = "goiania",
                    Estado = "go"
                };

                var imoveis = await Task.Run(async () =>
                    await scraper.BuscarImoveis(requestTeste), cts.Token);

                stopwatch.Stop();

                return Ok(new
                {
                    Sucesso = true,
                    Mensagem = "Scraper funcionando corretamente",
                    TotalImoveisEncontrados = imoveis.Count,
                    TempoDeExecucao = $"{stopwatch.ElapsedMilliseconds}ms",
                    ChromeVersion = "Disponível"
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Erro ao testar scraper");

                return StatusCode(500, new
                {
                    Sucesso = false,
                    Mensagem = "Erro ao testar scraper",
                    Erro = ex.Message,
                    TempoDeExecucao = $"{stopwatch.ElapsedMilliseconds}ms"
                });
            }
            finally
            {
                try
                {
                    scraper?.Dispose();
                }
                catch { }
            }
        }

        private object ObterObjetoRetorno(List<Imovel> imoveis, TipoDeAnuncio tipoDeAnuncio)
        {
            return new
            {
                TotalDeImoveis = imoveis.Count,
                Imoveis = imoveis.Select(imovel => new
                {
                    Titulo = imovel.Titulo,
                    Valor = imovel.Preco,
                    Condominio = imovel.PrecoCondominio,
                    Iptu = imovel.Iptu,
                    Area = imovel.AreaTotal,
                    Quartos = imovel.Quartos,
                    Banheiros = imovel.Banheiros,
                    VagasNaGaragem = imovel.Vagas,
                    Endereco = imovel.Endereco,
                    TipoDeImovel = imovel.TipoImovel,
                    TipoDeAnuncio = tipoDeAnuncio == TipoDeAnuncio.Aluguel
                        ? "Aluguel"
                        : "Venda",
                    LinkDoAnuncio = imovel.Link,
                    Imagens = imovel.Imagens
                }).ToList()
            };
        }
    }
}
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
        [HttpGet("ObterAnuncios")]
        public async Task<IActionResult> ObterAnuncios(ZapScrapRequest request)
        {
            try
            {
                var scraper = new ZapImoveisService();

                var imoveis = await scraper.BuscarImoveis(request);


                var objetoRetorno = ObterObjetoRetorno(imoveis, (TipoDeAnuncio)request.TipoAnuncio);
                return Ok(objetoRetorno);
            }
            catch (Exception ex)
            {
                return Ok(new { ex.Message });
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
                    imagens = imovel.Imagens
                }).ToList()
            };
        }
    }
}

